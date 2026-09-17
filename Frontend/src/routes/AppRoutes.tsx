import { lazy, Suspense } from 'react'
import type { ReactNode } from 'react'
import { Navigate, Route, Routes, useLocation } from 'react-router-dom'
import { useAuth } from '../auth/AuthProvider'
import { AppLayout } from '../layouts/AppLayout'
import { ProtectedRoute } from './ProtectedRoute'
import { routePermissions } from '@/app/navigation'
import { NotFoundPage } from '@/pages/errors/NotFoundPage'
import { safeIntendedDestination } from '@/shared/auth/intended-destination'
import { ScreenState } from '@/common/components'

const ProfilePage = lazy(() =>
  import('../pages/profile/ProfilePage').then((module) => ({ default: module.ProfilePage })),
)
const LoginPage = lazy(() =>
  import('../pages/login/LoginPage').then((module) => ({ default: module.LoginPage })),
)
const ChangePasswordPage = lazy(() =>
  import('../pages/profile/ChangePasswordPage').then((module) => ({
    default: module.ChangePasswordPage,
  })),
)
const DashboardPage = lazy(() =>
  import('../pages/dashboard/DashboardPage').then((m) => ({ default: m.DashboardPage })),
)
const ReportsPage = lazy(() =>
  import('../pages/reports/ReportsPage').then((m) => ({ default: m.ReportsPage })),
)
const NotificationsPage = lazy(() =>
  import('../pages/notifications/NotificationsPage').then((m) => ({ default: m.NotificationsPage })),
)
const BooksPage = lazy(() =>
  import('../pages/books/BooksPage').then((m) => ({ default: m.BooksPage })),
)
const BorrowingsPage = lazy(() =>
  import('../pages/borrowings/BorrowingsPage').then((m) => ({ default: m.BorrowingsPage })),
)
const CheckoutPage = lazy(() =>
  import('../pages/borrowings/CheckoutPage').then((m) => ({ default: m.CheckoutPage })),
)
const ReturnPage = lazy(() =>
  import('../pages/borrowings/ReturnPage').then((m) => ({ default: m.ReturnPage })),
)
const LoanDetailPage = lazy(() =>
  import('../pages/borrowings/LoanDetailPage').then((m) => ({ default: m.LoanDetailPage })),
)
const ReservationsPage = lazy(() =>
  import('../pages/reservations/ReservationsPage').then((m) => ({ default: m.ReservationsPage })),
)
const ViolationsPage = lazy(() =>
  import('../pages/violations/ViolationsPage').then((m) => ({ default: m.ViolationsPage })),
)
const PaymentsPage = lazy(() =>
  import('../pages/payments/PaymentsPage').then((m) => ({ default: m.PaymentsPage })),
)
const SettingsPage = lazy(() =>
  import('../pages/settings/SettingsPage').then((m) => ({ default: m.SettingsPage })),
)
const RolePermissionPage = lazy(() =>
  import('../pages/roles/RolePermissionPage').then((m) => ({ default: m.RolePermissionPage })),
)
const AccessAccountsPage = lazy(() =>
  import('../pages/access-accounts/AccessAccountsPage').then((m) => ({
    default: m.AccessAccountsPage,
  })),
)
const EmployeePage = lazy(() =>
  import('../pages/employee/EmployeePage').then((m) => ({ default: m.EmployeePage })),
)
const MemberPage = lazy(() =>
  import('../pages/members/MemberPage').then((m) => ({ default: m.MemberPage })),
)
const ConfigPage = lazy(() =>
  import('../pages/config/ConfigPage').then((m) => ({ default: m.ConfigPage })),
)
const ActivityLogPage = lazy(() =>
  import('../pages/activity-logs/ActivityLogPage').then((m) => ({ default: m.ActivityLogPage })),
)
const InfoPage = lazy(() => import('../pages/info/InfoPage').then((m) => ({ default: m.InfoPage })))
const ConfigurationPage = lazy(() =>
  import('../pages/settings/ConfigurationPage').then((m) => ({
    default: m.ConfigurationPage,
  })),
)

const page = (path: string, element: ReactNode) => (
  <ProtectedRoute requiredPermissions={routePermissions.get(path) ?? []}>
    <Suspense fallback={<p className="p-6 text-center">Đang tải...</p>}>{element}</Suspense>
  </ProtectedRoute>
)

function LoginRoute() {
  const { status, retrySession } = useAuth()
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
    return <p className="p-6 text-center">Đang kiểm tra đăng nhập...</p>
  }

  return status === 'authenticated' ? (
    <Navigate to={safeIntendedDestination(location.state?.from)} replace />
  ) : (
    <Suspense fallback={<p className="p-6 text-center">Đang tải...</p>}>
      <LoginPage />
    </Suspense>
  )
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
        <Route path="/dashboard" element={page('/dashboard', <DashboardPage />)} />
        <Route
          path="/access-accounts"
          element={page('/access-accounts', <AccessAccountsPage />)}
        />
        <Route path="/users" element={<Navigate to="/access-accounts" replace />} />
        <Route path="/books" element={page('/books', <BooksPage />)} />
        <Route path="/borrowings" element={page('/borrowings', <BorrowingsPage />)} />
        <Route path="/loans/:id" element={page('/loans/:id', <LoanDetailPage />)} />
        <Route path="/borrowings/:id" element={page('/borrowings/:id', <LoanDetailPage />)} />
        <Route path="/circulation/checkout" element={page('/circulation/checkout', <CheckoutPage />)} />
        <Route path="/circulation/return" element={page('/circulation/return', <ReturnPage />)} />
        <Route path="/reservations" element={page('/reservations', <ReservationsPage />)} />
        <Route path="/violations" element={page('/violations', <ViolationsPage />)} />
        <Route path="/payments" element={page('/payments', <PaymentsPage />)} />
        <Route path="/reports" element={page('/reports', <ReportsPage />)} />
        <Route path="/notifications" element={page('/notifications', <NotificationsPage />)} />
        <Route path="/staff" element={page('/staff', <EmployeePage />)} />
        <Route path="/employee" element={<Navigate to="/staff" replace />} />
        <Route path="/members" element={page('/members', <MemberPage />)} />
        <Route path="/roles" element={page('/roles', <RolePermissionPage initialView="roles" />)} />
        <Route
          path="/permissions"
          element={page('/permissions', <RolePermissionPage initialView="permissions" />)}
        />
        <Route path="/settings" element={page('/settings', <SettingsPage />)} />
        <Route path="/configuration" element={page('/configuration', <ConfigurationPage />)} />
        <Route
          path="/profile"
          element={
            <Suspense fallback={<p className="p-6 text-center">Đang tải hồ sơ...</p>}>
              <ProfilePage />
            </Suspense>
          }
        />
        <Route
          path="/profile/change-password"
          element={
            <Suspense fallback={<p className="p-6 text-center">Đang tải...</p>}>
              <ChangePasswordPage />
            </Suspense>
          }
        />
        {/* System Management Routes */}
        <Route path="/system/config" element={page('/system/config', <ConfigPage />)} />
        <Route path="/audit-log" element={page('/audit-log', <ActivityLogPage />)} />
        <Route path="/system/activity-log" element={<Navigate to="/audit-log" replace />} />
        <Route path="/system/info" element={page('/system/info', <InfoPage />)} />
        <Route path="/system/other-settings" element={<Navigate to="/settings" replace />} />
      </Route>
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  )
}
