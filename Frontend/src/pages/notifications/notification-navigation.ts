const routeRules: ReadonlyArray<{ prefix: string; permission?: string }> = [
  { prefix: '/dashboard' },
  { prefix: '/notifications' },
  { prefix: '/borrowings', permission: 'borrowings.read' },
  { prefix: '/loans', permission: 'borrowings.read' },
  { prefix: '/reservations', permission: 'reservations.read' },
  { prefix: '/violations', permission: 'violations.read' },
  { prefix: '/members', permission: 'members.read' },
  { prefix: '/catalog', permission: 'books.read' },
  { prefix: '/copies', permission: 'copies.read' },
  { prefix: '/stock-receipts', permission: 'stock-receipts.read' },
  { prefix: '/inventory-audits', permission: 'inventory-audits.read' },
  { prefix: '/staff', permission: 'employees.read' },
  { prefix: '/access-accounts', permission: 'users.read' },
]

export function resolveNotificationLink(
  deepLink: string | null | undefined,
  permissions: readonly string[],
): { allowed: boolean; path?: string; reason?: string } {
  if (!deepLink) return { allowed: false }
  if (!deepLink.startsWith('/') || deepLink.startsWith('//'))
    return { allowed: false, reason: 'Liên kết trong thông báo không hợp lệ.' }

  const pathname = deepLink.split(/[?#]/, 1)[0]
  const rule = routeRules.find(
    ({ prefix }) => pathname === prefix || pathname.startsWith(`${prefix}/`),
  )
  if (!rule) return { allowed: false, reason: 'Liên kết trong thông báo không được hỗ trợ.' }
  if (rule.permission && !permissions.includes(rule.permission))
    return { allowed: false, reason: 'Bạn không có quyền mở nội dung được liên kết.' }
  return { allowed: true, path: deepLink }
}

export const stripNotificationHtml = (value: string) => value.replace(/<[^>]*>?/gm, '')

export const formatNotificationTime = (value?: string | null) =>
  value ? new Date(value).toLocaleString('vi-VN') : 'Không xác định'
