import { useState } from 'react'
import { AlertCircle, Check, QrCode } from 'lucide-react'
import { Spinner } from '@/common/components/atoms/Spinner'
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
import type { LibraryReservation } from '../reservation-api'

type FulfillReservationDialogProps = {
  reservation: LibraryReservation | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onFulfill: (reservationId: string, barcode?: string, concurrencyToken?: string) => Promise<string | null>
}

export function FulfillReservationDialog({
  reservation,
  open,
  onOpenChange,
  onFulfill,
}: FulfillReservationDialogProps) {
  const [barcode, setBarcode] = useState('')
  const [error, setError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  if (!reservation) return null

  const handleOpenChange = (nextOpen: boolean) => {
    if (isSubmitting) return
    if (nextOpen) {
      setBarcode('')
      setError('')
    }
    onOpenChange(nextOpen)
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setIsSubmitting(true)

    try {
      const err = await onFulfill(reservation.id, barcode.trim() || undefined, reservation.concurrencyToken)
      if (err) {
        setError(err)
        return
      }
      onOpenChange(false)
    } finally {
      setIsSubmitting(false)
    }
  }

  // Dự kiến ngày trả (14 ngày sau hôm nay)
  const estimatedDueDate = new Date()
  estimatedDueDate.setDate(estimatedDueDate.getDate() + 14)
  const formattedDueDate = estimatedDueDate.toLocaleDateString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  })

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="max-w-md">
        <form onSubmit={handleSubmit}>
          <DialogHeader>
            <DialogTitle className="text-xl font-bold flex items-center gap-2">
              <Check className="size-5 text-emerald-600" />
              Xác nhận nhận sách
            </DialogTitle>
            <DialogDescription>
              Hoàn tất phiếu đặt trước và chuyển thành bản ghi mượn sách chính thức cho độc giả.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-4">
            {error ? (
              <div className="flex items-start gap-2.5 rounded-lg border border-destructive/30 bg-destructive/5 p-3 text-xs text-destructive">
                <AlertCircle className="size-4 shrink-0 mt-0.5" />
                <span>{error}</span>
              </div>
            ) : null}

            <div className="rounded-lg border bg-muted/40 p-3.5 space-y-2 text-xs">
              <div className="flex justify-between">
                <span className="text-muted-foreground">Tên sách:</span>
                <span className="font-semibold text-right max-w-[240px] truncate">{reservation.bookTitle}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Người nhận:</span>
                <span className="font-medium text-right">{reservation.reserverName}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Ngày hẹn trả dự kiến:</span>
                <span className="font-semibold text-primary">{formattedDueDate}</span>
              </div>
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="barcode" className="text-xs font-medium">
                Mã vạch bản sao sách (tùy chọn)
              </Label>
              <div className="relative">
                <QrCode className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground" />
                <Input
                  id="barcode"
                  placeholder="Để trống hệ thống sẽ tự động gán bản sao khả dụng..."
                  value={barcode}
                  onChange={(e) => setBarcode(e.target.value)}
                  className="pl-9 text-sm"
                  disabled={isSubmitting}
                />
              </div>
              <p className="text-[11px] text-muted-foreground">
                Nếu thư viện đã dán mã vạch cho cuốn sách trao cho độc giả, vui lòng quét hoặc nhập mã vạch trên.
              </p>
            </div>
          </div>

          <DialogFooter className="gap-2 sm:gap-0">
            <Button
              type="button"
              variant="outline"
              onClick={() => onOpenChange(false)}
              disabled={isSubmitting}
            >
              Đóng
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? (
                <>
                  <Spinner size="sm" decorative /> Đang xử lý...
                </>
              ) : (
                'Xác nhận nhận sách'
              )}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
