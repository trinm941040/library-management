import { Menu } from 'lucide-react'
import type { User } from '@/auth/auth-api'
import { Button } from '@/common/components/ui/button'
import { GlobalSearch } from '@/search/GlobalSearch'
import { UserMenu } from './UserMenu'
import { NotificationBellDropdown } from './NotificationBellDropdown'
import { ThemeToggle } from '@/common/components/ThemeToggle'

type HeaderProps = {
  user: User
  initials: string
  sidebarVisible: boolean
  sidebarPinned: boolean
  onToggleSidebar: () => void
  onLogout: () => void
  isLoggingOut: boolean
}

export function Header({
  user,
  initials,
  sidebarVisible,
  sidebarPinned,
  onToggleSidebar,
  onLogout,
  isLoggingOut,
}: HeaderProps) {
  return (
    <header className="topbar">
      <Button
        variant="ghost"
        size="icon"
        className="mobile-menu"
        aria-label={
          sidebarPinned
            ? 'Thanh bên đang được cố định'
            : sidebarVisible
              ? 'Ẩn thanh bên'
              : 'Hiện thanh bên'
        }
        aria-expanded={sidebarVisible}
        disabled={sidebarPinned}
        title={sidebarPinned ? 'Tắt cố định thanh bên trong trang Cài đặt để có thể ẩn' : undefined}
        type="button"
        onClick={onToggleSidebar}
      >
        <Menu />
      </Button>

      <div className="header-global-search">
        <GlobalSearch />
      </div>

      <div className="top-actions">
        <ThemeToggle className="header-theme-toggle" />
        <NotificationBellDropdown />
        <UserMenu user={user} initials={initials} isLoggingOut={isLoggingOut} onLogout={onLogout} />
      </div>
    </header>
  )
}
