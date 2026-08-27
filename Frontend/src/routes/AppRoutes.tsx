import { Navigate, Route, Routes } from 'react-router-dom'
import { useAuth } from '../auth/AuthProvider'
import { AppLayout } from '../layouts/AppLayout'
import { BooksPage } from '../pages/books/BooksPage'
import { BorrowingsPage } from '../pages/borrowings/BorrowingsPage'
import { DashboardPage } from '../pages/dashboard/DashboardPage'
import { LoginPage } from '../pages/login/LoginPage'
import { ReservationsPage } from '../pages/reservations/ReservationsPage'
import { SettingsPage } from '../pages/settings/SettingsPage'
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
        <Route path="/books" element={<BooksPage />} />
        <Route path="/borrowings" element={<BorrowingsPage />} />
        <Route path="/reservations" element={<ReservationsPage />} />
        <Route path="/settings" element={<SettingsPage />} />
      </Route>
      <Route path="*" element={<Navigate to="/dashboard" replace />} />
    </Routes>
  )
}
