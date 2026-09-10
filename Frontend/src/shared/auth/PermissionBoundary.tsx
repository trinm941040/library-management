import type { ReactNode } from 'react'
import { useAuth } from '@/auth/AuthProvider'
import { canAll } from './permissions'

export function PermissionBoundary({ requiredPermissions, children, fallback = null }: { requiredPermissions: readonly string[]; children: ReactNode; fallback?: ReactNode }) {
  const { user, status } = useAuth()
  if (status !== 'authenticated' || !canAll(user?.permissions ?? [], requiredPermissions)) return fallback
  return children
}
