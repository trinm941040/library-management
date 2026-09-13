import { History } from 'lucide-react'
import { Link } from 'react-router-dom'
import { Button } from '@/common/components/ui/button'
import { PermissionBoundary } from '@/shared/auth/PermissionBoundary'

type EntityActivityLinkProps = {
  entityType: string
  entityId: string
  label: string
}

export function EntityActivityLink({ entityType, entityId, label }: EntityActivityLinkProps) {
  const query = new URLSearchParams({ entityType, entityId }).toString()
  return (
    <PermissionBoundary requiredPermissions={['audit-logs.read']}>
      <Button asChild variant="ghost" size="icon">
        <Link to={`/audit-log?${query}`} aria-label={`Xem lịch sử thay đổi ${label}`}>
          <History />
        </Link>
      </Button>
    </PermissionBoundary>
  )
}
