import { useCallback, useEffect, useState } from 'react'
import {
  BookOpen,
  CircleAlert,
  Layers,
  Pencil,
  Plus,
  RefreshCw,
  Search,
  Trash2,
} from 'lucide-react'
import { Badge } from '@/common/components/ui/badge'
import { Button } from '@/common/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/common/components/ui/card'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/common/components/ui/dialog'
import { Input } from '@/common/components/ui/input'
import { Pagination } from '@/common/components/ui/pagination'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/common/components/ui/table'
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
  const [notice, setNotice] = useState('')
  const [formOpen, setFormOpen] = useState(false)
  const [editingBook, setEditingBook] = useState<LibraryBook | null>(null)
  const [deletingBook, setDeletingBook] = useState<LibraryBook | null>(null)
  const [deleteError, setDeleteError] = useState('')
  const [isDeleting, setIsDeleting] = useState(false)

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

  const refresh = useCallback((message?: string) => {
    if (message) setNotice(message)
    setReloadKey((value) => value + 1)
  }, [])

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

  const displayedFrom = page && page.totalCount > 0 ? (page.pageNumber - 1) * page.pageSize + 1 : 0
  const displayedTo = page ? Math.min(page.pageNumber * page.pageSize, page.totalCount) : 0
  const inStockCount = page?.items.filter((book) => book.quantity > 0).length ?? 0

  return (
    <>
      <div className="mx-auto w-full max-w-7xl px-5 py-10 md:px-12">
        <div className="mb-8 flex flex-col justify-between gap-4 sm:flex-row sm:items-end">
          <div>
            <p className="mb-2 text-xs font-bold tracking-widest text-primary uppercase">
              quản lý tác vụ
            </p>
            <h1 className="text-3xl font-bold tracking-tight">Kho sách</h1>
            <p className="mt-2 text-sm text-muted-foreground">
              Thêm, sửa, xóa và tìm kiếm đầu sách trong kho thư viện.
            </p>
          </div>
          <div className="flex gap-2">
            <Button
              variant="outline"
              aria-label="Tải lại danh sách sách"
              disabled={isLoading}
              onClick={() => refresh()}
            >
              <RefreshCw className={isLoading ? 'animate-spin' : ''} />
              Làm mới
            </Button>
            <Button
              onClick={() => {
                setEditingBook(null)
                setFormOpen(true)
              }}
            >
              <Plus /> Thêm sách
            </Button>
          </div>
        </div>

        {notice ? (
          <div
            className="mb-6 flex items-center justify-between gap-3 rounded-lg border border-primary/20 bg-primary/5 px-4 py-3 text-sm"
            role="status"
          >
            <span className="flex items-center gap-2">
              <BookOpen className="size-4 text-primary" />
              {notice}
            </span>
            <Button variant="ghost" size="xs" onClick={() => setNotice('')}>
              Đóng
            </Button>
          </div>
        ) : null}

        <div className="mb-6 grid gap-4 sm:grid-cols-2">
          <Card>
            <CardHeader className="flex flex-row items-center justify-between">
              <CardTitle className="text-sm font-medium text-muted-foreground">Tổng đầu sách</CardTitle>
              <Layers className="size-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <p className="text-2xl font-bold">{page?.totalCount ?? '—'}</p>
            </CardContent>
          </Card>
          <Card>
            <CardHeader className="flex flex-row items-center justify-between">
              <CardTitle className="text-sm font-medium text-muted-foreground">Còn trong kho (trang này)</CardTitle>
              <BookOpen className="size-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <p className="text-2xl font-bold">{page ? inStockCount : '—'}</p>
            </CardContent>
          </Card>
        </div>

        <Card>
          <CardHeader className="gap-4">
            <div className="flex flex-col justify-between gap-4 lg:flex-row lg:items-center">
              <div>
                <CardTitle>Danh sách sách</CardTitle>
                <p className="mt-1 text-sm text-muted-foreground">
                  {page ? `${page.totalCount} đầu sách phù hợp với bộ lọc.` : 'Đang tải dữ liệu.'}
                </p>
              </div>
              <div className="relative w-full lg:w-80">
                <Search className="absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  className="pl-9"
                  placeholder="Tìm theo tên, tác giả hoặc ISBN..."
                  value={searchInput}
                  onChange={(event) => setSearchInput(event.target.value)}
                />
              </div>
            </div>
          </CardHeader>
          <CardContent>
            {pageError ? (
              <div
                className="mb-4 flex flex-col items-center gap-3 rounded-lg border border-destructive/30 bg-destructive/5 p-6 text-center"
                role="alert"
              >
                <CircleAlert className="size-6 text-destructive" />
                <div>
                  <p className="font-medium">Không thể tải danh sách sách</p>
                  <p className="mt-1 text-sm text-muted-foreground">{pageError}</p>
                </div>
                <Button variant="outline" size="sm" onClick={() => refresh()}>
                  Thử lại
                </Button>
              </div>
            ) : null}

            <div className="overflow-x-auto rounded-md border">
              <Table aria-busy={isLoading}>
                <TableHeader>
                  <TableRow>
                    <TableHead>Sách</TableHead>
                    <TableHead>ISBN</TableHead>
                    <TableHead>Thể loại</TableHead>
                    <TableHead>Số lượng</TableHead>
                    <TableHead>Trạng thái</TableHead>
                    <TableHead className="text-right">Thao tác</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {isLoading && !page ? (
                    <TableRow>
                      <TableCell colSpan={6} className="h-32 text-center text-muted-foreground">
                        Đang tải...
                      </TableCell>
                    </TableRow>
                  ) : null}
                  {!isLoading && !pageError && page?.items.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={6} className="h-32 text-center text-muted-foreground">
                        Chưa có sách trong kho hoặc không tìm thấy kết quả.
                      </TableCell>
                    </TableRow>
                  ) : null}
                  {page?.items.map((book) => (
                    <TableRow key={book.id} className={isLoading ? 'opacity-60' : undefined}>
                      <TableCell>
                        <span className="grid gap-0.5">
                          <strong>{book.title}</strong>
                          <small className="text-muted-foreground">{book.author}</small>
                        </span>
                      </TableCell>
                      <TableCell>{book.isbn}</TableCell>
                      <TableCell>{book.category}</TableCell>
                      <TableCell>{book.quantity}</TableCell>
                      <TableCell>
                        <Badge variant={book.quantity > 0 ? 'secondary' : 'destructive'}>
                          {book.quantity > 0 ? 'Còn sách' : 'Hết sách'}
                        </Badge>
                      </TableCell>
                      <TableCell className="text-right">
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
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>

            {page && page.totalPages > 1 ? (
              <div className="mt-4 flex items-center justify-between gap-3">
                <p className="text-sm text-muted-foreground">
                  Hiển thị {displayedFrom}-{displayedTo} / {page.totalCount}
                </p>
                <Pagination
                  currentPage={page.pageNumber}
                  totalPages={page.totalPages}
                  onPageChange={setCurrentPage}
                />
              </div>
            ) : null}
          </CardContent>
        </Card>
      </div>

      <BookFormDialog
        open={formOpen}
        book={editingBook}
        onOpenChange={setFormOpen}
        onSave={handleSave}
      />

      <Dialog open={deletingBook !== null} onOpenChange={(open) => !open && !isDeleting && setDeletingBook(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Xóa sách?</DialogTitle>
            <DialogDescription>
              {deletingBook
                ? `Sách "${deletingBook.title}" sẽ bị xóa khỏi kho. Thao tác này không thể hoàn tác.`
                : ''}
            </DialogDescription>
          </DialogHeader>
          {deleteError ? (
            <p className="text-sm text-destructive" role="alert">
              {deleteError}
            </p>
          ) : null}
          <DialogFooter>
            <Button variant="outline" disabled={isDeleting} onClick={() => setDeletingBook(null)}>
              Hủy
            </Button>
            <Button variant="destructive" disabled={isDeleting} onClick={confirmDelete}>
              {isDeleting ? 'Đang xóa...' : 'Xóa sách'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  )
}
