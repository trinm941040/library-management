import { lazy, Suspense } from 'react'
import type { ReactNode } from 'react'
import { Navigate, Route, Routes, useLocation } from 'react-router-dom'
import { useAuth } from '../auth/AuthProvider'
import { AppLayout } from '../layouts/AppLayout'
import { ProtectedRoute } from './ProtectedRoute'
import { routePermissions } from '@/app/navigation'
import { NotFoundPage } from '@/pages/errors/NotFoundPage'
import { safeIntendedDestination } from '@/shared/auth/intended-destination'
import { LoadingBoundary, ScreenState } from '@/common/components'

const routeLoading = (label = 'Đang tải trang') => (
  <LoadingBoundary loading label={label} className="min-h-[40vh]" />
)

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
const StockReceiptDetailPage = lazy(() =>
  import('../pages/stock-receipts/StockReceiptDetailPage').then((m) => ({ default: m.StockReceiptDetailPage })),
)
const InventoryAuditsPage = lazy(() =>
  import('../pages/inventory-audits/InventoryAuditsPage').then((m) => ({ default: m.InventoryAuditsPage })),
)
const InventoryAuditDetailPage = lazy(() =>
  import('../pages/inventory-audits/InventoryAuditDetailPage').then((m) => ({ default: m.InventoryAuditDetailPage })),
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
    <Suspense fallback={routeLoading()}>{element}</Suspense>
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
    return routeLoading('Đang kiểm tra đăng nhập')
  }

  return status === 'authenticated' ? (
    <Navigate to={safeIntendedDestination(location.state?.from)} replace />
  ) : (
    <Suspense fallback={routeLoading()}>
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
        <Route path="/books" element={<Navigate to="/catalog" replace />} />
        <Route path="/catalog" element={page('/catalog', <BooksPage />)} />
        <Route path="/catalog/:bookId" element={page('/catalog', <CatalogDetailPage />)} />
        <Route path="/branches" element={page('/branches', <BranchesPage />)} />
        <Route path="/copies" element={page('/copies', <CopiesPage />)} />
        <Route path="/suppliers" element={page('/suppliers', <SuppliersPage />)} />
        <Route path="/stock-receipts" element={page('/stock-receipts', <StockReceiptsPage />)} />
        <Route path="/stock-receipts/new" element={page('/stock-receipts/new', <StockReceiptDetailPage />)} />
        <Route path="/stock-receipts/:id" element={page('/stock-receipts', <StockReceiptDetailPage />)} />
        <Route path="/inventory-audits" element={page('/inventory-audits', <InventoryAuditsPage />)} />
        <Route path="/inventory-audits/:id" element={page('/inventory-audits', <InventoryAuditDetailPage />)} />
        <Route path="/borrowings" element={page('/borrowings', <BorrowingsPage />)} />
        <Route path="/circulation/checkout" element={page('/circulation/checkout', <CheckoutPage />)} />
        <Route path="/circulation/return" element={page('/circulation/return', <ReturnPage />)} />
        <Route path="/reservations" element={page('/reservations', <ReservationsPage />)} />
        <Route path="/violations" element={page('/violations', <ViolationsPage />)} />
        <Route path="/staff" element={page('/staff', <EmployeePage />)} />
        <Route path="/employee" element={<Navigate to="/staff" replace />} />
        <Route path="/members" element={page('/members', <MemberPage />)} />
        <Route path="/members/:id" element={page('/members', <MemberPage />)} />
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
            <Suspense fallback={routeLoading('Đang tải hồ sơ')}>
              <ProfilePage />
            </Suspense>
          }
        />
        <Route
          path="/profile/change-password"
          element={
            <Suspense fallback={routeLoading()}>
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
