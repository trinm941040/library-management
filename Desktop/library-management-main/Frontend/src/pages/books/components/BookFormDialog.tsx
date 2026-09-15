import { useEffect, useState, type FormEvent } from 'react'
import { BarcodeInput, EntityForm, EntityFormField } from '@/common/components'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/common/components/ui/dialog'
import { Input } from '@/common/components/ui/input'
import type { LibraryBook } from '../book-api'

export type BookFormData = {
  title: string
  author: string
  isbn: string
  category: string
  quantity: string
  description: string
  editionStatement: string
  publicationYear: string
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
  description: '',
  editionStatement: '',
  publicationYear: '',
}

export function BookFormDialog({ open, book, onOpenChange, onSave }: BookFormDialogProps) {
  const [form, setForm] = useState<BookFormData>(emptyForm)
  const [fieldErrors, setFieldErrors] = useState<Partial<Record<keyof BookFormData, string>>>({})
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
            description: book.description ?? '',
            editionStatement: book.editionStatement ?? '',
            publicationYear: book.publicationYear ? String(book.publicationYear) : '',
          }
        : emptyForm,
    )
    setFieldErrors({})
    setError('')
  }, [open, book])

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const quantity = Number(form.quantity)
    const nextErrors: Partial<Record<keyof BookFormData, string>> = {}
    if (!form.title.trim()) nextErrors.title = 'Vui lòng nhập tên sách.'
    if (!form.author.trim()) nextErrors.author = 'Vui lòng nhập tác giả.'
    if (form.isbn.trim().length < 10) nextErrors.isbn = 'ISBN phải có ít nhất 10 ký tự.'
    if (!form.category.trim()) nextErrors.category = 'Vui lòng nhập thể loại.'
    if (!Number.isInteger(quantity) || quantity < 0) {
      nextErrors.quantity = 'Số lượng phải là số nguyên không âm.'
    }
    setFieldErrors(nextErrors)
    const firstInvalidField = Object.keys(nextErrors)[0] as keyof BookFormData | undefined
    if (firstInvalidField) {
      document.getElementById(`book-${firstInvalidField}`)?.focus()
      return
    }
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
        <DialogHeader>
          <DialogTitle>{book ? 'Chỉnh sửa sách' : 'Thêm sách mới'}</DialogTitle>
          <DialogDescription>
            {book
              ? 'Cập nhật thông tin đầu sách trong kho.'
              : 'Nhập thông tin sách để thêm vào kho.'}
          </DialogDescription>
        </DialogHeader>
        <EntityForm
          onSubmit={handleSubmit}
          isSubmitting={isSubmitting}
          submitLabel={book ? 'Lưu thay đổi' : 'Thêm sách'}
          serverError={error}
          isConflict={
            error.toLowerCase().includes('thay đổi') || error.toLowerCase().includes('xung đột')
          }
          onCancel={() => onOpenChange(false)}
        >
          <div className="grid gap-5 pt-2">
            <EntityFormField id="book-title" label="Tên sách" error={fieldErrors.title}>
              <Input
                id="book-title"
                required
                aria-invalid={Boolean(fieldErrors.title)}
                aria-describedby={fieldErrors.title ? 'book-title-error' : undefined}
                value={form.title}
                onChange={(event) => {
                  setForm({ ...form, title: event.target.value })
                  setFieldErrors((values) => ({ ...values, title: undefined }))
                }}
              />
            </EntityFormField>
            <EntityFormField id="book-author" label="Tác giả" error={fieldErrors.author}>
              <Input
                id="book-author"
                required
                aria-invalid={Boolean(fieldErrors.author)}
                aria-describedby={fieldErrors.author ? 'book-author-error' : undefined}
                value={form.author}
                onChange={(event) => {
                  setForm({ ...form, author: event.target.value })
                  setFieldErrors((values) => ({ ...values, author: undefined }))
                }}
              />
            </EntityFormField>
            <EntityFormField
              id="book-isbn"
              label="ISBN"
              hint="Có thể quét mã hoặc nhập tay, nhấn Enter để xác nhận."
              error={fieldErrors.isbn}
            >
              <BarcodeInput
                id="book-isbn"
                required
                minLength={10}
                value={form.isbn}
                onChange={(value) => {
                  setForm({ ...form, isbn: value })
                  setFieldErrors((values) => ({ ...values, isbn: undefined }))
                }}
                aria-invalid={Boolean(fieldErrors.isbn)}
                aria-describedby={fieldErrors.isbn ? 'book-isbn-error' : 'book-isbn-hint'}
              />
            </EntityFormField>
            <EntityFormField id="book-category" label="Thể loại" error={fieldErrors.category}>
              <Input
                id="book-category"
                required
                aria-invalid={Boolean(fieldErrors.category)}
                aria-describedby={fieldErrors.category ? 'book-category-error' : undefined}
                value={form.category}
                onChange={(event) => {
                  setForm({ ...form, category: event.target.value })
                  setFieldErrors((values) => ({ ...values, category: undefined }))
                }}
              />
            </EntityFormField>
            <EntityFormField id="book-quantity" label="Số lượng" error={fieldErrors.quantity}>
              <Input
                id="book-quantity"
                type="number"
                min={0}
                required
                aria-invalid={Boolean(fieldErrors.quantity)}
                aria-describedby={fieldErrors.quantity ? 'book-quantity-error' : undefined}
                value={form.quantity}
                onChange={(event) => {
                  setForm({ ...form, quantity: event.target.value })
                  setFieldErrors((values) => ({ ...values, quantity: undefined }))
                }}
              />
            </EntityFormField>
            <EntityFormField id="book-description" label="Mô tả">
              <Input
                id="book-description"
                value={form.description}
                onChange={(event) => setForm({ ...form, description: event.target.value })}
              />
            </EntityFormField>
            <EntityFormField id="book-edition" label="Mô tả ấn bản">
              <Input
                id="book-edition"
                value={form.editionStatement}
                onChange={(event) => setForm({ ...form, editionStatement: event.target.value })}
              />
            </EntityFormField>
            <EntityFormField id="book-year" label="Năm xuất bản">
              <Input
                id="book-year"
                type="number"
                min={0}
                max={9999}
                value={form.publicationYear}
                onChange={(event) => setForm({ ...form, publicationYear: event.target.value })}
              />
            </EntityFormField>
          </div>
        </EntityForm>
      </DialogContent>
    </Dialog>
  )
}
