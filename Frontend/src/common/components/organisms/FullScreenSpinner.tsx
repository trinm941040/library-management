import { Spinner } from '../atoms/Spinner'

export function FullScreenSpinner({ label = 'Đang xử lý dữ liệu' }: { label?: string }) {
  return (
    <div
      className="fixed inset-0 z-[200] grid place-items-center bg-black/55 p-4 backdrop-blur-[2px]"
      role="status"
      aria-live="polite"
      aria-label={label}
      aria-busy="true"
    >
      <div className="grid min-w-48 place-items-center gap-3 rounded-xl border bg-background px-7 py-6 text-center text-foreground shadow-2xl">
        <Spinner size="lg" decorative />
        <p className="text-sm font-medium" aria-hidden="true">
          {label}
        </p>
      </div>
    </div>
  )
}
