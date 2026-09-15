import { SlidersHorizontal, X } from 'lucide-react'
import type { ReactNode } from 'react'
import { Button } from '@/common/components/ui/button'
import { cn } from '@/utils/cn'

export function FilterPanel({
  children,
  hasFilters,
  onReset,
  resultCount,
  className,
}: {
  children: ReactNode
  hasFilters?: boolean
  onReset?: () => void
  resultCount?: number
  className?: string
}) {
  return (
    <section
      aria-label="Bộ lọc dữ liệu"
      className={cn(
        'flex flex-col gap-3 rounded-lg border bg-muted/20 p-3 sm:flex-row sm:items-center',
        className,
      )}
    >
      <SlidersHorizontal
        className="hidden size-4 shrink-0 text-muted-foreground sm:block"
        aria-hidden="true"
      />
      <div className="grid min-w-0 flex-1 gap-3 sm:flex sm:items-center">{children}</div>
      {typeof resultCount === 'number' ? (
        <span className="text-xs whitespace-nowrap text-muted-foreground" aria-live="polite">
          {resultCount} kết quả
        </span>
      ) : null}
      {onReset ? (
        <Button type="button" variant="ghost" size="sm" disabled={!hasFilters} onClick={onReset}>
          <X /> Xóa lọc
        </Button>
      ) : null}
    </section>
  )
}
