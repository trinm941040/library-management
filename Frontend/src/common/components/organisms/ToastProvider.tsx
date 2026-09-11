import { CheckCircle2, CircleAlert, Info, X } from 'lucide-react'
import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react'
import { Button } from '@/common/components/ui/button'

export type ToastTone = 'success' | 'error' | 'info'
export type ToastPosition =
  'top-left' | 'top-center' | 'top-right' | 'bottom-left' | 'bottom-center' | 'bottom-right'
export type ToastOptions = {
  tone?: ToastTone
  position?: ToastPosition
  durationMs?: number
}
type Toast = { id: number; message: string; tone: ToastTone; position: ToastPosition }
type ToastContextValue = {
  showToast: (message: string, options?: ToastOptions | ToastTone) => void
}
const ToastContext = createContext<ToastContextValue | null>(null)
let nextToastId = 1

const toneClasses: Record<ToastTone, { container: string; icon: string }> = {
  success: {
    container:
      'border-emerald-600/30 bg-emerald-50 text-emerald-950 dark:bg-emerald-950/90 dark:text-emerald-50',
    icon: 'text-emerald-700 dark:text-emerald-300',
  },
  error: {
    container: 'border-destructive/30 bg-red-50 text-red-950 dark:bg-red-950/90 dark:text-red-50',
    icon: 'text-destructive dark:text-red-300',
  },
  info: {
    container: 'border-blue-600/30 bg-blue-50 text-blue-950 dark:bg-blue-950/90 dark:text-blue-50',
    icon: 'text-blue-700 dark:text-blue-300',
  },
}

const positions: ToastPosition[] = [
  'top-left',
  'top-center',
  'top-right',
  'bottom-left',
  'bottom-center',
  'bottom-right',
]

const positionClasses: Record<ToastPosition, string> = {
  'top-left': 'top-4 left-4',
  'top-center': 'top-4 left-1/2 -translate-x-1/2',
  'top-right': 'top-4 right-4',
  'bottom-left': 'bottom-4 left-4',
  'bottom-center': 'bottom-4 left-1/2 -translate-x-1/2',
  'bottom-right': 'right-4 bottom-4',
}

export function ToastProvider({
  children,
  position = 'bottom-right',
  durationMs = 5000,
}: {
  children: ReactNode
  position?: ToastPosition
  durationMs?: number
}) {
  const [toasts, setToasts] = useState<Toast[]>([])
  const dismiss = useCallback(
    (id: number) => setToasts((values) => values.filter((toast) => toast.id !== id)),
    [],
  )
  const showToast = useCallback(
    (message: string, options: ToastOptions | ToastTone = {}) => {
      const resolvedOptions = typeof options === 'string' ? { tone: options } : options
      const id = nextToastId++
      setToasts((values) => [
        ...values,
        {
          id,
          message,
          tone: resolvedOptions.tone ?? 'success',
          position: resolvedOptions.position ?? position,
        },
      ])
      const toastDuration = resolvedOptions.durationMs ?? durationMs
      if (toastDuration > 0) window.setTimeout(() => dismiss(id), toastDuration)
    },
    [dismiss, durationMs, position],
  )
  const value = useMemo(() => ({ showToast }), [showToast])
  return (
    <ToastContext.Provider value={value}>
      {children}
      {positions.map((toastPosition) => {
        const positionedToasts = toasts.filter((toast) => toast.position === toastPosition)
        if (positionedToasts.length === 0) return null
        const isTop = toastPosition.startsWith('top')
        return (
          <div
            key={toastPosition}
            className={`fixed z-[100] grid w-[min(24rem,calc(100vw-2rem))] gap-2 ${positionClasses[toastPosition]}`}
            aria-live="polite"
            aria-label="Thông báo hệ thống"
          >
            {positionedToasts.map((toast) => {
              const Icon =
                toast.tone === 'success'
                  ? CheckCircle2
                  : toast.tone === 'error'
                    ? CircleAlert
                    : Info
              const classes = toneClasses[toast.tone]
              return (
                <div
                  key={toast.id}
                  role={toast.tone === 'error' ? 'alert' : 'status'}
                  className={`flex items-start gap-3 rounded-lg border p-4 shadow-lg motion-safe:animate-in ${isTop ? 'motion-safe:slide-in-from-top-2' : 'motion-safe:slide-in-from-bottom-2'} ${classes.container}`}
                >
                  <Icon className={`mt-0.5 size-5 ${classes.icon}`} aria-hidden="true" />
                  <p className="min-w-0 flex-1 text-sm">{toast.message}</p>
                  <Button
                    variant="ghost"
                    size="icon-xs"
                    aria-label="Đóng thông báo"
                    onClick={() => dismiss(toast.id)}
                  >
                    <X />
                  </Button>
                </div>
              )
            })}
          </div>
        )
      })}
    </ToastContext.Provider>
  )
}

export function useToast() {
  const context = useContext(ToastContext)
  if (!context) {
    return {
      showToast: (message: string, tone?: ToastOptions | ToastTone) => {
        console.warn('Toast:', message, tone)
      },
    }
  }
  return context
}
