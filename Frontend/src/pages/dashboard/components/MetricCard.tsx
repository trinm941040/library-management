import type { ReactNode } from 'react'
import { ArrowUpRight } from 'lucide-react'
import { Card } from '@/common/components/ui/card'

type MetricCardProps = {
  label: string
  value: string | number
  delta?: string
  positive?: boolean
  icon: ReactNode
  tone: string
  onClick?: () => void
  subtext?: string
}

export function MetricCard({
  label,
  value,
  delta,
  positive,
  icon,
  tone,
  onClick,
  subtext,
}: MetricCardProps) {
  return (
    <Card
      className={`metric-card transition-all duration-200 ${
        onClick ? 'cursor-pointer hover:shadow-md hover:border-primary/40 group' : ''
      }`}
      onClick={onClick}
      role={onClick ? 'button' : undefined}
      tabIndex={onClick ? 0 : undefined}
      onKeyDown={
        onClick
          ? (e) => {
              if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault()
                onClick()
              }
            }
          : undefined
      }
    >
      <div className="flex items-start justify-between w-full">
        <div className={`metric-icon ${tone}`}>{icon}</div>
        {onClick && (
          <span className="opacity-0 group-hover:opacity-100 transition-opacity text-muted-foreground">
            <ArrowUpRight className="w-4 h-4" />
          </span>
        )}
      </div>
      <div className="metric-copy mt-2">
        <p>{label}</p>
        <strong>{typeof value === 'number' ? value.toLocaleString('vi-VN') : value}</strong>
        {delta ? (
          <span className={positive ? 'positive' : 'negative'}>
            {positive ? '↗' : '↘'} {delta} <em>so với kỳ trước</em>
          </span>
        ) : subtext ? (
          <span className="text-xs text-muted-foreground mt-1">{subtext}</span>
        ) : null}
      </div>
    </Card>
  )
}
