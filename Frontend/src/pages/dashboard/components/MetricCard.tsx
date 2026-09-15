import type { ReactNode } from 'react'
import { Card } from '@/common/components/ui/card'

type MetricCardProps = {
  label: string
  value: string
  delta: string
  positive?: boolean
  icon: ReactNode
  tone: string
}

export function MetricCard({ label, value, delta, positive, icon, tone }: MetricCardProps) {
  return (
    <Card className="metric-card">
      <div className={`metric-icon ${tone}`}>{icon}</div>
      <div className="metric-copy">
        <p>{label}</p>
        <strong>{value}</strong>
        <span className={positive ? 'positive' : 'negative'}>
          {positive ? '↗' : '↘'} {delta} <em>so với tháng trước</em>
        </span>
      </div>
    </Card>
  )
}
