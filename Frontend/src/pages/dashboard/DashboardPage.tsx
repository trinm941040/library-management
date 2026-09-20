import { useCallback, useEffect, useMemo, useState } from 'react'
import {
  AlertCircle,
  ArrowRight,
  ArrowUpRight,
  BookOpen,
  Building2,
  Calendar,
  CheckCircle2,
  CircleAlert,
  Clock,
  FileText,
  Plus,
  RefreshCw,
  Search,
  ShieldAlert,
  Undo2,
  Users,
} from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '@/auth/AuthProvider'
import { Badge } from '@/common/components/ui/badge'
import { Button } from '@/common/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/common/components/ui/card'
import { Input } from '@/common/components/ui/input'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/common/components/ui/select'
import {
  fetchDashboardSummary,
  type DashboardSummaryResponse,
} from './dashboard-api'
import { MetricCard } from './components/MetricCard'

type AlertTab = 'overdue' | 'expiring' | 'damaged' | 'discrepancy'

export function DashboardPage() {
  const { user } = useAuth()
  const navigate = useNavigate()

  const [summary, setSummary] = useState<DashboardSummaryResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [refreshing, setRefreshing] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [currentTime, setCurrentTime] = useState(0)

  const [range, setRange] = useState<'today' | '7d' | '30d' | 'month'>('7d')
  const [selectedBranchId, setSelectedBranchId] = useState<string>('all')
  const [activeAlertTab, setActiveAlertTab] = useState<AlertTab>('overdue')
  const [alertSearch, setAlertSearch] = useState('')

  const isGlobalAdmin = useMemo(() => {
    if (!user) return false
    return user.roles.includes('Administrator') || !user.branch
  }, [user])

  const effectiveBranchId = useMemo(() => {
    if (!isGlobalAdmin && user?.branch) {
      return user.branch.id
    }
    return selectedBranchId === 'all' ? undefined : selectedBranchId
  }, [isGlobalAdmin, user, selectedBranchId])

  const loadData = useCallback(
    async (isManualRefresh = false) => {
      if (isManualRefresh) {
        setRefreshing(true)
      } else {
        setLoading(true)
      }
      setError(null)

      try {
        const offset = new Date().getTimezoneOffset()
        const data = await fetchDashboardSummary({
          range,
          branchId: effectiveBranchId,
          timezoneOffsetMinutes: offset,
        })
        setSummary(data)
      } catch (err) {
        const msg = err instanceof Error ? err.message : 'Không thể tải dữ liệu bảng điều khiển.'
        setError(msg)
      } finally {
        setLoading(false)
        setRefreshing(false)
      }
    },
    [range, effectiveBranchId],
  )

  useEffect(() => {
    void loadData()
  }, [loadData])

  useEffect(() => {
    const updateCurrentTime = () => setCurrentTime(Date.now())
    updateCurrentTime()
    const interval = window.setInterval(updateCurrentTime, 60_000)
    return () => window.clearInterval(interval)
  }, [])

  const formatCurrency = (val?: number) => {
    if (val === undefined || val === null) return '0 ₫'
    return `${val.toLocaleString('vi-VN')} ₫`
  }

  const formatDateTime = (dateStr?: string) => {
    if (!dateStr) return '-'
    const d = new Date(dateStr)
    return `${d.toLocaleDateString('vi-VN')} ${d.toLocaleTimeString('vi-VN', {
      hour: '2-digit',
      minute: '2-digit',
    })}`
  }

  const formatRelativeTime = (dateStr?: string) => {
    if (!dateStr) return '-'
    const d = new Date(dateStr)
      if (!currentTime) return d.toLocaleDateString('vi-VN')
      const diffMs = currentTime - d.getTime()
    const diffMin = Math.floor(diffMs / 60000)
    if (diffMin < 1) return 'Vừa xong'
    if (diffMin < 60) return `${diffMin} phút trước`
    const diffHours = Math.floor(diffMin / 60)
    if (diffHours < 24) return `${diffHours} giờ trước`
    return d.toLocaleDateString('vi-VN')
  }

  // Filtered operational alert items
  const filteredOverdue = useMemo(() => {
    if (!summary?.alerts.overdueLoans) return []
    if (!alertSearch.trim()) return summary.alerts.overdueLoans
    const q = alertSearch.toLowerCase()
    return summary.alerts.overdueLoans.filter(
      (item) =>
        item.borrowerName.toLowerCase().includes(q) ||
        item.bookTitle.toLowerCase().includes(q) ||
        (item.copyBarcode && item.copyBarcode.toLowerCase().includes(q)),
    )
  }, [summary, alertSearch])

  const filteredExpiring = useMemo(() => {
    if (!summary?.alerts.expiringReservations) return []
    if (!alertSearch.trim()) return summary.alerts.expiringReservations
    const q = alertSearch.toLowerCase()
    return summary.alerts.expiringReservations.filter(
      (item) =>
        item.reserverName.toLowerCase().includes(q) ||
        item.bookTitle.toLowerCase().includes(q),
    )
  }, [summary, alertSearch])

  const filteredDamaged = useMemo(() => {
    if (!summary?.alerts.damagedOrLostCopies) return []
    if (!alertSearch.trim()) return summary.alerts.damagedOrLostCopies
    const q = alertSearch.toLowerCase()
    return summary.alerts.damagedOrLostCopies.filter(
      (item) =>
        item.bookTitle.toLowerCase().includes(q) ||
        item.barcode.toLowerCase().includes(q),
    )
  }, [summary, alertSearch])

  const filteredDiscrepancy = useMemo(() => {
    if (!summary?.alerts.inventoryDiscrepancies) return []
    if (!alertSearch.trim()) return summary.alerts.inventoryDiscrepancies
    const q = alertSearch.toLowerCase()
    return summary.alerts.inventoryDiscrepancies.filter(
      (item) =>
        item.bookTitle.toLowerCase().includes(q) ||
        item.barcode.toLowerCase().includes(q) ||
        item.result.toLowerCase().includes(q),
    )
  }, [summary, alertSearch])

  const kpis = summary?.kpis
  const alerts = summary?.alerts

  return (
    <div className="content-wrap space-y-6 pb-12">
      {/* 1. Header & Filters */}
      <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between border-b pb-5">
        <div>
          <div className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wider text-muted-foreground">
            <Calendar className="w-3.5 h-3.5" />
            <span>
              {new Date().toLocaleDateString('vi-VN', {
                weekday: 'long',
                year: 'numeric',
                month: 'long',
                day: 'numeric',
              })}
            </span>
          </div>
          <h1 className="text-2xl font-bold tracking-tight text-foreground mt-1">
            Tổng quan bảng điều khiển
          </h1>
          <p className="text-sm text-muted-foreground">
            {summary?.selectedBranchName
              ? `Phạm vi chi nhánh: ${summary.selectedBranchName}`
              : 'Dữ liệu vận hành toàn hệ thống thư viện.'}
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-2.5">
          {/* Branch filter */}
          {isGlobalAdmin ? (
            <Select
              value={selectedBranchId}
              onValueChange={(val) => setSelectedBranchId(val)}
            >
              <SelectTrigger className="w-[180px] h-9 text-xs" aria-label="Chọn chi nhánh">
                <Building2 className="w-3.5 h-3.5 mr-1 text-muted-foreground" />
                <SelectValue placeholder="Chi nhánh" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Tất cả chi nhánh</SelectItem>
                {summary?.availableBranches?.map((b) => (
                  <SelectItem key={b.id} value={b.id}>
                    {b.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          ) : user?.branch ? (
            <Badge variant="outline" className="px-3 py-1.5 text-xs flex items-center gap-1.5 bg-background">
              <Building2 className="w-3.5 h-3.5 text-primary" />
              <span>{user.branch.name}</span>
            </Badge>
          ) : null}

          {/* Time range filter */}
          <Select
            value={range}
            onValueChange={(val: 'today' | '7d' | '30d' | 'month') => setRange(val)}
          >
            <SelectTrigger className="w-[140px] h-9 text-xs" aria-label="Chọn khoảng thời gian">
              <Clock className="w-3.5 h-3.5 mr-1 text-muted-foreground" />
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="today">Hôm nay</SelectItem>
              <SelectItem value="7d">7 ngày qua</SelectItem>
              <SelectItem value="30d">30 ngày qua</SelectItem>
              <SelectItem value="month">Tháng này</SelectItem>
            </SelectContent>
          </Select>

          {/* Refresh button */}
          <Button
            variant="outline"
            size="sm"
            className="h-9 px-3 text-xs"
            onClick={() => void loadData(true)}
            disabled={loading || refreshing}
            title="Làm mới số liệu"
          >
            <RefreshCw className={`w-3.5 h-3.5 mr-1.5 ${refreshing ? 'animate-spin' : ''}`} />
            Làm mới
          </Button>

          {/* Quick actions */}
          <Button
            size="sm"
            className="h-9 px-3 text-xs"
            onClick={() => navigate('/circulation/checkout')}
          >
            <Plus className="w-3.5 h-3.5 mr-1" /> Mượn sách
          </Button>
          <Button
            size="sm"
            variant="secondary"
            className="h-9 px-3 text-xs"
            onClick={() => navigate('/circulation/return')}
          >
            <Undo2 className="w-3.5 h-3.5 mr-1" /> Nhận trả
          </Button>
        </div>
      </div>

      {/* Timestamp info */}
      <div className="flex items-center justify-between text-xs text-muted-foreground -mt-3">
        <span>
          Cập nhật lúc:{' '}
          <strong className="text-foreground">
            {summary ? formatDateTime(summary.generatedAtUtc) : '...'}
          </strong>
        </span>
        {refreshing && <span className="text-primary animate-pulse">Đang đồng bộ số liệu mới nhất...</span>}
      </div>

      {/* Error state */}
      {error && (
        <Card className="border-destructive/40 bg-destructive/10 text-destructive p-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <AlertCircle className="w-5 h-5 text-destructive" />
              <div>
                <p className="font-semibold text-sm">Không thể tải dữ liệu bảng điều khiển</p>
                <p className="text-xs text-destructive/80 mt-0.5">{error}</p>
              </div>
            </div>
            <Button
              variant="destructive"
              size="sm"
              className="text-xs h-8"
              onClick={() => void loadData()}
            >
              Thử lại
            </Button>
          </div>
        </Card>
      )}

      {/* 2. Primary KPI Cards */}
      <section className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4" aria-label="Chỉ số KPI chính">
        <MetricCard
          label="Sách đang được mượn"
          value={loading ? '...' : kpis?.activeBorrowings ?? 0}
          delta={range === '7d' ? 'Lưu thông tích cực' : undefined}
          positive={true}
          icon={<BookOpen />}
          tone="blue"
          subtext="Nhấn để xem danh sách đang mượn"
          onClick={() => navigate('/borrowings?status=borrowed')}
        />
        <MetricCard
          label="Sách quá hạn cần xử lý"
          value={loading ? '...' : alerts?.overdueLoansCount ?? 0}
          delta={alerts?.overdueLoansCount && alerts.overdueLoansCount > 0 ? 'Cần xử lý' : 'An toàn'}
          positive={!alerts?.overdueLoansCount || alerts.overdueLoansCount === 0}
          icon={<CircleAlert />}
          tone={alerts?.overdueLoansCount && alerts.overdueLoansCount > 0 ? 'orange' : 'green'}
          subtext="Nhấn để xem danh sách quá hạn"
          onClick={() => navigate('/borrowings?status=overdue')}
        />
        <MetricCard
          label="Độc giả đang hoạt động"
          value={loading ? '...' : kpis?.activeMembers ?? 0}
          positive={true}
          icon={<Users />}
          tone="green"
          subtext="Độc giả có thẻ hợp lệ"
          onClick={() => navigate('/members')}
        />
        <MetricCard
          label="Phiếu đặt trước chờ nhận"
          value={loading ? '...' : kpis?.activeReservations ?? 0}
          delta={alerts?.expiringReservationsCount ? `${alerts.expiringReservationsCount} sắp hết hạn` : undefined}
          positive={false}
          icon={<Clock />}
          tone="violet"
          subtext="Nhấn để mở danh sách đặt sách"
          onClick={() => navigate('/reservations?status=pending')}
        />
      </section>

      {/* 3. Secondary KPI stats strip */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
        <Card
          className="p-3.5 hover:bg-muted/40 transition-colors cursor-pointer"
          onClick={() => navigate('/books')}
        >
          <div className="flex items-center justify-between text-muted-foreground text-xs mb-1">
            <span>Kho sách & Bản sao</span>
            <ArrowRight className="w-3 h-3" />
          </div>
          <div className="text-lg font-bold text-foreground">
            {loading ? '...' : `${kpis?.availableCopies ?? 0} / ${kpis?.totalCopies ?? 0}`}
          </div>
          <p className="text-[11px] text-muted-foreground mt-0.5">
            {kpis?.totalBooks ?? 0} tựa sách | Sẵn sàng mượn
          </p>
        </Card>

        <Card
          className="p-3.5 hover:bg-muted/40 transition-colors cursor-pointer"
          onClick={() => navigate('/borrowings?status=returned')}
        >
          <div className="flex items-center justify-between text-muted-foreground text-xs mb-1">
            <span>Đã trả trong kỳ</span>
            <ArrowRight className="w-3 h-3" />
          </div>
          <div className="text-lg font-bold text-emerald-600 dark:text-emerald-400">
            {loading ? '...' : kpis?.returnedInPeriod ?? 0}
          </div>
          <p className="text-[11px] text-muted-foreground mt-0.5">Lượt trả sách hoàn tất</p>
        </Card>

        <Card
          className="p-3.5 hover:bg-muted/40 transition-colors cursor-pointer"
          onClick={() => navigate('/violations')}
        >
          <div className="flex items-center justify-between text-muted-foreground text-xs mb-1">
            <span>Tiền phạt tồn đọng</span>
            <ArrowRight className="w-3 h-3" />
          </div>
          <div className="text-lg font-bold text-amber-600 dark:text-amber-400">
            {loading ? '...' : formatCurrency(kpis?.outstandingFineBalance)}
          </div>
          <p className="text-[11px] text-muted-foreground mt-0.5">Vi phạm chưa thanh toán</p>
        </Card>

        <Card
          className="p-3.5 hover:bg-muted/40 transition-colors cursor-pointer"
          onClick={() => navigate('/payments')}
        >
          <div className="flex items-center justify-between text-muted-foreground text-xs mb-1">
            <span>Tiền phạt đã thu</span>
            <ArrowRight className="w-3 h-3" />
          </div>
          <div className="text-lg font-bold text-blue-600 dark:text-blue-400">
            {loading ? '...' : formatCurrency(kpis?.collectedFineInPeriod)}
          </div>
          <p className="text-[11px] text-muted-foreground mt-0.5">Thu phí trong kỳ đã chọn</p>
        </Card>
      </div>

      {/* 4. Operational Alerts Center */}
      <Card className="shadow-xs">
        <CardHeader className="border-b pb-4">
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
            <div>
              <div className="flex items-center gap-2">
                <ShieldAlert className="w-5 h-5 text-amber-500" />
                <CardTitle className="text-base font-bold">
                  Trung tâm cảnh báo vận hành
                </CardTitle>
                <Badge
                  variant={alerts && alerts.totalAlertCount > 0 ? 'destructive' : 'secondary'}
                  className="ml-1 text-xs"
                >
                  {alerts?.totalAlertCount ?? 0} việc cần chú ý
                </Badge>
              </div>
              <p className="text-xs text-muted-foreground mt-1">
                Các sự vụ phát sinh cần thủ thư và quản lý kiểm tra xử lý kịp thời.
              </p>
            </div>

            {/* Alert search */}
            <div className="relative w-full sm:w-64">
              <Search className="absolute left-2.5 top-2.5 h-3.5 w-3.5 text-muted-foreground" />
              <Input
                placeholder="Tìm bạn đọc, tên sách, mã..."
                className="pl-8 h-8 text-xs"
                value={alertSearch}
                onChange={(e) => setAlertSearch(e.target.value)}
              />
            </div>
          </div>

          {/* Alert Tabs */}
          <div className="flex flex-wrap gap-2 mt-4 pt-2 border-t">
            <Button
              variant={activeAlertTab === 'overdue' ? 'default' : 'outline'}
              size="sm"
              className="text-xs h-8"
              onClick={() => setActiveAlertTab('overdue')}
            >
              Mượn quá hạn
              <Badge variant="secondary" className="ml-1.5 px-1.5 py-0 text-[10px]">
                {alerts?.overdueLoansCount ?? 0}
              </Badge>
            </Button>

            <Button
              variant={activeAlertTab === 'expiring' ? 'default' : 'outline'}
              size="sm"
              className="text-xs h-8"
              onClick={() => setActiveAlertTab('expiring')}
            >
              Đặt sách sắp hết hạn
              <Badge variant="secondary" className="ml-1.5 px-1.5 py-0 text-[10px]">
                {alerts?.expiringReservationsCount ?? 0}
              </Badge>
            </Button>

            <Button
              variant={activeAlertTab === 'damaged' ? 'default' : 'outline'}
              size="sm"
              className="text-xs h-8"
              onClick={() => setActiveAlertTab('damaged')}
            >
              Bản sao hỏng / mất
              <Badge variant="secondary" className="ml-1.5 px-1.5 py-0 text-[10px]">
                {alerts?.damagedOrLostCopiesCount ?? 0}
              </Badge>
            </Button>

            <Button
              variant={activeAlertTab === 'discrepancy' ? 'default' : 'outline'}
              size="sm"
              className="text-xs h-8"
              onClick={() => setActiveAlertTab('discrepancy')}
            >
              Chênh lệch kiểm kê
              <Badge variant="secondary" className="ml-1.5 px-1.5 py-0 text-[10px]">
                {alerts?.inventoryDiscrepanciesCount ?? 0}
              </Badge>
            </Button>
          </div>
        </CardHeader>

        <CardContent className="p-0">
          {/* TAB 1: Overdue Loans */}
          {activeAlertTab === 'overdue' && (
            <div>
              {loading ? (
                <div className="p-8 text-center text-xs text-muted-foreground animate-pulse">
                  Đang tải danh sách mượn quá hạn...
                </div>
              ) : filteredOverdue.length === 0 ? (
                <div className="p-8 text-center">
                  <CheckCircle2 className="w-8 h-8 text-emerald-500 mx-auto mb-2 opacity-80" />
                  <p className="text-sm font-medium">Không có sách mượn quá hạn nào.</p>
                  <p className="text-xs text-muted-foreground mt-0.5">Tất cả độc giả đang tuân thủ hạn trả.</p>
                </div>
              ) : (
                <div className="divide-y">
                  {filteredOverdue.map((item) => (
                    <div
                      key={item.borrowingId}
                      className="flex flex-col sm:flex-row sm:items-center justify-between p-4 hover:bg-muted/40 gap-3 transition-colors"
                    >
                      <div className="space-y-1">
                        <div className="flex items-center gap-2">
                          <span className="font-semibold text-sm text-foreground">
                            {item.borrowerName}
                          </span>
                          <Badge variant="outline" className="text-[10px] text-muted-foreground">
                            {item.borrowerEmail}
                          </Badge>
                          <Badge variant="destructive" className="text-[10px]">
                            Quá hạn {item.overdueDays} ngày
                          </Badge>
                        </div>
                        <p className="text-xs text-foreground/90 font-medium">
                          {item.bookTitle}{' '}
                          {item.copyBarcode && (
                            <span className="text-muted-foreground font-mono font-normal">
                              (Mã: {item.copyBarcode})
                            </span>
                          )}
                        </p>
                        <p className="text-[11px] text-muted-foreground">
                          Hạn trả: {formatDateTime(item.dueAtUtc)}
                        </p>
                      </div>
                      <div className="flex items-center gap-2 shrink-0">
                        <Button
                          size="sm"
                          variant="outline"
                          className="text-xs h-8"
                          onClick={() => navigate(`/loans/${item.borrowingId}`)}
                        >
                          Chi tiết mượn <ArrowUpRight className="w-3 h-3 ml-1" />
                        </Button>
                      </div>
                    </div>
                  ))}
                  <div className="p-3 bg-muted/20 text-center">
                    <Button
                      variant="link"
                      size="sm"
                      className="text-xs"
                      onClick={() => navigate('/borrowings?status=overdue')}
                    >
                      Xem toàn bộ danh sách quá hạn ({alerts?.overdueLoansCount ?? 0}) <ArrowRight className="w-3.5 h-3.5 ml-1" />
                    </Button>
                  </div>
                </div>
              )}
            </div>
          )}

          {/* TAB 2: Expiring Reservations */}
          {activeAlertTab === 'expiring' && (
            <div>
              {loading ? (
                <div className="p-8 text-center text-xs text-muted-foreground animate-pulse">
                  Đang tải phiếu đặt sách sắp hết hạn...
                </div>
              ) : filteredExpiring.length === 0 ? (
                <div className="p-8 text-center">
                  <CheckCircle2 className="w-8 h-8 text-emerald-500 mx-auto mb-2 opacity-80" />
                  <p className="text-sm font-medium">Không có phiếu đặt sách nào sắp hết hạn nhận.</p>
                  <p className="text-xs text-muted-foreground mt-0.5">Các phiếu đặt đều nằm trong thời hạn an toàn.</p>
                </div>
              ) : (
                <div className="divide-y">
                  {filteredExpiring.map((item) => (
                    <div
                      key={item.reservationId}
                      className="flex flex-col sm:flex-row sm:items-center justify-between p-4 hover:bg-muted/40 gap-3 transition-colors"
                    >
                      <div className="space-y-1">
                        <div className="flex items-center gap-2">
                          <span className="font-semibold text-sm text-foreground">
                            {item.reserverName}
                          </span>
                          <Badge variant="outline" className="text-[10px] text-muted-foreground">
                            {item.reserverEmail}
                          </Badge>
                          <Badge variant="secondary" className="text-[10px] text-amber-600 bg-amber-50 dark:bg-amber-950/40">
                            Còn ~{item.remainingHours} giờ
                          </Badge>
                        </div>
                        <p className="text-xs text-foreground/90 font-medium">
                          Tựa sách: {item.bookTitle}
                        </p>
                        <p className="text-[11px] text-muted-foreground">
                          Hạn nhận sách: {formatDateTime(item.expiresAtUtc)}
                        </p>
                      </div>
                      <div className="flex items-center gap-2 shrink-0">
                        <Button
                          size="sm"
                          variant="outline"
                          className="text-xs h-8"
                          onClick={() => navigate('/reservations?status=pending')}
                        >
                          Xử lý đặt sách <ArrowUpRight className="w-3 h-3 ml-1" />
                        </Button>
                      </div>
                    </div>
                  ))}
                  <div className="p-3 bg-muted/20 text-center">
                    <Button
                      variant="link"
                      size="sm"
                      className="text-xs"
                      onClick={() => navigate('/reservations?status=pending')}
                    >
                      Mở quản lý đặt trước <ArrowRight className="w-3.5 h-3.5 ml-1" />
                    </Button>
                  </div>
                </div>
              )}
            </div>
          )}

          {/* TAB 3: Damaged or Lost Copies */}
          {activeAlertTab === 'damaged' && (
            <div>
              {loading ? (
                <div className="p-8 text-center text-xs text-muted-foreground animate-pulse">
                  Đang tải danh sách bản sao hỏng hoặc mất...
                </div>
              ) : filteredDamaged.length === 0 ? (
                <div className="p-8 text-center">
                  <CheckCircle2 className="w-8 h-8 text-emerald-500 mx-auto mb-2 opacity-80" />
                  <p className="text-sm font-medium">Không có bản sao nào bị ghi nhận hỏng hoặc mất.</p>
                  <p className="text-xs text-muted-foreground mt-0.5">Tình trạng tài liệu thư viện đang tốt.</p>
                </div>
              ) : (
                <div className="divide-y">
                  {filteredDamaged.map((item) => (
                    <div
                      key={item.bookCopyId}
                      className="flex flex-col sm:flex-row sm:items-center justify-between p-4 hover:bg-muted/40 gap-3 transition-colors"
                    >
                      <div className="space-y-1">
                        <div className="flex items-center gap-2">
                          <span className="font-semibold text-sm text-foreground">
                            {item.bookTitle}
                          </span>
                          <span className="font-mono text-xs text-muted-foreground">
                            ({item.barcode})
                          </span>
                          <Badge variant="destructive" className="text-[10px]">
                            {item.status} / {item.condition}
                          </Badge>
                        </div>
                        <p className="text-xs text-muted-foreground">
                          Bản sao cần thanh lý, sửa chữa hoặc xử lý bồi thường vi phạm.
                        </p>
                      </div>
                      <div className="flex items-center gap-2 shrink-0">
                        <Button
                          size="sm"
                          variant="outline"
                          className="text-xs h-8"
                          onClick={() => navigate('/books')}
                        >
                          Xem danh mục sách <ArrowUpRight className="w-3 h-3 ml-1" />
                        </Button>
                      </div>
                    </div>
                  ))}
                  <div className="p-3 bg-muted/20 text-center">
                    <Button
                      variant="link"
                      size="sm"
                      className="text-xs"
                      onClick={() => navigate('/books')}
                    >
                      Mở kho sách & bản sao ({alerts?.damagedOrLostCopiesCount ?? 0}) <ArrowRight className="w-3.5 h-3.5 ml-1" />
                    </Button>
                  </div>
                </div>
              )}
            </div>
          )}

          {/* TAB 4: Inventory Discrepancies */}
          {activeAlertTab === 'discrepancy' && (
            <div>
              {loading ? (
                <div className="p-8 text-center text-xs text-muted-foreground animate-pulse">
                  Đang tải chênh lệch kiểm kê...
                </div>
              ) : filteredDiscrepancy.length === 0 ? (
                <div className="p-8 text-center">
                  <CheckCircle2 className="w-8 h-8 text-emerald-500 mx-auto mb-2 opacity-80" />
                  <p className="text-sm font-medium">Không có bản sao chênh lệch hoặc mất mát trong các đợt kiểm kê.</p>
                  <p className="text-xs text-muted-foreground mt-0.5">Số liệu kiểm kê trùng khớp với thực tế.</p>
                </div>
              ) : (
                <div className="divide-y">
                  {filteredDiscrepancy.map((item) => (
                    <div
                      key={item.auditItemId}
                      className="flex flex-col sm:flex-row sm:items-center justify-between p-4 hover:bg-muted/40 gap-3 transition-colors"
                    >
                      <div className="space-y-1">
                        <div className="flex items-center gap-2">
                          <span className="font-semibold text-sm text-foreground">
                            {item.bookTitle}
                          </span>
                          <span className="font-mono text-xs text-muted-foreground">
                            ({item.barcode})
                          </span>
                          <Badge variant="destructive" className="text-[10px]">
                            Kết quả: {item.result}
                          </Badge>
                        </div>
                        <p className="text-xs text-muted-foreground">
                          Ghi nhận quét:{' '}
                          {item.scannedAtUtc ? formatDateTime(item.scannedAtUtc) : 'Chưa quét'}
                        </p>
                      </div>
                      <div className="flex items-center gap-2 shrink-0">
                        <Button
                          size="sm"
                          variant="outline"
                          className="text-xs h-8"
                          onClick={() => navigate('/books')}
                        >
                          Kiểm tra tài liệu <ArrowUpRight className="w-3 h-3 ml-1" />
                        </Button>
                      </div>
                    </div>
                  ))}
                  <div className="p-3 bg-muted/20 text-center">
                    <Button
                      variant="link"
                      size="sm"
                      className="text-xs"
                      onClick={() => navigate('/books')}
                    >
                      Xem chi tiết ({alerts?.inventoryDiscrepanciesCount ?? 0}) <ArrowRight className="w-3.5 h-3.5 ml-1" />
                    </Button>
                  </div>
                </div>
              )}
            </div>
          )}
        </CardContent>
      </Card>

      {/* 5. Recent Activity Logs */}
      <Card className="shadow-xs">
        <CardHeader className="border-b pb-4 flex flex-row items-center justify-between">
          <div>
            <CardTitle className="text-base font-bold">Nhật ký hoạt động gần đây</CardTitle>
            <p className="text-xs text-muted-foreground mt-0.5">
              Các thao tác nghiệp vụ vừa diễn ra trên hệ thống.
            </p>
          </div>
          <Button
            variant="ghost"
            size="sm"
            className="text-xs h-8"
            onClick={() => navigate('/audit-logs')}
          >
            Xem nhật ký kiểm toán <ArrowRight className="w-3.5 h-3.5 ml-1" />
          </Button>
        </CardHeader>

        <CardContent className="p-0">
          {loading ? (
            <div className="p-8 text-center text-xs text-muted-foreground animate-pulse">
              Đang tải hoạt động...
            </div>
          ) : !summary?.recentActivities || summary.recentActivities.length === 0 ? (
            <div className="p-8 text-center text-xs text-muted-foreground">
              Chưa có nhật ký hoạt động nào được ghi nhận.
            </div>
          ) : (
            <div className="divide-y">
              {summary.recentActivities.map((act) => (
                <div
                  key={act.id}
                  className="flex items-center justify-between p-3.5 hover:bg-muted/30 transition-colors text-xs"
                >
                  <div className="flex items-center gap-3">
                    <div className="w-7 h-7 rounded-full bg-primary/10 text-primary flex items-center justify-center shrink-0">
                      <FileText className="w-3.5 h-3.5" />
                    </div>
                    <div>
                      <div className="flex items-center gap-1.5">
                        <strong className="text-foreground font-semibold">{act.action}</strong>
                        <Badge variant="outline" className="text-[10px] py-0 px-1 font-mono">
                          {act.entityType}
                        </Badge>
                      </div>
                      <p className="text-muted-foreground text-[11px] mt-0.5">
                        Thực hiện bởi <span className="font-medium text-foreground">{act.actorName}</span>
                        {act.details && ` • Mã theo dõi: ${act.details}`}
                      </p>
                    </div>
                  </div>

                  <span className="text-muted-foreground text-[11px] shrink-0">
                    {formatRelativeTime(act.timestampUtc)}
                  </span>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
