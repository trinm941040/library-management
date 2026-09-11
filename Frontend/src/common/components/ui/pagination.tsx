import { ChevronFirst, ChevronLast, ChevronLeft, ChevronRight } from 'lucide-react'
import { Button } from '@/common/components/ui/button'
import { cn } from '@/utils/cn'

type PaginationProps = {
  currentPage: number
  totalPages: number
  onPageChange: (page: number) => void
  className?: string
}

function Pagination({ currentPage, totalPages, onPageChange, className }: PaginationProps) {
  const lastPage = Math.max(1, totalPages)
  const page = Math.min(Math.max(currentPage, 1), lastPage)

  return (
    <nav aria-label="Phân trang" className={cn('flex items-center justify-end gap-2', className)}>
      <Button
        type="button"
        variant="outline"
        size="icon-sm"
        disabled={page === 1}
        onClick={() => onPageChange(1)}
        aria-label="Đến trang đầu"
      >
        <ChevronFirst aria-hidden="true" />
      </Button>
      <Button
        type="button"
        variant="outline"
        size="sm"
        disabled={page === 1}
        onClick={() => onPageChange(page - 1)}
        aria-label="Đến trang trước"
      >
        <ChevronLeft aria-hidden="true" />
        <span className="hidden sm:inline">Trước</span>
      </Button>

      <span
        className="min-w-24 text-center text-sm text-muted-foreground"
        aria-live="polite"
        aria-current="page"
      >
        Trang {page} / {lastPage}
      </span>

      <Button
        type="button"
        variant="outline"
        size="sm"
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
      >
        <ChevronLast aria-hidden="true" />
      </Button>
    </nav>
  )
}

export { Pagination }
