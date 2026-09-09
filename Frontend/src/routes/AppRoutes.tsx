import { lazy, Suspense } from 'react'
import { Navigate, Route, Routes } from 'react-router-dom'
import { useAuth } from '../auth/AuthProvider'
import { AppLayout } from '../layouts/AppLayout'
import { BooksPage } from '../pages/books/BooksPage'
import { BorrowingsPage } from '../pages/borrowings/BorrowingsPage'
import { DashboardPage } from '../pages/dashboard/DashboardPage'
import { LoginPage } from '../pages/login/LoginPage'
import { ReservationsPage } from '../pages/reservations/ReservationsPage'
import { SettingsPage } from '../pages/settings/SettingsPage'
import { RolePermissionPage } from '../pages/roles/RolePermissionPage'
import { UserPage } from '../pages/users/UserPage'
import { ViolationsPage } from '../pages/violations/ViolationsPage'
import { EmployeePage } from '../pages/employee/EmployeePage'
import { ConfigPage } from '../pages/config/ConfigPage'
import { ActivityLogPage } from '../pages/activity-logs/ActivityLogPage'
import { InfoPage } from '../pages/info/InfoPage'
import { OtherSettingsPage } from '../pages/other-settings/OtherSettingsPage'
import { MemberPage } from '../pages/members/MemberPage'
import { ProtectedRoute } from './ProtectedRoute'

const ProfilePage = lazy(() => import('../pages/profile/ProfilePage').then((module) => ({ default: module.ProfilePage })))
const ChangePasswordPage = lazy(() => import('../pages/profile/ChangePasswordPage').then((module) => ({ default: module.ChangePasswordPage })))

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
        <Route path="/books" element={<BooksPage />} />
        <Route path="/borrowings" element={<BorrowingsPage />} />
        <Route path="/reservations" element={<ReservationsPage />} />
        <Route path="/violations" element={<ViolationsPage />} />
        <Route path="/employee" element={<EmployeePage />} />
        <Route path="/members" element={<MemberPage />} />
        <Route
          path="/roles"
          element={
            <ProtectedRoute requiredRole="Administrator">
              <RolePermissionPage />
            </ProtectedRoute>
          }
        />
        <Route path="/settings" element={<SettingsPage />} />
        <Route path="/profile" element={<Suspense fallback={<p className="p-6 text-center">Đang tải hồ sơ...</p>}><ProfilePage /></Suspense>} />
        <Route path="/profile/change-password" element={<Suspense fallback={<p className="p-6 text-center">Đang tải...</p>}><ChangePasswordPage /></Suspense>} />
        {/* System Management Routes */}
        <Route path="/system/config" element={<ConfigPage />} />
        <Route path="/system/activity-log" element={<ActivityLogPage />} />
        <Route path="/system/info" element={<InfoPage />} />
        <Route path="/system/other-settings" element={<OtherSettingsPage />} />
      </Route>
      <Route path="*" element={<Navigate to="/dashboard" replace />} />
    </Routes>
  )
}
