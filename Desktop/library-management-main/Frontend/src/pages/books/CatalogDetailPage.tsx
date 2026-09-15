import { ArrowLeft, Pencil, RefreshCw } from 'lucide-react'
import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { Button } from '@/common/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/common/components/ui/card'
import { PageShell, ScreenState, StatusBadge, useToast } from '@/common/components'
import { PermissionBoundary } from '@/shared/auth/PermissionBoundary'
import { getBook, type LibraryBook } from './book-api'

export function CatalogDetailPage() {
  const { bookId } = useParams<{ bookId: string }>()
  const navigate = useNavigate()
  const { showToast } = useToast()
  const [book, setBook] = useState<LibraryBook | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState('')

  const load = () => {
    if (!bookId) return
    setIsLoading(true)
    setError('')
    getBook(bookId)
      .then(setBook)
      .catch((reason: unknown) => {
        setError(reason instanceof Error ? reason.message : 'Không thể tải biểu ghi.')
      })
      .finally(() => setIsLoading(false))
  }

  useEffect(load, [bookId])

  if (isLoading) return <p className="p-6 text-center">Đang tải biểu ghi...</p>
  if (error) {
    return (
      <ScreenState
        kind="error"
        title="Không thể tải biểu ghi"
        description={error}
        actionLabel="Thử lại"
        onAction={load}
      />
    )
  }
  if (!book) return <ScreenState kind="empty" title="Không tìm thấy biểu ghi" />

  return (
    <PageShell
      eyebrow="Biên mục"
      title={book.title}
      description="Chi tiết biểu ghi sách và các quan hệ biên mục chuẩn hóa."
      actions={
        <>
          <Button variant="outline" onClick={() => navigate('/catalog')}>
            <ArrowLeft /> Quay lại
          </Button>
          <Button variant="outline" onClick={() => { load(); showToast('Đã làm mới biểu ghi.') }}>
            <RefreshCw /> Làm mới
          </Button>
          <PermissionBoundary requiredPermissions={['books.update']}>
            <Button asChild>
              <Link to={`/catalog/${book.id}/edit`}><Pencil /> Chỉnh sửa</Link>
            </Button>
          </PermissionBoundary>
        </>
      }
    >
      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <CardTitle>{book.title}</CardTitle>
          <StatusBadge label={book.status === 'Inactive' ? 'Ngừng sử dụng' : 'Đang sử dụng'} tone={book.status === 'Inactive' ? 'danger' : 'success'} />
        </CardHeader>
        <CardContent className="grid gap-4 md:grid-cols-2">
          <Detail label="ISBN" value={book.isbn} />
          <Detail label="Tác giả" value={book.authors?.map((author) => author.name).join(', ') || book.author} />
          <Detail label="Thể loại" value={book.categories?.map((category) => category.name).join(', ') || book.category} />
          <Detail label="Nhà xuất bản" value={book.publisher?.name || 'Chưa cập nhật'} />
          <Detail label="Bản sao khả dụng" value={String(book.availableCopyCount ?? book.quantity)} />
          <Detail label="Năm xuất bản" value={book.publicationYear ? String(book.publicationYear) : 'Chưa cập nhật'} />
          <Detail label="Ấn bản" value={book.editionStatement || 'Chưa cập nhật'} />
          <Detail label="Mô tả" value={book.description || 'Chưa cập nhật'} />
        </CardContent>
      </Card>
    </PageShell>
  )
}

function Detail({ label, value }: { label: string; value: string }) {
  return <div><dt className="text-sm text-muted-foreground">{label}</dt><dd className="mt-1 font-medium">{value}</dd></div>
}
