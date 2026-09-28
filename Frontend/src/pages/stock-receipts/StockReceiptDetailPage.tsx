import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { Plus, Save, Trash2 } from 'lucide-react'
import { PageShell, ScreenState, useToast } from '@/common/components'
import { Button } from '@/common/components/ui/button'
import { Input } from '@/common/components/ui/input'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/common/components/ui/dialog'
import { useAuth } from '@/auth/AuthProvider'
import { canAll } from '@/shared/auth/permissions'
import { getActiveSuppliers, type Supplier } from '@/pages/suppliers/supplier-api'
import { getLocations, type LocationNode } from '@/pages/branches/branch-api'
import { getBooks } from '@/pages/books/book-api'
import { createReceipt, getReceipt, updateReceipt, type ReceiptInput, type StockReceipt } from './receipt-api'
import { ConfirmReceiptPanel } from './ConfirmReceiptPanel'

type Line = { id?: string; bookId: string; title: string; isbn: string; expected: string;
  received: string; damaged: string; cost: string }
const blank = (): Line => ({ bookId: '', title: '', isbn: '', expected: '1', received: '0', damaged: '0', cost: '' })
const localDate = (value: string) => new Date(value).toISOString().slice(0, 16)
export function StockReceiptDetailPage() {
  const { id } = useParams()
  const creating = id === undefined
  const navigate = useNavigate()
  const { user } = useAuth()
  const { showToast } = useToast()
  const [receipt, setReceipt] = useState<StockReceipt | null>(null)
  const [suppliers, setSuppliers] = useState<Supplier[]>([])
  const [branches, setBranches] = useState<LocationNode[]>([])
  const [supplierId, setSupplierId] = useState('')
  const [branchId, setBranchId] = useState('')
  const [receivedAt, setReceivedAt] = useState(localDate(new Date().toISOString()))
  const [notes, setNotes] = useState('')
  const [lines, setLines] = useState<Line[]>([blank()])
  const [loading, setLoading] = useState(!creating)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [dirty, setDirty] = useState(false)
  const [lookupBusy, setLookupBusy] = useState<number | null>(null)
  const [formOpen, setFormOpen] = useState(creating)
  const editable = (creating || receipt?.status === 'Draft' || receipt?.status === 'Received') &&
    canAll(user?.permissions ?? [], [creating ? 'stock-receipts.create' : 'stock-receipts.update'])
  useEffect(() => { const controller = new AbortController()
    void Promise.all([getActiveSuppliers(controller.signal), getLocations(controller.signal)])
      .then(([supplierList, locations]) => { setSuppliers(supplierList); setBranches(locations.filter(x => x.type === 'Branch' && x.isActive)) })
      .catch(() => setError('Không thể tải nhà cung cấp hoặc chi nhánh đang hoạt động.'))
    if (id) void getReceipt(id, controller.signal).then(value => {
      setReceipt(value); setSupplierId(value.supplierId); setBranchId(value.branchId)
      setReceivedAt(localDate(value.receivedAtUtc)); setNotes(value.notes ?? '')
      setLines(value.items.map(row => ({ id: row.id, bookId: row.bookId, title: row.bookTitle,
        isbn: row.isbn, expected: String(row.expectedQuantity), received: String(row.receivedQuantity),
        damaged: String(row.damagedQuantity), cost: row.unitCost === null ? '' : String(row.unitCost) })))
    }).catch(reason => { if (controller.signal.aborted) return; setError(reason instanceof Error ? reason.message : 'Không thể tải phiếu nhập.') })
      .finally(() => { if (!controller.signal.aborted) setLoading(false) })
    return () => controller.abort()
  }, [id])
  useEffect(() => { const warn = (event: BeforeUnloadEvent) => { if (dirty) event.preventDefault() }
    window.addEventListener('beforeunload', warn); return () => window.removeEventListener('beforeunload', warn) }, [dirty])
  useEffect(() => {
    if (!dirty) return
    const confirmLink = (event: MouseEvent) => {
      const target = event.target
      if (!(target instanceof Element)) return
      const anchor = target.closest('a[href]')
      if (anchor instanceof HTMLAnchorElement && anchor.origin === window.location.origin && anchor.pathname !== window.location.pathname &&
          !window.confirm('Bỏ thay đổi chưa lưu?')) {
        event.preventDefault(); event.stopPropagation()
      }
    }
    document.addEventListener('click', confirmLink, true)
    return () => document.removeEventListener('click', confirmLink, true)
  }, [dirty])
  const changeLine = (index: number, patch: Partial<Line>) => { setLines(all => all.map((row, i) => i === index ? { ...row, ...patch } : row)); setDirty(true) }
  const totals = useMemo(() => lines.reduce((sum, row) => ({ quantity: sum.quantity + (Number(row.received) || 0),
    value: sum.value + (Number(row.received) || 0) * (Number(row.cost) || 0) }), { quantity: 0, value: 0 }), [lines])
  const lookup = async (index: number) => { const isbn = lines[index]?.isbn.trim(); if (!isbn) return
    setLookupBusy(index); setError('')
    try { const page = await getBooks({ search: isbn, pageNumber: 1, pageSize: 100, status: 'Active' })
      const found = page.items.find(book => book.isbn.replace(/[-\s]/g, '') === isbn.replace(/[-\s]/g, ''))
      if (!found) throw new Error('Không tìm thấy biểu ghi đang hoạt động với ISBN này.')
      changeLine(index, { bookId: found.id, title: found.title, isbn: found.isbn })
    } catch (reason) { setError(reason instanceof Error ? reason.message : 'Không thể tra cứu ISBN.') }
    finally { setLookupBusy(null) }
  }
  const save = async () => { setError('')
    const quantity = (value: string) => Number.isInteger(Number(value)) && Number(value) >= 0 && Number(value) <= 1_000_000
    if (!supplierId || !branchId || !receivedAt || lines.length === 0 || lines.some(row =>
      !row.bookId || !quantity(row.expected) || !quantity(row.received) || !quantity(row.damaged) ||
      Number(row.damaged) > Number(row.received) ||
      (row.cost !== '' && (!Number.isFinite(Number(row.cost)) || Number(row.cost) < 0)))) {
      setError('Chọn nhà cung cấp, chi nhánh và sách; kiểm tra số lượng, hỏng và đơn giá từng dòng.'); return
    }
    const input: ReceiptInput = { supplierId, branchId, receivedAtUtc: new Date(receivedAt).toISOString(),
      notes: notes.trim() || null, concurrencyToken: receipt?.concurrencyToken,
      items: lines.map(row => ({ id: row.id, bookId: row.bookId, expectedQuantity: Number(row.expected),
        receivedQuantity: Number(row.received), damagedQuantity: Number(row.damaged),
        unitCost: row.cost === '' ? null : Number(row.cost) })) }
    setSaving(true)
    try { const saved = creating ? await createReceipt(input) : await updateReceipt(id!, input)
      setDirty(false); showToast(creating ? 'Đã tạo phiếu nhập.' : 'Đã cập nhật phiếu nhập.')
      navigate(`/stock-receipts/${saved.id}`, { replace: true })
      if (!creating) { setFormOpen(false); setReceipt(saved); setLines(saved.items.map(row => ({ id: row.id, bookId: row.bookId,
        title: row.bookTitle, isbn: row.isbn, expected: String(row.expectedQuantity),
        received: String(row.receivedQuantity), damaged: String(row.damagedQuantity),
        cost: row.unitCost === null ? '' : String(row.unitCost) }))) }
    } catch (reason) { setError(reason instanceof Error ? reason.message : 'Không thể lưu phiếu nhập. Tải lại nếu dữ liệu đã thay đổi.') }
    finally { setSaving(false) }
  }
  if (loading) return <ScreenState kind="loading" title="Đang tải phiếu nhập" />
  return <PageShell eyebrow="Nhập kho" title={creating ? 'Tạo phiếu nhập' : receipt?.receiptNumber ?? 'Chi tiết phiếu nhập'}
    description={creating ? 'Chọn nhà cung cấp, chi nhánh và thêm sách theo ISBN.' : `Trạng thái: ${receipt?.status ?? 'Không xác định'}`}
    actions={<div className="flex gap-2"><Button variant="outline" asChild><Link to="/stock-receipts">Danh sách</Link></Button>{!creating && editable ? <Button onClick={() => setFormOpen(true)}><Save /> Sửa phiếu nhập</Button> : null}</div>}>
    <Dialog open={formOpen} onOpenChange={(open) => { if (saving) return; if (!open && dirty && !window.confirm('Bỏ thay đổi chưa lưu?')) return; if (!open && creating) navigate('/stock-receipts'); else setFormOpen(open) }}>
      <DialogContent className="max-h-[92dvh] overflow-y-auto sm:max-w-5xl">
      <DialogHeader><DialogTitle>{creating ? 'Tạo phiếu nhập' : `Sửa ${receipt?.receiptNumber ?? 'phiếu nhập'}`}</DialogTitle><DialogDescription>Các trường có dấu * là bắt buộc.</DialogDescription></DialogHeader>
    <form className="grid gap-5" noValidate onSubmit={(event) => { event.preventDefault(); void save() }}>
      <div className="grid gap-3 md:grid-cols-3">
        <label className="grid gap-1 text-sm"><span>Nhà cung cấp <span className="text-destructive" aria-hidden="true">*</span></span><select required aria-invalid={Boolean(error && !supplierId)} className="h-10 rounded-md border bg-background px-3" value={supplierId} disabled={!editable} onChange={e => { setSupplierId(e.target.value); setDirty(true); setError('') }}><option value="">Chọn nhà cung cấp</option>{receipt && !suppliers.some(x => x.id === receipt.supplierId) ? <option value={receipt.supplierId}>{receipt.supplierName} (ngừng hoạt động)</option> : null}{suppliers.map(x => <option key={x.id} value={x.id}>{x.code} · {x.name}</option>)}</select>{error && !supplierId ? <span className="text-sm text-destructive">Vui lòng chọn nhà cung cấp.</span> : null}</label>
        <label className="grid gap-1 text-sm"><span>Chi nhánh <span className="text-destructive" aria-hidden="true">*</span></span><select required aria-invalid={Boolean(error && !branchId)} className="h-10 rounded-md border bg-background px-3" value={branchId} disabled={!editable} onChange={e => { setBranchId(e.target.value); setDirty(true); setError('') }}><option value="">Chọn chi nhánh</option>{receipt && !branches.some(x => x.id === receipt.branchId) ? <option value={receipt.branchId}>{receipt.branchCode} (ngừng hoạt động)</option> : null}{branches.map(x => <option key={x.id} value={x.id}>{x.code} · {x.name}</option>)}</select>{error && !branchId ? <span className="text-sm text-destructive">Vui lòng chọn chi nhánh.</span> : null}</label>
        <label className="grid gap-1 text-sm" htmlFor="receipt-date"><span>Ngày nhập <span className="text-destructive" aria-hidden="true">*</span></span><Input id="receipt-date" required aria-invalid={Boolean(error && !receivedAt)} type="datetime-local" value={receivedAt} disabled={!editable} onChange={e => { setReceivedAt(e.target.value); setDirty(true); setError('') }} /></label>
      </div>
      <label className="grid gap-1 text-sm">Ghi chú <textarea className="min-h-20 rounded-md border bg-background p-2" maxLength={2000} value={notes} disabled={!editable} onChange={e => { setNotes(e.target.value); setDirty(true) }} /></label>
      <h2 className="text-lg font-semibold">Sách trong phiếu</h2>
      {lines.map((row, index) => <div className="grid gap-2 rounded-lg border p-3" key={row.id ?? index}>
        <div className="flex flex-wrap gap-2"><label htmlFor={`receipt-isbn-${index}`} className="grid flex-1 gap-1 text-sm"><span>ISBN <span className="text-destructive" aria-hidden="true">*</span></span><Input id={`receipt-isbn-${index}`} required aria-invalid={Boolean(error && !row.bookId)} className="max-w-sm" aria-label={`ISBN dòng ${index + 1}`} placeholder="ISBN" value={row.isbn} disabled={!editable} onChange={e => changeLine(index, { isbn: e.target.value, bookId: '', title: '' })} /></label>
          {editable ? <Button type="button" variant="outline" disabled={lookupBusy === index} onClick={() => void lookup(index)}>{lookupBusy === index ? 'Đang tìm...' : 'Tra ISBN'}</Button> : null}
          <span className="self-center text-sm">{row.title || (row.bookId ? row.bookId : 'Chưa chọn sách')}</span></div>
        <div className="grid gap-2 sm:grid-cols-2 lg:grid-cols-5">
          {([['expected', 'Dự kiến'], ['received', 'Thực nhận'], ['damaged', 'Hỏng'], ['cost', 'Đơn giá']] as const).map(([field, label]) => <label className="grid gap-1 text-sm" key={field}>{label}<Input type="number" min="0" max={field === 'cost' ? undefined : 1000000} step={field === 'cost' ? '0.01' : '1'} value={row[field]} disabled={!editable} onChange={e => changeLine(index, { [field]: e.target.value })} /></label>)}
          {editable ? <Button className="self-end" type="button" variant="outline" disabled={lines.length === 1} onClick={() => { setLines(all => all.filter((_, i) => i !== index)); setDirty(true) }}><Trash2 /> Xóa dòng</Button> : null}
        </div></div>)}
      {editable ? <Button type="button" variant="outline" className="w-fit" onClick={() => { setLines(all => [...all, blank()]); setDirty(true) }}><Plus /> Thêm dòng</Button> : null}
      <div className="flex flex-wrap justify-between gap-3 border-t pt-4"><p>Tổng thực nhận: <strong>{totals.quantity}</strong> · Tổng giá trị: <strong>{totals.value.toLocaleString('vi-VN')}</strong></p>
        </div>
      {error ? <p role="alert" className="text-sm text-destructive">{error}</p> : null}
      <DialogFooter>{editable ? <Button type="submit" loading={saving}><Save /> Lưu phiếu nhập</Button> : null}</DialogFooter>
    </form>
      </DialogContent>
    </Dialog>
    {receipt ? <div className="mt-5"><ConfirmReceiptPanel key={receipt.concurrencyToken} receipt={receipt} onConfirmed={setReceipt} /></div> : null}
  </PageShell>
}
