import { useEffect, useRef, useState, type KeyboardEvent } from 'react'
import { BookOpen, LogOut, MapPin, RotateCcw, Search } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { ScreenState } from '@/common/components'
import { Badge } from '@/common/components/ui/badge'
import { Button } from '@/common/components/ui/button'
import { Card, CardContent } from '@/common/components/ui/card'
import { Input } from '@/common/components/ui/input'
import { Tabs, TabsList, TabsTrigger } from '@/common/components/ui/tabs'
import { Textarea } from '@/common/components/ui/textarea'
import { AiSparklesIcon } from '@/pages/books/components/AiSparklesIcon'
import { useSettings } from '@/settings/SettingsProvider'
import { KioskBookDetailDialog, type KioskBookSelection } from './KioskBookDetailDialog'
import { searchKioskBooks, semanticSearchKioskBooks } from './kiosk-api'

type SearchMode = 'keyword' | 'ai'
type KioskResult = {
  id: string
  title: string
  author: string
  isbn: string
  category: string
  description: string | null
  availableCopies: number
  totalCopies?: number
  similarity?: number
}

export function KioskSearchPage() {
  const navigate = useNavigate()
  const { aiEnabled } = useSettings()
  const [mode, setMode] = useState<SearchMode>('keyword')
  const [keywordQuery, setKeywordQuery] = useState('')
  const [aiQuery, setAiQuery] = useState('')
  const [results, setResults] = useState<KioskResult[] | null>(null)
  const [resultMode, setResultMode] = useState<SearchMode | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [selectedBook, setSelectedBook] = useState<KioskBookSelection | null>(null)
  const controllerRef = useRef<AbortController | null>(null)

  useEffect(() => () => controllerRef.current?.abort(), [])

  useEffect(() => {
    const removeRetryListeners = () => {
      window.removeEventListener('pointerdown', retryOnInteraction)
      window.removeEventListener('keydown', retryOnInteraction)
    }
    const enterFullscreen = async () => {
      if (document.fullscreenElement || !document.documentElement.requestFullscreen) return
      await document.documentElement.requestFullscreen().catch(() => undefined)
      if (document.fullscreenElement) removeRetryListeners()
    }
    function retryOnInteraction() {
      void enterFullscreen()
    }

    void enterFullscreen()
    window.addEventListener('pointerdown', retryOnInteraction)
    window.addEventListener('keydown', retryOnInteraction)
    return () => {
      removeRetryListeners()
      if (document.fullscreenElement) void document.exitFullscreen().catch(() => undefined)
    }
  }, [])

  useEffect(() => {
    if (aiEnabled) return
    if (mode === 'ai' || resultMode === 'ai') {
      controllerRef.current?.abort()
      controllerRef.current = null
      setMode('keyword')
      setResults(null)
      setResultMode(null)
      setError('')
      setLoading(false)
      setSelectedBook(null)
    }
  }, [aiEnabled, mode, resultMode])

  const search = async () => {
    const query = (mode === 'keyword' ? keywordQuery : aiQuery).trim()
    if (!query || loading || (mode === 'ai' && !aiEnabled)) return
    const controller = new AbortController()
    controllerRef.current?.abort()
    controllerRef.current = controller
    setLoading(true)
    setError('')
    try {
      const items =
        mode === 'keyword'
          ? (await searchKioskBooks(query, controller.signal)).items
          : (await semanticSearchKioskBooks(query, controller.signal)).items
      setResults(items)
      setResultMode(mode)
    } catch (reason) {
      if (!(reason instanceof DOMException && reason.name === 'AbortError'))
        setError('Không thể tìm kiếm lúc này. Vui lòng thử lại.')
    } finally {
      if (controllerRef.current === controller) {
        controllerRef.current = null
        setLoading(false)
      }
    }
  }

  const reset = () => {
    controllerRef.current?.abort()
    controllerRef.current = null
    setMode('keyword')
    setKeywordQuery('')
    setAiQuery('')
    setResults(null)
    setResultMode(null)
    setError('')
    setLoading(false)
    setSelectedBook(null)
  }

  const changeMode = (nextMode: SearchMode) => {
    controllerRef.current?.abort()
    controllerRef.current = null
    setMode(nextMode)
    setResults(null)
    setResultMode(null)
    setError('')
    setLoading(false)
    setSelectedBook(null)
  }

  const handleKeyDown = (event: KeyboardEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault()
      void search()
    }
  }

  const currentQuery = mode === 'keyword' ? keywordQuery : aiQuery

  const exitKiosk = async () => {
    if (document.fullscreenElement) await document.exitFullscreen().catch(() => undefined)
    navigate('/login', { replace: true })
  }

  return (
    <main className="min-h-dvh bg-muted/30 px-4 py-5 sm:px-8 sm:py-8 lg:px-12">
      <div className="mx-auto grid w-full max-w-6xl gap-7">
        <header className="flex border-b pb-6">
          <div className="flex items-center gap-4">
            <span className="grid size-14 place-items-center rounded-2xl bg-primary text-primary-foreground shadow-sm">
              <BookOpen className="size-8" aria-hidden="true" />
            </span>
            <div>
              <p className="text-sm font-semibold uppercase tracking-[0.2em] text-primary">Thư viện</p>
              <h1 className="text-2xl font-bold sm:text-3xl">Tìm sách trong thư viện</h1>
            </div>
          </div>
        </header>

        <section className="grid gap-5 rounded-2xl border bg-card p-5 shadow-sm sm:p-8" aria-labelledby="kiosk-search-title">
          <div className="text-center">
            <h2 id="kiosk-search-title" className="text-xl font-semibold sm:text-2xl">
              Bạn đang tìm cuốn sách nào?
            </h2>
            <p className="mt-1 text-muted-foreground">Tra cứu nhanh để biết sách có sẵn và nằm ở đâu.</p>
          </div>

          {aiEnabled ? (
            <Tabs value={mode} onValueChange={(value) => changeMode(value as SearchMode)}>
              <TabsList className="mx-auto min-h-12 w-full max-w-xl grid-cols-2" aria-label="Chế độ tìm sách">
                <TabsTrigger value="keyword" className="min-h-10 text-base">
                  <Search /> Tìm theo từ khóa
                </TabsTrigger>
                <TabsTrigger value="ai" className="min-h-10 text-base">
                  <AiSparklesIcon className="size-5" /> Tìm kiếm AI
                </TabsTrigger>
              </TabsList>
            </Tabs>
          ) : null}

          <div className="mx-auto grid w-full max-w-3xl gap-3">
            {mode === 'keyword' ? (
              <Input
                value={keywordQuery}
                onChange={(event) => setKeywordQuery(event.target.value)}
                onKeyDown={handleKeyDown}
                className="h-14 px-5 text-base sm:text-lg"
                placeholder="Nhập tên sách, tác giả, ISBN..."
                aria-label="Từ khóa tìm sách"
              />
            ) : (
              <>
                <Textarea
                  value={aiQuery}
                  onChange={(event) => setAiQuery(event.target.value)}
                  onKeyDown={handleKeyDown}
                  rows={3}
                  className="min-h-28 px-5 py-4 text-base sm:text-lg"
                  placeholder="Mô tả cuốn sách hoặc chủ đề bạn muốn tìm..."
                  aria-label="Mô tả nhu cầu tìm sách bằng AI"
                />
                <p className="text-sm text-muted-foreground">
                  Ví dụ: “Sách về tư tưởng phương Đông” hoặc “Tôi muốn học marketing”.
                </p>
              </>
            )}
            <Button
              type="button"
              size="lg"
              className="min-h-12 text-base"
              disabled={!currentQuery.trim() || loading}
              loading={loading}
              loadingLabel="Đang tìm sách"
              onClick={() => void search()}
            >
              {mode === 'ai' ? <AiSparklesIcon className="size-5" /> : <Search />}
              Tìm sách
            </Button>
          </div>
        </section>

        <section aria-live="polite" aria-busy={loading}>
          {loading ? (
            <ScreenState kind="loading" title="Đang tìm sách..." />
          ) : error ? (
            <ScreenState kind="error" title="Không thể tìm kiếm" description={error} actionLabel="Thử lại" onAction={() => void search()} />
          ) : results === null ? (
            <ScreenState kind="empty" title="Bạn đang tìm cuốn sách nào?" description="Nhập từ khóa hoặc mô tả chủ đề để bắt đầu." />
          ) : results.length === 0 ? (
            <ScreenState kind="empty" title="Không tìm thấy sách phù hợp" description={resultMode === 'ai' ? 'Hãy thử mô tả nhu cầu theo cách khác.' : 'Hãy kiểm tra từ khóa hoặc thử tên tác giả, ISBN.'} />
          ) : (
            <div className="grid gap-5">
              <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                <h2 className="text-xl font-semibold">Tìm thấy {results.length} cuốn sách</h2>
                <Button type="button" variant="outline" size="lg" onClick={reset}>
                  <RotateCcw /> Tìm kiếm mới
                </Button>
              </div>
              <div className="grid gap-4 md:grid-cols-2">
                {results.map((book) => (
                  <Card key={book.id} className="h-full">
                    <CardContent className="flex h-full gap-4 p-5">
                      <div className="grid h-28 w-20 shrink-0 place-items-center rounded-lg bg-muted text-muted-foreground">
                        <BookOpen className="size-8" aria-hidden="true" />
                      </div>
                      <div className="flex min-w-0 flex-1 flex-col gap-2">
                        <div>
                          <h3 className="text-lg font-semibold leading-snug">{book.title}</h3>
                          <p className="text-muted-foreground">{book.author}</p>
                        </div>
                        <div className="flex flex-wrap gap-2">
                          <Badge variant="secondary">{book.category}</Badge>
                          {resultMode === 'ai' && book.similarity !== undefined ? (
                            <Badge variant="outline">
                              <AiSparklesIcon className="size-3.5" /> Độ liên quan {Math.round(book.similarity * 100)}%
                            </Badge>
                          ) : null}
                        </div>
                        <p className="font-mono text-xs text-muted-foreground">ISBN: {book.isbn}</p>
                        {book.description ? <p className="line-clamp-2 text-sm text-muted-foreground">{book.description}</p> : null}
                        <p className="mt-auto font-medium text-emerald-700 dark:text-emerald-300">
                          ✓ Có sẵn: {book.availableCopies}{book.totalCopies !== undefined ? ` / ${book.totalCopies}` : ''}
                        </p>
                        <Button
                          type="button"
                          size="lg"
                          variant="outline"
                          onClick={() => setSelectedBook({ id: book.id, title: book.title, similarity: book.similarity })}
                        >
                          <MapPin /> Xem vị trí / Chi tiết
                        </Button>
                      </div>
                    </CardContent>
                  </Card>
                ))}
              </div>
            </div>
          )}
        </section>
      </div>

      <KioskBookDetailDialog
        selection={selectedBook}
        open={selectedBook !== null}
        onOpenChange={(open) => !open && setSelectedBook(null)}
      />
      <Button
        type="button"
        variant="ghost"
        size="sm"
        className="fixed bottom-2 right-2 z-40 h-8 px-2 text-xs opacity-35 transition-opacity hover:opacity-100 focus-visible:opacity-100"
        onPointerDown={(event) => event.stopPropagation()}
        onClick={() => void exitKiosk()}
        aria-label="Thoát kiosk"
      >
        <LogOut className="size-3.5" aria-hidden="true" />
        Thoát
      </Button>
    </main>
  )
}
