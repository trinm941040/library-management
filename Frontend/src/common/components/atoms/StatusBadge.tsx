import { Badge } from '@/common/components/ui/badge'
import { cn } from '@/utils/cn'

export type StatusTone = 'neutral' | 'info' | 'success' | 'warning' | 'danger'

const toneClasses: Record<StatusTone, string> = {
  neutral: 'border-border bg-muted text-muted-foreground',
  info: 'border-primary/20 bg-primary/10 text-primary',
  success: 'border-emerald-600/20 bg-emerald-600/10 text-emerald-700 dark:text-emerald-300',
  warning: 'border-amber-600/20 bg-amber-500/10 text-amber-700 dark:text-amber-300',
  danger: 'border-destructive/20 bg-destructive/10 text-destructive',
}

export function StatusBadge({
  label,
  tone = 'neutral',
  className,
}: {
  label: string
  tone?: StatusTone
  className?: string
}) {
  return (
    <Badge variant="outline" className={cn(toneClasses[tone], className)}>
      {label}
    </Badge>
  )
}
