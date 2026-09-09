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

export type ViolationFormData = {
  borrowerId: string
  bookId: string
  type: string
  note: string
  fineAmount: string
}

type ViolationFormDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSave: (data: ViolationFormData) => Promise<string | null>
}

export function ViolationFormDialog({ open, onOpenChange, onSave }: ViolationFormDialogProps) {
  const [form, setForm] = useState<ViolationFormData>({
    borrowerId: '',
    bookId: 'none',
    type: 'overdue',
    note: '',
    fineAmount: '0',
  })
  const [books, setBooks] = useState<LibraryBook[]>([])
  const [users, setUsers] = useState<Member[]>([])
  const [error, setError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  useEffect(() => {
    if (!open) return

    setForm({ borrowerId: '', bookId: 'none', type: 'overdue', note: '', fineAmount: '0' })
    setError('')

    const controller = new AbortController()
    Promise.all([
      getBooks({ pageNumber: 1, pageSize: 100 }, controller.signal),
      getMembers({ status: 'Active', pageNumber: 1, pageSize: 100 }, controller.signal),
    ])
      .then(([bookPage, userPage]) => {
        setBooks(bookPage.items)
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
            <DialogTitle>Ghi nhận vi phạm</DialogTitle>
            <DialogDescription>Ghi phạt trễ hạn, hư hỏng hoặc mất sách cho độc giả.</DialogDescription>
          </DialogHeader>

          <div className="grid gap-5 py-6">
            <div className="grid gap-2">
              <Label htmlFor="violation-borrower">Độc giả</Label>
              <Select
                value={form.borrowerId}
                onValueChange={(borrowerId) => setForm({ ...form, borrowerId })}
              >
                <SelectTrigger id="violation-borrower" className="w-full">
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
              <Label htmlFor="violation-book">Sách liên quan</Label>
              <Select value={form.bookId} onValueChange={(bookId) => setForm({ ...form, bookId })}>
                <SelectTrigger id="violation-book" className="w-full">
                  <SelectValue placeholder="Không gắn sách" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="none">Không gắn sách</SelectItem>
                  {books.map((book) => (
                    <SelectItem key={book.id} value={book.id}>
                      {book.title}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="grid gap-2">
              <Label htmlFor="violation-type">Loại vi phạm</Label>
              <Select value={form.type} onValueChange={(type) => setForm({ ...form, type })}>
                <SelectTrigger id="violation-type" className="w-full">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="overdue">Trễ hạn</SelectItem>
                  <SelectItem value="damage">Hư hỏng sách</SelectItem>
                  <SelectItem value="lost">Mất sách</SelectItem>
                  <SelectItem value="other">Khác</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="grid gap-2">
              <Label htmlFor="violation-fine">Số tiền phạt (VND)</Label>
              <Input
                id="violation-fine"
                type="number"
                min={0}
                step={1000}
                required
                value={form.fineAmount}
                onChange={(event) => setForm({ ...form, fineAmount: event.target.value })}
              />
            </div>
            <div className="grid gap-2">
              <Label htmlFor="violation-note">Ghi chú</Label>
              <Input
                id="violation-note"
                maxLength={500}
                value={form.note}
                onChange={(event) => setForm({ ...form, note: event.target.value })}
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
            <Button type="submit" disabled={isSubmitting || !form.borrowerId}>
              {isSubmitting ? 'Đang lưu...' : 'Ghi nhận'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
