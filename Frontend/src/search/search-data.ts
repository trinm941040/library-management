import { loadUsers } from '@/pages/users/user-store'

export type SearchItem = {
  id: string
  title: string
  description: string
  category: string
  path: string
  keywords: string
}

const pageItems: SearchItem[] = [
  {
    id: 'dashboard',
    title: 'Dashboard - Tổng quan',
    description: 'Xem số lượt mượn, thành viên, sách quá hạn và hoạt động gần đây.',
    category: 'Trang',
    path: '/dashboard',
    keywords: 'trang chủ thống kê overview báo cáo',
  },
  {
    id: 'new-checkout',
    title: 'Tạo phiếu mượn sách',
    description: 'Thực hiện checkout và ghi nhận sách được mượn.',
    category: 'Mượn / trả',
    path: '/dashboard',
    keywords: 'mượn sách checkout lưu thông độc giả',
  },
  {
    id: 'overdue-items',
    title: 'Sách sắp đến hạn và quá hạn',
    description: 'Theo dõi các lượt mượn cần trả hoặc đã quá hạn.',
    category: 'Mượn / trả',
    path: '/dashboard',
    keywords: 'due overdue trả sách vi phạm trễ hạn',
  },
  {
    id: 'recent-activity',
    title: 'Hoạt động gần đây',
    description: 'Xem các thao tác mới nhất của nhân viên trong hệ thống.',
    category: 'Dashboard',
    path: '/dashboard',
    keywords: 'lịch sử nhật ký activity nhân viên',
  },
  {
    id: 'users',
    title: 'Quản lý tài khoản',
    description: 'Xem và tìm kiếm toàn bộ tài khoản trong hệ thống.',
    category: 'Người dùng',
    path: '/users',
    keywords: 'user người dùng thành viên nhân viên độc giả account',
  },
  {
    id: 'add-user',
    title: 'Thêm user mới',
    description: 'Tạo tài khoản mới và chọn vai trò cho người dùng.',
    category: 'Người dùng',
    path: '/users',
    keywords: 'tạo thêm user tài khoản đăng ký',
  },
  {
    id: 'edit-user',
    title: 'Chỉnh sửa thông tin user',
    description: 'Thay đổi họ tên, email hoặc vai trò của tài khoản.',
    category: 'Người dùng',
    path: '/users',
    keywords: 'sửa cập nhật edit email role vai trò',
  },
  {
    id: 'lock-user',
    title: 'Khóa hoặc mở khóa user',
    description: 'Ngăn hoặc cho phép một tài khoản tiếp tục hoạt động.',
    category: 'Người dùng',
    path: '/users',
    keywords: 'lock unlock chặn vô hiệu hóa kích hoạt',
  },
  {
    id: 'delete-user',
    title: 'Xóa user',
    description: 'Xóa một tài khoản khỏi danh sách người dùng.',
    category: 'Người dùng',
    path: '/users',
    keywords: 'delete remove xoá tài khoản',
  },
  {
    id: 'settings',
    title: 'Cài đặt sidebar',
    description: 'Cố định sidebar để menu luôn hiển thị.',
    category: 'Cài đặt',
    path: '/settings',
    keywords: 'setting cấu hình ghim pin menu thanh bên',
  },
]

export function searchLocalContent(query: string): SearchItem[] {
  const normalizedQuery = normalize(query.trim())
  if (!normalizedQuery) return []

  const userItems: SearchItem[] = loadUsers().map((user) => ({
    id: `user-${user.id}`,
    title: user.name,
    description: `${user.email} · ${user.role} · ${user.status === 'active' ? 'Đang hoạt động' : 'Đã khóa'}`,
    category: 'Tài khoản',
    path: '/users',
    keywords: `${user.email} ${user.role} ${user.status}`,
  }))

  return [...pageItems, ...userItems]
    .map((item) => ({ item, score: getSearchScore(item, normalizedQuery) }))
    .filter((result) => result.score > 0)
    .sort((first, second) => second.score - first.score)
    .slice(0, 8)
    .map((result) => result.item)
}

function getSearchScore(item: SearchItem, query: string) {
  const title = normalize(item.title)
  const content = normalize(`${item.title} ${item.description} ${item.category} ${item.keywords}`)

  if (title.includes(query)) return 100
  if (content.includes(query)) return 70

  const contentWords = content.split(/[^a-z0-9]+/).filter(Boolean)
  const queryWords = query.split(/[^a-z0-9]+/).filter(Boolean)
  let score = 0

  for (const queryWord of queryWords) {
    const wordScore = Math.max(...contentWords.map((word) => compareWords(queryWord, word)))
    if (wordScore === 0) return 0
    score += wordScore
  }

  return score
}

function compareWords(query: string, word: string) {
  if (word === query) return 30
  if (word.startsWith(query)) return 20
  if (word.includes(query)) return 15

  const allowedDistance = query.length >= 6 ? 2 : query.length >= 4 ? 1 : 0
  if (allowedDistance === 0) return 0

  const distance = levenshteinDistance(query, word)
  return distance <= allowedDistance ? 10 - distance : 0
}

function normalize(value: string) {
  return value
    .normalize('NFD')
    .replace(/\p{Diacritic}/gu, '')
    .replace(/đ/g, 'd')
    .toLowerCase()
}

function levenshteinDistance(first: string, second: string) {
  const previous = Array.from({ length: second.length + 1 }, (_, index) => index)

  for (let firstIndex = 1; firstIndex <= first.length; firstIndex += 1) {
    let diagonal = previous[0]
    previous[0] = firstIndex

    for (let secondIndex = 1; secondIndex <= second.length; secondIndex += 1) {
      const oldValue = previous[secondIndex]
      const cost = first[firstIndex - 1] === second[secondIndex - 1] ? 0 : 1
      previous[secondIndex] = Math.min(
        previous[secondIndex] + 1,
        previous[secondIndex - 1] + 1,
        diagonal + cost,
      )
      diagonal = oldValue
    }
  }

  return previous[second.length]
}
