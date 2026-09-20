import { useCallback, useEffect, useState } from 'react'
import { Pencil, Plus, RefreshCw } from 'lucide-react'
import { ConfirmDialog, DataTable, PageShell, ScreenState, StatusBadge, useToast } from '@/common/components'
import { Button } from '@/common/components/ui/button'
import { Input } from '@/common/components/ui/input'
import { PermissionBoundary } from '@/shared/auth/PermissionBoundary'
import {
  changeSupplierStatus, createSupplier, getSuppliers, updateSupplier,
  type Supplier, type SupplierInput, type SupplierStatus,
} from './supplier-api'

const empty: SupplierInput = { code: '', name: '', contactName: '', email: '', phoneNumber: '', address: '' }

export function SuppliersPage() {
  const [items, setItems] = useState<Supplier[]>([])
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState<SupplierStatus | ''>('')
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(20)
  const [total, setTotal] = useState(0)
  const [pages, setPages] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [selected, setSelected] = useState<Supplier | null>(null)
  const [editing, setEditing] = useState<Supplier | null>(null)
  const [formOpen, setFormOpen] = useState(false)
  const [form, setForm] = useState<SupplierInput>(empty)
  const [actionError, setActionError] = useState('')
  const [pending, setPending] = useState(false)
  const [changingStatus, setChangingStatus] = useState<Supplier | null>(null)
  const { showToast } = useToast()

  const load = useCallback((signal?: AbortSignal) => {
    setLoading(true)
    setError('')
    return getSuppliers({ search, status: status || undefined, pageNumber: page, pageSize }, signal)
      .then((result) => {
        setItems(result.items); setTotal(result.totalCount); setPages(result.totalPages)
      })
      .catch((reason: unknown) => {
        if (reason instanceof DOMException && reason.name === 'AbortError') return
        setError(reason instanceof Error ? reason.message : 'Không thể tải nhà cung cấp.')
      })
      .finally(() => { if (!signal?.aborted) setLoading(false) })
  }, [search, status, page, pageSize])

  useEffect(() => {
    const controller = new AbortController()
    void load(controller.signal)
    return () => controller.abort()
  }, [load])

  const openForm = (supplier?: Supplier) => {
    setEditing(supplier ?? null)
    setForm(supplier ? {
      code: supplier.code, name: supplier.name, contactName: supplier.contactName,
      email: supplier.email, phoneNumber: supplier.phoneNumber, address: supplier.address,
    } : empty)
    setActionError('')
    setFormOpen(true)
  }

  const save = async () => {
    setPending(true); setActionError('')
    try {
      const saved = editing
        ? await updateSupplier(editing.id, { ...form, concurrencyToken: editing.concurrencyToken })
        : await createSupplier(form)
      setFormOpen(false); setSelected(saved); await load()
      showToast(editing ? 'Đã cập nhật nhà cung cấp.' : 'Đã tạo nhà cung cấp.')
    } catch (reason) {
      setActionError(reason instanceof Error ? reason.message : 'Không thể lưu nhà cung cấp.')
    } finally { setPending(false) }
  }

  const confirmStatus = async () => {
    if (!changingStatus) return
    setPending(true); setActionError('')
    try {
      const updated = await changeSupplierStatus(changingStatus,
        changingStatus.status === 'Active' ? 'Inactive' : 'Active')
      setChangingStatus(null); setSelected(updated); await load()
      showToast(updated.status === 'Active' ? 'Đã kích hoạt nhà cung cấp.' : 'Đã ngừng hoạt động nhà cung cấp.')
    } catch (reason) {
      setActionError(reason instanceof Error ? reason.message : 'Không thể đổi trạng thái nhà cung cấp.')
    } finally { setPending(false) }
  }

  return <PageShell eyebrow="Nhập kho" title="Nhà cung cấp"
    description="Quản lý mã, liên hệ và trạng thái nhà cung cấp."
    actions={<div className="flex gap-2">
      <Button variant="outline" onClick={() => void load()}><RefreshCw /> Làm mới</Button>
      <PermissionBoundary requiredPermissions={['suppliers.create']}>
        <Button onClick={() => openForm()}><Plus /> Thêm nhà cung cấp</Button>
      </PermissionBoundary>
    </div>}>
    <div className="grid gap-3">
      <div className="flex flex-wrap gap-2">
        <Input className="min-w-56 flex-1" aria-label="Tìm nhà cung cấp" placeholder="Mã, tên, liên hệ, email hoặc điện thoại"
          value={searchInput} onChange={(event) => setSearchInput(event.target.value)}
          onKeyDown={(event) => { if (event.key === 'Enter') { setPage(1); setSearch(searchInput.trim()) } }} />
        <select className="h-10 rounded-md border bg-background px-3" aria-label="Lọc trạng thái nhà cung cấp"
          value={status} onChange={(event) => { setPage(1); setStatus(event.target.value as SupplierStatus | '') }}>
          <option value="">Mọi trạng thái</option><option value="Active">Đang hoạt động</option>
          <option value="Inactive">Ngừng hoạt động</option>
        </select>
        <Button variant="outline" onClick={() => { setPage(1); setSearch(searchInput.trim()) }}>Tìm kiếm</Button>
      </div>
      <DataTable caption="Danh sách nhà cung cấp" rows={items} getRowId={(item) => item.id}
        isLoading={loading} error={error} onRetry={() => void load()} emptyTitle="Chưa có nhà cung cấp phù hợp"
        page={page} pageSize={pageSize} totalPages={pages} totalCount={total}
        onPageChange={setPage} onPageSizeChange={(value) => { setPage(1); setPageSize(value) }}
        columns={[
          { id: 'code', header: 'Mã', cell: (item) => <button className="font-medium text-primary underline" onClick={() => setSelected(item)}>{item.code}</button> },
          { id: 'name', header: 'Tên', cell: (item) => item.name },
          { id: 'contact', header: 'Liên hệ', cell: (item) => item.contactName ?? '—' },
          { id: 'email', header: 'Email', cell: (item) => item.email ?? '—' },
          { id: 'phone', header: 'Điện thoại', cell: (item) => item.phoneNumber ?? '—' },
          { id: 'status', header: 'Trạng thái', cell: (item) => <StatusBadge label={item.status === 'Active' ? 'Đang hoạt động' : 'Ngừng hoạt động'} tone={item.status === 'Active' ? 'success' : 'neutral'} /> },
          { id: 'actions', header: 'Thao tác', cell: (item) => <div className="flex gap-2">
            <PermissionBoundary requiredPermissions={['suppliers.update']}>
              <Button variant="outline" size="sm" onClick={() => openForm(item)}><Pencil /> Sửa</Button>
            </PermissionBoundary>
            <PermissionBoundary requiredPermissions={[item.status === 'Active' ? 'suppliers.deactivate' : 'suppliers.update']}>
              <Button variant="outline" size="sm" onClick={() => { setActionError(''); setChangingStatus(item) }}>
                {item.status === 'Active' ? 'Ngừng' : 'Kích hoạt'}
              </Button>
            </PermissionBoundary>
          </div> },
        ]} />
    </div>
    {selected ? <section className="mt-4 rounded-xl border bg-card p-4" aria-label="Chi tiết nhà cung cấp">
      <div className="flex items-start justify-between gap-3"><h2 className="font-semibold">{selected.code} · {selected.name}</h2><Button variant="outline" onClick={() => setSelected(null)}>Đóng</Button></div>
      <p className="text-sm">Liên hệ: {selected.contactName ?? '—'} · {selected.email ?? '—'} · {selected.phoneNumber ?? '—'}</p>
      <p className="text-sm">Địa chỉ: {selected.address ?? '—'}</p>
      <p className="text-sm">{selected.hasStockReceipts ? 'Có lịch sử phiếu nhập; dữ liệu được giữ khi ngừng hoạt động.' : 'Chưa có phiếu nhập.'}</p>
    </section> : null}
    {formOpen ? <section className="mt-4 grid gap-3 rounded-xl border bg-card p-4 md:grid-cols-2" aria-label={editing ? 'Sửa nhà cung cấp' : 'Tạo nhà cung cấp'}>
      <h2 className="font-semibold md:col-span-2">{editing ? 'Sửa nhà cung cấp' : 'Tạo nhà cung cấp'}</h2>
      <Input aria-label="Mã nhà cung cấp" placeholder="Mã nhà cung cấp" maxLength={30} value={form.code} onChange={(event) => setForm({ ...form, code: event.target.value })} />
      <Input aria-label="Tên nhà cung cấp" placeholder="Tên nhà cung cấp" maxLength={200} value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} />
      <Input aria-label="Người liên hệ" placeholder="Người liên hệ" maxLength={150} value={form.contactName ?? ''} onChange={(event) => setForm({ ...form, contactName: event.target.value })} />
      <Input aria-label="Email" type="email" placeholder="Email" maxLength={256} value={form.email ?? ''} onChange={(event) => setForm({ ...form, email: event.target.value })} />
      <Input aria-label="Số điện thoại" placeholder="Số điện thoại" maxLength={32} value={form.phoneNumber ?? ''} onChange={(event) => setForm({ ...form, phoneNumber: event.target.value })} />
      <Input aria-label="Địa chỉ" placeholder="Địa chỉ" maxLength={500} value={form.address ?? ''} onChange={(event) => setForm({ ...form, address: event.target.value })} />
      {actionError ? <div className="md:col-span-2"><ScreenState kind="error" title="Không thể lưu" description={actionError} /></div> : null}
      <div className="flex gap-2 md:col-span-2"><Button loading={pending} disabled={!form.code.trim() || !form.name.trim()} onClick={() => void save()}>Lưu</Button>
        <Button variant="outline" disabled={pending} onClick={() => setFormOpen(false)}>Hủy</Button></div>
    </section> : null}
    <ConfirmDialog open={changingStatus !== null} title="Đổi trạng thái nhà cung cấp"
      description={changingStatus?.status === 'Active' ? 'Nhà cung cấp sẽ không xuất hiện trong danh sách chọn cho phiếu nhập mới. Phiếu nhập cũ vẫn được giữ.' : 'Nhà cung cấp sẽ được chọn lại cho phiếu nhập mới.'}
      confirmLabel={changingStatus?.status === 'Active' ? 'Ngừng hoạt động' : 'Kích hoạt'}
      destructive={changingStatus?.status === 'Active'} isPending={pending} error={actionError}
      onConfirm={() => void confirmStatus()} onOpenChange={(open) => { if (!open) setChangingStatus(null) }} />
  </PageShell>
}
