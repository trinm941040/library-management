import React, { useState, useEffect, useCallback } from 'react'
import type {
  NotificationItem,
  NotificationPageResult,
} from '../notifications-api'
import {
  fetchNotificationHistory,
  retryNotification,
} from '../notifications-api'
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/common/components/ui/card'
import { Button } from '@/common/components/ui/button'
import { Input } from '@/common/components/ui/input'
import { Label } from '@/common/components/ui/label'
import { Badge } from '@/common/components/ui/badge'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/common/components/ui/select'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/common/components/ui/table'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '@/common/components/ui/dialog'
import { Pagination } from '@/common/components/ui/pagination'
import {
  Mail,
  MessageSquare,
  Bell,
  RefreshCw,
  CheckCircle2,
  XCircle,
  Clock,
  Eye,
  RotateCcw,
  Calendar,
  Filter,
} from 'lucide-react'

type Props = {
  isManager: boolean
}

export const NotificationHistoryTab: React.FC<Props> = ({ isManager }) => {
  const [data, setData] = useState<NotificationPageResult>({
    items: [],
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0,
    totalPages: 0,
  })
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // Filters
  const [channel, setChannel] = useState<string>('all')
  const [status, setStatus] = useState<string>('all')
  const [fromDate, setFromDate] = useState<string>('')
  const [toDate, setToDate] = useState<string>('')
  const [pageNumber, setPageNumber] = useState(1)

  // Detail Modal
  const [viewingItem, setViewingItem] = useState<NotificationItem | null>(null)
  const [retryingId, setRetryingId] = useState<string | null>(null)
  const [retrySuccess, setRetrySuccess] = useState<string | null>(null)

  const loadData = useCallback(async (page = 1) => {
    setLoading(true)
    setError(null)
    try {
      const res = await fetchNotificationHistory({
        channel,
        status,
        fromDate: fromDate || undefined,
        toDate: toDate || undefined,
        pageNumber: page,
        pageSize: 10,
      })
      setData(res)
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Không thể tải lịch sử thông báo.')
    } finally {
      setLoading(false)
    }
  }, [channel, status, fromDate, toDate])

  useEffect(() => {
    void loadData(1)
  }, [loadData])

  const handleApplyFilter = (e: React.FormEvent) => {
    e.preventDefault()
    setPageNumber(1)
    void loadData(1)
  }

  const handleResetFilter = () => {
    setChannel('all')
    setStatus('all')
    setFromDate('')
    setToDate('')
    setPageNumber(1)
  }

  const handleRetry = async (item: NotificationItem) => {
    setRetryingId(item.id)
    setError(null)
    setRetrySuccess(null)
    try {
      const updated = await retryNotification(item.id)
      setRetrySuccess(`Đã thử gửi lại thông báo #${item.id.slice(0, 8)} thành công.`)
      setData((prev) => ({
        ...prev,
        items: prev.items.map((i) => (i.id === item.id ? updated : i)),
      }))
      if (viewingItem && viewingItem.id === item.id) {
        setViewingItem(updated)
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Không thể thử gửi lại thông báo.')
    } finally {
      setRetryingId(null)
    }
  }

  const getChannelLabel = (ch: string) => {
    switch (ch) {
      case 'Email':
        return 'Email'
      case 'Sms':
        return 'Tin nhắn SMS'
      case 'InApp':
      default:
        return 'Nội bộ'
    }
  }

  const renderChannelIcon = (ch: string) => {
    switch (ch) {
      case 'Email':
        return <Mail className="size-4 text-sky-500" />
      case 'Sms':
        return <MessageSquare className="size-4 text-emerald-500" />
      case 'InApp':
      default:
        return <Bell className="size-4 text-amber-500" />
    }
  }


  const renderStatusBadge = (st: string) => {
    switch (st) {
      case 'Sent':
        return (
          <Badge variant="default" className="gap-1 bg-emerald-600 hover:bg-emerald-700">
            <CheckCircle2 className="size-3.5" /> Thành công
          </Badge>
        )
      case 'Failed':
        return (
          <Badge variant="destructive" className="gap-1">
            <XCircle className="size-3.5" /> Thất bại
          </Badge>
        )
      case 'Pending':
      default:
        return (
          <Badge variant="secondary" className="gap-1">
            <Clock className="size-3.5" /> Chờ gửi
          </Badge>
        )
    }
  }

  return (
    <div className="space-y-6">
      {/* Search & Filter Bar */}
      <Card>
        <CardHeader className="pb-4">
          <CardTitle className="text-base font-semibold">Bộ lọc tra cứu lịch sử</CardTitle>
          <CardDescription>Lọc theo kênh gửi, trạng thái giao nhận và mốc thời gian</CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleApplyFilter} className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-5 items-end">
            <div className="space-y-1.5">
              <Label htmlFor="channelFilter">Kênh thông báo</Label>
              <Select value={channel} onValueChange={setChannel}>
                <SelectTrigger id="channelFilter" className="w-full">
                  <SelectValue placeholder="Tất cả kênh" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">Tất cả kênh</SelectItem>
                  <SelectItem value="InApp">Nội bộ</SelectItem>
                  <SelectItem value="Email">Email</SelectItem>
                  <SelectItem value="Sms">Tin nhắn SMS</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="statusFilter">Trạng thái gửi</Label>
              <Select value={status} onValueChange={setStatus}>
                <SelectTrigger id="statusFilter" className="w-full">
                  <SelectValue placeholder="Tất cả trạng thái" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">Tất cả trạng thái</SelectItem>
                  <SelectItem value="Sent">Thành công</SelectItem>
                  <SelectItem value="Failed">Thất bại</SelectItem>
                  <SelectItem value="Pending">Chờ gửi</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="fromDate">Từ ngày</Label>
              <Input
                id="fromDate"
                type="date"
                value={fromDate}
                onChange={(e) => setFromDate(e.target.value)}
              />
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="toDate">Đến ngày</Label>
              <Input
                id="toDate"
                type="date"
                value={toDate}
                onChange={(e) => setToDate(e.target.value)}
              />
            </div>

            <div className="flex gap-2">
              <Button type="submit" size="default" className="flex-1 gap-1.5">
                <Filter className="size-4" /> Lọc
              </Button>
              <Button
                type="button"
                variant="outline"
                size="default"
                onClick={handleResetFilter}
              >
                Đặt lại
              </Button>
              <Button
                type="button"
                variant="outline"
                size="default"
                onClick={() => loadData(pageNumber)}
                disabled={loading}
                title="Tải lại"
              >
                <RefreshCw className={`size-4 ${loading ? 'animate-spin' : ''}`} />
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>

      {error && (
        <div className="rounded-lg border border-destructive/20 bg-destructive/10 p-4 text-sm text-destructive">
          {error}
        </div>
      )}

      {retrySuccess && (
        <div className="rounded-lg border border-emerald-500/20 bg-emerald-500/10 p-4 text-sm text-emerald-700 dark:text-emerald-300">
          {retrySuccess}
        </div>
      )}

      {/* History Table Card */}
      <Card>
        <CardHeader className="pb-4">
          <div className="flex items-center justify-between">
            <div>
              <CardTitle className="text-base font-semibold">Danh sách nhật ký thông báo</CardTitle>
              <CardDescription>
                {data.totalCount} thông báo được ghi nhận trong hệ thống
              </CardDescription>
            </div>
          </div>
        </CardHeader>
        <CardContent className="p-0">
          <div className="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-[180px]">Thời gian</TableHead>
                  <TableHead className="w-[110px]">Kênh</TableHead>
                  <TableHead>Người nhận</TableHead>
                  <TableHead>Mẫu / Tiêu đề</TableHead>
                  <TableHead className="w-[130px]">Trạng thái gửi</TableHead>
                  <TableHead className="w-[110px]">Đọc nội bộ</TableHead>
                  <TableHead className="text-right w-[150px]">Thao tác</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {loading && data.items.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={7} className="text-center py-10 text-muted-foreground">
                      <RefreshCw className="size-5 animate-spin mx-auto mb-2 text-primary" />
                      Đang tải lịch sử thông báo...
                    </TableCell>
                  </TableRow>
                ) : data.items.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={7} className="text-center py-10 text-muted-foreground">
                      Chưa có lịch sử thông báo nào phù hợp với bộ lọc.
                    </TableCell>
                  </TableRow>
                ) : (
                  data.items.map((item) => (
                    <TableRow key={item.id}>
                      <TableCell className="whitespace-nowrap font-medium">
                        <div className="flex items-center gap-1.5">
                          <Calendar className="size-3.5 text-muted-foreground" />
                          <span className="text-xs">
                            {item.sentAtUtc
                              ? new Date(item.sentAtUtc).toLocaleString('vi-VN')
                              : item.scheduledAtUtc
                              ? new Date(item.scheduledAtUtc).toLocaleString('vi-VN')
                              : '—'}
                          </span>
                        </div>
                        <span className="text-xs text-muted-foreground font-mono">#{item.id.slice(0, 8)}</span>
                      </TableCell>
                      <TableCell className="whitespace-nowrap">
                        <div className="flex items-center gap-1.5 text-xs font-medium">
                          {renderChannelIcon(item.channel)}
                          <span>{getChannelLabel(item.channel)}</span>
                        </div>
                      </TableCell>
                      <TableCell>
                        <p className="font-medium text-sm">{item.recipientName}</p>
                        <p className="text-xs text-muted-foreground truncate max-w-[180px]">
                          {item.destination}
                        </p>
                        <Badge variant="outline" className="mt-1 text-[10px] font-normal">
                          {item.recipientType === 'Staff' ? 'Nhân viên' : 'Độc giả'}
                        </Badge>
                      </TableCell>
                      <TableCell className="max-w-xs">
                        <p className="font-semibold text-sm truncate">
                          {item.subject || item.templateName}
                        </p>
                        <p className="text-xs text-muted-foreground line-clamp-1">
                          {item.body.replace(/<[^>]*>?/gm, '')}
                        </p>
                      </TableCell>
                      <TableCell className="whitespace-nowrap">
                        {renderStatusBadge(item.status)}
                        {item.failureReason && (
                          <p className="text-xs text-destructive truncate max-w-[140px] mt-1" title={item.failureReason}>
                            {item.failureReason}
                          </p>
                        )}
                      </TableCell>
                      <TableCell className="whitespace-nowrap">
                        {item.channel === 'InApp' ? (
                          item.isRead ? (
                            <span className="text-xs font-medium text-emerald-600 dark:text-emerald-400">Đã đọc</span>
                          ) : (
                            <span className="text-xs font-medium text-amber-600 dark:text-amber-400">Chưa đọc</span>
                          )
                        ) : (
                          <span className="text-muted-foreground text-xs">N/A</span>
                        )}
                      </TableCell>
                      <TableCell className="text-right whitespace-nowrap">
                        <div className="flex items-center justify-end gap-1.5">
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => setViewingItem(item)}
                            className="gap-1"
                            title="Xem chi tiết"
                          >
                            <Eye className="size-4" />
                            <span className="hidden sm:inline">Chi tiết</span>
                          </Button>
                          {item.status === 'Failed' && isManager && (
                            <Button
                              variant="outline"
                              size="sm"
                              onClick={() => handleRetry(item)}
                              disabled={retryingId === item.id}
                              className="gap-1 text-destructive hover:bg-destructive/10"
                              title="Gửi lại thông báo bị lỗi"
                            >
                              <RotateCcw className={`size-3.5 ${retryingId === item.id ? 'animate-spin' : ''}`} />
                              <span className="hidden sm:inline">Thử lại</span>
                            </Button>
                          )}
                        </div>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </div>

          {data.totalCount > 0 && (
            <div className="flex items-center justify-between p-4 border-t">
              <span className="text-xs text-muted-foreground">
                Hiển thị trang {data.pageNumber} / {data.totalPages || 1} (Tổng cộng {data.totalCount} thông báo)
              </span>
              <Pagination
                currentPage={data.pageNumber}
                totalPages={data.totalPages || 1}
                onPageChange={(page) => {
                  setPageNumber(page)
                  void loadData(page)
                }}
              />
            </div>
          )}
        </CardContent>
      </Card>

      {/* Detail Dialog */}
      <Dialog open={Boolean(viewingItem)} onOpenChange={(open) => { if (!open) setViewingItem(null) }}>
        {viewingItem && (
          <DialogContent className="sm:max-w-2xl">
            <DialogHeader>
              <DialogTitle className="flex items-center gap-2">
                {renderChannelIcon(viewingItem.channel)}
                <span>Chi tiết thông báo #{viewingItem.id.slice(0, 8)}</span>
              </DialogTitle>
              <DialogDescription>
                Thông tin gửi và snapshot nội dung được lưu trữ bất biến
              </DialogDescription>
            </DialogHeader>

            <div className="space-y-4 text-sm">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 p-3.5 rounded-lg border bg-muted/30">
                <div>
                  <span className="text-xs text-muted-foreground block">Kênh gửi:</span>
                  <span className="font-semibold">{getChannelLabel(viewingItem.channel)}</span>
                </div>
                <div>
                  <span className="text-xs text-muted-foreground block">Trạng thái:</span>
                  <div className="mt-0.5">{renderStatusBadge(viewingItem.status)}</div>
                </div>
                <div>
                  <span className="text-xs text-muted-foreground block">Mẫu áp dụng:</span>
                  <span className="font-medium">
                    {viewingItem.templateName}
                  </span>
                </div>
                <div>
                  <span className="text-xs text-muted-foreground block">Thời gian gửi:</span>
                  <span className="font-medium">
                    {viewingItem.sentAtUtc ? new Date(viewingItem.sentAtUtc).toLocaleString('vi-VN') : '—'}
                  </span>
                </div>
                <div>
                  <span className="text-xs text-muted-foreground block">Người nhận:</span>
                  <span className="font-medium">
                    {viewingItem.recipientName} ({viewingItem.recipientType === 'Staff' ? 'Nhân viên' : 'Độc giả'})
                  </span>
                </div>
                <div>
                  <span className="text-xs text-muted-foreground block">Đích đến:</span>
                  <span className="font-mono">{viewingItem.destination}</span>
                </div>
                {viewingItem.channel === 'InApp' && (
                  <div>
                    <span className="text-xs text-muted-foreground block">Thời điểm đọc:</span>
                    <span className="font-medium">
                      {viewingItem.readAtUtc ? new Date(viewingItem.readAtUtc).toLocaleString('vi-VN') : 'Chưa đọc'}
                    </span>
                  </div>
                )}
              </div>

              {viewingItem.failureReason && (
                <div className="rounded-lg border border-destructive/20 bg-destructive/10 p-3 text-destructive text-xs">
                  <span className="font-semibold block mb-0.5">Lý do thất bại:</span>
                  {viewingItem.failureReason}
                </div>
              )}

              {viewingItem.subject && (
                <div className="space-y-1">
                  <Label className="text-xs font-semibold">Tiêu đề thông báo:</Label>
                  <div className="rounded-md border bg-muted/40 p-2.5 font-medium">
                    {viewingItem.subject}
                  </div>
                </div>
              )}

              <div className="space-y-1">
                <Label className="text-xs font-semibold">Nội dung đã gửi (Snapshot):</Label>
                <div className="rounded-md border bg-card p-3.5 text-xs font-mono whitespace-pre-wrap leading-relaxed">
                  {viewingItem.body}
                </div>
              </div>
            </div>

            <DialogFooter className="gap-2 sm:gap-0">
              {viewingItem.status === 'Failed' && isManager && (
                <Button
                  variant="outline"
                  onClick={() => handleRetry(viewingItem)}
                  disabled={retryingId === viewingItem.id}
                  className="gap-1.5 text-destructive border-destructive/30 hover:bg-destructive/10 mr-auto"
                >
                  <RotateCcw className={`size-3.5 ${retryingId === viewingItem.id ? 'animate-spin' : ''}`} />
                  Thử gửi lại
                </Button>
              )}
              <Button variant="secondary" onClick={() => setViewingItem(null)}>
                Đóng
              </Button>
            </DialogFooter>
          </DialogContent>
        )}
      </Dialog>
    </div>
  )
}

export default NotificationHistoryTab
