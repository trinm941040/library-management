import { cn } from '@/utils/cn'

const sizes = {
  sm: 'size-4',
  md: 'size-6',
  lg: 'size-12',
} as const

export function Spinner({
  size = 'md',
  label = 'Đang tải',
  decorative = false,
  className,
}: {
  size?: keyof typeof sizes
  label?: string
  decorative?: boolean
  className?: string
}) {
  return (
    <span
      className={cn('inline-flex shrink-0 items-center justify-center text-current', className)}
      role={decorative ? undefined : 'status'}
      aria-label={decorative ? undefined : label}
      aria-hidden={decorative || undefined}
    >
      <img
        src="/assets/library-spinner.svg"
        alt=""
        aria-hidden="true"
        className={cn('block shrink-0', sizes[size])}
      />
    </span>
  )
}
