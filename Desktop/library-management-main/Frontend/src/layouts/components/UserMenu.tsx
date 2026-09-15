import { ChevronDown, KeyRound, LogOut, UserRound } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { DropdownMenu } from 'radix-ui'
import type { User } from '@/auth/auth-api'

type Props = { user: User; initials: string; isLoggingOut: boolean; onLogout: () => void }

export function UserMenu({ user, initials, isLoggingOut, onLogout }: Props) {
  const navigate = useNavigate()
  return (
    <DropdownMenu.Root>
      <DropdownMenu.Trigger asChild>
        <button type="button" className="user-menu-trigger" aria-label={`Mở menu tài khoản của ${user.displayName}`}>
          <span className="top-avatar">{initials}</span>
          <span className="user-menu-trigger-copy"><span className="top-name">{user.displayName}</span><small>Tài khoản</small></span>
          <ChevronDown className="user-menu-chevron" aria-hidden="true" />
        </button>
      </DropdownMenu.Trigger>
      <DropdownMenu.Portal>
        <DropdownMenu.Content className="user-menu-content" side="bottom" sideOffset={10} align="end" collisionPadding={12}>
          <div className="user-menu-summary">
            <span className="top-avatar">{initials}</span>
            <span><strong>{user.displayName}</strong><small>{user.loginIdentifier}</small></span>
          </div>
          <DropdownMenu.Separator className="user-menu-separator" />
          <DropdownMenu.Item className="user-menu-item" onSelect={() => navigate('/profile')}><span className="user-menu-item-icon"><UserRound /></span><span>Thông tin cá nhân<small>Xem và cập nhật hồ sơ</small></span></DropdownMenu.Item>
          <DropdownMenu.Item className="user-menu-item" onSelect={() => navigate('/profile/change-password')}><span className="user-menu-item-icon"><KeyRound /></span><span>Đổi mật khẩu<small>Bảo mật tài khoản</small></span></DropdownMenu.Item>
          <DropdownMenu.Separator className="user-menu-separator" />
          <DropdownMenu.Item className="user-menu-item danger" disabled={isLoggingOut} onSelect={onLogout}>
            <span className="user-menu-item-icon"><LogOut /></span><span>{isLoggingOut ? 'Đang đăng xuất...' : 'Đăng xuất'}<small>Kết thúc phiên hiện tại</small></span>
          </DropdownMenu.Item>
        </DropdownMenu.Content>
      </DropdownMenu.Portal>
    </DropdownMenu.Root>
  )
}
