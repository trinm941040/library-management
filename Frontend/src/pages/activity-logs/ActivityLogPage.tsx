import { type FormEvent, useCallback, useEffect, useMemo, useState } from 'react'
import { Download, Eye, RefreshCw, Search } from 'lucide-react'
import { useSearchParams } from 'react-router-dom'
import { DataTable, FilterPanel, PageShell, type DataTableColumn } from '@/common/components'
import { Badge } from '@/common/components/ui/badge'
import { Button } from '@/common/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/common/components/ui/card'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/common/components/ui/dialog'
import { Input } from '@/common/components/ui/input'
import { PermissionBoundary } from '@/shared/auth/PermissionBoundary'
import { exportAuditLogs, getAuditLog, getAuditLogs, type AuditLog, type AuditLogFilters } from './audit-log-api'
import { downloadResponse } from '@/shared/data/table-contracts'

const filterKeys = [
  'actorUserId', 'action', 'entityType', 'entityId',
  'correlationId', 'ipAddress', 'fromUtc', 'toUtc',
] as const
type FilterKey = (typeof filterKeys)[number]
type FilterDraft = Record<FilterKey, string>

const emptyDraft = (): FilterDraft =>
  Object.fromEntries(filterKeys.map((key) => [key, ''])) as FilterDraft
const safePage = (value: string | null, fallback: number) => {
  const parsed = Number.parseInt(value ?? '', 10)
  return Number.isFinite(parsed) && parsed > 0 ? parsed : fallback
}
const displayDate = (value: string) => new Intl.DateTimeFormat('vi-VN', {
  dateStyle: 'short', timeStyle: 'medium',
}).format(new Date(value))
const toLocalDateTimeInput = (value: string) => {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ''
  return new Date(date.getTime() - date.getTimezoneOffset() * 60_000).toISOString().slice(0, 16)
}

function parseAuditObject(value: string | null): Record<string, unknown> {
  if (!value) return {}
  try {
    const parsed: unknown = JSON.parse(value)
    return parsed && typeof parsed === 'object' && !Array.isArray(parsed)
      ? (parsed as Record<string, unknown>) : { value: parsed }
  } catch {
    return { value: '[DỮ LIỆU KHÔNG HỢP LỆ]' }
  }
}

function displayValue(key: string, value: unknown) {
  if (/password|token|secret|email|phone|address|dateofbirth|fullname|displayname|username|borrowername|reservername|contactname|contactinfo/i.test(key))
    return '[ĐÃ ẨN]'
  if (value === undefined) return '—'
  if (value === null) return 'null'
  return typeof value === 'object' ? JSON.stringify(value) : String(value)
}

function AuditDiff({ item }: { item: AuditLog }) {
  const before = parseAuditObject(item.beforeJson)
  const after = parseAuditObject(item.afterJson)
  const keys = Array.from(new Set([...Object.keys(before), ...Object.keys(after)]))
    .filter((key) => JSON.stringify(before[key]) !== JSON.stringify(after[key]))
    .sort()

  if (keys.length === 0)
    return <p className="rounded-md bg-muted p-4 text-sm text-muted-foreground">Không có thuộc tính thay đổi để hiển thị.</p>

  return (
    <div className="max-h-[50vh] overflow-auto rounded-md border">
      <table className="w-full min-w-[640px] text-left text-sm">
        <thead className="sticky top-0 bg-background shadow-sm">
          <tr><th className="p-3">Thuộc tính</th><th className="p-3">Trước</th><th className="p-3">Sau</th></tr>
        </thead>
        <tbody>
          {keys.map((key) => (
            <tr key={key} className="border-t align-top">
              <th className="p-3 font-medium">{key}</th>
              <td className="max-w-64 break-words p-3 font-mono text-xs">{displayValue(key, before[key])}</td>
              <td className="max-w-64 break-words p-3 font-mono text-xs">{displayValue(key, after[key])}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

export function ActivityLogPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const pageNumber = safePage(searchParams.get('pageNumber'), 1)
  const pageSize = safePage(searchParams.get('pageSize'), 20)
  const activeFilters = useMemo(
    () => Object.fromEntries(filterKeys.map((key) => [key, searchParams.get(key) ?? ''])) as FilterDraft,
    [searchParams],
  )
  const [draft, setDraft] = useState<FilterDraft>(activeFilters)
  const [items, setItems] = useState<AuditLog[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [totalPages, setTotalPages] = useState(0)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState('')
  const [reloadKey, setReloadKey] = useState(0)
  const [detail, setDetail] = useState<AuditLog | null>(null)
  const [detailError, setDetailError] = useState('')
  const [detailLoading, setDetailLoading] = useState(false)

  useEffect(() => setDraft(activeFilters), [activeFilters])
  useEffect(() => {
    const controller = new AbortController()
    setIsLoading(true)
    setError('')
    const filters: AuditLogFilters = {
      ...Object.fromEntries(Object.entries(activeFilters).filter(([, value]) => value)),
      pageNumber, pageSize,
    }
    getAuditLogs(filters, controller.signal)
      .then((page) => {
        setItems(page.items)
        setTotalCount(page.totalCount)
        setTotalPages(page.totalPages)
        if (page.totalPages > 0 && pageNumber > page.totalPages)
          setSearchParams((current) => {
            const next = new URLSearchParams(current)
            next.set('pageNumber', String(page.totalPages))
            return next
          }, { replace: true })
      })
      .catch((reason: unknown) => {
        if (reason instanceof DOMException && reason.name === 'AbortError') return
        setError(reason instanceof Error ? reason.message : 'Không thể tải nhật ký kiểm toán.')
      })
      .finally(() => { if (!controller.signal.aborted) setIsLoading(false) })
    return () => controller.abort()
  }, [activeFilters, pageNumber, pageSize, reloadKey, setSearchParams])

  const updateParams = useCallback((changes: Record<string, string | number | undefined>) => {
    setSearchParams((current) => {
      const next = new URLSearchParams(current)
      Object.entries(changes).forEach(([key, value]) => {
        if (value === undefined || value === '') next.delete(key)
        else next.set(key, String(value))
      })
      return next
    })
  }, [setSearchParams])

  const submitFilters = (event: FormEvent) => {
    event.preventDefault()
    updateParams({ ...draft, pageNumber: 1 })
  }

  const openDetail = async (item: AuditLog) => {
    setDetail(item)
    setDetailError('')
    setDetailLoading(true)
    try { setDetail(await getAuditLog(item.id)) }
    catch (reason) { setDetailError(reason instanceof Error ? reason.message : 'Không thể tải chi tiết.') }
    finally { setDetailLoading(false) }
  }

  const exportFiltered = async () => {
    const filters: AuditLogFilters = {
      ...Object.fromEntries(Object.entries(activeFilters).filter(([, value]) => value)),
      pageNumber: 1, pageSize,
    }
    try { await downloadResponse(await exportAuditLogs(filters), 'audit-log.csv') }
    catch (reason) { setError(reason instanceof Error ? reason.message : 'Không thể xuất nhật ký kiểm toán.') }
  }

  const columns = useMemo<DataTableColumn<AuditLog>[]>(() => [
    { id: 'createdAtUtc', header: 'Thời gian', cell: (item) => <time dateTime={item.createdAtUtc}>{displayDate(item.createdAtUtc)}</time> },
    { id: 'actor', header: 'Người thực hiện', cell: (item) => <span className="grid gap-0.5"><strong>{item.actorName ?? 'Hệ thống'}</strong><small className="font-mono text-muted-foreground">{item.actorUserId ?? '—'}</small></span> },
    { id: 'action', header: 'Hành động', cell: (item) => <Badge variant="outline">{item.action}</Badge> },
    { id: 'entity', header: 'Đối tượng', cell: (item) => <span className="grid gap-0.5"><strong>{item.entityType}</strong><button className="text-left font-mono text-xs text-primary hover:underline" onClick={() => updateParams({ entityType: item.entityType, entityId: item.entityId, pageNumber: 1 })}>{item.entityId}</button></span> },
    { id: 'ipAddress', header: 'Địa chỉ IP', cell: (item) => <span className="font-mono text-xs">{item.ipAddress ?? '—'}</span> },
    { id: 'correlationId', header: 'Correlation ID', cell: (item) => item.correlationId ? <button className="max-w-48 truncate font-mono text-xs text-primary hover:underline" title={item.correlationId} onClick={() => updateParams({ correlationId: item.correlationId ?? '', pageNumber: 1 })}>{item.correlationId}</button> : '—' },
    { id: 'details', header: 'Chi tiết', className: 'text-right', cell: (item) => <Button variant="ghost" size="icon" aria-label={`Xem chi tiết ${item.action}`} onClick={() => void openDetail(item)}><Eye /></Button> },
  ], [updateParams])

  const hasFilters = Object.values(activeFilters).some(Boolean)
  return (
    <PageShell
      eyebrow="Quản lý hệ thống"
      title="Nhật ký kiểm toán"
      description="Tra cứu lịch sử thay đổi bất biến theo người thực hiện, đối tượng và yêu cầu."
      actions={<><Button variant="outline" disabled={isLoading} loading={isLoading && items.length > 0} loadingLabel="Đang tải lại nhật ký" onClick={() => setReloadKey((value) => value + 1)}><RefreshCw />Làm mới</Button><PermissionBoundary requiredPermissions={['audit-logs.export']}><Button disabled={totalCount === 0} onClick={() => void exportFiltered()}><Download />Xuất kết quả đã lọc</Button></PermissionBoundary></>}
    >
      <Card>
        <CardHeader><CardTitle>Danh sách bản ghi</CardTitle><p className="text-sm text-muted-foreground">{totalCount} bản ghi phù hợp với bộ lọc.</p></CardHeader>
        <CardContent>
          <DataTable
            caption="Danh sách nhật ký kiểm toán"
            rows={items}
            columns={columns}
            getRowId={(item) => item.id}
            isLoading={isLoading && items.length === 0}
            error={error || undefined}
            onRetry={() => setReloadKey((value) => value + 1)}
            emptyTitle="Không có bản ghi kiểm toán"
            emptyDescription="Thay đổi bộ lọc hoặc thực hiện một nghiệp vụ có ghi nhận kiểm toán."
            filters={<FilterPanel hasFilters={hasFilters} onReset={() => { setDraft(emptyDraft()); setSearchParams({}) }} resultCount={totalCount}>
              <form className="grid w-full gap-3 md:grid-cols-2 xl:grid-cols-4" onSubmit={submitFilters}>
                <Input value={draft.action} onChange={(event) => setDraft((value) => ({ ...value, action: event.target.value }))} placeholder="Hành động" aria-label="Lọc theo hành động" />
                <Input value={draft.entityType} onChange={(event) => setDraft((value) => ({ ...value, entityType: event.target.value }))} placeholder="Loại đối tượng" aria-label="Lọc theo loại đối tượng" />
                <Input value={draft.entityId} onChange={(event) => setDraft((value) => ({ ...value, entityId: event.target.value }))} placeholder="Mã đối tượng" aria-label="Lọc theo mã đối tượng" />
                <Input value={draft.actorUserId} onChange={(event) => setDraft((value) => ({ ...value, actorUserId: event.target.value }))} placeholder="Mã người thực hiện" aria-label="Lọc theo mã người thực hiện" />
                <Input value={draft.ipAddress} onChange={(event) => setDraft((value) => ({ ...value, ipAddress: event.target.value }))} placeholder="Địa chỉ IP" aria-label="Lọc theo địa chỉ IP" />
                <Input value={draft.correlationId} onChange={(event) => setDraft((value) => ({ ...value, correlationId: event.target.value }))} placeholder="Correlation ID" aria-label="Lọc theo correlation ID" />
                <Input type="datetime-local" value={toLocalDateTimeInput(draft.fromUtc)} onChange={(event) => setDraft((value) => ({ ...value, fromUtc: event.target.value ? new Date(event.target.value).toISOString() : '' }))} aria-label="Từ thời điểm" />
                <Input type="datetime-local" value={toLocalDateTimeInput(draft.toUtc)} onChange={(event) => setDraft((value) => ({ ...value, toUtc: event.target.value ? new Date(event.target.value).toISOString() : '' }))} aria-label="Đến thời điểm" />
                <Button type="submit" className="xl:col-start-4"><Search />Áp dụng bộ lọc</Button>
              </form>
            </FilterPanel>}
            page={pageNumber}
            totalPages={totalPages}
            totalCount={totalCount}
            onPageChange={(page) => updateParams({ pageNumber: page })}
            pageSize={pageSize}
            onPageSizeChange={(size) => updateParams({ pageSize: size, pageNumber: 1 })}
          />
        </CardContent>
      </Card>

      <Dialog open={detail !== null} onOpenChange={(open) => { if (!open) setDetail(null) }}>
        <DialogContent className="sm:max-w-4xl">
          <DialogHeader><DialogTitle>Chi tiết thay đổi</DialogTitle><DialogDescription>{detail ? `${detail.action} · ${detail.entityType} · ${detail.entityId}` : ''}</DialogDescription></DialogHeader>
          {detailError ? <p role="alert" className="text-sm text-destructive">{detailError}</p> : null}
          {detailLoading ? <p className="text-sm text-muted-foreground">Đang tải chi tiết...</p> : detail ? <><dl className="grid gap-2 text-sm sm:grid-cols-2"><div><dt className="text-muted-foreground">Correlation ID</dt><dd className="break-all font-mono">{detail.correlationId ?? '—'}</dd></div><div><dt className="text-muted-foreground">Địa chỉ IP</dt><dd className="font-mono">{detail.ipAddress ?? '—'}</dd></div></dl><AuditDiff item={detail} /></> : null}
        </DialogContent>
      </Dialog>
    </PageShell>
  )
}
