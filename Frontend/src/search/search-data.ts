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
    title: 'Bảng điều khiển - Tổng quan',
    description: 'Xem số lượt mượn, thành viên, sách quá hạn và hoạt động gần đây.',
    category: 'Trang',
    path: '/dashboard',
    keywords: 'trang chủ thống kê overview báo cáo',
  },
  {
    id: 'new-checkout',
    title: 'Tạo phiếu mượn sách',
    description: 'Tạo phiếu và ghi nhận sách được mượn.',
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
    category: 'Bảng điều khiển',
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
    id: 'employees',
    title: 'Quản lý nhân viên',
    description: 'Quản lý hồ sơ, việc làm và tài khoản truy cập của nhân viên.',
    category: 'Nhân sự',
    path: '/employee',
    keywords: 'nhân viên employee hồ sơ chức vụ chi nhánh tài khoản nhân sự',
  },
  {
    id: 'members',
    title: 'Quản lý độc giả',
    description: 'Quản lý hồ sơ, thẻ, hạn chế, lịch sử và tiền phạt của độc giả.',
    category: 'Thành viên',
    path: '/members',
    keywords: 'độc giả thành viên member thẻ thư viện hạn chế tiền phạt',
  },
  {
    id: 'add-employee',
    title: 'Tạo hồ sơ nhân viên',
    description: 'Tạo hồ sơ nhân sự mới và ghi nhận thông tin công tác.',
    category: 'Nhân sự',
    path: '/employee',
    keywords: 'thêm tạo nhân viên employee hồ sơ nhân sự',
  },
  {
    id: 'add-user',
    title: 'Thêm người dùng mới',
    description: 'Tạo tài khoản mới với vai trò người dùng mặc định.',
    category: 'Người dùng',
    path: '/users',
    keywords: 'tạo thêm user tài khoản đăng ký',
  },
  {
    id: 'edit-user',
    title: 'Chỉnh sửa thông tin người dùng',
    description: 'Thay đổi họ tên hoặc email của tài khoản.',
    category: 'Người dùng',
    path: '/users',
    keywords: 'sửa cập nhật edit email role vai trò',
  },
  {
    id: 'deactivate-user',
    title: 'Vô hiệu hóa người dùng',
    description: 'Ngăn tài khoản đăng nhập và thu hồi các phiên hiện tại.',
    category: 'Người dùng',
    path: '/users',
    keywords: 'lock chặn vô hiệu hóa deactivate tài khoản',
  },
  {
    id: 'books',
    title: 'Kho sách',
    description: 'Xem, thêm, sửa và xóa đầu sách trong kho thư viện.',
    category: 'Tác vụ',
    path: '/books',
    keywords: 'sách kho sách isbn tác giả thể loại books inventory',
  },
  {
    id: 'add-book',
    title: 'Thêm sách mới',
    description: 'Nhập thông tin sách để thêm vào kho.',
    category: 'Tác vụ',
    path: '/books',
    keywords: 'thêm sách create book isbn',
  },
  {
    id: 'borrowings',
    title: 'Mượn/trả sách',
    description: 'Tạo phiếu mượn, theo dõi hạn trả và ghi nhận trả sách.',
    category: 'Tác vụ',
    path: '/borrowings',
    keywords: 'mượn trả checkout return phiếu mượn overdue',
  },
  {
    id: 'reservations',
    title: 'Đặt trước sách',
    description: 'Giữ chỗ sách và chuyển thành phiếu mượn khi sách có sẵn.',
    category: 'Tác vụ',
    path: '/reservations',
    keywords: 'đặt trước reservation hold giữ chỗ nhận sách',
  },
  {
    id: 'violations',
    title: 'Vi phạm',
    description: 'Ghi nhận phạt trễ hạn, hư hỏng hoặc mất sách.',
    category: 'Tác vụ',
    path: '/violations',
    keywords: 'vi phạm phạt trễ hạn hư hỏng mất sách fine overdue',
  },
  {
    id: 'settings',
    title: 'Cài đặt thanh bên',
    description: 'Cố định thanh bên để trình đơn luôn hiển thị.',
    category: 'Cài đặt',
    path: '/settings',
    keywords: 'setting cấu hình ghim pin menu thanh bên',
  },
]

export function searchLocalContent(query: string): SearchItem[] {
  const normalizedQuery = normalize(query.trim())
  if (!normalizedQuery) return []

  return pageItems
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
