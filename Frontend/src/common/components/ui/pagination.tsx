import { ChevronFirst, ChevronLast, ChevronLeft, ChevronRight } from 'lucide-react'
import { type FormEvent, useEffect, useId, useState } from 'react'
import { Button } from '@/common/components/ui/button'
import { Input } from '@/common/components/ui/input'
import { cn } from '@/utils/cn'

type PaginationProps = {
  currentPage: number
  totalPages: number
  onPageChange: (page: number) => void
  pageSize?: number
  pageSizeOptions?: readonly number[]
  onPageSizeChange?: (pageSize: number) => void
  className?: string
}

function Pagination({
  currentPage,
  totalPages,
  onPageChange,
  pageSize,
  pageSizeOptions = [10, 20, 50, 100],
  onPageSizeChange,
  className,
}: PaginationProps) {
  const lastPage = Math.max(1, totalPages)
  const page = Math.min(Math.max(currentPage, 1), lastPage)
  const [targetPage, setTargetPage] = useState(String(page))
  const targetPageId = useId()

  const pageItems: Array<number | 'start-ellipsis' | 'end-ellipsis'> = (() => {
    if (lastPage <= 5) return Array.from({ length: lastPage }, (_, index) => index + 1)
    if (page <= 3) return [1, 2, 3, 'end-ellipsis', lastPage]
    if (page >= lastPage - 2)
      return [1, 'start-ellipsis', lastPage - 2, lastPage - 1, lastPage]
    return [1, 'start-ellipsis', page - 1, page, page + 1, 'end-ellipsis', lastPage]
  })()

  useEffect(() => setTargetPage(String(page)), [page])

  const goToPage = (event: FormEvent) => {
    event.preventDefault()
    const parsed = Number.parseInt(targetPage, 10)
    const nextPage = Number.isFinite(parsed) ? Math.min(Math.max(parsed, 1), lastPage) : page
    setTargetPage(String(nextPage))
    onPageChange(nextPage)
  }

  return (
    <nav
      aria-label="Phân trang"
      className={cn(
        'grid min-w-0 grid-cols-2 items-center gap-2 sm:flex sm:flex-wrap sm:justify-end',
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
        disabled={page === lastPage}
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
  )
}

export { Pagination }
