import { useCallback, useEffect, useMemo, useState } from 'react'
import {
  BookOpen,
  Download,
  Layers,
  Pencil,
  Plus,
  RefreshCw,
  Search,
  ArchiveX,
  Upload,
} from 'lucide-react'
import { Link, useSearchParams } from 'react-router-dom'
import {
  BulkResultSummary,
  ConfirmDialog,
  DataTable,
  FilterPanel,
  ImportPreviewDialog,
  PageShell,
  StatusBadge,
  useToast,
  type DataTableColumn,
  type SortDirection,
} from '@/common/components'
import { Button } from '@/common/components/ui/button'
import { PermissionBoundary } from '@/shared/auth/PermissionBoundary'
import { Card, CardContent, CardHeader, CardTitle } from '@/common/components/ui/card'
import { Input } from '@/common/components/ui/input'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/common/components/ui/select'
import { BookFormDialog, type BookFormData } from './components/BookFormDialog'
import { EntityActivityLink } from '@/pages/activity-logs/EntityActivityLink'
import {
  bulkDeleteBooks,
  confirmBookImport,
  createBook,
  deleteBook,
  exportBooks,
  getBooks,
  previewBookImport,
  updateBook,
  type BookImportPreview,
  type BookPageResponse,
  type LibraryBook,
} from './book-api'
import {
  downloadResponse,
  parseTableUrlState,
  updateSearchParams,
  type BulkResult,
} from '@/shared/data/table-contracts'

const bookSortFields = ['title', 'author', 'isbn', 'category', 'quantity', 'createdAtUtc'] as const

export function BooksPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const tableState = useMemo(
    () => parseTableUrlState(searchParams, bookSortFields, 'title'),
    [searchParams],
  )
  const { search, pageNumber: currentPage, pageSize, sortBy, sortDirection } = tableState
  const status = searchParams.get('status') === 'Inactive' ? 'Inactive' : 'Active'
  const [page, setPage] = useState<BookPageResponse | null>(null)
  const [searchInput, setSearchInput] = useState(search)
  const [reloadKey, setReloadKey] = useState(0)
  const [isLoading, setIsLoading] = useState(true)
  const [pageError, setPageError] = useState('')
  const [formOpen, setFormOpen] = useState(false)
  const [editingBook, setEditingBook] = useState<LibraryBook | null>(null)
  const [deletingBook, setDeletingBook] = useState<LibraryBook | null>(null)
  const [deleteError, setDeleteError] = useState('')
  const [isDeleting, setIsDeleting] = useState(false)
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set())
  const [bulkDeleteOpen, setBulkDeleteOpen] = useState(false)
  const [bulkResult, setBulkResult] = useState<BulkResult | null>(null)
  const [importOpen, setImportOpen] = useState(false)
  const [importFile, setImportFile] = useState<File | null>(null)
  const [importPreview, setImportPreview] = useState<BookImportPreview | null>(null)
  const [transferAction, setTransferAction] = useState<'export' | 'preview' | 'confirm' | null>(
    null,
  )
  const { showToast } = useToast()

  const updateUrl = useCallback(
    (changes: Record<string, string | number | undefined>) => {
      setSearchParams((current) => updateSearchParams(current, changes))
    },
    [setSearchParams],
  )

  useEffect(() => setSearchInput(search), [search])

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      const value = searchInput.trim()
      if (value !== search) updateUrl({ search: value || undefined, pageNumber: 1 })
    }, 350)

    return () => window.clearTimeout(timeout)
  }, [search, searchInput, updateUrl])

  useEffect(() => {
    const controller = new AbortController()
    setIsLoading(true)
    setPageError('')

    getBooks(
      {
        search: search || undefined,
        pageNumber: currentPage,
        pageSize,
        sortBy: sortBy as (typeof bookSortFields)[number],
        sortDirection,
        status,
      },
      controller.signal,
    )
      .then((response) => {
        setPage(response)
        if (response.totalPages > 0 && currentPage > response.totalPages) {
          updateUrl({ pageNumber: response.totalPages })
        }
      })
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === 'AbortError') return
        setPageError(error instanceof Error ? error.message : 'Không thể tải danh sách sách.')
      })
      .finally(() => {
        if (!controller.signal.aborted) setIsLoading(false)
      })

    return () => controller.abort()
  }, [currentPage, pageSize, reloadKey, search, sortBy, sortDirection, status, updateUrl])

  const refresh = useCallback(
    (message?: string) => {
      if (message) showToast(message)
      setReloadKey((value) => value + 1)
    },
    [showToast],
  )

  const handleSave = async (data: BookFormData) => {
    try {
      const quantity = Number(data.quantity)
      if (!Number.isInteger(quantity) || quantity < 0) return 'Số lượng phải là số nguyên không âm.'

      const input = {
        title: data.title.trim(),
        author: data.author.trim(),
        isbn: data.isbn.trim(),
        category: data.category.trim(),
        quantity,
        publisherName: data.publisherName.trim() || null,
        description: data.description.trim() || null,
        editionStatement: data.editionStatement.trim() || null,
        publicationYear: data.publicationYear ? Number(data.publicationYear) : null,
        language: data.language.trim() || null,
        pageCount: data.pageCount ? Number(data.pageCount) : null,
        concurrencyToken: editingBook?.concurrencyToken,
      }

      if (editingBook) {
        await updateBook(editingBook.id, input)
        refresh('Đã cập nhật sách thành công.')
      } else {
        await createBook(input)
        updateUrl({ pageNumber: 1 })
        refresh('Đã thêm sách vào kho thành công.')
      }

      return null
    } catch (error) {
      return error instanceof Error ? error.message : 'Không thể lưu sách.'
    }
  }

  const confirmBulkDelete = async () => {
    setIsDeleting(true)
    setDeleteError('')
    try {
      const result = await bulkDeleteBooks([...selectedIds])
      setBulkResult(result)
      setSelectedIds(new Set())
      setBulkDeleteOpen(false)
      refresh(`Đã xóa ${result.succeededCount} sách; ${result.failedCount} thất bại.`)
    } catch (error) {
      setDeleteError(error instanceof Error ? error.message : 'Không thể xóa hàng loạt.')
    } finally {
      setIsDeleting(false)
    }
  }

  const handleExport = async () => {
    setTransferAction('export')
    try {
      await downloadResponse(
        await exportBooks({
          search: search || undefined,
          pageNumber: 1,
          pageSize,
          sortBy: sortBy as (typeof bookSortFields)[number],
          sortDirection,
          status,
        }),
        'books.csv',
      )
    } catch (error) {
      showToast(error instanceof Error ? error.message : 'Không thể xuất dữ liệu.', {
        tone: 'error',
      })
    } finally {
      setTransferAction(null)
    }
  }

  const handlePreviewImport = async () => {
    if (!importFile) return
    setTransferAction('preview')
    try {
      setImportPreview(await previewBookImport(importFile))
    } catch (error) {
      showToast(error instanceof Error ? error.message : 'Không thể đọc tệp nhập.', {
        tone: 'error',
      })
    } finally {
      setTransferAction(null)
    }
  }

  const handleConfirmImport = async () => {
    if (!importPreview) return
    setTransferAction('confirm')
    try {
      const result = await confirmBookImport(importPreview)
      if (result.errors.length > 0) {
        setImportPreview((value) =>
          value ? { ...value, errors: result.errors, canConfirm: false } : value,
        )
      } else {
        setImportOpen(false)
        setImportFile(null)
        setImportPreview(null)
        updateUrl({ pageNumber: 1 })
        refresh(`Đã nhập ${result.importedCount} đầu sách.`)
      }
    } catch (error) {
      showToast(error instanceof Error ? error.message : 'Không thể nhập dữ liệu.', {
        tone: 'error',
      })
    } finally {
      setTransferAction(null)
    }
  }

  const confirmDelete = async () => {
    if (!deletingBook) return
    setDeleteError('')
    setIsDeleting(true)

    try {
      await deleteBook(deletingBook.id)
      setDeletingBook(null)
      refresh('Đã ngừng sử dụng biểu ghi.')
    } catch (error) {
      setDeleteError(error instanceof Error ? error.message : 'Không thể xóa sách.')
    } finally {
      setIsDeleting(false)
    }
  }

  const inStockCount = page?.items.filter((book) => book.quantity > 0).length ?? 0
  const columns = useMemo<DataTableColumn<LibraryBook>[]>(
    () => [
      {
        id: 'title',
        header: 'Sách',
        sortable: true,
        cell: (book) => (
          <span className="grid gap-0.5">
            <Link className="font-semibold text-primary hover:underline" to={`/catalog/${book.id}`}>{book.title}</Link>
            <small className="text-muted-foreground">{book.author}</small>
          </span>
        ),
      },
      {
        id: 'isbn',
        header: 'ISBN',
        sortable: true,
        cell: (book) => <span className="font-mono">{book.isbn}</span>,
      },
      { id: 'category', header: 'Thể loại', sortable: true, cell: (book) => book.category },
      { id: 'quantity', header: 'Số lượng', sortable: true, cell: (book) => book.quantity },
      {
        id: 'status',
        header: 'Trạng thái',
        cell: (book) => (
          <StatusBadge
            label={book.status === 'Active' ? 'Đang sử dụng' : 'Ngừng sử dụng'}
            tone={book.status === 'Active' ? 'success' : 'neutral'}
          />
        ),
      },
      {
        id: 'actions',
        header: 'Thao tác',
        className: 'text-right',
        cell: (book) => (
          <div className="flex justify-end">
            <EntityActivityLink entityType="Book" entityId={book.id} label={book.title} />
            <PermissionBoundary requiredPermissions={['books.update']}>
              <Button
                variant="ghost"
                size="icon"
                aria-label={`Sửa ${book.title}`}
                onClick={() => {
                  setEditingBook(book)
                  setFormOpen(true)
                }}
              >
                <Pencil />
              </Button>
            </PermissionBoundary>
            <PermissionBoundary requiredPermissions={['books.delete']}>
              <Button
                variant="ghost"
                size="icon"
                aria-label={`Ngừng sử dụng ${book.title}`}
                title={`Ngừng sử dụng ${book.title}`}
                onClick={() => {
                  setDeleteError('')
                  setDeletingBook(book)
                }}
              >
                <ArchiveX />
              </Button>
            </PermissionBoundary>
          </div>
        ),
      },
    ],
    [],
  )

  return (
    <>
      <PageShell
        eyebrow="Quản lý tác vụ"
        title="Biểu ghi sách"
        description="Tra cứu, thêm, cập nhật và ngừng sử dụng biểu ghi biên mục."
        actions={
          <>
            <Button
              variant="outline"
              aria-label="Tải lại danh sách sách"
              disabled={isLoading}
              loading={isLoading && Boolean(page)}
              loadingLabel="Đang tải lại danh sách sách"
              onClick={() => refresh()}
            >
              <RefreshCw />
              Làm mới
            </Button>
            <PermissionBoundary requiredPermissions={['books.create']}>
              <Button variant="outline" onClick={() => setImportOpen(true)}>
                <Upload /> Nhập CSV
              </Button>
              <Button
                onClick={() => {
                  setEditingBook(null)
                  setFormOpen(true)
                }}
              >
                <Plus /> Thêm sách
              </Button>
            </PermissionBoundary>
            <PermissionBoundary requiredPermissions={['books.read']}>
              <Button
                variant="outline"
                loading={transferAction === 'export'}
                loadingLabel="Đang xuất danh mục sách"
                onClick={() => void handleExport()}
              >
                <Download /> Xuất CSV
              </Button>
            </PermissionBoundary>
          </>
        }
      >
        <div className="mb-6 grid gap-4 sm:grid-cols-2">
          <Card>
            <CardHeader className="flex flex-row items-center justify-between">
              <CardTitle className="text-sm font-medium text-muted-foreground">
                Tổng đầu sách
              </CardTitle>
              <Layers className="size-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <p className="text-2xl font-bold">{page?.totalCount ?? '—'}</p>
            </CardContent>
          </Card>
          <Card>
            <CardHeader className="flex flex-row items-center justify-between">
              <CardTitle className="text-sm font-medium text-muted-foreground">
                Còn trong kho (trang này)
              </CardTitle>
              <BookOpen className="size-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <p className="text-2xl font-bold">{page ? inStockCount : '—'}</p>
            </CardContent>
          </Card>
        </div>

        <Card>
          <CardHeader>
            <CardTitle>Danh sách sách</CardTitle>
            <p className="text-sm text-muted-foreground">
              {page ? `${page.totalCount} đầu sách phù hợp với bộ lọc.` : 'Đang tải dữ liệu.'}
            </p>
          </CardHeader>
          <CardContent>
            <FilterPanel className="mb-4">
              <div className="grid gap-1 text-sm font-medium">
                <label htmlFor="catalog-status">Trạng thái</label>
                <Select value={status} onValueChange={(value) => updateUrl({ status: value, pageNumber: 1 })}>
                  <SelectTrigger id="catalog-status" className="h-10 w-full">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Active">Đang sử dụng</SelectItem>
                    <SelectItem value="Inactive">Ngừng sử dụng</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </FilterPanel>
            <DataTable
              caption="Danh sách sách trong kho"
              rows={page?.items ?? []}
              columns={columns}
              getRowId={(book) => book.id}
              isLoading={isLoading}
              error={pageError || undefined}
              onRetry={() => refresh()}
              emptyTitle="Chưa có sách trong kho"
              emptyDescription="Thay đổi bộ lọc hoặc thêm đầu sách mới."
              selectedIds={selectedIds}
              onSelectionChange={setSelectedIds}
              bulkActions={
                <>
                  <PermissionBoundary requiredPermissions={['books.delete']}>
                    <Button variant="destructive" size="sm" onClick={() => setBulkDeleteOpen(true)}>
                      Ngừng sử dụng mục đã chọn
                    </Button>
                  </PermissionBoundary>
                  <Button variant="outline" size="sm" onClick={() => setSelectedIds(new Set())}>
                    Bỏ chọn tất cả
                  </Button>
                </>
              }
              filters={
                <FilterPanel
                  hasFilters={Boolean(searchInput)}
                  onReset={() => setSearchInput('')}
                  resultCount={page?.totalCount}
                >
                  <div className="relative w-full lg:w-80">
                    <Search
                      className="absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground"
                      aria-hidden="true"
                    />
                    <Input
                      className="pl-9"
                      placeholder="Tìm theo tên, tác giả hoặc ISBN..."
                      value={searchInput}
                      onChange={(event) => setSearchInput(event.target.value)}
                      aria-label="Tìm kiếm sách"
                    />
                  </div>
                </FilterPanel>
              }
              sort={{ columnId: sortBy, direction: sortDirection as SortDirection }}
              onSortChange={(columnId, direction) =>
                updateUrl({ sortBy: columnId, sortDirection: direction, pageNumber: 1 })
              }
              page={page?.pageNumber}
              totalPages={page?.totalPages}
              totalCount={page?.totalCount}
              onPageChange={(value) => updateUrl({ pageNumber: value })}
              pageSize={pageSize}
              onPageSizeChange={(size) => {
                updateUrl({ pageNumber: 1, pageSize: size })
              }}
            />
            {bulkResult ? (
              <div className="mt-3">
                <BulkResultSummary result={bulkResult} />
              </div>
            ) : null}
          </CardContent>
        </Card>
      </PageShell>

      <PermissionBoundary requiredPermissions={[editingBook ? 'books.update' : 'books.create']}>
        <BookFormDialog
          open={formOpen}
          book={editingBook}
          onOpenChange={setFormOpen}
          onSave={handleSave}
        />
      </PermissionBoundary>

      <PermissionBoundary requiredPermissions={['books.delete']}>
        <ConfirmDialog
          open={bulkDeleteOpen}
          title="Ngừng sử dụng các sách đã chọn?"
          description={`${selectedIds.size} sách sẽ được xử lý. Các dòng không thể ngừng sử dụng sẽ được báo riêng.`}
          confirmLabel="Ngừng sử dụng"
          destructive
          isPending={isDeleting}
          error={deleteError}
          onConfirm={confirmBulkDelete}
          onOpenChange={setBulkDeleteOpen}
        />
        <ConfirmDialog
          open={deletingBook !== null}
          title="Ngừng sử dụng biểu ghi?"
          description={
            deletingBook
              ? `Biểu ghi "${deletingBook.title}" chỉ có thể ngừng sử dụng khi không còn bản sao, lượt mượn hoặc đặt trước đang hoạt động.`
              : ''
          }
          confirmLabel="Ngừng sử dụng"
          destructive
          isPending={isDeleting}
          error={deleteError}
          onConfirm={confirmDelete}
          onOpenChange={(open) => !open && setDeletingBook(null)}
        />
      </PermissionBoundary>

      <PermissionBoundary requiredPermissions={['books.create']}>
        <ImportPreviewDialog
          open={importOpen}
          title="Nhập danh mục sách"
          description="CSV gồm Title, Author, ISBN, Category, Quantity; Quantity phải là 0. Tạo bản sao có mã vạch và kệ riêng sau khi nhập biểu ghi."
          file={importFile}
          errors={importPreview?.errors ?? []}
          canConfirm={importPreview?.canConfirm ?? false}
          pendingAction={
            transferAction === 'preview' || transferAction === 'confirm' ? transferAction : null
          }
          onOpenChange={setImportOpen}
          onFileChange={(file) => {
            setImportFile(file)
            setImportPreview(null)
          }}
          onPreview={() => void handlePreviewImport()}
          onConfirm={() => void handleConfirmImport()}
        >
          {importPreview ? (
            <div className="max-h-64 overflow-auto rounded-md border">
              <table className="w-full min-w-[680px] text-left text-sm">
                <thead className="sticky top-0 bg-background">
                  <tr>
                    <th className="p-2">Dòng</th>
                    <th className="p-2">Tên sách</th>
                    <th className="p-2">Tác giả</th>
                    <th className="p-2">ISBN</th>
                    <th className="p-2">Thể loại</th>
                    <th className="p-2">SL</th>
                  </tr>
                </thead>
                <tbody>
                  {importPreview.rows.map((row) => (
                    <tr key={row.rowNumber} className="border-t">
                      <td className="p-2">{row.rowNumber}</td>
                      <td className="p-2">{row.title}</td>
                      <td className="p-2">{row.author}</td>
                      <td className="p-2 font-mono">{row.isbn}</td>
                      <td className="p-2">{row.category}</td>
                      <td className="p-2">{row.quantity}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : null}
        </ImportPreviewDialog>
      </PermissionBoundary>
    </>
  )
}
