import { ArrowLeft, RefreshCw } from 'lucide-react'
import { useCallback, useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Button } from '@/common/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/common/components/ui/card'
import { LoadingBoundary, PageShell, ScreenState, StatusBadge, useToast } from '@/common/components'
import { getBook, type LibraryBook } from './book-api'

export function CatalogDetailPage() {
  const { bookId } = useParams<{ bookId: string }>()
  const navigate = useNavigate()
  const { showToast } = useToast()
  const [book, setBook] = useState<LibraryBook | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState('')

  const load = useCallback(() => {
    if (!bookId) return
    const controller = new AbortController()
    setIsLoading(true)
    setError('')
    getBook(bookId, controller.signal)
      .then(setBook)
      .catch((reason: unknown) => {
        if (reason instanceof DOMException && reason.name === 'AbortError') return
        setError(reason instanceof Error ? reason.message : 'Không thể tải biểu ghi.')
      })
      .finally(() => setIsLoading(false))
    return () => controller.abort()
  }, [bookId])

  useEffect(() => load(), [load])

  if (isLoading) return <LoadingBoundary loading label="Đang tải biểu ghi" className="min-h-[40vh]" />
  if (error) return <ScreenState kind="error" title="Không thể tải biểu ghi" description={error} actionLabel="Thử lại" onAction={load} />
  if (!book) return <ScreenState kind="empty" title="Không tìm thấy biểu ghi" />

  return (
    <PageShell
      eyebrow="Biên mục"
      title={book.title}
      description="Chi tiết biểu ghi sách và quan hệ biên mục chuẩn hóa."
      actions={<><Button variant="outline" onClick={() => navigate('/catalog')}><ArrowLeft /> Quay lại</Button><Button variant="outline" onClick={() => { load(); showToast('Đã làm mới biểu ghi.') }}><RefreshCw /> Làm mới</Button></>}
    >
      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <CardTitle>{book.title}</CardTitle>
          <StatusBadge label={book.status === 'Inactive' ? 'Ngừng sử dụng' : 'Đang sử dụng'} tone={book.status === 'Inactive' ? 'danger' : 'success'} />
        </CardHeader>
        <CardContent className="grid gap-4 md:grid-cols-2">
          <Detail label="ISBN" value={book.isbn} />
          <Detail label="Tác giả" value={book.authors?.map((item) => item.name).join(', ') || book.author} />
          <Detail label="Thể loại" value={book.categories?.map((item) => item.name).join(', ') || book.category} />
          <Detail label="Nhà xuất bản" value={book.publisher?.name || 'Chưa cập nhật'} />
          <Detail label="Bản sao khả dụng" value={String(book.availableCopyCount ?? book.quantity)} />
          <Detail label="Ngôn ngữ" value={book.language || 'Chưa cập nhật'} />
          <Detail label="Năm xuất bản" value={book.publicationYear ? String(book.publicationYear) : 'Chưa cập nhật'} />
          <Detail label="Số trang" value={book.pageCount ? String(book.pageCount) : 'Chưa cập nhật'} />
          <Detail label="Ấn bản" value={book.editionStatement || 'Chưa cập nhật'} />
          <Detail label="Mô tả" value={book.description || 'Chưa cập nhật'} />
        </CardContent>
      </Card>
    </PageShell>
  )
}

function Detail({ label, value }: { label: string; value: string }) {
  return <dl><dt className="text-sm text-muted-foreground">{label}</dt><dd className="mt-1 font-medium">{value}</dd></dl>
}
