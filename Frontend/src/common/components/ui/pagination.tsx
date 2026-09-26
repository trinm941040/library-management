import { ChevronFirst, ChevronLast, ChevronLeft, ChevronRight } from 'lucide-react'
import { type FormEvent, useEffect, useId, useRef, useState } from 'react'
import { Button } from '@/common/components/ui/button'
import { Input } from '@/common/components/ui/input'
import { Spinner } from '@/common/components/atoms/Spinner'
import { cn } from '@/utils/cn'

type PaginationProps = {
  currentPage: number
  totalPages: number
  totalCount?: number
  itemCount?: number
  loading?: boolean
  preserveMobileAnchor?: boolean
  mobileFirstLoadedPage?: number
  mobileLastLoadedPage?: number
  onPageChange: (page: number) => void
  pageSize?: number
  pageSizeOptions?: readonly number[]
  onPageSizeChange?: (pageSize: number) => void
  mobileLazy?: boolean
  className?: string
}

function Pagination({
  currentPage,
  totalPages,
  totalCount,
  itemCount,
  loading,
  preserveMobileAnchor = false,
  mobileFirstLoadedPage,
  mobileLastLoadedPage,
  onPageChange,
  pageSize,
  pageSizeOptions = [10, 20, 50, 100],
  onPageSizeChange,
  mobileLazy = true,
  className,
}: PaginationProps) {
  const lastPage = Math.max(1, totalPages)
  const page = Math.min(Math.max(currentPage, 1), lastPage)
  const effectiveLastLoadedPage = mobileLastLoadedPage ?? page
  const effectiveFirstLoadedPage = mobileFirstLoadedPage ?? page
  const loadedThrough = pageSize === undefined
    ? undefined
    : (effectiveLastLoadedPage - 1) * pageSize + (itemCount ?? pageSize)
  const hasNextPage = effectiveLastLoadedPage < lastPage
    && (itemCount === undefined || itemCount > 0)
    && (totalCount === undefined || loadedThrough === undefined || loadedThrough < totalCount)
  const [targetPage, setTargetPage] = useState(String(page))
  const [mobileLoading, setMobileLoading] = useState(false)
  const targetPageId = useId()
  const mobileMarkerRef = useRef<HTMLDivElement>(null)
  const loadingOverlayRef = useRef<HTMLDivElement>(null)
  const pendingMobilePageRef = useRef(false)
  const suppressMobileScrollRef = useRef(false)
  const mobileDirectionRef = useRef<'previous' | 'next' | null>(null)
  const previousPageRef = useRef(page)

  const pageItems: Array<number | 'start-ellipsis' | 'end-ellipsis'> = (() => {
    if (lastPage <= 5) return Array.from({ length: lastPage }, (_, index) => index + 1)
    if (page <= 3) return [1, 2, 3, 'end-ellipsis', lastPage]
    if (page >= lastPage - 2)
      return [1, 'start-ellipsis', lastPage - 2, lastPage - 1, lastPage]
    return [1, 'start-ellipsis', page - 1, page, page + 1, 'end-ellipsis', lastPage]
  })()

  useEffect(() => setTargetPage(String(page)), [page])

  useEffect(() => {
    if (!mobileLazy || !window.matchMedia('(max-width: 639px)').matches) return
    const marker = mobileMarkerRef.current
    if (!marker) return

    let scope: HTMLElement | null = marker.parentElement
    let tableContainer: HTMLElement | null = null
    while (scope && !tableContainer) {
      tableContainer = scope.querySelector<HTMLElement>("[data-slot='table-container']")
      scope = scope.parentElement
    }
    let listStart: HTMLElement | null = null
    if (!tableContainer) {
      const previous = marker.previousElementSibling as HTMLElement | null
      listStart = previous?.tagName === 'P'
        ? marker.parentElement?.previousElementSibling as HTMLElement | null
        : previous
    }
    const scrollRoot = tableContainer ?? marker.closest<HTMLElement>('.content-scroll-region')
    if ((!tableContainer && !listStart) || !scrollRoot) return

    let lastScrollTop = scrollRoot.scrollTop
    let scrollDirection = 0
    let userHasScrolled = false
    const requestPage = (direction: 'previous' | 'next') => {
      if (pendingMobilePageRef.current || !userHasScrolled) return
      if (direction === 'next' && !hasNextPage) return
      if (direction === 'previous' && effectiveFirstLoadedPage <= 1) return
      pendingMobilePageRef.current = true
      setMobileLoading(true)
      mobileDirectionRef.current = direction
      onPageChange(direction === 'next' ? effectiveLastLoadedPage + 1 : effectiveFirstLoadedPage - 1)
    }
    const handleScroll = () => {
      const nextScrollTop = scrollRoot.scrollTop
      const delta = nextScrollTop - lastScrollTop
      if (suppressMobileScrollRef.current) {
        lastScrollTop = nextScrollTop
        return
      }
      if (Math.abs(delta) > 2) {
        scrollDirection = Math.sign(delta)
        userHasScrolled = true
      }
      lastScrollTop = nextScrollTop
      if (tableContainer && scrollDirection > 0
        && scrollRoot.scrollHeight - nextScrollTop - scrollRoot.clientHeight <= 4)
        requestPage('next')
      if (tableContainer && scrollDirection < 0 && nextScrollTop <= 4 && effectiveFirstLoadedPage > 1)
        requestPage('previous')
    }
    scrollRoot.addEventListener('scroll', handleScroll, { passive: true })

    if (tableContainer) return () => scrollRoot.removeEventListener('scroll', handleScroll)

    const observer = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          if (!entry.isIntersecting) continue
          if (entry.target === marker && scrollDirection > 0 && hasNextPage) requestPage('next')
          if (entry.target === listStart && scrollDirection < 0 && effectiveFirstLoadedPage > 1) requestPage('previous')
        }
      },
      { root: scrollRoot, rootMargin: '0px', threshold: 0.9 },
    )
    observer.observe(marker)
    observer.observe(listStart!)
    return () => {
      observer.disconnect()
      scrollRoot.removeEventListener('scroll', handleScroll)
    }
  }, [effectiveFirstLoadedPage, effectiveLastLoadedPage, hasNextPage, mobileLazy, onPageChange, page])

  useEffect(() => {
    if (previousPageRef.current === page) return
    previousPageRef.current = page
    if (!window.matchMedia('(max-width: 639px)').matches) return
    const marker = mobileMarkerRef.current
    const direction = mobileDirectionRef.current
    mobileDirectionRef.current = null
    if (!marker || !direction) return
    if (preserveMobileAnchor) return
    let scope: HTMLElement | null = marker.parentElement
    let tableContainer: HTMLElement | null = null
    while (scope && !tableContainer) {
      tableContainer = scope.querySelector<HTMLElement>("[data-slot='table-container']")
      scope = scope.parentElement
    }
    let listStart: HTMLElement | null = tableContainer?.querySelector<HTMLElement>('tbody tr') ?? null
    if (!tableContainer) {
      const previous = marker.previousElementSibling as HTMLElement | null
      listStart = previous?.tagName === 'P'
        ? marker.parentElement?.previousElementSibling as HTMLElement | null
        : previous
    }
    const scrollRoot = tableContainer ?? marker.closest<HTMLElement>('.content-scroll-region')
    suppressMobileScrollRef.current = true
    requestAnimationFrame(() => {
      if (tableContainer && direction === 'next') {
        tableContainer.scrollTo({ top: 96 })
      } else if (tableContainer) {
        tableContainer.scrollTo({ top: Math.max(0, tableContainer.scrollHeight - tableContainer.clientHeight - 96) })
      } else if (direction === 'next') {
        listStart?.scrollIntoView({ block: 'start' })
        scrollRoot?.scrollBy({ top: 96 })
      } else {
        marker.scrollIntoView({ block: 'end' })
        scrollRoot?.scrollBy({ top: -96 })
      }
      window.setTimeout(() => { suppressMobileScrollRef.current = false }, 120)
    })
  }, [page, preserveMobileAnchor])

  useEffect(() => {
    if (!pendingMobilePageRef.current || loading) return
    const timeout = window.setTimeout(() => {
      pendingMobilePageRef.current = false
      setMobileLoading(false)
    }, loading === undefined ? 700 : 150)
    return () => window.clearTimeout(timeout)
  }, [itemCount, loading, page])

  useEffect(() => {
    if (!mobileLoading) return
    loadingOverlayRef.current?.focus({ preventScroll: true })
    const blockInteraction = (event: Event) => {
      event.preventDefault()
      event.stopImmediatePropagation()
    }
    document.addEventListener('keydown', blockInteraction, true)
    document.addEventListener('wheel', blockInteraction, { capture: true, passive: false })
    document.addEventListener('touchmove', blockInteraction, { capture: true, passive: false })
    return () => {
      document.removeEventListener('keydown', blockInteraction, true)
      document.removeEventListener('wheel', blockInteraction, true)
      document.removeEventListener('touchmove', blockInteraction, true)
    }
  }, [mobileLoading])

  const goToPage = (event: FormEvent) => {
    event.preventDefault()
    const parsed = Number.parseInt(targetPage, 10)
    const nextPage = Number.isFinite(parsed) ? Math.min(Math.max(parsed, 1), lastPage) : page
    setTargetPage(String(nextPage))
    onPageChange(nextPage)
  }

  return (
    <>
      {mobileLazy ? (
        <div
          ref={mobileMarkerRef}
          className="h-px overflow-hidden sm:hidden"
          aria-hidden="true"
        >
          &nbsp;
        </div>
      ) : null}
      {mobileLazy && mobileLoading ? (
        <div
          ref={loadingOverlayRef}
          className="fixed inset-0 z-[200] grid touch-none place-items-center bg-slate-950/65 backdrop-blur-[2px] sm:hidden"
          role="alertdialog"
          aria-modal="true"
          aria-live="assertive"
          aria-label="Đang tải trang dữ liệu tiếp theo"
          aria-busy="true"
          tabIndex={-1}
        >
          <span className="grid size-20 place-items-center rounded-2xl border border-white/15 bg-slate-950/90 shadow-2xl">
            <Spinner size="lg" decorative />
          </span>
        </div>
      ) : null}
      <nav
      aria-label="Phân trang"
      className={cn(
        'grid min-w-0 grid-cols-2 items-center gap-2 sm:flex sm:flex-wrap sm:justify-end',
        mobileLazy && 'max-sm:hidden',
        className,
      )}
    >
      {pageSize !== undefined && onPageSizeChange ? (
        <label className="col-span-2 flex items-center justify-between gap-2 text-sm text-muted-foreground sm:mr-1 sm:justify-start">
          <span>Số dòng</span>
          <select
            value={pageSize}
            onChange={(event) => onPageSizeChange(Number(event.target.value))}
            className="h-9 rounded-md border border-input bg-background px-2 text-foreground shadow-xs outline-none focus-visible:ring-2 focus-visible:ring-ring"
            aria-label="Số dòng mỗi trang"
          >
            {pageSizeOptions.map((size) => (
              <option key={size} value={size}>
                {size}
              </option>
            ))}
          </select>
        </label>
      ) : null}
      <Button
        type="button"
        variant="outline"
        size="icon-sm"
        disabled={page === 1}
        onClick={() => onPageChange(1)}
        aria-label="Đến trang đầu"
        className="hidden sm:inline-flex"
      >
        <ChevronFirst aria-hidden="true" />
      </Button>
      <Button
        type="button"
        variant="outline"
        size="sm"
        className="w-full sm:w-auto"
        disabled={page === 1}
        onClick={() => onPageChange(page - 1)}
        aria-label="Đến trang trước"
      >
        <ChevronLeft aria-hidden="true" />
        <span className="hidden sm:inline">Trước</span>
      </Button>

      <div className="hidden items-center gap-1 sm:flex" aria-live="polite">
        {pageItems.map((item) =>
          typeof item === 'number' ? (
            <Button
              key={item}
              type="button"
              variant={item === page ? 'default' : 'outline'}
              size="icon-sm"
              onClick={() => onPageChange(item)}
              aria-label={`Đến trang ${item}`}
              aria-current={item === page ? 'page' : undefined}
            >
              {item}
            </Button>
          ) : (
            <span
              key={item}
              className="grid size-9 place-items-center text-muted-foreground"
              aria-hidden="true"
            >
              …
            </span>
          ),
        )}
      </div>

      <span
        className="col-span-2 row-start-3 text-center text-sm text-muted-foreground sm:hidden"
        aria-live="polite"
      >
        Trang {page} / {lastPage}
      </span>

      <Button
        type="button"
        variant="outline"
        size="sm"
        className="w-full sm:w-auto"
        disabled={!hasNextPage}
        onClick={() => onPageChange(page + 1)}
        aria-label="Đến trang sau"
      >
        <span className="hidden sm:inline">Sau</span>
        <ChevronRight aria-hidden="true" />
      </Button>
      <Button
        type="button"
        variant="outline"
        size="icon-sm"
        disabled={page === lastPage}
        onClick={() => onPageChange(lastPage)}
        aria-label="Đến trang cuối"
        className="hidden sm:inline-flex"
      >
        <ChevronLast aria-hidden="true" />
      </Button>
      <form
        className="col-span-2 flex items-center justify-end gap-2 border-t pt-2 sm:ml-1 sm:border-0 sm:pt-0"
        onSubmit={goToPage}
      >
        <label htmlFor={targetPageId} className="text-sm text-muted-foreground">
          Đến trang
        </label>
        <Input
          id={targetPageId}
          type="number"
          min={1}
          max={lastPage}
          value={targetPage}
          onChange={(event) => setTargetPage(event.target.value)}
          className="h-9 w-20"
          aria-label={`Nhập trang từ 1 đến ${lastPage}`}
        />
        <Button type="submit" variant="outline" size="sm">
          Đi
        </Button>
      </form>
      </nav>
    </>
  )
}

export { Pagination }
