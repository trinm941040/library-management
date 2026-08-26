import {
  BookOpen,
  ChevronDown,
  Clock,
  LayoutDashboard,
  Layers,
  RefreshCw,
  Settings,
  Users,
  type LucideIcon,
} from 'lucide-react'
import { NavLink } from 'react-router-dom'
import { Button } from '@/common/components/ui/button'

type NavigationItem = { label: string; icon: LucideIcon; to?: string; requiredRole?: string }

const navigationAdmin: { groupName: string; items: NavigationItem[] }[] = [
  {
    groupName: 'Tổng quan',
    items: [{ label: 'Dashboard', icon: LayoutDashboard, to: '/dashboard' }],
  },
  {
    groupName: 'Quản lý người dùng',
    items: [
      { label: 'Tài khoản', icon: Users, to: '/users' },
      { label: 'Vai trò & Quyền hạn', icon: BookOpen, to: '/roles', requiredRole: 'Administrator' },
      { label: 'Nhân viên', icon: RefreshCw },
      { label: 'Đọc giả', icon: Users },
    ],
  },
  {
    groupName: 'Quản lý tác vụ',
    items: [
      { label: 'Kho sách', icon: Layers },
      { label: 'Mượn/trả', icon: RefreshCw },
      { label: 'Đặt trước', icon: Clock },
      { label: 'Vi phạm', icon: Settings },
    ],
  },
  {
    groupName: 'Quản lý hệ thống',
    items: [
      { label: 'Cấu hình', icon: Layers },
      { label: 'Nhật ký hoạt động', icon: RefreshCw },
      { label: 'Thông tin', icon: Clock },
      { label: 'Thiết lập khác', icon: Settings },
    ],
  },
]

type SidebarProps = {
  isVisible: boolean
  displayName: string
  initials: string
  roles: string[]
  onClose: () => void
}

export function Sidebar({ isVisible, displayName, initials, roles, onClose }: SidebarProps) {
  const displayRole = roles.includes('Administrator') ? 'Administrator' : (roles[0] ?? 'User')

  return (
    <>
      <aside className={`sidebar ${isVisible ? 'is-open' : 'is-hidden'}`}>
        <div className="brand">
          <span className="brand-mark">
            <BookOpen aria-hidden="true" />
          </span>
          <span>
            Northstar Library
            <br />
            <b>Portal</b>
          </span>
        </div>

        <nav aria-label="Main navigation">
          {navigationAdmin.map((group) => (
            <div className="pb-2 pt-2" key={group.groupName}>
              <p className="nav-label pd-[10px]">{group.groupName}</p>
              {group.items
                .filter(({ requiredRole }) => !requiredRole || roles.includes(requiredRole))
                .map(({ label, icon: Icon, to }) =>
                to ? (
                  <NavLink
                    to={to}
                    key={label}
                    className={({ isActive }) =>
                      `nav-item justify-start ${isActive ? 'active' : ''}`
                    }
                  >
                    <Icon aria-hidden="true" />
                    <span>{label}</span>
                  </NavLink>
                ) : (
                  <Button
                    variant="ghost"
                    className="nav-item justify-start"
                    key={label}
                    type="button"
                  >
                    <Icon aria-hidden="true" />
                    <span>{label}</span>
                  </Button>
                ),
              )}
            </div>
          ))}
        </nav>

        <div className="sidebar-bottom">
          <NavLink
            to="/settings"
            className={({ isActive }) => `nav-item sidebar-settings ${isActive ? 'active' : ''}`}
          >
            <Settings aria-hidden="true" />
            <span>Cài đặt</span>
          </NavLink>
          <Button variant="ghost" className="profile" type="button">
            <span className="avatar avatar-indigo">{initials}</span>
            <span>
              <b>{displayName}</b>
              <small>{displayRole}</small>
            </span>
            <ChevronDown aria-hidden="true" />
          </Button>
        </div>
      </aside>

      {isVisible && (
        <Button
          variant="ghost"
          size="icon"
          className="sidebar-scrim"
          aria-label="Đóng menu"
          type="button"
          onClick={onClose}
        />
      )}
    </>
  )
}
