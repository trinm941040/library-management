import { useCallback, useEffect, useState } from 'react'
import { Barcode, Download, Plus, RefreshCw, Upload } from 'lucide-react'
import { BarcodeInput, ConfirmDialog, DataTable, PageShell, ScreenState, StatusBadge, useToast } from '@/common/components'
import { ImportPreviewDialog } from '@/common/components/organisms/ImportPreviewDialog'
import { Button } from '@/common/components/ui/button'
import { Input } from '@/common/components/ui/input'
import { useAuth } from '@/auth/AuthProvider'
import { PermissionBoundary } from '@/shared/auth/PermissionBoundary'
import { canAny } from '@/shared/auth/permissions'
import { getBooks, type LibraryBook } from '@/pages/books/book-api'
import {
  confirmCopyImport, createCopy, exportCopies, getActiveShelves, getCopies, getCopyByBarcode,
  previewCopyImport, relocateCopy, runCopyBulk, updateCopyStatus,
  type ActiveShelf, type BookCopy, type CopyBulkOperation, type CopyBulkResult,
  type CopyCondition, type CopyImportPreview, type CopyImportRow, type CopyStatus,
} from './copy-api'
import { downloadCopyTemplate, parseCopyImport } from './copy-import'

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
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set())
  const [bulkOperation, setBulkOperation] = useState<CopyBulkOperation | null>(null)
  const [bulkShelfId, setBulkShelfId] = useState('')
  const [bulkStatus, setBulkStatus] = useState<CopyStatus>('Available')
  const [bulkCondition, setBulkCondition] = useState<CopyCondition>('Good')
  const [bulkReason, setBulkReason] = useState('')
  const [bulkResults, setBulkResults] = useState<CopyBulkResult[]>([])
  const [importOpen, setImportOpen] = useState(false)
  const [importFile, setImportFile] = useState<File | null>(null)
  const [importRows, setImportRows] = useState<CopyImportRow[]>([])
  const [importPreview, setImportPreview] = useState<CopyImportPreview[]>([])
  const [importError, setImportError] = useState('')
  const [importPending, setImportPending] = useState<'preview' | 'confirm' | null>(null)
  const { user } = useAuth()
  const canOperate = canAny(user?.permissions ?? [], ['copies.update', 'copies.withdraw'])
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

  const executeBulk = async () => {
    if (!bulkOperation) return
    const chosen = copies.filter(copy => selectedIds.has(copy.id))
    if (!chosen.length || (bulkOperation === 'relocate' && !bulkShelfId) ||
      (bulkOperation === 'withdraw' && !bulkReason.trim())) return
    setPending(true); setActionError('')
    try {
      const result = await runCopyBulk(bulkOperation, chosen.map(copy => ({
        copyId: copy.id, concurrencyToken: copy.concurrencyToken,
        shelfId: bulkOperation === 'relocate' ? bulkShelfId : undefined,
        status: bulkOperation === 'status' ? bulkStatus : undefined,
        condition: bulkOperation === 'condition' ? bulkCondition : undefined,
        reason: bulkOperation === 'withdraw' ? bulkReason.trim() : undefined,
      })))
      setBulkResults(result); setBulkOperation(null)
      setSelectedIds(new Set(result.filter(row => !row.succeeded).map(row => row.copyId)))
      await load()
      showToast(`${result.filter(row => row.succeeded).length} thành công, ${result.filter(row => !row.succeeded).length} thất bại.`)
    } catch (reason) { setActionError(reason instanceof Error ? reason.message : 'Không thể xử lý hàng loạt.') }
    finally { setPending(false) }
  }

  const previewImport = async () => {
    if (!importFile) return
    setImportError(''); setImportPreview([]); setImportRows([]); setImportPending('preview')
    try {
      if (importFile.size > 1_000_000) throw new Error('Tệp vượt quá 1 MB.')
      const rows = parseCopyImport(await importFile.text())
      const preview = await previewCopyImport(rows)
      setImportRows(rows); setImportPreview(preview)
    } catch (reason) { setImportError(reason instanceof Error ? reason.message : 'Không thể xem trước tệp.') }
    finally { setImportPending(null) }
  }

  const confirmImport = async () => {
    if (!importRows.length || importPreview.some(row => !row.valid)) return
    setImportPending('confirm'); setImportError('')
    try { const created = await confirmCopyImport(importRows)
      setImportOpen(false); setImportRows([]); setImportPreview([]); setImportFile(null)
      await load(); showToast(`Đã nhập ${created.length} bản sao.`) }
    catch (reason) { setImportError(reason instanceof Error ? reason.message : 'Không thể nhập tệp; hãy xem trước lại.') }
    finally { setImportPending(null) }
  }

  return <PageShell eyebrow="Kho vật lý" title="Bản sao và mã vạch"
    description="Tra cứu, đổi trạng thái và vị trí của từng bản sao vật lý."
    actions={<div className="flex flex-wrap gap-2"><Button variant="outline" onClick={() => void load()}><RefreshCw /> Làm mới</Button>
      <PermissionBoundary requiredPermissions={['copies.read']}><Button variant="outline" onClick={() => void exportCopies({ search, status: status || undefined, condition: condition || undefined, shelfId: shelfId || undefined }).catch((reason: unknown) => setError(reason instanceof Error ? reason.message : 'Không thể xuất dữ liệu.'))}><Download /> Xuất CSV</Button></PermissionBoundary>
      <PermissionBoundary requiredPermissions={['copies.create']}><Button variant="outline" onClick={() => setImportOpen(true)}><Upload /> Nhập CSV</Button></PermissionBoundary>
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
        selectedIds={selectedIds} onSelectionChange={canOperate ? setSelectedIds : undefined}
        bulkActions={<><PermissionBoundary requiredPermissions={['copies.update']}><Button size="sm" variant="outline" onClick={() => setBulkOperation('relocate')}>Chuyển kệ</Button><Button size="sm" variant="outline" onClick={() => setBulkOperation('status')}>Đổi trạng thái</Button><Button size="sm" variant="outline" onClick={() => setBulkOperation('condition')}>Đổi tình trạng</Button></PermissionBoundary><PermissionBoundary requiredPermissions={['copies.withdraw']}><Button size="sm" variant="destructive" onClick={() => setBulkOperation('withdraw')}>Thanh lý</Button></PermissionBoundary></>}
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
    {bulkResults.length ? <section className="mt-4 rounded-xl border bg-card p-4" role="status" aria-label="Kết quả thao tác hàng loạt"><h2 className="font-semibold">Kết quả: {bulkResults.filter(row => row.succeeded).length} thành công, {bulkResults.filter(row => !row.succeeded).length} thất bại</h2><ul className="mt-2 max-h-48 overflow-y-auto text-sm">{bulkResults.map(row => <li key={row.copyId} className={row.succeeded ? 'text-success' : 'text-destructive'}>{row.copy?.barcode ?? row.copyId}: {row.succeeded ? 'Thành công' : row.error}</li>)}</ul><Button size="sm" variant="outline" className="mt-2" onClick={() => setBulkResults([])}>Đóng kết quả</Button></section> : null}
    {bulkOperation ? <section className="mt-4 grid gap-3 rounded-xl border bg-card p-4" aria-label="Xác nhận thao tác hàng loạt"><h2 className="font-semibold">Áp dụng cho {selectedIds.size} bản sao trên trang hiện tại</h2>
      {bulkOperation === 'relocate' ? <select aria-label="Kệ đích" className="h-10 rounded-md border bg-background px-3" value={bulkShelfId} onChange={event => setBulkShelfId(event.target.value)}><option value="">Chọn kệ hoạt động</option>{shelves.map(shelf => <option key={shelf.id} value={shelf.id}>{shelf.branchCode} / {shelf.areaCode} / {shelf.code}</option>)}</select> : null}
      {bulkOperation === 'status' ? <select aria-label="Trạng thái mới" className="h-10 rounded-md border bg-background px-3" value={bulkStatus} onChange={event => setBulkStatus(event.target.value as CopyStatus)}>{statuses.filter(value => value !== 'Borrowed' && value !== 'Withdrawn').map(value => <option key={value} value={value}>{statusLabels[value]}</option>)}</select> : null}
      {bulkOperation === 'condition' ? <select aria-label="Tình trạng mới" className="h-10 rounded-md border bg-background px-3" value={bulkCondition} onChange={event => setBulkCondition(event.target.value as CopyCondition)}>{conditions.map(value => <option key={value} value={value}>{value}</option>)}</select> : null}
      {bulkOperation === 'withdraw' ? <Input aria-label="Lý do thanh lý" placeholder="Lý do thanh lý bắt buộc" maxLength={500} value={bulkReason} onChange={event => setBulkReason(event.target.value)} /> : null}
      {actionError ? <p role="alert" className="text-sm text-destructive">{actionError}</p> : null}
      <div className="flex gap-2"><Button variant={bulkOperation === 'withdraw' ? 'destructive' : 'default'} loading={pending} disabled={bulkOperation === 'relocate' && !bulkShelfId || bulkOperation === 'withdraw' && !bulkReason.trim()} onClick={() => void executeBulk()}>Xác nhận {bulkOperation === 'withdraw' ? 'thanh lý' : 'cập nhật'}</Button><Button variant="outline" onClick={() => setBulkOperation(null)}>Hủy</Button></div>
    </section> : null}
    {selected ? <section className="mt-4 grid gap-3 rounded-xl border bg-card p-4" aria-label="Chi tiết bản sao">
      <div className="flex justify-between gap-3"><h2 className="font-semibold">{selected.barcode} · {selected.bookTitle}</h2><Button variant="outline" onClick={() => setSelected(null)}>Đóng</Button></div>
      <p className="text-sm">Vị trí: {selected.branchCode ?? 'Chưa có'} / {selected.shelfCode ?? 'Chưa có'} · Tình trạng: {selected.condition} · Ngày nhập: {new Date(selected.acquiredAtUtc).toLocaleDateString('vi-VN')}</p>
      <PermissionBoundary requiredPermissions={['copies.update']}><div className="flex flex-wrap gap-2">
        <select className="h-10 rounded-md border bg-background px-3" aria-label="Trạng thái mới" value={targetStatus} onChange={(event) => setTargetStatus(event.target.value as CopyStatus)}>
          {statuses.filter((item) => item !== 'Borrowed' && item !== 'Withdrawn').map((item) => <option key={item} value={item}>{statusLabels[item]}</option>)}
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
    <ImportPreviewDialog open={importOpen} title="Nhập danh sách bản sao" description="CSV gồm Barcode,BookId,ShelfId,Condition. Xem trước toàn bộ trước khi xác nhận."
      file={importFile} errors={importPreview.filter(row => !row.valid).map(row => ({ rowNumber: row.rowNumber, field: 'Barcode/BookId/ShelfId/Condition', message: row.error ?? 'Không hợp lệ' }))}
      canConfirm={importRows.length > 0 && importPreview.length === importRows.length && importPreview.every(row => row.valid)} pendingAction={importPending}
      onOpenChange={setImportOpen} onFileChange={file => { setImportFile(file); setImportRows([]); setImportPreview([]); setImportError('') }}
      onPreview={() => void previewImport()} onConfirm={() => void confirmImport()}>
      <div className="grid gap-2"><Button size="sm" variant="outline" className="w-fit" onClick={downloadCopyTemplate}><Download /> Tải mẫu CSV</Button>
        {importPreview.length ? <p className="text-sm">Đã xem trước {importPreview.length} dòng · {importPreview.filter(row => row.valid).length} hợp lệ · {importPreview.filter(row => !row.valid).length} lỗi. Chưa có dữ liệu nào được lưu.</p> : null}
        {importError ? <p className="text-sm text-destructive" role="alert">{importError}</p> : null}</div>
    </ImportPreviewDialog>
  </PageShell>
}
