import { Navigate, Route, Routes } from 'react-router-dom'
import { useAuth } from '../auth/AuthProvider'
import { AppLayout } from '../layouts/AppLayout'
import { DashboardPage } from '../pages/dashboard/DashboardPage'
import { LoginPage } from '../pages/login/LoginPage'
import { SettingsPage } from '../pages/settings/SettingsPage'
import { RolePermissionPage } from '../pages/roles/RolePermissionPage'
import { UserPage } from '../pages/users/UserPage'
import { ProtectedRoute } from './ProtectedRoute'

function LoginRoute() {
  const { status } = useAuth()

  if (status === 'loading') {
    return <p className="p-6 text-center">Đang kiểm tra đăng nhập...</p>
  }

  return status === 'authenticated' ? <Navigate to="/dashboard" replace /> : <LoginPage />
}

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/" element={<Navigate to="/dashboard" replace />} />
      <Route path="/login" element={<LoginRoute />} />
      <Route
        element={
          <ProtectedRoute>
            <AppLayout />
          </ProtectedRoute>
        }
      >
        <Route path="/dashboard" element={<DashboardPage />} />
        <Route path="/users" element={<UserPage />} />
        <Route
          path="/roles"
          element={
            <ProtectedRoute requiredRole="Administrator">
              <RolePermissionPage />
            </ProtectedRoute>
          }
        />
        <Route path="/settings" element={<SettingsPage />} />
      </Route>
      <Route path="*" element={<Navigate to="/dashboard" replace />} />
    </Routes>
  )
}
