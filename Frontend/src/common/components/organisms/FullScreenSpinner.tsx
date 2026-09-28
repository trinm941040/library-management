import { Spinner } from '../atoms/Spinner'

export function FullScreenSpinner({ label = '' }: { label?: string }) {
  return (
    <div
      className="fixed inset-0 z-[200] grid place-items-center bg-black/55 p-4 backdrop-blur-[2px]"
      role="status"
      aria-live="polite"
      aria-label={label}
      aria-busy="true"
    >
      <div className="grid min-w-48 place-items-center gap-3 bg-transparent px-7 py-6 text-center text-white">
        <Spinner size="xl" decorative />
        <p className="text-sm font-semibold drop-shadow-md" aria-hidden="true">
          {label}
        </p>
      </div>
    </div>
  )
}
