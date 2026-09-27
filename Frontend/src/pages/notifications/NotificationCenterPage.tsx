import { useMemo, useState } from 'react'
import { Bell, CheckCheck, ExternalLink, Eye } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '@/auth/AuthProvider'
import { PageShell, ScreenState, StatusBadge, useToast } from '@/common/components'
import { Button } from '@/common/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/common/components/ui/card'
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '@/common/components/ui/dialog'
import { Input } from '@/common/components/ui/input'
import { Label } from '@/common/components/ui/label'
import { Pagination } from '@/common/components/ui/pagination'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/common/components/ui/select'
import type { NotificationItem, NotificationPageResult, NotificationSeverity } from './notifications-api'
import { useMarkAllNotificationsRead, useMarkNotificationRead, useMyNotifications } from './notification-queries'
import { formatNotificationTime, resolveNotificationLink, stripNotificationHtml } from './notification-navigation'
import { readUrlFilter, readUrlPage, useFilterUrlSync } from '@/shared/data/use-filter-url-sync'

const severityLabel: Record<NotificationSeverity, string> = {
  Info: 'Thông tin', Success: 'Thành công', Warning: 'Cảnh báo', Error: 'Quan trọng',
}

const emptyPage: NotificationPageResult = { items: [], pageNumber: 1, pageSize: 10, totalCount: 0, totalPages: 0 }

export function NotificationCenterPage() {
  const { user } = useAuth()
  const navigate = useNavigate()
  const { showToast } = useToast()
  const [unreadOnly, setUnreadOnly] = useState(() => readUrlFilter('state') === 'unread')
  const [severity, setSeverity] = useState<NotificationSeverity | 'all'>(() => readUrlFilter('severity', 'all') as NotificationSeverity | 'all')
  const [fromDate, setFromDate] = useState(() => readUrlFilter('fromDate'))
  const [toDate, setToDate] = useState(() => readUrlFilter('toDate'))
  const [pageNumber, setPageNumber] = useState(() => readUrlPage('pageNumber', 1))
  const [pageSize, setPageSize] = useState(() => readUrlPage('pageSize', 10))
  const [selected, setSelected] = useState<NotificationItem | null>(null)
  useFilterUrlSync({ state: unreadOnly ? 'unread' : undefined, severity, fromDate, toDate, pageNumber, pageSize })
  const params = useMemo(() => ({
    unreadOnly,
    severity,
    fromDate: fromDate || undefined,
    toDate: toDate ? `${toDate}T23:59:59.999Z` : undefined,
    pageNumber,
    pageSize,
  }), [fromDate, pageNumber, pageSize, severity, toDate, unreadOnly])
  const notificationsQuery = useMyNotifications(params)
  const markReadMutation = useMarkNotificationRead()
  const markAllMutation = useMarkAllNotificationsRead()
  const data = notificationsQuery.data ?? emptyPage
  const mutating = markReadMutation.isPending || markAllMutation.isPending

  const markRead = async (item: NotificationItem) => {
    if (item.isRead) return
    try {
      await markReadMutation.mutateAsync(item.id)
      const readAtUtc = new Date().toISOString()
      setSelected((current) => current?.id === item.id ? { ...current, isRead: true, readAtUtc } : current)
    } catch (caught) {
      showToast(caught instanceof Error ? caught.message : 'Không thể đánh dấu đã đọc.', 'error')
    }
  }

  const openDetail = (item: NotificationItem) => {
    setSelected(item)
    void markRead(item)
  }

  const markAll = async () => {
    if (mutating) return
    try {
      await markAllMutation.mutateAsync()
      showToast('Đã đánh dấu tất cả thông báo là đã đọc.', 'success')
    } catch (caught) {
      showToast(caught instanceof Error ? caught.message : 'Không thể cập nhật thông báo.', 'error')
    }
  }

  const openLinkedContent = (item: NotificationItem) => {
    const result = resolveNotificationLink(item.deepLink, user?.permissions ?? [])
    if (!result.allowed || !result.path) {
      showToast(result.reason ?? 'Thông báo này không có liên kết.', 'error')
      return
    }
    setSelected(null)
    navigate(result.path)
  }

  return (
    <PageShell eyebrow="Thông báo" title="Trung tâm thông báo" description="Theo dõi các cập nhật dành riêng cho tài khoản của bạn."
      actions={<Button className="w-full sm:w-auto" onClick={markAll} disabled={mutating || !data.items.some((item) => !item.isRead)}><CheckCheck />Đọc tất cả</Button>}>
      <Card>
        <CardHeader><CardTitle className="flex items-center gap-2"><Bell className="size-5" />Thông báo của tôi</CardTitle></CardHeader>
        <CardContent className="space-y-4">
          <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-4">
            <div><Label htmlFor="notification-state">Trạng thái</Label><Select value={unreadOnly ? 'unread' : 'all'} onValueChange={(value) => { setUnreadOnly(value === 'unread'); setPageNumber(1) }}><SelectTrigger id="notification-state"><SelectValue /></SelectTrigger><SelectContent><SelectItem value="all">Tất cả</SelectItem><SelectItem value="unread">Chưa đọc</SelectItem></SelectContent></Select></div>
            <div><Label htmlFor="notification-severity">Mức độ</Label><Select value={severity} onValueChange={(value) => { setSeverity(value as NotificationSeverity | 'all'); setPageNumber(1) }}><SelectTrigger id="notification-severity"><SelectValue /></SelectTrigger><SelectContent><SelectItem value="all">Tất cả</SelectItem>{Object.entries(severityLabel).map(([value, label]) => <SelectItem key={value} value={value}>{label}</SelectItem>)}</SelectContent></Select></div>
            <div><Label htmlFor="notification-from">Từ ngày</Label><Input id="notification-from" type="date" value={fromDate} onInput={(event) => { setFromDate(event.currentTarget.value); setPageNumber(1) }} onChange={(event) => { setFromDate(event.target.value); setPageNumber(1) }} /></div>
            <div><Label htmlFor="notification-to">Đến ngày</Label><Input id="notification-to" type="date" min={fromDate || undefined} value={toDate} onInput={(event) => { setToDate(event.currentTarget.value); setPageNumber(1) }} onChange={(event) => { setToDate(event.target.value); setPageNumber(1) }} /></div>
          </div>

          {notificationsQuery.isLoading ? <ScreenState kind="loading" title="Đang tải thông báo" /> : notificationsQuery.isError ? <ScreenState kind="error" title="Không thể tải thông báo" description={notificationsQuery.error instanceof Error ? notificationsQuery.error.message : 'Không thể tải thông báo.'} actionLabel="Thử lại" onAction={() => void notificationsQuery.refetch()} /> : data.items.length === 0 ? <ScreenState kind="empty" title="Không có thông báo" description="Không tìm thấy thông báo phù hợp với bộ lọc." /> : (
            <div className="divide-y rounded-lg border">
              {data.items.map((item) => <button key={item.id} type="button" onClick={() => openDetail(item)} className={`flex w-full items-start gap-3 p-4 text-left transition-colors hover:bg-muted/60 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring ${item.isRead ? '' : 'bg-primary/5'}`}>
                <span className={`mt-2 size-2 shrink-0 rounded-full ${item.isRead ? 'bg-muted-foreground/30' : 'bg-primary'}`} aria-label={item.isRead ? 'Đã đọc' : 'Chưa đọc'} />
                <span className="min-w-0 flex-1"><span className="flex flex-wrap items-center justify-between gap-2"><strong className="truncate">{item.subject || item.templateName}</strong><StatusBadge tone={item.severity === 'Error' ? 'danger' : item.severity === 'Warning' ? 'warning' : item.severity === 'Success' ? 'success' : 'info'} label={severityLabel[item.severity]} /></span><span className="mt-1 line-clamp-2 block text-sm text-muted-foreground">{stripNotificationHtml(item.body)}</span><span className="mt-2 block text-xs text-muted-foreground">{formatNotificationTime(item.createdAtUtc || item.sentAtUtc)}</span></span><Eye className="mt-1 size-4 shrink-0" aria-hidden="true" />
              </button>)}
            </div>
          )}
          {data.totalCount > 0 ? <div className="border-t pt-4"><p className="mb-3 text-sm text-muted-foreground">Tổng cộng {data.totalCount} thông báo</p><Pagination currentPage={data.pageNumber} totalPages={data.totalPages || 1} totalCount={data.totalCount} itemCount={data.items.length} loading={notificationsQuery.isFetching} pageSize={pageSize} onPageChange={setPageNumber} onPageSizeChange={(value) => { setPageSize(value); setPageNumber(1) }} /></div> : null}
        </CardContent>
      </Card>

      <Dialog open={Boolean(selected)} onOpenChange={(open) => { if (!open) setSelected(null) }}>
        {selected ? <DialogContent><DialogHeader><DialogTitle>{selected.subject || selected.templateName}</DialogTitle><DialogDescription>{formatNotificationTime(selected.createdAtUtc || selected.sentAtUtc)}</DialogDescription></DialogHeader><div className="space-y-4"><StatusBadge tone={selected.severity === 'Error' ? 'danger' : selected.severity === 'Warning' ? 'warning' : selected.severity === 'Success' ? 'success' : 'info'} label={severityLabel[selected.severity]} /><p className="whitespace-pre-wrap text-sm leading-6">{stripNotificationHtml(selected.body)}</p>{selected.deepLink ? <Button onClick={() => openLinkedContent(selected)}><ExternalLink />Mở nội dung liên quan</Button> : null}</div></DialogContent> : null}
      </Dialog>
    </PageShell>
  )
}

export default NotificationCenterPage
