import { useCallback, useEffect, useState } from 'react'
import { Barcode, Plus, RefreshCw } from 'lucide-react'
import { BarcodeInput, ConfirmDialog, DataTable, PageShell, ScreenState, StatusBadge, useToast } from '@/common/components'
import { Button } from '@/common/components/ui/button'
import { Input } from '@/common/components/ui/input'
import { PermissionBoundary } from '@/shared/auth/PermissionBoundary'
import { getBooks, type LibraryBook } from '@/pages/books/book-api'
import {
  createCopy, getActiveShelves, getCopies, getCopyByBarcode, relocateCopy,
  updateCopyStatus, type ActiveShelf, type BookCopy, type CopyCondition, type CopyStatus,
} from './copy-api'

const statuses: CopyStatus[] = ['Available', 'Borrowed', 'Reserved', 'InTransit', 'Lost', 'Damaged', 'Withdrawn']
const conditions: CopyCondition[] = ['New', 'Good', 'Worn', 'Damaged', 'Lost']
const statusLabels: Record<CopyStatus, string> = {
  Available: 'Sẵn sàng', Borrowed: 'Đang mượn', Reserved: 'Đã giữ chỗ',
  InTransit: 'Đang chuyển', Lost: 'Mất', Damaged: 'Hư hỏng', Withdrawn: 'Thanh lý',
}
type Action = { copy: BookCopy; status?: CopyStatus; shelfId?: string }

export function CopiesPage() {
  const [copies, setCopies] = useState<BookCopy[]>([])
  const [total, setTotal] = useState(0)
  const [pages, setPages] = useState(0)
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(20)
  const [search, setSearch] = useState('')
  const [barcode, setBarcode] = useState('')
  const [status, setStatus] = useState<CopyStatus | ''>('')
  const [condition, setCondition] = useState<CopyCondition | ''>('')
  const [shelfId, setShelfId] = useState('')
  const [shelves, setShelves] = useState<ActiveShelf[]>([])
  const [books, setBooks] = useState<LibraryBook[]>([])
  const [bookSearch, setBookSearch] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [selected, setSelected] = useState<BookCopy | null>(null)
  const [creating, setCreating] = useState(false)
  const [createBookId, setCreateBookId] = useState('')
  const [createBarcode, setCreateBarcode] = useState('')
  const [createShelfId, setCreateShelfId] = useState('')
  const [createCondition, setCreateCondition] = useState<CopyCondition>('Good')
  const [targetStatus, setTargetStatus] = useState<CopyStatus>('Available')
  const [targetShelfId, setTargetShelfId] = useState('')
  const [action, setAction] = useState<Action | null>(null)
  const [actionError, setActionError] = useState('')
  const [pending, setPending] = useState(false)
  const { showToast } = useToast()

  const load = useCallback((signal?: AbortSignal) => {
    setLoading(true)
    setError('')
    return getCopies({ search, status: status || undefined, condition: condition || undefined,
      shelfId: shelfId || undefined, pageNumber: page, pageSize }, signal)
      .then((result) => { setCopies(result.items); setTotal(result.totalCount); setPages(result.totalPages) })
      .catch((reason: unknown) => {
        if (reason instanceof DOMException && reason.name === 'AbortError') return
        setError(reason instanceof Error ? reason.message : 'Không thể tải bản sao.')
      })
      .finally(() => { if (!signal?.aborted) setLoading(false) })
  }, [search, status, condition, shelfId, page, pageSize])

  useEffect(() => {
    const controller = new AbortController()
    void load(controller.signal)
    return () => controller.abort()
  }, [load])
  useEffect(() => {
    const controller = new AbortController()
    void getActiveShelves(controller.signal).then(setShelves).catch(() => undefined)
    return () => controller.abort()
  }, [])
  useEffect(() => {
    const controller = new AbortController()
    void getBooks({ search: bookSearch || undefined, pageNumber: 1, pageSize: 100 }, controller.signal)
      .then((result) => setBooks(result.items.filter((book) => book.status === 'Active')))
      .catch(() => undefined)
    return () => controller.abort()
  }, [bookSearch])

  const lookup = async (value: string) => {
    if (!value.trim()) return
    setError(''); setLoading(true)
    try { const copy = await getCopyByBarcode(value.trim()); setSelected(copy); setCopies([copy]); setTotal(1); setPages(1) }
    catch (reason) { setError(reason instanceof Error ? reason.message : 'Không tìm thấy mã vạch.') }
    finally { setLoading(false) }
  }

  const saveCopy = async () => {
    setActionError(''); setPending(true)
    try {
      const copy = await createCopy({ bookId: createBookId, barcode: createBarcode,
        condition: createCondition, shelfId: createShelfId })
      setCreating(false); setSelected(copy); setCreateBarcode('')
      await load(); showToast('Đã tạo bản sao.')
    } catch (reason) { setActionError(reason instanceof Error ? reason.message : 'Không thể tạo bản sao.') }
    finally { setPending(false) }
  }

  const confirm = async () => {
    if (!action) return
    setPending(true); setActionError('')
    try {
      const updated = action.status
        ? await updateCopyStatus(action.copy, action.status)
        : await relocateCopy(action.copy, action.shelfId!)
      setSelected(updated); setAction(null); await load(); showToast('Đã cập nhật bản sao.')
    } catch (reason) { setActionError(reason instanceof Error ? reason.message : 'Không thể cập nhật bản sao.') }
    finally { setPending(false) }
  }

  return <PageShell eyebrow="Kho vật lý" title="Bản sao và mã vạch"
    description="Tra cứu, đổi trạng thái và vị trí của từng bản sao vật lý."
    actions={<div className="flex gap-2"><Button variant="outline" onClick={() => void load()}><RefreshCw /> Làm mới</Button>
      <PermissionBoundary requiredPermissions={['copies.create']}><Button onClick={() => setCreating(true)}><Plus /> Tạo bản sao</Button></PermissionBoundary></div>}>
    <div className="grid gap-4 rounded-xl border bg-card p-4">
      <div className="flex gap-2"><BarcodeInput value={barcode} onChange={setBarcode} onScan={(value) => void lookup(value)}
        aria-label="Quét hoặc nhập mã vạch" placeholder="Quét mã vạch rồi nhấn Enter" />
        <Button onClick={() => void lookup(barcode)}><Barcode /> Tra cứu</Button></div>
      <div className="grid gap-2 sm:grid-cols-3">
        <Input value={search} onChange={(event) => { setPage(1); setSearch(event.target.value) }} placeholder="Tên sách hoặc mã vạch" aria-label="Tìm bản sao" />
        <select className="h-10 rounded-md border bg-background px-3" value={status} aria-label="Lọc trạng thái" onChange={(event) => { setPage(1); setStatus(event.target.value as CopyStatus | '') }}>
          <option value="">Mọi trạng thái</option>{statuses.map((item) => <option key={item} value={item}>{statusLabels[item]}</option>)}
        </select>
        <select className="h-10 rounded-md border bg-background px-3" value={condition} aria-label="Lọc tình trạng" onChange={(event) => { setPage(1); setCondition(event.target.value as CopyCondition | '') }}>
          <option value="">Mọi tình trạng</option>{conditions.map((item) => <option key={item} value={item}>{item}</option>)}
        </select>
        <select className="h-10 rounded-md border bg-background px-3 sm:col-span-3" value={shelfId} aria-label="Lọc kệ" onChange={(event) => { setPage(1); setShelfId(event.target.value) }}>
          <option value="">Mọi kệ</option>{shelves.map((shelf) => <option key={shelf.id} value={shelf.id}>{shelf.branchCode} / {shelf.areaCode} / {shelf.code}</option>)}
        </select>
      </div>
      <DataTable caption="Danh sách bản sao" rows={copies} getRowId={(copy) => copy.id}
        isLoading={loading} error={error} onRetry={() => void load()} emptyTitle="Chưa có bản sao phù hợp"
        page={page} pageSize={pageSize} totalPages={pages} totalCount={total}
        onPageChange={setPage} onPageSizeChange={(value) => { setPage(1); setPageSize(value) }}
        columns={[
          { id: 'barcode', header: 'Mã vạch', cell: (copy) => <button className="font-mono text-primary underline" onClick={() => setSelected(copy)}>{copy.barcode}</button> },
          { id: 'book', header: 'Sách', cell: (copy) => copy.bookTitle },
          { id: 'location', header: 'Vị trí', cell: (copy) => `${copy.branchCode ?? '—'} / ${copy.shelfCode ?? '—'}` },
          { id: 'condition', header: 'Tình trạng', cell: (copy) => copy.condition },
          { id: 'status', header: 'Trạng thái', cell: (copy) => <StatusBadge label={statusLabels[copy.status]} tone={copy.status === 'Available' ? 'success' : copy.status === 'Damaged' || copy.status === 'Lost' ? 'danger' : 'neutral'} /> },
        ]} />
    </div>
    {selected ? <section className="mt-4 grid gap-3 rounded-xl border bg-card p-4" aria-label="Chi tiết bản sao">
      <div className="flex justify-between gap-3"><h2 className="font-semibold">{selected.barcode} · {selected.bookTitle}</h2><Button variant="outline" onClick={() => setSelected(null)}>Đóng</Button></div>
      <p className="text-sm">Vị trí: {selected.branchCode ?? 'Chưa có'} / {selected.shelfCode ?? 'Chưa có'} · Tình trạng: {selected.condition} · Ngày nhập: {new Date(selected.acquiredAtUtc).toLocaleDateString('vi-VN')}</p>
      <PermissionBoundary requiredPermissions={['copies.update']}><div className="flex flex-wrap gap-2">
        <select className="h-10 rounded-md border bg-background px-3" aria-label="Trạng thái mới" value={targetStatus} onChange={(event) => setTargetStatus(event.target.value as CopyStatus)}>
          {statuses.filter((item) => item !== 'Borrowed').map((item) => <option key={item} value={item}>{statusLabels[item]}</option>)}
        </select><Button variant="outline" disabled={targetStatus === selected.status} onClick={() => { setActionError(''); setAction({ copy: selected, status: targetStatus }) }}>Đổi trạng thái</Button>
        <select className="h-10 rounded-md border bg-background px-3" aria-label="Kệ mới" value={targetShelfId} onChange={(event) => setTargetShelfId(event.target.value)}>
          <option value="">Chọn kệ mới</option>{shelves.map((shelf) => <option key={shelf.id} value={shelf.id}>{shelf.branchCode} / {shelf.areaCode} / {shelf.code}</option>)}
        </select><Button variant="outline" disabled={!targetShelfId || targetShelfId === selected.shelfId} onClick={() => { setActionError(''); setAction({ copy: selected, shelfId: targetShelfId }) }}>Chuyển kệ</Button>
      </div></PermissionBoundary>
      <PermissionBoundary requiredPermissions={['audit-logs.read']}>
        <a className="text-sm text-primary underline" href={`/audit-log?entityType=BookCopy&entityId=${selected.id}`}>Xem lịch sử thay đổi</a>
      </PermissionBoundary>
    </section> : null}
    {creating ? <div className="mt-4 grid gap-3 rounded-xl border bg-card p-4" role="dialog" aria-label="Tạo bản sao">
      <h2 className="font-semibold">Tạo bản sao</h2>
      <Input value={bookSearch} onChange={(event) => setBookSearch(event.target.value)} aria-label="Tìm biểu ghi sách" placeholder="Tìm sách theo tên hoặc ISBN" />
      <select className="h-10 rounded-md border bg-background px-3" aria-label="Biểu ghi sách" value={createBookId} onChange={(event) => setCreateBookId(event.target.value)}>
        <option value="">Chọn sách từ kết quả tìm kiếm</option>{books.map((book) => <option key={book.id} value={book.id}>{book.title} · {book.isbn}</option>)}
      </select>
      <BarcodeInput value={createBarcode} onChange={setCreateBarcode} aria-label="Mã vạch mới" placeholder="Mã vạch duy nhất" />
      <select className="h-10 rounded-md border bg-background px-3" aria-label="Tình trạng" value={createCondition} onChange={(event) => setCreateCondition(event.target.value as CopyCondition)}>
        {conditions.map((item) => <option key={item} value={item}>{item}</option>)}
      </select>
      <select className="h-10 rounded-md border bg-background px-3" aria-label="Kệ" value={createShelfId} onChange={(event) => setCreateShelfId(event.target.value)}>
        <option value="">Chọn kệ active</option>{shelves.map((shelf) => <option key={shelf.id} value={shelf.id}>{shelf.branchCode} / {shelf.areaCode} / {shelf.code}</option>)}
      </select>
      {shelves.length === 0 ? <p className="text-sm text-muted-foreground">Chưa có kệ hoạt động. <a className="text-primary underline" href="/branches">Thiết lập vị trí trước</a>.</p> : null}
      {actionError ? <ScreenState kind="error" title="Không thể lưu" description={actionError} /> : null}
      <div className="flex gap-2"><Button onClick={() => void saveCopy()} disabled={!createBookId || !createBarcode.trim() || !createShelfId} loading={pending}>Lưu bản sao</Button>
        <Button variant="outline" onClick={() => setCreating(false)}>Hủy</Button></div>
    </div> : null}
    <ConfirmDialog open={action !== null} title="Xác nhận thay đổi bản sao" description={action?.status ? `Chuyển ${action.copy.barcode} sang ${statusLabels[action.status]}?` : `Chuyển ${action?.copy.barcode ?? ''} sang kệ mới?`}
      onOpenChange={(open) => { if (!open) setAction(null) }} onConfirm={() => void confirm()} isPending={pending} error={actionError} />
  </PageShell>
}
