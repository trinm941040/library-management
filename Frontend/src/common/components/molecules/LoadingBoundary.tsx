import type { ReactNode } from 'react'
import { cn } from '@/utils/cn'
import { Spinner } from '../atoms/Spinner'

type LoadingMode = 'inline' | 'region' | 'overlay'

export function LoadingBoundary({
  loading,
  label = 'Đang tải dữ liệu',
  mode = 'region',
  children,
  className,
}: {
  loading: boolean
  label?: string
  mode?: LoadingMode
  children?: ReactNode
  className?: string
}) {
  if (mode === 'inline') {
    if (!loading) return <>{children}</>
    return (
      <span
        className={cn('inline-flex items-center gap-2 text-sm text-muted-foreground', className)}
      >
        <Spinner size="sm" label={label} />
        <span aria-hidden="true">{label}</span>
      </span>
    )
  }

  if (mode === 'overlay') {
    return (
      <div className={cn('relative', className)} aria-busy={loading}>
        <div inert={loading || undefined}>{children}</div>
        {loading ? (
          <div className="absolute inset-0 z-10 grid place-items-center rounded-[inherit] bg-background/75 backdrop-blur-[1px]">
            <div className="inline-flex items-center gap-2 rounded-md border bg-background px-4 py-3 text-sm shadow-sm">
              <Spinner label={label} />
              <span aria-hidden="true">{label}</span>
            </div>
          </div>
        ) : null}
      </div>
    )
  }

  if (!loading) return <>{children}</>

  return (
    <div
      className={cn('grid min-h-32 place-items-center', className)}
      aria-busy={loading}
      role={loading ? 'status' : undefined}
      aria-label={loading ? label : undefined}
    >
      {loading ? (
        <span className="inline-flex items-center gap-3 text-sm text-muted-foreground">
          <Spinner size="lg" decorative />
          <span aria-hidden="true">{label}</span>
        </span>
      ) : (
        children
      )}
    </div>
  )
}
