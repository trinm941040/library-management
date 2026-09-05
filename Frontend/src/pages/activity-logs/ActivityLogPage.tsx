import { RefreshCw, Search, Download } from 'lucide-react'
import { Button } from '@/common/components/ui/button'
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/common/components/ui/card'
import { Input } from '@/common/components/ui/input'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/common/components/ui/table'
import { Badge } from '@/common/components/ui/badge'
import { useState, useMemo } from 'react'

const mockLogs = [
  {
    id: 1,
    admin: 'Nguyễn Văn A',
    email: 'admin_a@example.com',
    action: 'Cập nhật cấu hình hệ thống',
    ip: '192.168.1.45',
    time: '29/08/2026 10:30',
    status: 'Thành công',
  },
  {
    id: 2,
    admin: 'Trần Thị B',
    email: 'admin_b@example.com',
    action: 'Xóa người dùng',
    ip: '192.168.1.102',
    time: '29/08/2026 09:15',
    status: 'Thành công',
  },
  {
    id: 3,
    admin: 'Lê Văn C',
    email: 'admin_c@example.com',
    action: 'Đăng nhập sai mật khẩu',
    ip: '14.232.12.11',
    time: '28/08/2026 15:45',
    status: 'Thất bại',
  },
]

export function ActivityLogPage() {
  const [searchQuery, setSearchQuery] = useState('')
  const [isRefreshing, setIsRefreshing] = useState(false)
  const [isExporting, setIsExporting] = useState(false)

  // Filter logs based on search query
  const filteredLogs = useMemo(() => {
    return mockLogs.filter(
      (log) =>
        log.email.toLowerCase().includes(searchQuery.toLowerCase()) ||
        log.action.toLowerCase().includes(searchQuery.toLowerCase()) ||
        log.admin.toLowerCase().includes(searchQuery.toLowerCase())
    )
  }, [searchQuery])

  const handleRefresh = () => {
    setIsRefreshing(true)
    setTimeout(() => {
      setIsRefreshing(false)
    }, 600)
  }

  const handleExport = () => {
    setIsExporting(true)
    setTimeout(() => {
      setIsExporting(false)
      
      // Tạo nội dung file CSV (dùng dấu chấm phẩy để Excel VN tự chia cột)
      const headers = ['Quản trị viên', 'Email', 'Hành động', 'Địa chỉ IP', 'Thời gian', 'Trạng thái']
      const csvContent = [
        headers.join(';'),
        ...filteredLogs.map(log => 
          `"${log.admin}";"${log.email}";"${log.action}";"${log.ip}";"${log.time}";"${log.status}"`
        )
      ].join('\n')

      // Dùng BOM để Excel hiển thị đúng tiếng Việt
      const blob = new Blob(['\uFEFF' + csvContent], { type: 'text/csv;charset=utf-8;' })
      const link = document.createElement('a')
      const url = URL.createObjectURL(blob)
      link.setAttribute('href', url)
      link.setAttribute('download', 'nhat_ky_hoat_dong.csv')
      link.style.visibility = 'hidden'
      document.body.appendChild(link)
      link.click()
      document.body.removeChild(link)
    }, 1000)
  }

  return (
    <div className="mx-auto w-full max-w-6xl px-5 py-10 md:px-12">
      {/* Header section matching the style in the images */}
      <div className="mb-7 flex items-start justify-between">
        <div>
          <p className="mb-2 text-xs font-bold uppercase tracking-widest text-primary">
            Quản lý hệ thống
          </p>
          <h1 className="text-3xl font-bold tracking-tight">Nhật ký hoạt động</h1>
          <p className="mt-2 text-sm text-muted-foreground">
            Theo dõi lịch sử thao tác của quản trị viên và người dùng trên hệ thống.
          </p>
        </div>
        <div className="flex gap-3">
          <Button variant="outline" onClick={handleRefresh} disabled={isRefreshing || isExporting}>
            <RefreshCw className={`mr-2 h-4 w-4 ${isRefreshing ? 'animate-spin' : ''}`} /> 
            {isRefreshing ? 'Đang tải...' : 'Làm mới'}
          </Button>
          <Button onClick={handleExport} disabled={isExporting || isRefreshing}>
            <Download className={`mr-2 h-4 w-4 ${isExporting ? 'animate-bounce' : ''}`} /> 
            {isExporting ? 'Đang xuất...' : 'Xuất báo cáo'}
          </Button>
        </div>
      </div>

      <div className="grid gap-5">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between">
            <div>
              <CardTitle>Danh sách lịch sử</CardTitle>
              <CardDescription>
                {filteredLogs.length} bản ghi phù hợp với bộ lọc.
              </CardDescription>
            </div>
            <div className="relative w-72">
              <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
              <Input
                type="search"
                placeholder="Tìm theo email hoặc hành động..."
                className="pl-8"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
              />
            </div>
          </CardHeader>
          <CardContent>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Quản trị viên</TableHead>
                  <TableHead>Hành động</TableHead>
                  <TableHead>Địa chỉ IP</TableHead>
                  <TableHead>Thời gian</TableHead>
                  <TableHead>Trạng thái</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {filteredLogs.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={5} className="h-24 text-center">
                      Không tìm thấy kết quả nào.
                    </TableCell>
                  </TableRow>
                ) : (
                  filteredLogs.map((log) => (
                    <TableRow key={log.id}>
                      <TableCell>
                        <p className="font-medium">{log.admin}</p>
                        <p className="text-xs text-muted-foreground">{log.email}</p>
                      </TableCell>
                      <TableCell>{log.action}</TableCell>
                      <TableCell>{log.ip}</TableCell>
                      <TableCell>{log.time}</TableCell>
                      <TableCell>
                        <Badge variant={log.status === 'Thành công' ? 'default' : 'destructive'}>
                          {log.status}
                        </Badge>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
