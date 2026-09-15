import { useEffect, useState, type FormEvent } from 'react'
import { Calculator, Sparkles } from 'lucide-react'
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
import { previewFine, type FinePreviewResponse } from '../violation-api'

export type ViolationFormData = {
  borrowerId: string
  bookId: string
  type: string
  note: string
  fineAmount: string
  overdueDays: number
  bookPrice: number
  damageLevel: string
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
    overdueDays: 1,
    bookPrice: 100000,
    damageLevel: 'moderate',
  })
  const [books, setBooks] = useState<LibraryBook[]>([])
  const [users, setUsers] = useState<Member[]>([])
  const [preview, setPreview] = useState<FinePreviewResponse | null>(null)
  const [isPreviewLoading, setIsPreviewLoading] = useState(false)
  const [error, setError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  useEffect(() => {
    if (!open) return

    setForm({
      borrowerId: '',
      bookId: 'none',
      type: 'overdue',
      note: '',
      fineAmount: '0',
      overdueDays: 1,
      bookPrice: 100000,
      damageLevel: 'moderate',
    })
    setPreview(null)
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

  // Tự động tính toán tiền phạt dự kiến khi thay đổi dữ liệu
  useEffect(() => {
    if (!open || !form.borrowerId) {
      setPreview(null)
      return
    }

    const controller = new AbortController()
    setIsPreviewLoading(true)

    previewFine({
      borrowerId: form.borrowerId,
      bookId: form.bookId !== 'none' ? form.bookId : null,
      type: form.type,
      overdueDays: form.type === 'overdue' ? form.overdueDays : 0,
      bookPrice: form.type === 'lost' || form.type === 'damage' ? form.bookPrice : 0,
      damageLevel: form.type === 'damage' ? form.damageLevel : null,
    })
      .then((res) => {
        setPreview(res)
        setForm((prev) => ({ ...prev, fineAmount: String(res.calculatedFine) }))
      })
      .catch(() => {
        // bỏ qua lỗi preview ngầm
      })
      .finally(() => {
        if (!controller.signal.aborted) setIsPreviewLoading(false)
      })

    return () => controller.abort()
  }, [open, form.borrowerId, form.bookId, form.type, form.overdueDays, form.bookPrice, form.damageLevel])

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
      <DialogContent className="max-w-md max-h-[90vh] overflow-y-auto">
        <form onSubmit={handleSubmit}>
          <DialogHeader>
            <DialogTitle>Ghi nhận vi phạm</DialogTitle>
            <DialogDescription>
              Ghi phạt trả trễ hạn, hư hỏng hoặc mất sách theo chính sách lưu thông.
            </DialogDescription>
          </DialogHeader>

          <div className="grid gap-4 py-4">
            <div className="grid gap-1.5">
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

            <div className="grid gap-1.5">
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

            <div className="grid gap-1.5">
              <Label htmlFor="violation-type">Loại vi phạm</Label>
              <Select value={form.type} onValueChange={(type) => setForm({ ...form, type })}>
                <SelectTrigger id="violation-type" className="w-full">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="overdue">Quá hạn trả sách</SelectItem>
                  <SelectItem value="damage">Hư hỏng sách</SelectItem>
                  <SelectItem value="lost">Làm mất sách</SelectItem>
                  <SelectItem value="other">Vi phạm khác</SelectItem>
                </SelectContent>
              </Select>
            </div>

            {/* Trường bổ sung theo loại vi phạm */}
            {form.type === 'overdue' ? (
              <div className="grid gap-1.5">
                <Label htmlFor="violation-days">Số ngày quá hạn</Label>
                <Input
                  id="violation-days"
                  type="number"
                  min={1}
                  max={365}
                  value={form.overdueDays}
                  onChange={(e) => setForm({ ...form, overdueDays: Math.max(1, Number(e.target.value) || 1) })}
                />
              </div>
            ) : null}

            {form.type === 'lost' || form.type === 'damage' ? (
              <div className="grid gap-1.5">
                <Label htmlFor="violation-bookprice">Giá bìa sách (VND)</Label>
                <Input
                  id="violation-bookprice"
                  type="number"
                  min={0}
                  step={5000}
                  value={form.bookPrice}
                  onChange={(e) => setForm({ ...form, bookPrice: Math.max(0, Number(e.target.value) || 0) })}
                />
              </div>
            ) : null}

            {form.type === 'damage' ? (
              <div className="grid gap-1.5">
                <Label htmlFor="damage-level">Mức độ hư hỏng</Label>
                <Select
                  value={form.damageLevel}
                  onValueChange={(damageLevel) => setForm({ ...form, damageLevel })}
                >
                  <SelectTrigger id="damage-level" className="w-full">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="minor">Nhẹ (rách trang, viết bẩn - 20% giá sách)</SelectItem>
                    <SelectItem value="moderate">Vừa (hỏng bìa, ẩm mốc - 50% giá sách)</SelectItem>
                    <SelectItem value="severe">Nặng (rách nát, mất trang quan trọng - 80% giá sách)</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            ) : null}

            {/* Hộp gợi ý công thức tính phạt */}
            {preview ? (
              <div className="rounded-lg border border-primary/20 bg-primary/5 p-3 text-xs space-y-1">
                <div className="flex items-center gap-1.5 font-semibold text-primary">
                  <Sparkles className="size-3.5" />
                  Căn cứ tính phạt theo chính sách:
                </div>
                <p className="text-muted-foreground">{preview.formula}</p>
              </div>
            ) : null}

            <div className="grid gap-1.5">
              <div className="flex justify-between items-center">
                <Label htmlFor="violation-fine">Số tiền phạt áp dụng (VND)</Label>
                {preview ? (
                  <button
                    type="button"
                    onClick={() => setForm({ ...form, fineAmount: String(preview.calculatedFine) })}
                    className="text-[11px] text-primary hover:underline flex items-center gap-1"
                  >
                    <Calculator className="size-3" /> Điền lại mức tính
                  </button>
                ) : null}
              </div>
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

            <div className="grid gap-1.5">
              <Label htmlFor="violation-note">Ghi chú diễn giải</Label>
              <Input
                id="violation-note"
                maxLength={500}
                placeholder="Ví dụ: Trả muộn 3 ngày, bìa sau bị rách góc..."
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
              {isSubmitting ? 'Đang lưu...' : 'Ghi nhận vi phạm'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
