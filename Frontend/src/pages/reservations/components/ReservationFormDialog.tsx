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
import { getUsers, type SystemUser } from '@/pages/users/user-api'

export type ReservationFormData = {
  bookId: string
  reserverId: string
  holdDays: string
}

type ReservationFormDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSave: (data: ReservationFormData) => Promise<string | null>
}

export function ReservationFormDialog({ open, onOpenChange, onSave }: ReservationFormDialogProps) {
  const [form, setForm] = useState<ReservationFormData>({ bookId: '', reserverId: '', holdDays: '7' })
  const [books, setBooks] = useState<LibraryBook[]>([])
  const [users, setUsers] = useState<SystemUser[]>([])
  const [error, setError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  useEffect(() => {
    if (!open) return

    setForm({ bookId: '', reserverId: '', holdDays: '7' })
    setError('')

    const controller = new AbortController()
    Promise.all([
      getBooks({ pageNumber: 1, pageSize: 100 }, controller.signal),
      getUsers({ isActive: true, pageNumber: 1, pageSize: 100 }, controller.signal),
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
            <DialogTitle>Tạo phiếu đặt trước</DialogTitle>
            <DialogDescription>
              Giữ chỗ sách cho độc giả. Khi sách có trong kho, có thể chuyển thành phiếu mượn.
            </DialogDescription>
          </DialogHeader>

          <div className="grid gap-5 py-6">
            <div className="grid gap-2">
              <Label htmlFor="reservation-book">Sách</Label>
              <Select value={form.bookId} onValueChange={(bookId) => setForm({ ...form, bookId })}>
                <SelectTrigger id="reservation-book" className="w-full">
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
              <Label htmlFor="reservation-reserver">Độc giả</Label>
              <Select
                value={form.reserverId}
                onValueChange={(reserverId) => setForm({ ...form, reserverId })}
              >
                <SelectTrigger id="reservation-reserver" className="w-full">
                  <SelectValue placeholder="Chọn độc giả" />
                </SelectTrigger>
                <SelectContent>
                  {users.map((user) => (
                    <SelectItem key={user.id} value={user.id}>
                      {user.displayName} · {user.email}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="grid gap-2">
              <Label htmlFor="reservation-days">Số ngày giữ chỗ</Label>
              <Input
                id="reservation-days"
                type="number"
                min={1}
                max={365}
                required
                value={form.holdDays}
                onChange={(event) => setForm({ ...form, holdDays: event.target.value })}
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
            <Button type="submit" disabled={isSubmitting || !form.bookId || !form.reserverId}>
              {isSubmitting ? 'Đang lưu...' : 'Tạo phiếu đặt trước'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
