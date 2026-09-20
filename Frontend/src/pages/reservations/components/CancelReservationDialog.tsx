import { useState } from 'react'
import { AlertCircle, AlertTriangle, Loader2 } from 'lucide-react'
import { Button } from '@/common/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/common/components/ui/dialog'
import { Label } from '@/common/components/ui/label'
import { Textarea } from '@/common/components/ui/textarea'
import type { LibraryReservation } from '../reservation-api'

type CancelReservationDialogProps = {
  reservation: LibraryReservation | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onCancelReservation: (reservationId: string, reason?: string, concurrencyToken?: string) => Promise<string | null>
}

export function CancelReservationDialog({
  reservation,
  open,
  onOpenChange,
  onCancelReservation,
}: CancelReservationDialogProps) {
  const [reason, setReason] = useState('')
  const [error, setError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  if (!reservation) return null

  const handleOpenChange = (nextOpen: boolean) => {
    if (isSubmitting) return
    if (nextOpen) {
      setReason('')
      setError('')
    }
    onOpenChange(nextOpen)
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setIsSubmitting(true)

    try {
      const err = await onCancelReservation(reservation.id, reason.trim() || undefined, reservation.concurrencyToken)
      if (err) {
        setError(err)
        return
      }
      onOpenChange(false)
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="max-w-md">
        <form onSubmit={handleSubmit}>
          <DialogHeader>
            <DialogTitle className="text-xl font-bold flex items-center gap-2 text-destructive">
              <AlertTriangle className="size-5" />
              Hủy phiếu đặt trước
            </DialogTitle>
            <DialogDescription>
              Thao tác này sẽ hủy yêu cầu đặt trước sách của độc giả và nhường lượt ưu tiên cho người kế tiếp trong hàng đợi.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-4">
            {error ? (
              <div className="flex items-start gap-2.5 rounded-lg border border-destructive/30 bg-destructive/5 p-3 text-xs text-destructive">
                <AlertCircle className="size-4 shrink-0 mt-0.5" />
                <span>{error}</span>
              </div>
            ) : null}

            <div className="rounded-lg border bg-muted/40 p-3.5 space-y-1.5 text-xs">
              <div className="flex justify-between">
                <span className="text-muted-foreground">Tên sách:</span>
                <span className="font-semibold text-right max-w-[240px] truncate">{reservation.bookTitle}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Độc giả:</span>
                <span className="font-medium text-right">{reservation.reserverName}</span>
              </div>
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="cancel-reason" className="text-xs font-medium">
                Lý do hủy (tùy chọn)
              </Label>
              <Textarea
                id="cancel-reason"
                placeholder="Ví dụ: Độc giả không còn nhu cầu mượn, quá thời hạn liên hệ..."
                value={reason}
                onChange={(e) => setReason(e.target.value)}
                rows={3}
                className="text-sm resize-none"
                disabled={isSubmitting}
              />
            </div>
          </div>

          <DialogFooter className="gap-2 sm:gap-0">
            <Button
              type="button"
              variant="outline"
              onClick={() => onOpenChange(false)}
              disabled={isSubmitting}
            >
              Quay lại
            </Button>
            <Button type="submit" variant="destructive" disabled={isSubmitting}>
              {isSubmitting ? (
                <>
                  <Loader2 className="size-4 animate-spin" /> Đang hủy...
                </>
              ) : (
                'Xác nhận hủy'
              )}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
