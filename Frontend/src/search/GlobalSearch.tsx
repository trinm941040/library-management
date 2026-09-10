import { useEffect, useMemo, useRef, useState } from 'react'
import { ArrowUpRight, Search, SearchX } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { Button } from '@/common/components/ui/button'
import { Input } from '@/common/components/ui/input'
import { searchLocalContent } from './search-data'

export function GlobalSearch() {
  const navigate = useNavigate()
  const containerRef = useRef<HTMLDivElement>(null)
  const [query, setQuery] = useState('')
  const [isOpen, setIsOpen] = useState(false)
  const results = useMemo(() => searchLocalContent(query), [query])

  useEffect(() => {
    const closeWhenClickOutside = (event: PointerEvent) => {
      if (!containerRef.current?.contains(event.target as Node)) setIsOpen(false)
    }

    document.addEventListener('pointerdown', closeWhenClickOutside)
    return () => document.removeEventListener('pointerdown', closeWhenClickOutside)
  }, [])

  const openResult = (path: string) => {
    navigate(path)
    setQuery('')
    setIsOpen(false)
  }

  return (
    <div ref={containerRef} className="relative min-w-0 max-w-xl flex-1">
      <Search className="pointer-events-none absolute top-1/2 left-3 z-10 size-4 -translate-y-1/2 text-muted-foreground" />
      <Input
        value={query}
        placeholder="Tìm trang hoặc chức năng..."
        className="bg-background pr-3 pl-9"
        role="combobox"
        aria-label="Tìm kiếm toàn hệ thống"
        aria-expanded={isOpen && query.trim().length > 0}
        aria-controls="global-search-results"
        autoComplete="off"
        onFocus={() => setIsOpen(true)}
        onChange={(event) => {
          setQuery(event.target.value)
          setIsOpen(true)
        }}
        onKeyDown={(event) => {
          if (event.key === 'Escape') setIsOpen(false)
          if (event.key === 'Enter' && results[0]) openResult(results[0].path)
        }}
      />

      {isOpen && query.trim() ? (
        <div
          id="global-search-results"
          className="fixed top-16 right-3 left-3 z-[70] max-h-[min(28rem,calc(100vh-5rem))] overflow-y-auto rounded-lg border bg-popover p-2 text-popover-foreground shadow-xl md:absolute md:top-[calc(100%+0.5rem)] md:right-0 md:left-0"
          role="listbox"
        >
          {results.length > 0 ? (
            <>
              <p className="px-3 py-2 text-xs font-medium text-muted-foreground">
                Tìm thấy {results.length} kết quả
              </p>
              {results.map((result) => (
                <Button
                  key={result.id}
                  type="button"
                  variant="ghost"
                  className="h-auto w-full items-start justify-start gap-3 px-3 py-3 text-left whitespace-normal"
                  role="option"
                  onClick={() => openResult(result.path)}
                >
                  <Search className="mt-0.5 size-4 shrink-0 text-primary" />
                  <span className="min-w-0 flex-1">
                    <span className="flex items-start justify-between gap-3">
                      <strong className="text-sm">{result.title}</strong>
                      <small className="shrink-0 rounded-full bg-secondary px-2 py-0.5 text-secondary-foreground">
                        {result.category}
                      </small>
                    </span>
                    <small className="mt-1 block font-normal text-muted-foreground">
                      {result.description}
                    </small>
                  </span>
                  <ArrowUpRight className="mt-0.5 size-4 shrink-0 text-muted-foreground" />
                </Button>
              ))}
            </>
          ) : (
            <div className="grid place-items-center gap-2 px-4 py-8 text-center text-muted-foreground">
              <SearchX className="size-6" />
              <p className="text-sm">Không tìm thấy trang hoặc chức năng phù hợp.</p>
            </div>
          )}
        </div>
      ) : null}
    </div>
  )
}
