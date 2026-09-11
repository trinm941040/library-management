import { Bell, ChevronDown, LogOut, Menu } from 'lucide-react'
import { Button } from '@/common/components/ui/button'
import { GlobalSearch } from '@/search/GlobalSearch'

type HeaderProps = {
  displayName: string
  initials: string
  sidebarVisible: boolean
  sidebarPinned: boolean
  onToggleSidebar: () => void
  onLogout: () => void
}

export function Header({
  displayName,
  initials,
  sidebarVisible,
  sidebarPinned,
  onToggleSidebar,
  onLogout,
}: HeaderProps) {
  return (
    <header className="topbar">
      <Button
        variant="ghost"
        size="icon"
        className="mobile-menu"
        aria-label={
          sidebarPinned
            ? 'Sidebar đang được cố định'
            : sidebarVisible
              ? 'Ẩn sidebar'
              : 'Hiện sidebar'
        }
        aria-expanded={sidebarVisible}
        disabled={sidebarPinned}
        title={sidebarPinned ? 'Tắt cố định sidebar trong trang Cài đặt để có thể ẩn' : undefined}
        type="button"
        onClick={onToggleSidebar}
      >
        <Menu />
      </Button>

      <GlobalSearch />

      <div className="top-actions mr-12">
        <Button
          variant="ghost"
          size="icon"
          className="icon-button"
          type="button"
          aria-label="Xem thông báo"
        >
          <Bell />
          <i />
        </Button>
        <Button
          variant="ghost"
          size="icon"
          className="icon-button"
          type="button"
          aria-label="Đăng xuất"
          onClick={onLogout}
        >
          <LogOut />
        </Button>
        <span className="top-avatar">{initials}</span>
        <span className="top-name">{displayName}</span>
        <ChevronDown className="top-chevron" aria-hidden="true" />
      </div>
    </header>
  )
}
