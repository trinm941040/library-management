import type { ReactNode } from 'react'
import { Navigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthProvider'
import { canAll } from '@/shared/auth/permissions'
import { ForbiddenPage } from '@/pages/errors/ForbiddenPage'

type ProtectedRouteProps = {
  children: ReactNode
  requiredPermissions?: readonly string[]
}

export function ProtectedRoute({ children, requiredPermissions = [] }: ProtectedRouteProps) {
  const { status, user } = useAuth()

  if (status === 'loading') {
    return <p className="p-6 text-center">Đang kiểm tra đăng nhập...</p>
  }

  if (status === 'unauthenticated') {
    return <Navigate to="/login" replace />
  }

  if (!canAll(user?.permissions ?? [], requiredPermissions)) {
    return <ForbiddenPage />
  }

  return children
}
