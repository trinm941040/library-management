import { useCallback, useEffect, useState } from 'react'
import { Barcode, Download, Plus, RefreshCw, Upload } from 'lucide-react'
import {
  BarcodeInput,
  ConfirmDialog,
  DataTable,
  PageShell,
  StatusBadge,
  useToast,
} from '@/common/components'
import { ImportPreviewDialog } from '@/common/components/organisms/ImportPreviewDialog'
import { Button } from '@/common/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/common/components/ui/dialog'
import { Input } from '@/common/components/ui/input'
import { Label } from '@/common/components/ui/label'
import { useAuth } from '@/auth/AuthProvider'
import { PermissionBoundary } from '@/shared/auth/PermissionBoundary'
import { canAny } from '@/shared/auth/permissions'
import { getBooks, type LibraryBook } from '@/pages/books/book-api'
import {
  confirmCopyImport,
  createCopy,
  exportCopies,
  getActiveShelves,
  getCopies,
  getCopy,
  getCopyByBarcode,
  previewCopyImport,
  relocateCopy,
  runCopyBulk,
  updateCopyStatus,
  type ActiveShelf,
  type BookCopy,
  type CopyBulkOperation,
  type CopyBulkResult,
  type CopyCondition,
  type CopyImportPreview,
  type CopyImportRow,
  type CopyStatus,
  copyConditionLabels as conditionLabels,
  copyStatusLabels as statusLabels,
  copyStatuses as statuses,
} from './copy-api'
import { downloadCopyTemplate, parseCopyImport } from './copy-import'
import { readUrlFilter, readUrlPage, useFilterUrlSync } from '@/shared/data/use-filter-url-sync'

const conditions: CopyCondition[] = ['New', 'Good', 'Worn', 'Damaged', 'Lost']
type Action = { copy: BookCopy; status?: CopyStatus; shelfId?: string }

export function CopiesPage() {
  const [copies, setCopies] = useState<BookCopy[]>([])
  const [total, setTotal] = useState(0)
  const [pages, setPages] = useState(0)
  const [page, setPage] = useState(() => readUrlPage('pageNumber', 1))
  const [pageSize, setPageSize] = useState(() => readUrlPage('pageSize', 20))
  const [search, setSearch] = useState(() => readUrlFilter('search'))
  const [barcode, setBarcode] = useState('')
  const [status, setStatus] = useState<CopyStatus | ''>(
    () => readUrlFilter('status') as CopyStatus | '',
  )
  const [condition, setCondition] = useState<CopyCondition | ''>(
    () => readUrlFilter('condition') as CopyCondition | '',
  )
  const [shelfId, setShelfId] = useState(() => readUrlFilter('shelfId'))
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
  useFilterUrlSync({ search, status, condition, shelfId, pageNumber: page, pageSize })

  const load = useCallback(
    (signal?: AbortSignal) => {
      setLoading(true)
      setError('')
      return getCopies(
        {
          search,
          status: status || undefined,
          condition: condition || undefined,
          shelfId: shelfId || undefined,
          pageNumber: page,
          pageSize,
        },
        signal,
      )
        .then((result) => {
          setCopies(result.items)
          setTotal(result.totalCount)
          setPages(result.totalPages)
        })
        .catch((reason: unknown) => {
          if (reason instanceof DOMException && reason.name === 'AbortError') return
          setError(reason instanceof Error ? reason.message : 'Không thể tải bản sao.')
        })
        .finally(() => {
          if (!signal?.aborted) setLoading(false)
        })
    },
    [search, status, condition, shelfId, page, pageSize],
  )

  useEffect(() => {
    const controller = new AbortController()
    void load(controller.signal)
    return () => controller.abort()
  }, [load])
  useEffect(() => {
    const controller = new AbortController()
    void getActiveShelves(controller.signal)
      .then(setShelves)
      .catch(() => undefined)
    return () => controller.abort()
  }, [])
  useEffect(() => {
    const controller = new AbortController()
    void getBooks(
      { search: bookSearch || undefined, pageNumber: 1, pageSize: 100 },
      controller.signal,
    )
      .then((result) => setBooks(result.items.filter((book) => book.status === 'Active')))
      .catch(() => undefined)
    return () => controller.abort()
  }, [bookSearch])

  const openDetails = async (copy: BookCopy) => {
    setError('')
    try {
      const freshCopy = await getCopy(copy.id)
      setSelected(freshCopy)
      setTargetStatus(freshCopy.status)
      setTargetShelfId(freshCopy.shelfId ?? '')
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Không thể tải thông tin bản sao.')
    }
  }

  const lookup = async (value: string) => {
    if (!value.trim()) return
    setError('')
    setLoading(true)
    try {
      const copy = await getCopyByBarcode(value.trim())
      setSelected(copy)
      setTargetStatus(copy.status)
      setTargetShelfId(copy.shelfId ?? '')
      setCopies([copy])
      setTotal(1)
      setPages(1)
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Không tìm thấy mã vạch.')
    } finally {
      setLoading(false)
    }
  }

  const saveCopy = async () => {
    if (!createBookId || !createBarcode.trim() || !createShelfId) {
      setActionError('Vui lòng nhập đầy đủ các trường bắt buộc.')
      return
    }
    setActionError('')
    setPending(true)
    try {
      const copy = await createCopy({
        bookId: createBookId,
        barcode: createBarcode,
        condition: createCondition,
        shelfId: createShelfId,
      })
      setCreating(false)
      setSelected(copy)
      setCreateBarcode('')
      await load()
      showToast('Đã tạo bản sao.')
    } catch (reason) {
      setActionError(reason instanceof Error ? reason.message : 'Không thể tạo bản sao.')
    } finally {
      setPending(false)
    }
  }

  const confirm = async () => {
    if (!action) return
    setPending(true)
    setActionError('')
    try {
      const freshCopy = await getCopy(action.copy.id)
      setSelected(freshCopy)
      const updated = action.status
        ? await updateCopyStatus(freshCopy, action.status)
        : await relocateCopy(freshCopy, action.shelfId!)
      setSelected(updated)
      setAction(null)
      await load()
      showToast('Đã cập nhật bản sao.')
    } catch (reason) {
      setActionError(reason instanceof Error ? reason.message : 'Không thể cập nhật bản sao.')
    } finally {
      setPending(false)
    }
  }

  const executeBulk = async () => {
    if (!bulkOperation) return
    const chosen = copies.filter((copy) => selectedIds.has(copy.id))
    if (
      !chosen.length ||
      (bulkOperation === 'relocate' && !bulkShelfId) ||
      (bulkOperation === 'withdraw' && !bulkReason.trim())
    ) {
      setActionError(
        !chosen.length
          ? 'Vui lòng chọn ít nhất một bản sao.'
          : bulkOperation === 'relocate'
            ? 'Vui lòng chọn kệ đích.'
            : 'Vui lòng nhập lý do thanh lý.',
      )
      return
    }
    setPending(true)
    setActionError('')
    try {
      const result = await runCopyBulk(
        bulkOperation,
        chosen.map((copy) => ({
          copyId: copy.id,
          concurrencyToken: copy.concurrencyToken,
          shelfId: bulkOperation === 'relocate' ? bulkShelfId : undefined,
          status: bulkOperation === 'status' ? bulkStatus : undefined,
          condition: bulkOperation === 'condition' ? bulkCondition : undefined,
          reason: bulkOperation === 'withdraw' ? bulkReason.trim() : undefined,
        })),
      )
      setBulkResults(result)
      setBulkOperation(null)
      setSelectedIds(new Set(result.filter((row) => !row.succeeded).map((row) => row.copyId)))
      await load()
      showToast(
        `${result.filter((row) => row.succeeded).length} thành công, ${result.filter((row) => !row.succeeded).length} thất bại.`,
      )
    } catch (reason) {
      setActionError(reason instanceof Error ? reason.message : 'Không thể xử lý hàng loạt.')
    } finally {
      setPending(false)
    }
  }

  const openBulkDialog = (operation: CopyBulkOperation) => {
    setActionError('')
    if (operation === 'withdraw') setBulkReason('')
    setBulkOperation(operation)
  }

  const previewImport = async () => {
    if (!importFile) return
    setImportError('')
    setImportPreview([])
    setImportRows([])
    setImportPending('preview')
    try {
      if (importFile.size > 1_000_000) throw new Error('Tệp vượt quá 1 MB.')
      const rows = parseCopyImport(await importFile.text())
      const preview = await previewCopyImport(rows)
      setImportRows(rows)
      setImportPreview(preview)
    } catch (reason) {
      setImportError(reason instanceof Error ? reason.message : 'Không thể xem trước tệp.')
    } finally {
      setImportPending(null)
    }
  }

  const confirmImport = async () => {
    if (!importRows.length || importPreview.some((row) => !row.valid)) return
    setImportPending('confirm')
    setImportError('')
    try {
      const created = await confirmCopyImport(importRows)
      setImportOpen(false)
      setImportRows([])
      setImportPreview([])
      setImportFile(null)
      await load()
      showToast(`Đã nhập ${created.length} bản sao.`)
    } catch (reason) {
      setImportError(
        reason instanceof Error ? reason.message : 'Không thể nhập tệp; hãy xem trước lại.',
      )
    } finally {
      setImportPending(null)
    }
  }

  return (
    <PageShell
      eyebrow="Kho vật lý"
      title="Bản sao và mã vạch"
      description="Tra cứu, đổi trạng thái và vị trí của từng bản sao vật lý."
      actions={
        <div className="flex flex-wrap gap-2">
          <Button variant="outline" onClick={() => void load()}>
            <RefreshCw /> Làm mới
          </Button>
          <PermissionBoundary requiredPermissions={['copies.read']}>
            <Button
              variant="outline"
              onClick={() =>
                void exportCopies({
                  search,
                  status: status || undefined,
                  condition: condition || undefined,
                  shelfId: shelfId || undefined,
                }).catch((reason: unknown) =>
                  setError(reason instanceof Error ? reason.message : 'Không thể xuất dữ liệu.'),
                )
              }
            >
              <Download /> Xuất CSV
            </Button>
          </PermissionBoundary>
          <PermissionBoundary requiredPermissions={['copies.create']}>
            <Button variant="outline" onClick={() => setImportOpen(true)}>
              <Upload /> Nhập CSV
            </Button>
          </PermissionBoundary>
          <PermissionBoundary requiredPermissions={['copies.create']}>
            <Button onClick={() => setCreating(true)}>
              <Plus /> Tạo bản sao
            </Button>
          </PermissionBoundary>
        </div>
      }
    >
      <div className="grid gap-4 rounded-xl border bg-card p-4">
        <div className="flex gap-2">
          <BarcodeInput
            value={barcode}
            onChange={setBarcode}
            onScan={(value) => void lookup(value)}
            aria-label="Quét hoặc nhập mã vạch"
            placeholder="Quét mã vạch rồi nhấn Enter"
          />
          <Button onClick={() => void lookup(barcode)}>
            <Barcode /> Tra cứu
          </Button>
        </div>
        <div className="grid gap-2 sm:grid-cols-3">
          <Input
            value={search}
            onChange={(event) => {
              setPage(1)
              setSearch(event.target.value)
            }}
            placeholder="Tên sách hoặc mã vạch"
            aria-label="Tìm bản sao"
          />
          <select
            className="h-10 rounded-md border bg-background px-3"
            value={status}
            aria-label="Lọc trạng thái"
            onChange={(event) => {
              setPage(1)
              setStatus(event.target.value as CopyStatus | '')
            }}
          >
            <option value="">Mọi trạng thái</option>
            {statuses.map((item) => (
              <option key={item} value={item}>
                {statusLabels[item]}
              </option>
            ))}
          </select>
          <select
            className="h-10 rounded-md border bg-background px-3"
            value={condition}
            aria-label="Lọc tình trạng"
            onChange={(event) => {
              setPage(1)
              setCondition(event.target.value as CopyCondition | '')
            }}
          >
            <option value="">Mọi tình trạng</option>
            {conditions.map((item) => (
              <option key={item} value={item}>
                {conditionLabels[item]}
              </option>
            ))}
          </select>
          <select
            className="h-10 rounded-md border bg-background px-3 sm:col-span-3"
            value={shelfId}
            aria-label="Lọc kệ"
            onChange={(event) => {
              setPage(1)
              setShelfId(event.target.value)
            }}
          >
            <option value="">Mọi kệ</option>
            {shelves.map((shelf) => (
              <option key={shelf.id} value={shelf.id}>
                {shelf.branchCode} / {shelf.areaCode} / {shelf.code}
              </option>
            ))}
          </select>
        </div>
        <DataTable
          caption="Danh sách bản sao"
          rows={copies}
          getRowId={(copy) => copy.id}
          selectedIds={selectedIds}
          onSelectionChange={canOperate ? setSelectedIds : undefined}
          bulkActions={
            <>
              <PermissionBoundary requiredPermissions={['copies.update']}>
                <Button size="sm" variant="outline" onClick={() => openBulkDialog('relocate')}>
                  Chuyển kệ
                </Button>
                <Button size="sm" variant="outline" onClick={() => openBulkDialog('status')}>
                  Đổi trạng thái
                </Button>
                <Button size="sm" variant="outline" onClick={() => openBulkDialog('condition')}>
                  Đổi tình trạng
                </Button>
              </PermissionBoundary>
              <PermissionBoundary requiredPermissions={['copies.withdraw']}>
                <Button size="sm" variant="destructive" onClick={() => openBulkDialog('withdraw')}>
                  Thanh lý
                </Button>
              </PermissionBoundary>
            </>
          }
          isLoading={loading}
          error={error}
          onRetry={() => void load()}
          emptyTitle="Chưa có bản sao phù hợp"
          page={page}
          pageSize={pageSize}
          totalPages={pages}
          totalCount={total}
          onPageChange={setPage}
          onPageSizeChange={(value) => {
            setPage(1)
            setPageSize(value)
          }}
          columns={[
            {
              id: 'barcode',
              header: 'Mã vạch',
              cell: (copy) => (
                <button
                  type="button"
                  className="font-mono text-primary underline"
                  onClick={() => void openDetails(copy)}
                >
                  {copy.barcode}
                </button>
              ),
            },
            { id: 'book', header: 'Sách', cell: (copy) => copy.bookTitle },
            {
              id: 'location',
              header: 'Vị trí',
              cell: (copy) => `${copy.branchCode ?? '—'} / ${copy.shelfCode ?? '—'}`,
            },
            {
              id: 'condition',
              header: 'Tình trạng',
              cell: (copy) => conditionLabels[copy.condition],
            },
            {
              id: 'status',
              header: 'Trạng thái',
              cell: (copy) => (
                <StatusBadge
                  label={statusLabels[copy.status]}
                  tone={
                    copy.status === 'Available'
                      ? 'success'
                      : copy.status === 'Damaged' || copy.status === 'Lost'
                        ? 'danger'
                        : 'neutral'
                  }
                />
              ),
            },
          ]}
        />
      </div>
      <Dialog
        open={bulkResults.length > 0}
        onOpenChange={(open) => {
          if (!open) setBulkResults([])
        }}
      >
        <DialogContent className="sm:max-w-xl">
          <DialogHeader>
            <DialogTitle>Kết quả thao tác</DialogTitle>
            <DialogDescription>
              {bulkResults.filter((row) => row.succeeded).length} thành công,{' '}
              {bulkResults.filter((row) => !row.succeeded).length} thất bại.
            </DialogDescription>
          </DialogHeader>
          <ul
            className="grid max-h-[50vh] gap-2 overflow-y-auto"
            role="status"
            aria-label="Kết quả thao tác hàng loạt"
          >
            {bulkResults.map((row) => (
              <li
                key={row.copyId}
                className={`break-words rounded-md border p-3 text-sm ${row.succeeded ? 'border-success/30 bg-success/10 text-success' : 'border-destructive/30 bg-destructive/10 text-destructive'}`}
              >
                <strong>{row.copy?.barcode ?? row.copyId}</strong>
                <span>: {row.succeeded ? 'Thành công' : row.error}</span>
              </li>
            ))}
          </ul>
          <DialogFooter>
            <Button variant="outline" onClick={() => setBulkResults([])}>
              Đóng
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
      <Dialog
        open={bulkOperation !== null}
        onOpenChange={(open) => {
          if (!open && !pending) {
            setBulkOperation(null)
            setActionError('')
          }
        }}
      >
        <DialogContent className="overflow-x-hidden sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>
              {bulkOperation === 'relocate'
                ? 'Chuyển kệ'
                : bulkOperation === 'status'
                  ? 'Đổi trạng thái'
                  : bulkOperation === 'condition'
                    ? 'Đổi tình trạng'
                    : 'Thanh lý bản sao'}
            </DialogTitle>
            <DialogDescription>
              Áp dụng cho {selectedIds.size} bản sao đã chọn trên trang hiện tại.
            </DialogDescription>
          </DialogHeader>
          <form
            className="grid min-w-0 gap-4"
            noValidate
            onSubmit={(event) => {
              event.preventDefault()
              void executeBulk()
            }}
          >
            {bulkOperation === 'relocate' ? (
              <div className="grid min-w-0 gap-2">
                <Label htmlFor="bulk-copy-shelf">
                  Kệ đích <span className="text-destructive">*</span>
                </Label>
                <select
                  id="bulk-copy-shelf"
                  required
                  aria-invalid={Boolean(actionError && !bulkShelfId)}
                  className="h-10 min-w-0 w-full truncate rounded-md border bg-background px-3"
                  value={bulkShelfId}
                  onChange={(event) => {
                    setBulkShelfId(event.target.value)
                    setActionError('')
                  }}
                >
                  <option value="">Chọn kệ hoạt động</option>
                  {shelves.map((shelf) => (
                    <option key={shelf.id} value={shelf.id}>
                      {shelf.branchCode} / {shelf.areaCode} / {shelf.code}
                    </option>
                  ))}
                </select>
              </div>
            ) : null}
            {bulkOperation === 'status' ? (
              <div className="grid gap-2">
                <Label htmlFor="bulk-copy-status">
                  Trạng thái mới <span className="text-destructive">*</span>
                </Label>
                <select
                  id="bulk-copy-status"
                  required
                  className="h-10 min-w-0 w-full rounded-md border bg-background px-3"
                  value={bulkStatus}
                  onChange={(event) => setBulkStatus(event.target.value as CopyStatus)}
                >
                  {statuses
                    .filter((value) => value !== 'Borrowed' && value !== 'Withdrawn')
                    .map((value) => (
                      <option key={value} value={value}>
                        {statusLabels[value]}
                      </option>
                    ))}
                </select>
              </div>
            ) : null}
            {bulkOperation === 'condition' ? (
              <div className="grid gap-2">
                <Label htmlFor="bulk-copy-condition">
                  Tình trạng mới <span className="text-destructive">*</span>
                </Label>
                <select
                  id="bulk-copy-condition"
                  required
                  className="h-10 min-w-0 w-full rounded-md border bg-background px-3"
                  value={bulkCondition}
                  onChange={(event) => setBulkCondition(event.target.value as CopyCondition)}
                >
                  {conditions.map((value) => (
                    <option key={value} value={value}>
                      {conditionLabels[value]}
                    </option>
                  ))}
                </select>
              </div>
            ) : null}
            {bulkOperation === 'withdraw' ? (
              <div className="grid gap-2">
                <Label htmlFor="bulk-copy-reason">
                  Lý do thanh lý <span className="text-destructive">*</span>
                </Label>
                <Input
                  id="bulk-copy-reason"
                  required
                  aria-invalid={Boolean(actionError && !bulkReason.trim())}
                  placeholder="Nhập lý do thanh lý"
                  maxLength={500}
                  value={bulkReason}
                  onChange={(event) => {
                    setBulkReason(event.target.value)
                    setActionError('')
                  }}
                />
              </div>
            ) : null}
            {actionError ? (
              <p role="alert" className="text-sm text-destructive">
                {actionError}
              </p>
            ) : null}
            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                disabled={pending}
                onClick={() => {
                  setBulkOperation(null)
                  setActionError('')
                }}
              >
                Hủy
              </Button>
              <Button
                type="submit"
                variant={bulkOperation === 'withdraw' ? 'destructive' : 'default'}
                loading={pending}
              >
                Xác nhận {bulkOperation === 'withdraw' ? 'thanh lý' : 'cập nhật'}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
      <Dialog
        open={selected !== null}
        onOpenChange={(open) => {
          if (!open) setSelected(null)
        }}
      >
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-2xl">
          {selected ? (
            <>
              <DialogHeader>
                <DialogTitle>
                  {selected.barcode} · {selected.bookTitle}
                </DialogTitle>
                <DialogDescription>Thông tin và thao tác trên bản sao vật lý.</DialogDescription>
              </DialogHeader>
              <p className="text-sm">
                Vị trí: {selected.branchCode ?? 'Chưa có'} / {selected.shelfCode ?? 'Chưa có'} ·
                Tình trạng: {conditionLabels[selected.condition]} · Ngày nhập:{' '}
                {new Date(selected.acquiredAtUtc).toLocaleDateString('vi-VN')}
              </p>
              <PermissionBoundary requiredPermissions={['copies.update']}>
                <div className="flex flex-wrap gap-2">
                  <select
                    className="h-10 rounded-md border bg-background px-3"
                    aria-label="Trạng thái mới"
                    value={targetStatus}
                    onChange={(event) => setTargetStatus(event.target.value as CopyStatus)}
                  >
                    {statuses
                      .filter((item) => item !== 'Borrowed' && item !== 'Withdrawn')
                      .map((item) => (
                        <option key={item} value={item}>
                          {statusLabels[item]}
                        </option>
                      ))}
                  </select>
                  <Button
                    variant="outline"
                    disabled={targetStatus === selected.status}
                    onClick={() => {
                      setActionError('')
                      setAction({ copy: selected, status: targetStatus })
                    }}
                  >
                    Đổi trạng thái
                  </Button>
                  <select
                    className="h-10 rounded-md border bg-background px-3"
                    aria-label="Kệ mới"
                    value={targetShelfId}
                    onChange={(event) => setTargetShelfId(event.target.value)}
                  >
                    <option value="">Chọn kệ mới</option>
                    {shelves.map((shelf) => (
                      <option key={shelf.id} value={shelf.id}>
                        {shelf.branchCode} / {shelf.areaCode} / {shelf.code}
                      </option>
                    ))}
                  </select>
                  <Button
                    variant="outline"
                    disabled={!targetShelfId || targetShelfId === selected.shelfId}
                    onClick={() => {
                      setActionError('')
                      setAction({ copy: selected, shelfId: targetShelfId })
                    }}
                  >
                    Chuyển kệ
                  </Button>
                </div>
              </PermissionBoundary>
              <PermissionBoundary requiredPermissions={['audit-logs.read']}>
                <a
                  className="text-sm text-primary underline"
                  href={`/audit-log?entityType=BookCopy&entityId=${selected.id}`}
                >
                  Xem lịch sử thay đổi
                </a>
              </PermissionBoundary>
              <DialogFooter>
                <Button variant="outline" onClick={() => setSelected(null)}>
                  Đóng
                </Button>
              </DialogFooter>
            </>
          ) : null}
        </DialogContent>
      </Dialog>
      <Dialog
        open={creating}
        onOpenChange={(open) => {
          if (!pending) {
            setCreating(open)
            if (!open) setActionError('')
          }
        }}
      >
        <DialogContent className="max-h-[90dvh] overflow-x-hidden overflow-y-auto sm:max-w-xl">
          <DialogHeader>
            <DialogTitle>Tạo bản sao</DialogTitle>
            <DialogDescription>Nhập thông tin bản sao và vị trí lưu trữ.</DialogDescription>
          </DialogHeader>
          <form
            className="grid min-w-0 gap-4 [&>*]:min-w-0"
            noValidate
            onSubmit={(event) => {
              event.preventDefault()
              void saveCopy()
            }}
          >
            <div className="grid min-w-0 gap-2">
              <Label htmlFor="copy-book-search">Tìm biểu ghi sách</Label>
              <Input
                className="min-w-0 max-w-full"
                id="copy-book-search"
                value={bookSearch}
                onChange={(event) => setBookSearch(event.target.value)}
                placeholder="Tìm sách theo tên hoặc ISBN"
              />
            </div>
            <div className="grid min-w-0 gap-2">
              <Label htmlFor="copy-book">
                Biểu ghi sách <span className="text-destructive">*</span>
              </Label>
              <select
                id="copy-book"
                required
                aria-invalid={Boolean(actionError && !createBookId)}
                className="h-10 min-w-0 w-full max-w-full truncate rounded-md border bg-background px-3"
                value={createBookId}
                onChange={(event) => setCreateBookId(event.target.value)}
              >
                <option value="">Chọn sách từ kết quả tìm kiếm</option>
                {books.map((book) => (
                  <option key={book.id} value={book.id}>
                    {book.title} · {book.isbn}
                  </option>
                ))}
              </select>
              {actionError && !createBookId ? (
                <p className="text-sm text-destructive" role="alert">
                  Vui lòng chọn biểu ghi sách.
                </p>
              ) : null}
            </div>
            <div className="grid gap-2">
              <Label htmlFor="copy-barcode">
                Mã vạch <span className="text-destructive">*</span>
              </Label>
              <BarcodeInput
                id="copy-barcode"
                required
                aria-invalid={Boolean(actionError && !createBarcode.trim())}
                value={createBarcode}
                onChange={setCreateBarcode}
                placeholder="Mã vạch duy nhất"
              />
              {actionError && !createBarcode.trim() ? (
                <p className="text-sm text-destructive" role="alert">
                  Vui lòng nhập mã vạch.
                </p>
              ) : null}
            </div>
            <div className="grid min-w-0 gap-2">
              <Label htmlFor="copy-condition">
                Tình trạng <span className="text-destructive">*</span>
              </Label>
              <select
                id="copy-condition"
                required
                className="h-10 min-w-0 w-full max-w-full rounded-md border bg-background px-3"
                value={createCondition}
                onChange={(event) => setCreateCondition(event.target.value as CopyCondition)}
              >
                {conditions.map((item) => (
                  <option key={item} value={item}>
                    {conditionLabels[item]}
                  </option>
                ))}
              </select>
            </div>
            <div className="grid min-w-0 gap-2">
              <Label htmlFor="copy-shelf">
                Kệ <span className="text-destructive">*</span>
              </Label>
              <select
                id="copy-shelf"
                required
                aria-invalid={Boolean(actionError && !createShelfId)}
                className="h-10 min-w-0 w-full max-w-full truncate rounded-md border bg-background px-3"
                value={createShelfId}
                onChange={(event) => setCreateShelfId(event.target.value)}
              >
                <option value="">Chọn kệ hoạt động</option>
                {shelves.map((shelf) => (
                  <option key={shelf.id} value={shelf.id}>
                    {shelf.branchCode} / {shelf.areaCode} / {shelf.code}
                  </option>
                ))}
              </select>
              {actionError && !createShelfId ? (
                <p className="text-sm text-destructive" role="alert">
                  Vui lòng chọn kệ.
                </p>
              ) : null}
            </div>
            {shelves.length === 0 ? (
              <p className="text-sm text-muted-foreground">
                Chưa có kệ hoạt động.{' '}
                <a className="text-primary underline" href="/branches">
                  Thiết lập vị trí trước
                </a>
                .
              </p>
            ) : null}
            {actionError && createBookId && createBarcode.trim() && createShelfId ? (
              <p className="text-sm text-destructive" role="alert">
                {actionError}
              </p>
            ) : null}
            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                disabled={pending}
                onClick={() => setCreating(false)}
              >
                Hủy
              </Button>
              <Button type="submit" loading={pending}>
                Lưu bản sao
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
      <ConfirmDialog
        open={action !== null}
        title="Xác nhận thay đổi bản sao"
        description={
          action?.status
            ? `Chuyển ${action.copy.barcode} sang ${statusLabels[action.status]}?`
            : `Chuyển ${action?.copy.barcode ?? ''} sang kệ mới?`
        }
        onOpenChange={(open) => {
          if (!open) setAction(null)
        }}
        onConfirm={() => void confirm()}
        isPending={pending}
        error={actionError}
      />
      <ImportPreviewDialog
        open={importOpen}
        title="Nhập danh sách bản sao"
        description="Bắt buộc: Barcode, ISBN, ShelfCode. Condition là cột tuỳ chọn, mặc định là Good. Có thể đổi thứ tự cột; tên cột không phân biệt hoa thường, khoảng trắng hoặc dấu gạch."
        file={importFile}
        errors={importPreview
          .filter((row) => !row.valid)
          .map((row) => ({
            rowNumber: row.rowNumber,
            field: 'Barcode/ISBN/ShelfCode/Condition',
            message: row.error ?? 'Không hợp lệ',
          }))}
        canConfirm={
          importRows.length > 0 &&
          importPreview.length === importRows.length &&
          importPreview.every((row) => row.valid)
        }
        pendingAction={importPending}
        onOpenChange={setImportOpen}
        onFileChange={(file) => {
          setImportFile(file)
          setImportRows([])
          setImportPreview([])
          setImportError('')
        }}
        onPreview={() => void previewImport()}
        onConfirm={() => void confirmImport()}
      >
        <div className="grid gap-2">
          <Button size="sm" variant="outline" className="w-fit" onClick={downloadCopyTemplate}>
            <Download /> Tải mẫu CSV
          </Button>
          {importPreview.length ? (
            <p className="text-sm">
              Đã xem trước {importPreview.length} dòng ·{' '}
              {importPreview.filter((row) => row.valid).length} hợp lệ ·{' '}
              {importPreview.filter((row) => !row.valid).length} lỗi. Chưa có dữ liệu nào được lưu.
            </p>
          ) : null}
          {importError ? (
            <p className="text-sm text-destructive" role="alert">
              {importError}
            </p>
          ) : null}
        </div>
      </ImportPreviewDialog>
    </PageShell>
  )
}
