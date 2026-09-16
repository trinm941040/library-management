import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '../auth/AuthProvider'
import { canAll } from '@/shared/auth/permissions'
import { ForbiddenPage } from '@/pages/errors/ForbiddenPage'
import { ScreenState } from '@/common/components'

type ProtectedRouteProps = {
  children: ReactNode
  requiredPermissions?: readonly string[]
}

export function ProtectedRoute({ children, requiredPermissions = [] }: ProtectedRouteProps) {
  const { status, user, retrySession } = useAuth()
  const location = useLocation()

  if (status === 'error')
    return (
      <ScreenState
        kind="error"
        title="Không thể kiểm tra phiên đăng nhập"
        actionLabel="Thử lại"
        onAction={retrySession}
      />
    )

  if (status === 'loading') {
    return <ScreenState kind="loading" title="Đang kiểm tra đăng nhập" />
  }

  if (status === 'unauthenticated') {
    return (
      <Navigate
        to="/login"
        replace
        state={{
          from: location.pathname + location.search + location.hash,
          notice: 'Vui lòng đăng nhập để tiếp tục.',
        }}
      />
    )
  }

  if (!canAll(user?.permissions ?? [], requiredPermissions)) {
    return <ForbiddenPage />
  }

  return children
}
