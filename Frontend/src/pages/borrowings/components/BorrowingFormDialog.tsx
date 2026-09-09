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
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/common/components/ui/select'
import { getBooks, type LibraryBook } from '@/pages/books/book-api'
import { getMembers, type Member } from '@/pages/members/member-api'

export type BorrowingFormData = {
  bookId: string
  borrowerId: string
  loanDays: string
}

type BorrowingFormDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSave: (data: BorrowingFormData) => Promise<string | null>
}

export function BorrowingFormDialog({ open, onOpenChange, onSave }: BorrowingFormDialogProps) {
  const [form, setForm] = useState<BorrowingFormData>({ bookId: '', borrowerId: '', loanDays: '14' })
  const [books, setBooks] = useState<LibraryBook[]>([])
  const [users, setUsers] = useState<Member[]>([])
  const [error, setError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  useEffect(() => {
    if (!open) return

    setForm({ bookId: '', borrowerId: '', loanDays: '14' })
    setError('')

    const controller = new AbortController()
    Promise.all([
      getBooks({ pageNumber: 1, pageSize: 100 }, controller.signal),
      getMembers({ status: 'Active', pageNumber: 1, pageSize: 100 }, controller.signal),
    ])
      .then(([bookPage, userPage]) => {
        setBooks(bookPage.items.filter((book) => book.quantity > 0))
        setUsers(userPage.items)
      })
      .catch((caught: unknown) => {
        if (!(caught instanceof DOMException && caught.name === 'AbortError')) {
          setError(caught instanceof Error ? caught.message : 'Không thể tải sách hoặc độc giả.')
        }
      })

    return () => controller.abort()
  }, [open])

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
            <DialogTitle>Tạo phiếu mượn</DialogTitle>
            <DialogDescription>Chọn sách còn trong kho và độc giả đang hoạt động.</DialogDescription>
          </DialogHeader>

          <div className="grid gap-5 py-6">
            <div className="grid gap-2">
              <Label htmlFor="borrowing-book">Sách</Label>
              <Select value={form.bookId} onValueChange={(bookId) => setForm({ ...form, bookId })}>
                <SelectTrigger id="borrowing-book" className="w-full">
                  <SelectValue placeholder="Chọn sách" />
                </SelectTrigger>
                <SelectContent>
                  {books.map((book) => (
                    <SelectItem key={book.id} value={book.id}>
                      {book.title} · còn {book.quantity}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="grid gap-2">
              <Label htmlFor="borrowing-borrower">Độc giả</Label>
              <Select
                value={form.borrowerId}
                onValueChange={(borrowerId) => setForm({ ...form, borrowerId })}
              >
                <SelectTrigger id="borrowing-borrower" className="w-full">
                  <SelectValue placeholder="Chọn độc giả" />
                </SelectTrigger>
                <SelectContent>
                  {users.map((user) => (
                    <SelectItem key={user.id} value={user.id}>
                      {user.fullName} · {user.email}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="grid gap-2">
              <Label htmlFor="borrowing-days">Số ngày mượn</Label>
              <Input
                id="borrowing-days"
                type="number"
                min={1}
                max={365}
                required
                value={form.loanDays}
                onChange={(event) => setForm({ ...form, loanDays: event.target.value })}
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
            <Button type="submit" disabled={isSubmitting || !form.bookId || !form.borrowerId}>
              {isSubmitting ? 'Đang lưu...' : 'Tạo phiếu mượn'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
