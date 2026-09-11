import { useEffect, useState, type FormEvent } from 'react'
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
import type { LibraryBook } from '../book-api'

export type BookFormData = {
  title: string
  author: string
  isbn: string
  category: string
  quantity: string
}

type BookFormDialogProps = {
  open: boolean
  book: LibraryBook | null
  onOpenChange: (open: boolean) => void
  onSave: (data: BookFormData) => Promise<string | null>
}

const emptyForm: BookFormData = {
  title: '',
  author: '',
  isbn: '',
  category: '',
  quantity: '1',
}

export function BookFormDialog({ open, book, onOpenChange, onSave }: BookFormDialogProps) {
  const [form, setForm] = useState<BookFormData>(emptyForm)
  const [error, setError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  useEffect(() => {
    setForm(
      book
        ? {
            title: book.title,
            author: book.author,
            isbn: book.isbn,
            category: book.category,
            quantity: String(book.quantity),
          }
        : emptyForm,
    )
    setError('')
  }, [open, book])

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError('')
    setIsSubmitting(true)

    try {
      const saveError = await onSave(form)
      if (saveError) {
        setError(saveError)
        return
      }
      onOpenChange(false)
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={(nextOpen) => !isSubmitting && onOpenChange(nextOpen)}>
      <DialogContent>
        <form onSubmit={handleSubmit}>
          <DialogHeader>
            <DialogTitle>{book ? 'Chỉnh sửa sách' : 'Thêm sách mới'}</DialogTitle>
            <DialogDescription>
              {book
                ? 'Cập nhật thông tin đầu sách trong kho.'
                : 'Nhập thông tin sách để thêm vào kho.'}
            </DialogDescription>
          </DialogHeader>

          <div className="grid gap-5 py-6">
            <div className="grid gap-2">
              <Label htmlFor="book-title">Tên sách</Label>
              <Input
                id="book-title"
                required
                value={form.title}
                onChange={(event) => setForm({ ...form, title: event.target.value })}
              />
            </div>
            <div className="grid gap-2">
              <Label htmlFor="book-author">Tác giả</Label>
              <Input
                id="book-author"
                required
                value={form.author}
                onChange={(event) => setForm({ ...form, author: event.target.value })}
              />
            </div>
            <div className="grid gap-2">
              <Label htmlFor="book-isbn">ISBN</Label>
              <Input
                id="book-isbn"
                required
                minLength={10}
                value={form.isbn}
                onChange={(event) => setForm({ ...form, isbn: event.target.value })}
              />
            </div>
            <div className="grid gap-2">
              <Label htmlFor="book-category">Thể loại</Label>
              <Input
                id="book-category"
                required
                value={form.category}
                onChange={(event) => setForm({ ...form, category: event.target.value })}
              />
            </div>
            <div className="grid gap-2">
              <Label htmlFor="book-quantity">Số lượng</Label>
              <Input
                id="book-quantity"
                type="number"
                min={0}
                required
                value={form.quantity}
                onChange={(event) => setForm({ ...form, quantity: event.target.value })}
              />
            </div>
            {error ? (
              <p className="text-sm text-destructive" role="alert">
                {error}
              </p>
            ) : null}
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" disabled={isSubmitting} onClick={() => onOpenChange(false)}>
              Hủy
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? 'Đang lưu...' : 'Lưu'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
