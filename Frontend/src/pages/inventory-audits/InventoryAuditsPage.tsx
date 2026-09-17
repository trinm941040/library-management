import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Plus, RefreshCw } from 'lucide-react'
import { DataTable, PageShell } from '@/common/components'
import { Button } from '@/common/components/ui/button'
import { Input } from '@/common/components/ui/input'
import { PermissionBoundary } from '@/shared/auth/PermissionBoundary'
import type { LocationNode } from '@/pages/branches/branch-api'
import { createAudit, getAuditLocations, getAudits, type AuditStatus, type InventoryAudit } from './inventory-audit-api'

export function InventoryAuditsPage() {
  const navigate = useNavigate()
  const [items, setItems] = useState<InventoryAudit[]>([])
  const [branches, setBranches] = useState<LocationNode[]>([])
  const [branchId, setBranchId] = useState('')
  const [areaId, setAreaId] = useState('')
  const [shelfId, setShelfId] = useState('')
  const [notes, setNotes] = useState('')
  const [status, setStatus] = useState<AuditStatus | ''>('')
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(20)
  const [total, setTotal] = useState(0)
  const [pages, setPages] = useState(0)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const currentBranch = branches.find(node => node.id === branchId)
  const areas = currentBranch?.children.filter(node => node.type === 'Area' && node.isActive) ?? []
  const shelves = areas.find(node => node.id === areaId)?.children.filter(node => node.type === 'Shelf' && node.isActive) ?? []
  const load = useCallback((signal?: AbortSignal) => { setLoading(true); setError('')
    return getAudits({ branchId: branchId || undefined, status: status || undefined,
      pageNumber: page, pageSize }, signal)
      .then(result => { setItems(result.items); setTotal(result.totalCount); setPages(result.totalPages) })
      .catch((reason: unknown) => { if (reason instanceof DOMException && reason.name === 'AbortError') return
        setError(reason instanceof Error ? reason.message : 'Không thể tải đợt kiểm kê.') })
      .finally(() => { if (!signal?.aborted) setLoading(false) })
  }, [branchId, status, page, pageSize])
  useEffect(() => { const controller = new AbortController(); void load(controller.signal); return () => controller.abort() }, [load])
  useEffect(() => { const controller = new AbortController()
    void getAuditLocations(controller.signal).then(nodes => setBranches(nodes.filter(node => node.type === 'Branch' && node.isActive)))
      .catch(() => setError('Không thể tải vị trí thư viện.'))
    return () => controller.abort() }, [])
  const create = async () => { if (!branchId) { setError('Chọn chi nhánh trước khi bắt đầu.'); return }
    setSaving(true); setError('')
    try { const audit = await createAudit({ branchId, areaId: areaId || null,
      shelfId: shelfId || null, notes: notes.trim() || null }); navigate(`/inventory-audits/${audit.id}`) }
    catch (reason) { setError(reason instanceof Error ? reason.message : 'Không thể tạo đợt kiểm kê.') }
    finally { setSaving(false) }
  }
  return <PageShell eyebrow="Kho vật lý" title="Kiểm kê" description="Tạo snapshot, quét mã vạch và đối soát bản sao."
    actions={<Button variant="outline" onClick={() => void load()}><RefreshCw /> Làm mới</Button>}>
    <PermissionBoundary requiredPermissions={['inventory-audits.create']}>
      <section className="mb-6 grid gap-3 rounded-xl border bg-card p-4" aria-label="Tạo đợt kiểm kê">
        <h2 className="text-lg font-semibold">Tạo đợt mới</h2>
        <div className="grid gap-2 md:grid-cols-3">
          <label className="grid gap-1 text-sm">Chi nhánh<select className="h-10 rounded-md border bg-background px-3" value={branchId} onChange={event => { setBranchId(event.target.value); setAreaId(''); setShelfId(''); setPage(1) }}><option value="">Chọn chi nhánh</option>{branches.map(node => <option key={node.id} value={node.id}>{node.code} · {node.name}</option>)}</select></label>
          <label className="grid gap-1 text-sm">Khu vực<select className="h-10 rounded-md border bg-background px-3" value={areaId} onChange={event => { setAreaId(event.target.value); setShelfId('') }}><option value="">Toàn chi nhánh</option>{areas.map(node => <option key={node.id} value={node.id}>{node.code} · {node.name}</option>)}</select></label>
          <label className="grid gap-1 text-sm">Kệ<select className="h-10 rounded-md border bg-background px-3" value={shelfId} onChange={event => setShelfId(event.target.value)}><option value="">Toàn khu vực</option>{shelves.map(node => <option key={node.id} value={node.id}>{node.code} · {node.name}</option>)}</select></label>
        </div>
        <label className="grid gap-1 text-sm" htmlFor="audit-notes">Ghi chú <Input id="audit-notes" maxLength={2000} value={notes} onChange={event => setNotes(event.target.value)} /></label>
        <Button className="w-fit" disabled={!branchId || saving} onClick={() => void create()}><Plus /> {saving ? 'Đang tạo...' : 'Bắt đầu kiểm kê'}</Button>
      </section>
    </PermissionBoundary>
    <DataTable caption="Danh sách đợt kiểm kê" rows={items} getRowId={item => item.id}
      isLoading={loading} error={error} onRetry={() => void load()}
      page={page} pageSize={pageSize} totalPages={pages} totalCount={total}
      onPageChange={setPage} onPageSizeChange={size => { setPageSize(size); setPage(1) }}
      filters={<div className="flex flex-wrap gap-2"><select aria-label="Lọc trạng thái" className="h-10 rounded-md border bg-background px-3" value={status} onChange={event => { setStatus(event.target.value as AuditStatus | ''); setPage(1) }}><option value="">Tất cả trạng thái</option>{(['InProgress', 'Completed', 'Cancelled'] as const).map(value => <option key={value} value={value}>{value}</option>)}</select></div>}
      columns={[
        { id: 'id', header: 'Đợt kiểm kê', cell: row => <Link className="text-primary underline" to={`/inventory-audits/${row.id}`}>{row.id.slice(0, 8)}</Link> },
        { id: 'scope', header: 'Phạm vi', cell: row => row.shelfId ? 'Kệ' : row.areaId ? 'Khu vực' : 'Chi nhánh' },
        { id: 'started', header: 'Bắt đầu', cell: row => new Date(row.startedAtUtc).toLocaleString('vi-VN') },
        { id: 'status', header: 'Trạng thái', cell: row => row.status === 'InProgress' ? 'Đang kiểm kê' : row.status === 'Completed' ? 'Đã hoàn tất' : row.status },
        { id: 'progress', header: 'Tiến độ', cell: row => `${row.scannedCount}/${row.expectedCount}` },
        { id: 'differences', header: 'Chênh lệch', cell: row => row.discrepancyCount },
      ]} />
  </PageShell>
}
