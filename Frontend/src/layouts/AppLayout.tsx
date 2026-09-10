import { useEffect, useState } from 'react'
import { Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '@/auth/AuthProvider'
import { useSettings } from '@/settings/SettingsProvider'
import { Header } from './components/Header'
import { Sidebar } from './components/Sidebar'
import '@/styles/app-shell.css'

export function AppLayout() {
  const navigate = useNavigate()
  const { user, logout } = useAuth()
  const { sidebarPinned } = useSettings()
  const [sidebarVisible, setSidebarVisible] = useState(true)
  const [isLoggingOut, setIsLoggingOut] = useState(false)

  const displayName = user?.displayName ?? 'Người dùng'
  const initials = displayName
    .split(' ')
    .map((part) => part[0])
    .join('')
    .slice(0, 2)
    .toUpperCase()

  useEffect(() => {
    if (sidebarPinned) setSidebarVisible(true)
  }, [sidebarPinned])

  const handleLogout = async () => {
    if (isLoggingOut) return
    setIsLoggingOut(true)
    try {
      await logout()
      navigate('/login', { replace: true })
    } catch {
      navigate('/login', {
        replace: true,
        state: { notice: 'Đã xóa phiên trên thiết bị. Máy chủ chưa xác nhận đăng xuất do lỗi kết nối.' },
      })
    }
  }

  return (
    <div className="app-shell">
      <Sidebar
        isVisible={sidebarVisible}
        permissions={user?.permissions ?? []}
        onClose={() => {
          if (!sidebarPinned) setSidebarVisible(false)
        }}
      />

      <main className="main-content">
        {user ? <Header
          user={user}
          initials={initials}
          sidebarVisible={sidebarVisible}
          sidebarPinned={sidebarPinned}
          onToggleSidebar={() => {
            if (!sidebarPinned) setSidebarVisible((visible) => !visible)
          }}
          onLogout={handleLogout}
          isLoggingOut={isLoggingOut}
        /> : null}
        <Outlet />
      </main>
    </div>
  )
}
