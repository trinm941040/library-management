import { AlertTriangle, CircleCheck, Inbox, LoaderCircle, LockKeyhole } from 'lucide-react'
import { Button } from '@/common/components/ui/button'

type StateKind = 'loading' | 'empty' | 'error' | 'forbidden' | 'conflict' | 'success'
const icons = {
  loading: LoaderCircle,
  empty: Inbox,
  error: AlertTriangle,
  forbidden: LockKeyhole,
  conflict: AlertTriangle,
  success: CircleCheck,
}

export function ScreenState({
  kind,
  title,
  description,
  actionLabel,
  onAction,
  compact = false,
}: {
  kind: StateKind
  title: string
  description?: string
  actionLabel?: string
  onAction?: () => void
  compact?: boolean
}) {
  const Icon = icons[kind]
  return (
    <div
      className={
        compact
          ? 'grid place-items-center gap-2 p-6 text-center'
          : 'grid min-h-52 place-items-center gap-3 rounded-lg border border-dashed p-8 text-center'
      }
      role={kind === 'error' || kind === 'conflict' ? 'alert' : 'status'}
      aria-live="polite"
    >
      <Icon
        className={
          kind === 'loading'
            ? 'size-6 animate-spin motion-reduce:animate-none text-primary'
            : 'size-6 text-muted-foreground'
        }
        aria-hidden="true"
      />
      <div>
        <p className="font-medium">{title}</p>
        {description ? (
          <p className="mt-1 max-w-md text-sm text-muted-foreground">{description}</p>
        ) : null}
      </div>
      {actionLabel && onAction ? (
        <Button type="button" variant="outline" size="sm" onClick={onAction}>
          {actionLabel}
        </Button>
      ) : null}
    </div>
  )
}
