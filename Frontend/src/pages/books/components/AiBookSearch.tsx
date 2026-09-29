import { useEffect, useRef, useState, type KeyboardEvent } from 'react'
import { BookOpen, ExternalLink, Search } from 'lucide-react'
import { Link } from 'react-router-dom'
import { ScreenState } from '@/common/components'
import { Badge } from '@/common/components/ui/badge'
import { Button } from '@/common/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/common/components/ui/card'
import { Label } from '@/common/components/ui/label'
import { Pagination } from '@/common/components/ui/pagination'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/common/components/ui/select'
import { Switch } from '@/common/components/ui/switch'
import { Textarea } from '@/common/components/ui/textarea'
import { ApiError } from '@/auth/auth-api'
import {
  getCatalogReferences,
  semanticSearchBooks,
  type BookReference,
  type SemanticBookSearchItem,
} from '../book-api'
import { AiBookInventoryDialog } from './AiBookInventoryDialog'
import { AiSparklesIcon } from './AiSparklesIcon'

const SUGGESTIONS = [
  'Kiến trúc backend',
  'Machine learning cho người mới',
  'Thiết kế cơ sở dữ liệu',
  'Kỹ nghệ phần mềm',
  'Quản trị kinh doanh',
]
const RESULTS_PER_PAGE = 8

function semanticSearchError(error: unknown) {
  if (!(error instanceof ApiError))
    return 'Không thể thực hiện tìm kiếm AI lúc này. Vui lòng thử lại.'
  if (error.status === 408) return 'Tìm kiếm AI mất quá nhiều thời gian. Vui lòng thử lại.'
  if (error.status === 401) return 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.'
  if (error.status === 403) return 'Bạn không có quyền sử dụng chức năng tìm kiếm này.'
  if (error.status === 429) return 'Dịch vụ tìm kiếm đang quá tải. Vui lòng thử lại sau.'
  if (error.status === 400 || error.status === 422) return 'Nội dung tìm kiếm không hợp lệ.'
  if (error.status === 503) return 'Dịch vụ tìm kiếm AI tạm thời không khả dụng.'
  return 'Không thể thực hiện tìm kiếm AI lúc này. Vui lòng thử lại.'
}

export function AiBookSearch() {
  const [query, setQuery] = useState('')
  const [categoryId, setCategoryId] = useState('all')
  const [availableOnly, setAvailableOnly] = useState(false)
  const [categories, setCategories] = useState<BookReference[]>([])
  const [results, setResults] = useState<SemanticBookSearchItem[] | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [selectedBook, setSelectedBook] = useState<SemanticBookSearchItem | null>(null)
  const [resultPage, setResultPage] = useState(1)
  const [submittedQuery, setSubmittedQuery] = useState('')
  const searchController = useRef<AbortController | null>(null)

  useEffect(() => {
    const controller = new AbortController()
    void getCatalogReferences('categories', controller.signal)
      .then(setCategories)
      .catch((reason: unknown) => {
        if (!(reason instanceof DOMException && reason.name === 'AbortError')) setCategories([])
      })
    return () => controller.abort()
  }, [])

  useEffect(() => () => searchController.current?.abort(), [])

  const search = async () => {
    const normalizedQuery = query.trim()
    if (!normalizedQuery || loading) return
    const controller = new AbortController()
    searchController.current = controller
    setLoading(true)
    setError('')
    try {
      const response = await semanticSearchBooks(
        {
          query: normalizedQuery,
          categoryId: categoryId === 'all' ? null : categoryId,
          availableOnly,
          topK: 10,
        },
        controller.signal,
      )
      setResults(response.items)
      setSubmittedQuery(normalizedQuery)
      setResultPage(1)
    } catch (reason) {
      if (!(reason instanceof DOMException && reason.name === 'AbortError'))
        setError(semanticSearchError(reason))
    } finally {
      if (searchController.current === controller) {
        searchController.current = null
        setLoading(false)
      }
    }
  }

  const handleKeyDown = (event: KeyboardEvent<HTMLTextAreaElement>) => {
    if (event.key === 'Enter' && (event.ctrlKey || event.metaKey)) {
      event.preventDefault()
      void search()
    }
  }

  return (
    <div className="grid gap-5">
      <Card className="gap-4">
        <CardHeader>
          <div className="flex items-center gap-2">
            <span className="grid size-9 place-items-center rounded-lg bg-primary/10 text-primary">
              <AiSparklesIcon />
            </span>
            <div>
              <CardTitle>Tìm sách bằng AI</CardTitle>
              <p className="mt-1 text-sm text-muted-foreground">
                Mô tả nội dung bạn quan tâm để khám phá các sách liên quan.
              </p>
            </div>
          </div>
        </CardHeader>
        <CardContent className="grid gap-4">
          <div className="grid gap-2">
            <Label htmlFor="semantic-book-query">Mô tả cuốn sách bạn đang tìm</Label>
            <Textarea
              id="semantic-book-query"
              value={query}
              rows={4}
              maxLength={1000}
              onChange={(event) => setQuery(event.target.value)}
              onKeyDown={handleKeyDown}
              placeholder="Mô tả nhu cầu, ví dụ: sách về thiết kế hệ thống backend có khả năng mở rộng..."
              aria-describedby="semantic-book-query-hint"
            />
            <p id="semantic-book-query-hint" className="text-xs text-muted-foreground">
              Nhấn Ctrl + Enter hoặc Command + Enter để tìm kiếm.
            </p>
          </div>

          <div className="flex flex-wrap items-center gap-2">
            <span className="text-sm text-muted-foreground">Gợi ý:</span>
            {SUGGESTIONS.map((suggestion) => (
              <Button
                key={suggestion}
                type="button"
                variant="outline"
                size="sm"
                className="h-auto whitespace-normal rounded-full py-1.5"
                onClick={() => setQuery(suggestion)}
              >
                {suggestion}
              </Button>
            ))}
          </div>

          <div className="grid items-end gap-4 sm:grid-cols-[minmax(0,1fr)_auto_auto]">
            <div className="grid gap-1.5">
              <Label htmlFor="semantic-category">Thể loại</Label>
              <Select value={categoryId} onValueChange={setCategoryId}>
                <SelectTrigger id="semantic-category" className="w-full">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">Tất cả thể loại</SelectItem>
                  {categories.map((category) => (
                    <SelectItem key={category.id} value={category.id}>
                      {category.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="flex min-h-10 items-center gap-2 rounded-md border px-3">
              <Switch
                id="semantic-available-only"
                checked={availableOnly}
                onCheckedChange={setAvailableOnly}
              />
              <Label htmlFor="semantic-available-only" className="whitespace-nowrap">
                Chỉ sách còn bản sao
              </Label>
            </div>
            <Button
              type="button"
              disabled={!query.trim() || loading}
              loading={loading}
              loadingLabel="AI đang tìm sách"
              onClick={() => void search()}
            >
              <AiSparklesIcon className="size-4" /> Tìm kiếm
            </Button>
          </div>
        </CardContent>
      </Card>

      <section aria-live="polite" aria-busy={loading}>
        {loading ? (
          <ScreenState kind="loading" title="AI đang tìm các sách phù hợp..." />
        ) : error ? (
          <ScreenState
            kind="error"
            title="Không thể tìm kiếm"
            description={error}
            actionLabel="Thử lại"
            onAction={() => void search()}
          />
        ) : results === null ? (
          <ScreenState
            kind="empty"
            title="Mô tả cuốn sách bạn đang tìm"
            description="AI sẽ phân tích nhu cầu và gợi ý những đầu sách liên quan."
          />
        ) : results.length === 0 ? (
          <ScreenState
            kind="empty"
            title="Không tìm thấy sách phù hợp"
            description="Hãy thử mô tả nhu cầu theo cách khác hoặc nới lỏng bộ lọc."
          />
        ) : (
          <div className="grid gap-4">
            <h3 className="flex items-center gap-2 font-semibold">
              <AiSparklesIcon className="size-4" /> Tìm thấy {results.length} cuốn sách liên quan
            </h3>
            <div className="grid gap-4 lg:grid-cols-2">
              {results
                .slice((resultPage - 1) * RESULTS_PER_PAGE, resultPage * RESULTS_PER_PAGE)
                .map((book) => (
                  <Card key={book.id} className="h-full gap-4 py-4">
                    <CardContent className="flex h-full gap-4">
                      <div className="grid h-28 w-20 shrink-0 place-items-center overflow-hidden rounded-lg border bg-muted text-muted-foreground">
                        <BookOpen className="size-8" aria-hidden="true" />
                      </div>
                      <div className="flex min-w-0 flex-1 flex-col gap-2">
                        <div>
                          <h4 className="font-semibold leading-snug">{book.title}</h4>
                          <p className="mt-1 text-sm text-muted-foreground">{book.author}</p>
                        </div>
                        <div className="flex flex-wrap gap-2">
                          <Badge variant="secondary">{book.category}</Badge>
                          {book.similarity !== undefined ? (
                            <Badge variant="outline">
                              Độ liên quan {Math.round(book.similarity * 100)}%
                            </Badge>
                          ) : null}
                        </div>
                        <p className="font-mono text-xs text-muted-foreground">ISBN: {book.isbn}</p>
                        {book.description ? (
                          <p className="line-clamp-2 text-sm text-muted-foreground">
                            {book.description}
                          </p>
                        ) : null}
                        <div className="grid grid-cols-2 gap-x-4 gap-y-1 rounded-md bg-muted/40 p-2 text-sm">
                          <span>
                            Tổng bản sao: <strong>{book.totalCopies}</strong>
                          </span>
                          <span className="text-emerald-700 dark:text-emerald-300">
                            Sẵn sàng: <strong>{book.availableCopies}</strong>
                          </span>
                        </div>
                        <div className="mt-auto flex flex-wrap gap-2">
                          <Button type="button" size="sm" onClick={() => setSelectedBook(book)}>
                            <Search /> Xem tồn kho
                          </Button>
                          <Button asChild variant="outline" size="sm">
                            <Link to={`/catalog/${book.id}`}>
                              <ExternalLink /> Biên mục
                            </Link>
                          </Button>
                        </div>
                      </div>
                    </CardContent>
                  </Card>
                ))}
            </div>
            {results.length > RESULTS_PER_PAGE ? (
              <div className="flex flex-col gap-2 border-t pt-4 sm:flex-row sm:items-center sm:justify-between">
                <p className="text-sm text-muted-foreground">
                  Hiển thị {(resultPage - 1) * RESULTS_PER_PAGE + 1}–
                  {Math.min(resultPage * RESULTS_PER_PAGE, results.length)} trong {results.length}{' '}
                  kết quả
                </p>
                <Pagination
                  currentPage={resultPage}
                  totalPages={Math.ceil(results.length / RESULTS_PER_PAGE)}
                  totalCount={results.length}
                  itemCount={Math.min(
                    RESULTS_PER_PAGE,
                    results.length - (resultPage - 1) * RESULTS_PER_PAGE,
                  )}
                  pageSize={RESULTS_PER_PAGE}
                  onPageChange={setResultPage}
                />
              </div>
            ) : null}
          </div>
        )}
      </section>
      <AiBookInventoryDialog
        book={selectedBook}
        query={submittedQuery}
        open={selectedBook !== null}
        onOpenChange={(open) => !open && setSelectedBook(null)}
      />
    </div>
  )
}
