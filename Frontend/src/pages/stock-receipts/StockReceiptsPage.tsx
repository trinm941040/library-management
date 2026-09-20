import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { Plus, RefreshCw } from 'lucide-react'
import { DataTable, PageShell } from '@/common/components'
import { Button } from '@/common/components/ui/button'
import { Input } from '@/common/components/ui/input'
import { PermissionBoundary } from '@/shared/auth/PermissionBoundary'
import { getActiveSuppliers, type Supplier } from '@/pages/suppliers/supplier-api'
import { getLocations, type LocationNode } from '@/pages/branches/branch-api'
import { getReceipts, type ReceiptStatus, type StockReceipt } from './receipt-api'

export function StockReceiptsPage() {
  const [items, setItems] = useState<StockReceipt[]>([])
  const [suppliers, setSuppliers] = useState<Supplier[]>([])
  const [branches, setBranches] = useState<LocationNode[]>([])
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [supplierId, setSupplierId] = useState('')
  const [branchId, setBranchId] = useState('')
  const [status, setStatus] = useState<ReceiptStatus | ''>('')
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(20)
  const [total, setTotal] = useState(0)
  const [pages, setPages] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const load = useCallback((signal?: AbortSignal) => {
    setLoading(true); setError('')
    return getReceipts({ search, supplierId: supplierId || undefined, branchId: branchId || undefined,
      status: status || undefined, fromUtc: from ? `${from}T00:00:00Z` : undefined,
      toUtc: to ? `${to}T23:59:59Z` : undefined, pageNumber: page, pageSize }, signal)
      .then(result => { setItems(result.items); setTotal(result.totalCount); setPages(result.totalPages) })
      .catch((reason: unknown) => { if (reason instanceof DOMException && reason.name === 'AbortError') return
        setError(reason instanceof Error ? reason.message : 'Không thể tải phiếu nhập.') })
      .finally(() => { if (!signal?.aborted) setLoading(false) })
  }, [search, supplierId, branchId, status, from, to, page, pageSize])
  useEffect(() => { const controller = new AbortController(); void load(controller.signal); return () => controller.abort() }, [load])
  useEffect(() => { const controller = new AbortController()
    void Promise.all([getActiveSuppliers(controller.signal), getLocations(controller.signal)])
      .then(([supplierList, locations]) => { setSuppliers(supplierList); setBranches(locations.filter(node => node.type === 'Branch' && node.isActive)) })
      .catch(() => { /* Filters remain optional when lookup permission is unavailable. */ })
    return () => controller.abort() }, [])
  return <PageShell eyebrow="Nhập kho" title="Phiếu nhập sách" description="Tra cứu và cập nhật phiếu nhập."
    actions={<><Button variant="outline" onClick={() => void load()}><RefreshCw /> Làm mới</Button>
      <PermissionBoundary requiredPermissions={['stock-receipts.create']}><Button asChild><Link to="/stock-receipts/new"><Plus /> Tạo phiếu nhập</Link></Button></PermissionBoundary></>}>
    <DataTable caption="Danh sách phiếu nhập" rows={items} getRowId={row => row.id}
      isLoading={loading} error={error} onRetry={() => void load()}
      emptyTitle="Chưa có phiếu nhập" page={page} pageSize={pageSize} totalPages={pages} totalCount={total}
      onPageChange={setPage} onPageSizeChange={size => { setPageSize(size); setPage(1) }}
      filters={<div className="grid gap-2 sm:grid-cols-2 lg:grid-cols-4">
        <form className="flex gap-2" onSubmit={event => { event.preventDefault(); setPage(1); setSearch(searchInput.trim()) }}>
          <Input aria-label="Tìm số phiếu" placeholder="Số phiếu hoặc tên nhà cung cấp" value={searchInput} onChange={event => setSearchInput(event.target.value)} />
          <Button type="submit" variant="outline">Tìm</Button></form>
        <select className="rounded-md border bg-background px-3" aria-label="Nhà cung cấp" value={supplierId} onChange={event => { setSupplierId(event.target.value); setPage(1) }}><option value="">Tất cả nhà cung cấp</option>{suppliers.map(x => <option key={x.id} value={x.id}>{x.name}</option>)}</select>
        <select className="rounded-md border bg-background px-3" aria-label="Chi nhánh" value={branchId} onChange={event => { setBranchId(event.target.value); setPage(1) }}><option value="">Tất cả chi nhánh</option>{branches.map(x => <option key={x.id} value={x.id}>{x.code} · {x.name}</option>)}</select>
        <select className="rounded-md border bg-background px-3" aria-label="Trạng thái" value={status} onChange={event => { setStatus(event.target.value as ReceiptStatus | ''); setPage(1) }}><option value="">Tất cả trạng thái</option>{(['Draft', 'Received', 'Confirmed', 'Cancelled'] as const).map(x => <option key={x} value={x}>{x}</option>)}</select>
        <label className="text-sm" htmlFor="receipt-from">Từ ngày <Input id="receipt-from" type="date" value={from} onChange={event => { setFrom(event.target.value); setPage(1) }} /></label>
        <label className="text-sm" htmlFor="receipt-to">Đến ngày <Input id="receipt-to" type="date" value={to} onChange={event => { setTo(event.target.value); setPage(1) }} /></label>
      </div>}
      columns={[
        { id: 'number', header: 'Số phiếu', cell: row => <Link className="font-medium text-primary underline" to={`/stock-receipts/${row.id}`}>{row.receiptNumber}</Link> },
        { id: 'supplier', header: 'Nhà cung cấp', cell: row => row.supplierName },
        { id: 'branch', header: 'Chi nhánh', cell: row => row.branchCode },
        { id: 'date', header: 'Ngày nhập', cell: row => new Date(row.receivedAtUtc).toLocaleDateString('vi-VN') },
        { id: 'status', header: 'Trạng thái', cell: row => row.status },
        { id: 'quantity', header: 'Số lượng', cell: row => row.totalQuantity },
        { id: 'value', header: 'Thành tiền', cell: row => row.totalValue.toLocaleString('vi-VN') },
      ]} />
  </PageShell>
}
