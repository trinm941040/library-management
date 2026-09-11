import React, { useState, useEffect, useCallback } from 'react'
import { useSearchParams } from 'react-router-dom'
import {
  RefreshCw,
  Search,
  Download,
  Filter,
  Eye,
  XCircle,
  Hash,
  Globe,
  ChevronLeft,
  ChevronRight,
  ShieldAlert,
} from 'lucide-react'
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
import { useToast } from '@/common/components'
import {
  getAuditLogs,
  downloadAuditCsv,
  type AuditLogItem,
  type AuditLogQueryParams,
} from './audit-log-api'
import { AuditDiffDialog } from './components/AuditDiffDialog'

const ENTITY_TYPES = [
  { label: 'Tất cả phân hệ', value: '' },
  { label: 'Sách (Book)', value: 'Book' },
  { label: 'Chính sách lưu thông', value: 'CirculationPolicy' },
  { label: 'Thành viên (Member)', value: 'Member' },
  { label: 'Nhân viên (Employee)', value: 'Employee' },
  { label: 'Tài khoản (User)', value: 'ApplicationUser' },
  { label: 'Vai trò (Role)', value: 'ApplicationRole' },
  { label: 'Mượn trả (Borrowing)', value: 'Borrowing' },
  { label: 'Đặt trước (Reservation)', value: 'Reservation' },
  { label: 'Vi phạm (Violation)', value: 'Violation' },
]

const ACTION_TYPES = [
  { label: 'Tất cả thao tác', value: '' },
  { label: 'Tạo mới (Added)', value: 'added' },
  { label: 'Cập nhật (Modified)', value: 'modified' },
  { label: 'Xóa bỏ (Deleted)', value: 'deleted' },
]

export function ActivityLogPage() {
  const { showToast } = useToast()
  const [searchParams, setSearchParams] = useSearchParams()

  // Extract query filters from URL
  const search = searchParams.get('search') || ''
  const entityType = searchParams.get('entityType') || ''
  const entityId = searchParams.get('entityId') || ''
  const action = searchParams.get('action') || ''
  const ipAddress = searchParams.get('ipAddress') || ''
  const correlationId = searchParams.get('correlationId') || ''
  const fromDate = searchParams.get('fromDate') || ''
  const toDate = searchParams.get('toDate') || ''
  const pageNumber = Number(searchParams.get('page') || '1')
  const pageSize = Number(searchParams.get('pageSize') || '20')

  // Local state
  const [logs, setLogs] = useState<AuditLogItem[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [totalPages, setTotalPages] = useState(1)
  const [isLoading, setIsLoading] = useState(false)
  const [isExporting, setIsExporting] = useState(false)
  const [selectedLog, setSelectedLog] = useState<AuditLogItem | null>(null)
  const [isDiffOpen, setIsDiffOpen] = useState(false)

  // Local filter inputs before applying or debounce
  const [localSearch, setLocalSearch] = useState(search)
  const [localIp, setLocalIp] = useState(ipAddress)
  const [localCorrelation, setLocalCorrelation] = useState(correlationId)

  // Sync local inputs when URL params change
  useEffect(() => {
    setLocalSearch(search)
    setLocalIp(ipAddress)
    setLocalCorrelation(correlationId)
  }, [search, ipAddress, correlationId])

  const updateFilters = useCallback(
    (newParams: Record<string, string | number | undefined | null>) => {
      setSearchParams((prev) => {
        const next = new URLSearchParams(prev)
        for (const [k, v] of Object.entries(newParams)) {
          if (v === undefined || v === null || v === '') {
            next.delete(k)
          } else {
            next.set(k, String(v))
          }
        }
        return next
      })
    },
    [setSearchParams],
  )

  const fetchLogs = useCallback(async () => {
    setIsLoading(true)
    try {
      const params: AuditLogQueryParams = {
        search: search || undefined,
        entityType: entityType || undefined,
        entityId: entityId || undefined,
        action: action || undefined,
        ipAddress: ipAddress || undefined,
        correlationId: correlationId || undefined,
        fromDateUtc: fromDate ? new Date(fromDate).toISOString() : undefined,
        toDateUtc: toDate ? new Date(toDate + 'T23:59:59.999Z').toISOString() : undefined,
        pageNumber,
        pageSize,
      }

      const res = await getAuditLogs(params)
      setLogs(res.items)
      setTotalCount(res.totalCount)
      setTotalPages(res.totalPages || 1)
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Không thể tải nhật ký kiểm toán.'
      showToast(msg, 'error')
    } finally {
      setIsLoading(false)
    }
  }, [
    search,
    entityType,
    entityId,
    action,
    ipAddress,
    correlationId,
    fromDate,
    toDate,
    pageNumber,
    pageSize,
    showToast,
  ])

  useEffect(() => {
    fetchLogs()
  }, [fetchLogs])

  const handleApplySearch = (e: React.FormEvent) => {
    e.preventDefault()
    updateFilters({
      search: localSearch.trim(),
      ipAddress: localIp.trim(),
      correlationId: localCorrelation.trim(),
      page: 1,
    })
  }

  const handleResetFilters = () => {
    setLocalSearch('')
    setLocalIp('')
    setLocalCorrelation('')
    setSearchParams(new URLSearchParams())
  }

  const handleExportCsv = async () => {
    setIsExporting(true)
    try {
      const blob = await downloadAuditCsv({
        search: search || undefined,
        entityType: entityType || undefined,
        entityId: entityId || undefined,
        action: action || undefined,
        ipAddress: ipAddress || undefined,
        correlationId: correlationId || undefined,
        fromDateUtc: fromDate ? new Date(fromDate).toISOString() : undefined,
        toDateUtc: toDate ? new Date(toDate + 'T23:59:59.999Z').toISOString() : undefined,
      })

      const url = window.URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = `nhat_ky_kiem_toan_${new Date().toISOString().slice(0, 10)}.csv`
      document.body.appendChild(link)
      link.click()
      document.body.removeChild(link)
      window.URL.revokeObjectURL(url)
      showToast('Xuất báo cáo nhật ký kiểm toán CSV thành công.', 'success')
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Xuất CSV thất bại.'
      showToast(msg, 'error')
    } finally {
      setIsExporting(false)
    }
  }

  const handleViewDetail = (log: AuditLogItem) => {
    setSelectedLog(log)
    setIsDiffOpen(true)
  }

  const handleFilterCorrelation = (cId: string) => {
    updateFilters({ correlationId: cId, page: 1 })
    showToast(`Đã lọc theo Correlation ID: ${cId}`, 'info')
  }

  const handleFilterEntity = (eType: string, eId: string) => {
    updateFilters({ entityType: eType, entityId: eId, page: 1 })
    showToast(`Đang xem lịch sử của ${eType} #${eId.slice(0, 8)}`, 'info')
  }

  const renderActionBadge = (act: string) => {
    if (act.includes('added') || act.includes('create')) {
      return <Badge className="bg-emerald-600 hover:bg-emerald-700 text-white">Tạo mới</Badge>
    }
    if (act.includes('deleted') || act.includes('remove')) {
      return <Badge variant="destructive">Xóa</Badge>
    }
    if (act.includes('modified') || act.includes('update')) {
      return <Badge className="bg-amber-600 hover:bg-amber-700 text-white">Cập nhật</Badge>
    }
    return <Badge variant="secondary">{act}</Badge>
  }

  const hasActiveFilters = Boolean(
    search || entityType || entityId || action || ipAddress || correlationId || fromDate || toDate,
  )

  return (
    <div className="mx-auto w-full max-w-7xl px-4 py-8 md:px-8 space-y-6">
      {/* Header section */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div>
          <p className="text-xs font-bold uppercase tracking-widest text-primary">
            Hệ thống &amp; Bảo mật
          </p>
          <h1 className="text-2xl sm:text-3xl font-bold tracking-tight">
            Nhật ký hoạt động
          </h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Bản ghi bất biến theo dõi toàn bộ hoạt động thay đổi dữ liệu, tài khoản, cấu hình và bảo mật.
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={fetchLogs}
            disabled={isLoading || isExporting}
          >
            <RefreshCw className={`mr-1.5 h-3.5 w-3.5 ${isLoading ? 'animate-spin' : ''}`} />
            {isLoading ? 'Đang tải...' : 'Làm mới'}
          </Button>

          <Button
            variant="default"
            size="sm"
            onClick={handleExportCsv}
            disabled={isExporting || isLoading}
          >
            <Download className={`mr-1.5 h-3.5 w-3.5 ${isExporting ? 'animate-bounce' : ''}`} />
            {isExporting ? 'Đang xuất...' : 'Xuất CSV'}
          </Button>
        </div>
      </div>

      {/* Filter Toolbar Card */}
      <Card>
        <CardHeader className="pb-3 pt-4">
          <div className="flex items-center justify-between">
            <CardTitle className="text-sm font-semibold flex items-center gap-1.5">
              <Filter className="h-4 w-4 text-primary" />
              Bộ lọc nâng cao
            </CardTitle>
            {hasActiveFilters && (
              <Button
                variant="ghost"
                size="sm"
                className="h-7 text-xs text-muted-foreground hover:text-destructive"
                onClick={handleResetFilters}
              >
                <XCircle className="mr-1 h-3.5 w-3.5" />
                Xóa tất cả bộ lọc
              </Button>
            )}
          </div>
        </CardHeader>
        <CardContent className="space-y-3 pt-0">
          <form onSubmit={handleApplySearch} className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-3">
            {/* Search query */}
            <div className="relative">
              <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder="Tìm người dùng, thao tác..."
                className="pl-8 text-xs"
                value={localSearch}
                onChange={(e) => setLocalSearch(e.target.value)}
              />
            </div>

            {/* Entity Select */}
            <div>
              <select
                className="w-full rounded-md border border-input bg-background px-3 py-2 text-xs ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                value={entityType}
                onChange={(e) => updateFilters({ entityType: e.target.value, page: 1 })}
              >
                {ENTITY_TYPES.map((e) => (
                  <option key={e.value} value={e.value}>
                    {e.label}
                  </option>
                ))}
              </select>
            </div>

            {/* Action Select */}
            <div>
              <select
                className="w-full rounded-md border border-input bg-background px-3 py-2 text-xs ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                value={action}
                onChange={(e) => updateFilters({ action: e.target.value, page: 1 })}
              >
                {ACTION_TYPES.map((a) => (
                  <option key={a.value} value={a.value}>
                    {a.label}
                  </option>
                ))}
              </select>
            </div>

            {/* IP Address */}
            <div className="relative">
              <Globe className="absolute left-2.5 top-2.5 h-3.5 w-3.5 text-muted-foreground" />
              <Input
                placeholder="Địa chỉ IP nguồn..."
                className="pl-8 text-xs font-mono"
                value={localIp}
                onChange={(e) => setLocalIp(e.target.value)}
              />
            </div>

            {/* Correlation ID */}
            <div className="relative sm:col-span-2">
              <Hash className="absolute left-2.5 top-2.5 h-3.5 w-3.5 text-muted-foreground" />
              <Input
                placeholder="Correlation ID (Mã định danh phiên liên kết)..."
                className="pl-8 text-xs font-mono"
                value={localCorrelation}
                onChange={(e) => setLocalCorrelation(e.target.value)}
              />
            </div>

            {/* Date Range: From */}
            <div>
              <Input
                type="date"
                title="Từ ngày"
                className="text-xs"
                value={fromDate}
                onChange={(e) => updateFilters({ fromDate: e.target.value, page: 1 })}
              />
            </div>

            {/* Date Range: To */}
            <div className="flex gap-2">
              <Input
                type="date"
                title="Đến ngày"
                className="text-xs flex-1"
                value={toDate}
                onChange={(e) => updateFilters({ toDate: e.target.value, page: 1 })}
              />
              <Button type="submit" size="sm" className="text-xs px-3">
                Lọc
              </Button>
            </div>
          </form>

          {/* Active filter badges */}
          {hasActiveFilters && (
            <div className="flex flex-wrap items-center gap-1.5 pt-1 text-xs">
              <span className="text-muted-foreground text-[11px]">Đang lọc theo:</span>
              {search && (
                <Badge variant="outline" className="text-[11px] gap-1">
                  Từ khóa: {search}
                  <span
                    className="cursor-pointer font-bold hover:text-destructive"
                    onClick={() => updateFilters({ search: undefined, page: 1 })}
                  >
                    ×
                  </span>
                </Badge>
              )}
              {entityType && (
                <Badge variant="outline" className="text-[11px] gap-1">
                  Đối tượng: {entityType}
                  <span
                    className="cursor-pointer font-bold hover:text-destructive"
                    onClick={() => updateFilters({ entityType: undefined, page: 1 })}
                  >
                    ×
                  </span>
                </Badge>
              )}
              {action && (
                <Badge variant="outline" className="text-[11px] gap-1">
                  Hành động: {action}
                  <span
                    className="cursor-pointer font-bold hover:text-destructive"
                    onClick={() => updateFilters({ action: undefined, page: 1 })}
                  >
                    ×
                  </span>
                </Badge>
              )}
              {ipAddress && (
                <Badge variant="outline" className="text-[11px] gap-1 font-mono">
                  IP: {ipAddress}
                  <span
                    className="cursor-pointer font-bold hover:text-destructive"
                    onClick={() => updateFilters({ ipAddress: undefined, page: 1 })}
                  >
                    ×
                  </span>
                </Badge>
              )}
              {entityId && (
                <Badge variant="outline" className="text-[11px] gap-1 font-mono">
                  Mã đối tượng: {entityId.slice(0, 8)}...
                  <span
                    className="cursor-pointer font-bold hover:text-destructive"
                    onClick={() => updateFilters({ entityId: undefined, page: 1 })}
                  >
                    ×
                  </span>
                </Badge>
              )}
              {correlationId && (
                <Badge variant="outline" className="text-[11px] gap-1 font-mono">
                  Corr: {correlationId.slice(0, 8)}...
                  <span
                    className="cursor-pointer font-bold hover:text-destructive"
                    onClick={() => updateFilters({ correlationId: undefined, page: 1 })}
                  >
                    ×
                  </span>
                </Badge>
              )}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Audit Log Table */}
      <Card>
        <CardHeader className="py-3 px-6 border-b flex flex-row items-center justify-between">
          <div>
            <CardTitle className="text-base font-semibold">Danh sách nhật ký</CardTitle>
            <CardDescription className="text-xs">
              Hiển thị {logs.length} / {totalCount} bản ghi kiểm toán
            </CardDescription>
          </div>
          <div className="flex items-center gap-2 text-xs text-muted-foreground">
            <span>Hiển thị</span>
            <select
              className="rounded border border-input bg-background px-2 py-1 text-xs"
              value={pageSize}
              onChange={(e) => updateFilters({ pageSize: Number(e.target.value), page: 1 })}
            >
              <option value="10">10</option>
              <option value="20">20</option>
              <option value="50">50</option>
              <option value="100">100</option>
            </select>
            <span>dòng / trang</span>
          </div>
        </CardHeader>
        <CardContent className="p-0">
          <div className="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow className="bg-muted/40 text-xs">
                  <TableHead className="w-[180px]">Thời gian (VN)</TableHead>
                  <TableHead>Người thực hiện</TableHead>
                  <TableHead>Thao tác</TableHead>
                  <TableHead>Đối tượng</TableHead>
                  <TableHead>Địa chỉ IP</TableHead>
                  <TableHead>Correlation ID</TableHead>
                  <TableHead className="text-right w-[100px]">Chi tiết</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {isLoading ? (
                  <TableRow>
                    <TableCell colSpan={7} className="h-32 text-center text-sm text-muted-foreground">
                      <div className="inline-flex items-center gap-2">
                        <RefreshCw className="h-4 w-4 animate-spin text-primary" />
                        Đang tải danh sách nhật ký kiểm toán...
                      </div>
                    </TableCell>
                  </TableRow>
                ) : logs.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={7} className="h-32 text-center text-sm text-muted-foreground">
                      <div className="flex flex-col items-center justify-center gap-1">
                        <ShieldAlert className="h-6 w-6 text-muted-foreground/60 mb-1" />
                        <p className="font-medium">Không tìm thấy bản ghi kiểm toán nào.</p>
                        <p className="text-xs text-muted-foreground">
                          Thử thay đổi bộ lọc hoặc kiểm tra lại khoảng thời gian.
                        </p>
                      </div>
                    </TableCell>
                  </TableRow>
                ) : (
                  logs.map((log) => (
                    <TableRow
                      key={log.id}
                      className="cursor-pointer hover:bg-muted/50 transition-colors text-xs"
                      onClick={() => handleViewDetail(log)}
                    >
                      {/* Time */}
                      <TableCell className="font-mono text-muted-foreground whitespace-nowrap">
                        {new Date(log.createdAtUtc).toLocaleString('vi-VN', {
                          timeZone: 'Asia/Ho_Chi_Minh',
                          year: 'numeric',
                          month: '2-digit',
                          day: '2-digit',
                          hour: '2-digit',
                          minute: '2-digit',
                          second: '2-digit',
                        })}
                      </TableCell>

                      {/* Actor */}
                      <TableCell>
                        <p className="font-medium text-foreground">
                          {log.actorDisplayName || 'Hệ thống'}
                        </p>
                        {log.actorEmail && (
                          <p className="text-[11px] text-muted-foreground truncate max-w-[160px]">
                            {log.actorEmail}
                          </p>
                        )}
                      </TableCell>

                      {/* Action */}
                      <TableCell>{renderActionBadge(log.action)}</TableCell>

                      {/* Entity */}
                      <TableCell>
                        <p className="font-semibold">{log.entityType}</p>
                        <p
                          className="text-[10px] text-muted-foreground font-mono truncate max-w-[120px] hover:text-primary hover:underline cursor-pointer"
                          title={`Xem toàn bộ lịch sử của đối tượng này (${log.entityId})`}
                          onClick={(e) => {
                            e.stopPropagation()
                            handleFilterEntity(log.entityType, log.entityId)
                          }}
                        >
                          #{log.entityId.slice(0, 8)}...
                        </p>
                      </TableCell>

                      {/* IP Address */}
                      <TableCell className="font-mono">
                        {log.ipAddress ? (
                          <span className="rounded bg-secondary/80 px-1.5 py-0.5 text-[11px]">
                            {log.ipAddress}
                          </span>
                        ) : (
                          <span className="italic text-muted-foreground text-[11px]">-</span>
                        )}
                      </TableCell>

                      {/* Correlation ID */}
                      <TableCell className="font-mono">
                        {log.correlationId ? (
                          <span
                            className="rounded bg-muted px-1.5 py-0.5 text-[11px] hover:underline"
                            title={log.correlationId}
                            onClick={(e) => {
                              e.stopPropagation()
                              handleFilterCorrelation(log.correlationId!)
                            }}
                          >
                            {log.correlationId.slice(0, 8)}...
                          </span>
                        ) : (
                          <span className="italic text-muted-foreground text-[11px]">-</span>
                        )}
                      </TableCell>

                      {/* Actions */}
                      <TableCell className="text-right">
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-7 w-7 text-muted-foreground hover:text-foreground"
                          onClick={(e) => {
                            e.stopPropagation()
                            handleViewDetail(log)
                          }}
                          title="Xem so sánh Before / After"
                        >
                          <Eye className="h-4 w-4" />
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </div>

          {/* Pagination Toolbar */}
          <div className="flex flex-wrap items-center justify-between gap-2 border-t px-6 py-3 text-xs text-muted-foreground">
            <div>
              Trang <span className="font-semibold text-foreground">{pageNumber}</span> /{' '}
              <span className="font-semibold text-foreground">{totalPages}</span> (Tổng số{' '}
              <span className="font-semibold text-foreground">{totalCount}</span> bản ghi)
            </div>

            <div className="flex items-center gap-1.5">
              <Button
                variant="outline"
                size="sm"
                className="h-8 px-2"
                onClick={() => updateFilters({ page: pageNumber - 1 })}
                disabled={pageNumber <= 1 || isLoading}
              >
                <ChevronLeft className="mr-1 h-3.5 w-3.5" />
                Trang trước
              </Button>

              <Button
                variant="outline"
                size="sm"
                className="h-8 px-2"
                onClick={() => updateFilters({ page: pageNumber + 1 })}
                disabled={pageNumber >= totalPages || isLoading}
              >
                Trang sau
                <ChevronRight className="ml-1 h-3.5 w-3.5" />
              </Button>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Diff Dialog */}
      <AuditDiffDialog
        open={isDiffOpen}
        onOpenChange={setIsDiffOpen}
        log={selectedLog}
        onFilterCorrelationId={handleFilterCorrelation}
        onFilterEntity={handleFilterEntity}
      />
    </div>
  )
}
