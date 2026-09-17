import {
  AlertTriangle,
  Barcode,
  Bell,
  Clock,
  CreditCard,
  FileSpreadsheet,
  KeyRound,
  LayoutDashboard,
  Layers,
  RefreshCw,
  RotateCcw,
  Settings,
  ShieldCheck,
  UserRoundCog,
  Users,
  type LucideIcon,
} from 'lucide-react'

export type NavigationItem = {
  path: string
  label: string
  icon: LucideIcon
  requiredPermissions: readonly string[]
}
export type NavigationGroup = { label: string; items: readonly NavigationItem[] }
export const navigationGroups: readonly NavigationGroup[] = [
  {
    label: 'Tổng quan',
    items: [
      {
        path: '/dashboard',
        label: 'Bảng điều khiển',
        icon: LayoutDashboard,
        requiredPermissions: [],
      },
    ],
  },
  {
    label: 'Quản lý người dùng',
    items: [
      {
        path: '/access-accounts',
        label: 'Tài khoản truy cập',
        icon: Users,
        requiredPermissions: ['users.read'],
      },
      { path: '/roles', label: 'Vai trò', icon: ShieldCheck, requiredPermissions: ['roles.read'] },
      {
        path: '/permissions',
        label: 'Quyền hạn',
        icon: KeyRound,
        requiredPermissions: ['permissions.read'],
      },
      {
        path: '/staff',
        label: 'Hồ sơ nhân viên',
        icon: UserRoundCog,
        requiredPermissions: ['employees.read'],
      },
      { path: '/members', label: 'Độc giả', icon: Users, requiredPermissions: ['members.read'] },
    ],
  },
  {
    label: 'Quản lý tác vụ',
    items: [
      { path: '/books', label: 'Kho sách', icon: Layers, requiredPermissions: ['books.read'] },
      {
        path: '/circulation/checkout',
        label: 'Lập phiếu mượn',
        icon: Barcode,
        requiredPermissions: ['borrowings.create'],
      },
      {
        path: '/circulation/return',
        label: 'Trả sách & Xử lý',
        icon: RotateCcw,
        requiredPermissions: ['borrowings.return'],
      },
      {
        path: '/borrowings',
        label: 'Mượn/trả',
        icon: RefreshCw,
        requiredPermissions: ['borrowings.read'],
      },
      {
        path: '/reservations',
        label: 'Đặt trước',
        icon: Clock,
        requiredPermissions: ['reservations.read'],
      },
      {
        path: '/violations',
        label: 'Vi phạm',
        icon: AlertTriangle,
        requiredPermissions: ['violations.read'],
      },
      {
        path: '/payments',
        label: 'Thu tiền phạt',
        icon: CreditCard,
        requiredPermissions: ['violations.read'],
      },
      {
        path: '/reports',
        label: 'Báo cáo & Xuất tệp',
        icon: FileSpreadsheet,
        requiredPermissions: [],
      },
      {
        path: '/notifications',
        label: 'Thông báo',
        icon: Bell,
        requiredPermissions: [],
      },
    ],
  },
  {
    label: 'Quản lý hệ thống',
    items: [
      {
        path: '/system/config',
        label: 'Cấu hình',
        icon: Layers,
        requiredPermissions: ['circulation-policies.read'],
      },
      {
        path: '/audit-log',
        label: 'Nhật ký kiểm toán',
        icon: RefreshCw,
        requiredPermissions: ['audit-logs.read'],
      },
      { path: '/system/info', label: 'Thông tin', icon: Clock, requiredPermissions: [] },
      {
        path: '/configuration',
        label: 'Gói cấu hình',
        icon: Settings,
        requiredPermissions: ['settings.read'],
      },
    ],
  },
]
export const settingsNavigationItem: NavigationItem = {
  path: '/settings',
  label: 'Cài đặt',
  icon: Settings,
  requiredPermissions: ['settings.read'],
}
export const routePermissions = new Map([
  ...navigationGroups.flatMap((group) =>
    group.items.map((item) => [item.path, item.requiredPermissions] as const),
  ),
  [settingsNavigationItem.path, settingsNavigationItem.requiredPermissions] as const,
  ['/loans/:id', ['borrowings.read']] as const,
  ['/borrowings/:id', ['borrowings.read']] as const,
])

