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

  const displayName = user?.displayName ?? 'User'
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
    await logout()
    navigate('/login', { replace: true })
  }

  return (
    <div className="app-shell">
      <Sidebar
        isVisible={sidebarVisible}
        displayName={displayName}
        initials={initials}
        roles={user?.roles ?? []}
        onClose={() => {
          if (!sidebarPinned) setSidebarVisible(false)
        }}
      />

      <main className="main-content">
        <Header
          displayName={displayName}
          initials={initials}
          sidebarVisible={sidebarVisible}
          sidebarPinned={sidebarPinned}
          onToggleSidebar={() => {
            if (!sidebarPinned) setSidebarVisible((visible) => !visible)
          }}
          onLogout={handleLogout}
        />
        <Outlet />
      </main>
    </div>
  )
}
