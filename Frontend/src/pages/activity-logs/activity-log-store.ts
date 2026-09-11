export type ActivityModule =
  | 'Cấu hình'
  | 'Mượn trả'
  | 'Đặt trước'
  | 'Vi phạm'
  | 'Độc giả'
  | 'Nhân viên'
  | 'Kho sách'
  | 'Tài khoản'

export type ActivityStatus = 'Thành công' | 'Cảnh báo' | 'Thất bại'

export type ActivityLogDetail = {
  note?: string
  before?: Record<string, unknown> | null
  after?: Record<string, unknown> | null
}

export type ActivityLogItem = {
  id: string
  admin: string
  email: string
  action: string
  module: ActivityModule
  ip: string
  time: string
  status: ActivityStatus
  details?: ActivityLogDetail
}

const STORAGE_KEY = 'library_activity_logs'

const initialLogs: ActivityLogItem[] = [
  {
    id: 'log-1',
    admin: 'Nguyễn Văn A (Admin)',
    email: 'admin@example.com',
    action: 'Cập nhật quy định mượn/trả sách',
    module: 'Cấu hình',
    ip: '192.168.1.15',
    time: '09/09/2026 14:15:30',
    status: 'Thành công',
    details: {
      note: 'Thay đổi thời hạn mượn và phí phạt quá hạn theo kỳ học mới',
      before: { maxDays: 14, finePerDay: 5000, blockOverdue: true },
      after: { maxDays: 21, finePerDay: 7000, blockOverdue: true },
    },
  },
  {
    id: 'log-2',
    admin: 'Trần Thị Mai',
    email: 'mai.tran@library.local',
    action: 'Cấp thẻ độc giả mới',
    module: 'Độc giả',
    ip: '192.168.1.42',
    time: '09/09/2026 11:20:10',
    status: 'Thành công',
    details: {
      note: 'Cấp thẻ thư viện số LIB-2026-089 cho độc giả Lê Quốc Hưng',
      after: { memberCode: 'DG00128', cardNumber: 'LIB-2026-089', expiry: '09/09/2027' },
    },
  },
  {
    id: 'log-3',
    admin: 'Lê Văn Cường',
    email: 'cuong.le@library.local',
    action: 'Ghi nhận phiếu mượn sách',
    module: 'Mượn trả',
    ip: '192.168.1.55',
    time: '09/09/2026 10:05:44',
    status: 'Thành công',
    details: {
      note: 'Mượn 2 cuốn: Nhập môn Trí tuệ Nhân tạo, Clean Code',
      after: { memberCode: 'DG00095', count: 2, dueDate: '23/09/2026' },
    },
  },
  {
    id: 'log-4',
    admin: 'Phạm Minh Tuấn',
    email: 'tuan.pm@library.local',
    action: 'Thu tiền phạt trễ hạn',
    module: 'Vi phạm',
    ip: '192.168.1.80',
    time: '08/09/2026 16:40:12',
    status: 'Thành công',
    details: {
      note: 'Độc giả thanh toán tiền phạt trễ hạn 3 ngày (15.000 VNĐ)',
      before: { balance: 15000, violationType: 'Overdue' },
      after: { balance: 0, paymentMethod: 'Cash', resolved: true },
    },
  },
  {
    id: 'log-5',
    admin: 'Hệ thống (Tự động)',
    email: 'system@northstarlibrary.com',
    action: 'Tự động khóa tài khoản quá hạn',
    module: 'Cấu hình',
    ip: '127.0.0.1',
    time: '08/09/2026 00:01:00',
    status: 'Cảnh báo',
    details: {
      note: 'Áp dụng quy tắc blockOverdue: Khóa quyền mượn sách độc giả DG00041 do quá hạn 10 ngày',
      after: { memberCode: 'DG00041', restriction: 'Borrowing', reason: 'Sách quá hạn chưa trả' },
    },
  },
  {
    id: 'log-6',
    admin: 'Nguyễn Văn A (Admin)',
    email: 'admin@example.com',
    action: 'Cập nhật cấu hình máy chủ Email (SMTP)',
    module: 'Cấu hình',
    ip: '192.168.1.15',
    time: '07/09/2026 15:30:22',
    status: 'Thành công',
    details: {
      note: 'Đổi cổng SMTP và tài khoản gửi thư thông báo',
      before: { smtpHost: 'smtp.gmail.com', smtpPort: 465 },
      after: { smtpHost: 'smtp.gmail.com', smtpPort: 587 },
    },
  },
  {
    id: 'log-7',
    admin: 'Trần Thị Mai',
    email: 'mai.tran@library.local',
    action: 'Thêm mới đầu sách vào kho',
    module: 'Kho sách',
    ip: '192.168.1.42',
    time: '07/09/2026 09:12:05',
    status: 'Thành công',
    details: {
      note: 'Nhập sách mới: Giáo trình Công nghệ Phần mềm (Tái bản 2026)',
      after: { isbn: '978-604-0-12345-6', quantity: 20, category: 'Công nghệ thông tin' },
    },
  },
  {
    id: 'log-8',
    admin: 'Khách vãng lai',
    email: 'unknown@external.net',
    action: 'Đăng nhập thất bại (Sai mật khẩu 5 lần)',
    module: 'Tài khoản',
    ip: '14.232.12.11',
    time: '06/09/2026 21:18:50',
    status: 'Thất bại',
    details: {
      note: 'Cảnh báo bảo mật: Địa chỉ IP bị tạm khóa 15 phút do thử mật khẩu sai liên tục',
    },
  },
  {
    id: 'log-9',
    admin: 'Nguyễn Văn A (Admin)',
    email: 'admin@example.com',
    action: 'Cập nhật thông tin liên hệ thư viện',
    module: 'Cấu hình',
    ip: '192.168.1.15',
    time: '05/09/2026 14:00:10',
    status: 'Thành công',
    details: {
      note: 'Cập nhật giờ mở cửa ngày Thứ 7 từ 17:00 lên 20:30',
      before: { libHours: '07:30 - 17:00' },
      after: { libHours: '07:30 - 20:30 (Thứ 2 - Thứ 7)' },
    },
  },
]

export function loadActivityLogs(): ActivityLogItem[] {
  const saved = localStorage.getItem(STORAGE_KEY)
  if (!saved) {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(initialLogs))
    return initialLogs
  }
  try {
    return JSON.parse(saved) as ActivityLogItem[]
  } catch {
    return initialLogs
  }
}

export function recordActivityLog(item: Omit<ActivityLogItem, 'id' | 'time'>): ActivityLogItem {
  const currentLogs = loadActivityLogs()
  const now = new Date()
  const pad = (n: number) => n.toString().padStart(2, '0')
  const timeStr = `${pad(now.getDate())}/${pad(now.getMonth() + 1)}/${now.getFullYear()} ${pad(now.getHours())}:${pad(now.getMinutes())}:${pad(now.getSeconds())}`

  const newLog: ActivityLogItem = {
    id: 'log-' + Date.now(),
    time: timeStr,
    ...item,
  }

  const updated = [newLog, ...currentLogs]
  localStorage.setItem(STORAGE_KEY, JSON.stringify(updated))
  return newLog
}

export function clearActivityLogs(): void {
  localStorage.setItem(STORAGE_KEY, JSON.stringify([]))
}
