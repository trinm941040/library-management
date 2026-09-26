import { AlertTriangle, CircleCheck, Inbox, LockKeyhole } from 'lucide-react'
import { Button } from '@/common/components/ui/button'
import { Spinner } from '../atoms/Spinner'

type StateKind = 'loading' | 'empty' | 'error' | 'forbidden' | 'conflict' | 'success'
const icons = {
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
  const Icon = kind === 'loading' ? null : icons[kind]
  return (
    <div
      className={
        compact
          ? 'grid min-w-0 place-items-center gap-2 p-4 text-center sm:p-6'
          : 'grid min-h-52 min-w-0 place-items-center gap-3 rounded-lg border border-dashed p-4 text-center sm:p-8'
      }
      role={kind === 'error' || kind === 'conflict' ? 'alert' : 'status'}
      aria-label={kind === 'loading' ? title : undefined}
      aria-live="polite"
      aria-busy={kind === 'loading' || undefined}
    >
      {kind === 'loading' ? (
        <Spinner size="lg" decorative className="text-primary" />
      ) : Icon ? (
        <Icon className="size-6 text-muted-foreground" aria-hidden="true" />
      ) : null}
      <div className="min-w-0">
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
