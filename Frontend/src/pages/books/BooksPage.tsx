import { useCallback, useEffect, useMemo, useState } from 'react'
import { BookOpen, Layers, Pencil, Plus, RefreshCw, Search, Trash2 } from 'lucide-react'
import {
  ConfirmDialog,
  DataTable,
  FilterPanel,
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
import { BookFormDialog, type BookFormData } from './components/BookFormDialog'
import {
  createBook,
  deleteBook,
  getBooks,
  updateBook,
  type BookPageResponse,
  type LibraryBook,
} from './book-api'

const BOOKS_PER_PAGE = 20

export function BooksPage() {
  const [page, setPage] = useState<BookPageResponse | null>(null)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [currentPage, setCurrentPage] = useState(1)
  const [reloadKey, setReloadKey] = useState(0)
  const [isLoading, setIsLoading] = useState(true)
  const [pageError, setPageError] = useState('')
  const [formOpen, setFormOpen] = useState(false)
  const [editingBook, setEditingBook] = useState<LibraryBook | null>(null)
  const [deletingBook, setDeletingBook] = useState<LibraryBook | null>(null)
  const [deleteError, setDeleteError] = useState('')
  const [isDeleting, setIsDeleting] = useState(false)
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set())
  const [sort, setSort] = useState<{ columnId: string; direction: SortDirection }>({
    columnId: 'title',
    direction: 'asc',
  })
  const { showToast } = useToast()

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      setSearch(searchInput.trim())
      setCurrentPage(1)
    }, 350)

    return () => window.clearTimeout(timeout)
  }, [searchInput])

  useEffect(() => {
    const controller = new AbortController()
    setIsLoading(true)
    setPageError('')

    getBooks(
      {
        search: search || undefined,
        pageNumber: currentPage,
        pageSize: BOOKS_PER_PAGE,
      },
      controller.signal,
    )
      .then((response) => {
        setPage(response)
        if (response.totalPages > 0 && currentPage > response.totalPages) {
          setCurrentPage(response.totalPages)
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
  }, [currentPage, reloadKey, search])

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
      }

      if (editingBook) {
        await updateBook(editingBook.id, input)
        refresh('Đã cập nhật sách thành công.')
      } else {
        await createBook(input)
        setCurrentPage(1)
        refresh('Đã thêm sách vào kho thành công.')
      }

      return null
    } catch (error) {
      return error instanceof Error ? error.message : 'Không thể lưu sách.'
    }
  }

  const confirmDelete = async () => {
    if (!deletingBook) return
    setDeleteError('')
    setIsDeleting(true)

    try {
      await deleteBook(deletingBook.id)
      setDeletingBook(null)
      refresh('Đã xóa sách khỏi kho.')
    } catch (error) {
      setDeleteError(error instanceof Error ? error.message : 'Không thể xóa sách.')
    } finally {
      setIsDeleting(false)
    }
  }

  const inStockCount = page?.items.filter((book) => book.quantity > 0).length ?? 0
  const sortedBooks = useMemo(
    () =>
      [...(page?.items ?? [])].sort((left, right) => {
        const leftValue = left[sort.columnId as keyof LibraryBook]
        const rightValue = right[sort.columnId as keyof LibraryBook]
        return (
          String(leftValue).localeCompare(String(rightValue), 'vi', { numeric: true }) *
          (sort.direction === 'asc' ? 1 : -1)
        )
      }),
    [page?.items, sort],
  )
  const columns = useMemo<DataTableColumn<LibraryBook>[]>(
    () => [
      {
        id: 'title',
        header: 'Sách',
        sortable: true,
        cell: (book) => (
          <span className="grid gap-0.5">
            <strong>{book.title}</strong>
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
            label={book.quantity > 0 ? 'Còn sách' : 'Hết sách'}
            tone={book.quantity > 0 ? 'success' : 'danger'}
          />
        ),
      },
      {
        id: 'actions',
        header: 'Thao tác',
        className: 'text-right',
        cell: (book) => (
          <div className="flex justify-end">
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
                aria-label={`Xóa ${book.title}`}
                onClick={() => {
                  setDeleteError('')
                  setDeletingBook(book)
                }}
              >
                <Trash2 />
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
        title="Kho sách"
        description="Thêm, sửa, xóa và tìm kiếm đầu sách trong kho thư viện."
        actions={
          <>
            <Button
              variant="outline"
              aria-label="Tải lại danh sách sách"
              disabled={isLoading}
              onClick={() => refresh()}
            >
              <RefreshCw className={isLoading ? 'animate-spin' : ''} />
              Làm mới
            </Button>
            <PermissionBoundary requiredPermissions={['books.create']}>
              <Button
                onClick={() => {
                  setEditingBook(null)
                  setFormOpen(true)
                }}
              >
                <Plus /> Thêm sách
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
            <DataTable
              caption="Danh sách sách trong kho"
              rows={sortedBooks}
              columns={columns}
              getRowId={(book) => book.id}
              isLoading={isLoading && !page}
              error={pageError || undefined}
              onRetry={() => refresh()}
              emptyTitle="Chưa có sách trong kho"
              emptyDescription="Thay đổi bộ lọc hoặc thêm đầu sách mới."
              selectedIds={selectedIds}
              onSelectionChange={setSelectedIds}
              bulkActions={
                <Button variant="outline" size="sm" onClick={() => setSelectedIds(new Set())}>
                  Bỏ chọn tất cả
                </Button>
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
              sort={sort}
              onSortChange={(columnId, direction) => setSort({ columnId, direction })}
              page={page?.pageNumber}
              totalPages={page?.totalPages}
              totalCount={page?.totalCount}
              onPageChange={setCurrentPage}
            />
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
          open={deletingBook !== null}
          title="Xóa sách?"
          description={
            deletingBook
              ? `Sách "${deletingBook.title}" sẽ bị xóa khỏi kho. Thao tác này không thể hoàn tác.`
              : ''
          }
          confirmLabel="Xóa sách"
          destructive
          isPending={isDeleting}
          error={deleteError}
          onConfirm={confirmDelete}
          onOpenChange={(open) => !open && setDeletingBook(null)}
        />
      </PermissionBoundary>
    </>
  )
}
