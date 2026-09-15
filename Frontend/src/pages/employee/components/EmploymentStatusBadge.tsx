import { StatusBadge, type StatusTone } from '@/common/components'
import { employmentStatusLabels, type EmploymentStatus } from '../employee-api'

const statusTones: Record<EmploymentStatus, StatusTone> = {
  Active: 'success',
  OnLeave: 'warning',
  Inactive: 'neutral',
  Terminated: 'danger',
}

export function EmploymentStatusBadge({ status }: { status: EmploymentStatus }) {
  return <StatusBadge label={employmentStatusLabels[status]} tone={statusTones[status]} />
}
