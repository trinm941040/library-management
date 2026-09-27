import { useCallback, useEffect, useState } from 'react'
import { Pencil, Plus, RefreshCw } from 'lucide-react'
import { ConfirmDialog, DataTable, PageShell, StatusBadge, useToast } from '@/common/components'
import { Button } from '@/common/components/ui/button'
import { Input } from '@/common/components/ui/input'
import { Label } from '@/common/components/ui/label'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/common/components/ui/dialog'
import { PermissionBoundary } from '@/shared/auth/PermissionBoundary'
import {
  changeSupplierStatus, createSupplier, getSuppliers, updateSupplier,
  type Supplier, type SupplierInput, type SupplierStatus,
} from './supplier-api'
import { readUrlFilter, readUrlPage, useFilterUrlSync } from '@/shared/data/use-filter-url-sync'

const empty: SupplierInput = { code: '', name: '', contactName: '', email: '', phoneNumber: '', address: '' }

export function SuppliersPage() {
  const [items, setItems] = useState<Supplier[]>([])
  const [searchInput, setSearchInput] = useState(() => readUrlFilter('search'))
  const [search, setSearch] = useState(() => readUrlFilter('search'))
  const [status, setStatus] = useState<SupplierStatus | ''>(() => readUrlFilter('status') as SupplierStatus | '')
  const [page, setPage] = useState(() => readUrlPage('pageNumber', 1))
  const [pageSize, setPageSize] = useState(() => readUrlPage('pageSize', 20))
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
  const [formErrors, setFormErrors] = useState<{ code?: string; name?: string; email?: string }>({})
  const { showToast } = useToast()
  useFilterUrlSync({ search, status, pageNumber: page, pageSize })

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
    const nextErrors = {
      code: form.code.trim() ? undefined : 'Vui lòng nhập mã nhà cung cấp.',
      name: form.name.trim() ? undefined : 'Vui lòng nhập tên nhà cung cấp.',
      email: form.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email) ? 'Email không hợp lệ.' : undefined,
    }
    setFormErrors(nextErrors)
    if (Object.values(nextErrors).some(Boolean)) return
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
      <form className="flex flex-wrap gap-2" onSubmit={(event) => {
        event.preventDefault()
        const value = new FormData(event.currentTarget).get('search')?.toString().trim() ?? ''
        setSearchInput(value)
        setSearch(value)
        setPage(1)
      }}>
        <Input className="min-w-56 flex-1" aria-label="Tìm nhà cung cấp" placeholder="Mã, tên, liên hệ, email hoặc điện thoại"
          name="search"
          value={searchInput} onChange={(event) => setSearchInput(event.target.value)}
        />
        <select className="h-10 rounded-md border bg-background px-3" aria-label="Lọc trạng thái nhà cung cấp"
          value={status} onChange={(event) => { setPage(1); setStatus(event.target.value as SupplierStatus | '') }}>
          <option value="">Mọi trạng thái</option><option value="Active">Đang hoạt động</option>
          <option value="Inactive">Ngừng hoạt động</option>
        </select>
        <Button type="submit" variant="outline">Tìm kiếm</Button>
      </form>
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
    <Dialog open={formOpen} onOpenChange={(open) => { if (!pending) { setFormOpen(open); if (!open) { setFormErrors({}); setActionError('') } } }}>
      <DialogContent className="max-h-[90dvh] overflow-y-auto sm:max-w-2xl">
        <DialogHeader><DialogTitle>{editing ? 'Sửa nhà cung cấp' : 'Tạo nhà cung cấp'}</DialogTitle><DialogDescription>Nhập thông tin nhà cung cấp. Các trường có dấu * là bắt buộc.</DialogDescription></DialogHeader>
        <form className="grid gap-4 sm:grid-cols-2" noValidate onSubmit={(event) => { event.preventDefault(); void save() }}>
          <div className="grid gap-2"><Label htmlFor="supplier-code">Mã nhà cung cấp <span className="text-destructive">*</span></Label><Input id="supplier-code" required aria-invalid={Boolean(formErrors.code)} aria-describedby={formErrors.code ? 'supplier-code-error' : undefined} maxLength={30} value={form.code} onChange={(event) => { setForm({ ...form, code: event.target.value }); setFormErrors((value) => ({ ...value, code: undefined })) }} />{formErrors.code ? <p id="supplier-code-error" role="alert" className="text-sm text-destructive">{formErrors.code}</p> : null}</div>
          <div className="grid gap-2"><Label htmlFor="supplier-name">Tên nhà cung cấp <span className="text-destructive">*</span></Label><Input id="supplier-name" required aria-invalid={Boolean(formErrors.name)} aria-describedby={formErrors.name ? 'supplier-name-error' : undefined} maxLength={200} value={form.name} onChange={(event) => { setForm({ ...form, name: event.target.value }); setFormErrors((value) => ({ ...value, name: undefined })) }} />{formErrors.name ? <p id="supplier-name-error" role="alert" className="text-sm text-destructive">{formErrors.name}</p> : null}</div>
          <div className="grid gap-2"><Label htmlFor="supplier-contact">Người liên hệ</Label><Input id="supplier-contact" maxLength={150} value={form.contactName ?? ''} onChange={(event) => setForm({ ...form, contactName: event.target.value })} /></div>
          <div className="grid gap-2"><Label htmlFor="supplier-email">Email</Label><Input id="supplier-email" type="email" aria-invalid={Boolean(formErrors.email)} aria-describedby={formErrors.email ? 'supplier-email-error' : undefined} maxLength={256} value={form.email ?? ''} onChange={(event) => { setForm({ ...form, email: event.target.value }); setFormErrors((value) => ({ ...value, email: undefined })) }} />{formErrors.email ? <p id="supplier-email-error" role="alert" className="text-sm text-destructive">{formErrors.email}</p> : null}</div>
          <div className="grid gap-2"><Label htmlFor="supplier-phone">Số điện thoại</Label><Input id="supplier-phone" maxLength={32} value={form.phoneNumber ?? ''} onChange={(event) => setForm({ ...form, phoneNumber: event.target.value })} /></div>
          <div className="grid gap-2"><Label htmlFor="supplier-address">Địa chỉ</Label><Input id="supplier-address" maxLength={500} value={form.address ?? ''} onChange={(event) => setForm({ ...form, address: event.target.value })} /></div>
          {actionError ? <p className="text-sm text-destructive sm:col-span-2" role="alert">{actionError}</p> : null}
          <DialogFooter className="sm:col-span-2"><Button type="button" variant="outline" disabled={pending} onClick={() => setFormOpen(false)}>Hủy</Button><Button type="submit" loading={pending}>Lưu</Button></DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
    <ConfirmDialog open={changingStatus !== null} title="Đổi trạng thái nhà cung cấp"
      description={changingStatus?.status === 'Active' ? 'Nhà cung cấp sẽ không xuất hiện trong danh sách chọn cho phiếu nhập mới. Phiếu nhập cũ vẫn được giữ.' : 'Nhà cung cấp sẽ được chọn lại cho phiếu nhập mới.'}
      confirmLabel={changingStatus?.status === 'Active' ? 'Ngừng hoạt động' : 'Kích hoạt'}
      destructive={changingStatus?.status === 'Active'} isPending={pending} error={actionError}
      onConfirm={() => void confirmStatus()} onOpenChange={(open) => { if (!open) setChangingStatus(null) }} />
  </PageShell>
}
