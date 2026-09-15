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
const BooksPage = lazy(() =>
  import('../pages/books/BooksPage').then((m) => ({ default: m.BooksPage })),
)
const CatalogDetailPage = lazy(() =>
  import('../pages/books/CatalogDetailPage').then((m) => ({ default: m.CatalogDetailPage })),
)
const BorrowingsPage = lazy(() =>
  import('../pages/borrowings/BorrowingsPage').then((m) => ({ default: m.BorrowingsPage })),
)
const ReservationsPage = lazy(() =>
  import('../pages/reservations/ReservationsPage').then((m) => ({ default: m.ReservationsPage })),
)
const ViolationsPage = lazy(() =>
  import('../pages/violations/ViolationsPage').then((m) => ({ default: m.ViolationsPage })),
)
const SettingsPage = lazy(() =>
  import('../pages/settings/SettingsPage').then((m) => ({ default: m.SettingsPage })),
)
const RolePermissionPage = lazy(() =>
  import('../pages/roles/RolePermissionPage').then((m) => ({ default: m.RolePermissionPage })),
)
const UserPage = lazy(() =>
  import('../pages/users/UserPage').then((m) => ({ default: m.UserPage })),
)
const EmployeePage = lazy(() =>
  import('../pages/employee/EmployeePage').then((m) => ({ default: m.EmployeePage })),
)
const BranchesPage = lazy(() =>
  import('../pages/branches/BranchesPage').then((m) => ({ default: m.BranchesPage })),
)
const CopiesPage = lazy(() =>
  import('../pages/copies/CopiesPage').then((m) => ({ default: m.CopiesPage })),
)
const SuppliersPage = lazy(() =>
  import('../pages/suppliers/SuppliersPage').then((m) => ({ default: m.SuppliersPage })),
)
const StockReceiptsPage = lazy(() =>
  import('../pages/stock-receipts/StockReceiptsPage').then((m) => ({ default: m.StockReceiptsPage })),
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
const OtherSettingsPage = lazy(() =>
  import('../pages/other-settings/OtherSettingsPage').then((m) => ({
    default: m.OtherSettingsPage,
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
        <Route path="/users" element={page('/users', <UserPage />)} />
        <Route path="/books" element={page('/books', <BooksPage />)} />
        <Route path="/catalog" element={page('/books', <BooksPage />)} />
        <Route path="/catalog/:bookId" element={page('/books', <CatalogDetailPage />)} />
        <Route path="/borrowings" element={page('/borrowings', <BorrowingsPage />)} />
        <Route path="/reservations" element={page('/reservations', <ReservationsPage />)} />
        <Route path="/violations" element={page('/violations', <ViolationsPage />)} />
        <Route path="/employee" element={page('/employee', <EmployeePage />)} />
        <Route path="/branches" element={page('/branches', <BranchesPage />)} />
        <Route path="/copies" element={page('/copies', <CopiesPage />)} />
        <Route path="/suppliers" element={page('/suppliers', <SuppliersPage />)} />
        <Route path="/stock-receipts" element={page('/stock-receipts', <StockReceiptsPage />)} />
        <Route path="/members" element={page('/members', <MemberPage />)} />
        <Route path="/roles" element={page('/roles', <RolePermissionPage initialView="roles" />)} />
        <Route
          path="/permissions"
          element={page('/permissions', <RolePermissionPage initialView="permissions" />)}
        />
        <Route path="/settings" element={page('/settings', <SettingsPage />)} />
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
        <Route
          path="/system/activity-log"
          element={page('/system/activity-log', <ActivityLogPage />)}
        />
        <Route path="/system/info" element={page('/system/info', <InfoPage />)} />
        <Route
          path="/system/other-settings"
          element={page('/system/other-settings', <OtherSettingsPage />)}
        />
      </Route>
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  )
}
