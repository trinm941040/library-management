import { CheckCircle2, Printer } from 'lucide-react'
import { Badge } from '@/common/components/ui/badge'
import { Button } from '@/common/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/common/components/ui/dialog'
import { PAYMENT_METHOD_LABELS, type FinePaymentMethod, type FinePaymentReceipt } from '../payment-api'

const violationTypeLabels: Record<string, string> = {
  overdue: 'Quá hạn sách',
  damage: 'Hư hỏng sách',
  lost: 'Làm mất sách',
  other: 'Khác',
}

type PaymentReceiptDialogProps = {
  receipt: FinePaymentReceipt | null
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function PaymentReceiptDialog({ receipt, open, onOpenChange }: PaymentReceiptDialogProps) {
  if (!receipt) return null

  const formatMoney = (val?: number) =>
    (val ?? 0).toLocaleString('vi-VN') + ' ₫'

  const formatDate = (val?: string | null) => {
    if (!val) return '—'
    return new Date(val).toLocaleString('vi-VN', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
    })
  }

  const handlePrint = () => {
    window.print()
  }

  const methodLabel =
    PAYMENT_METHOD_LABELS[receipt.method as FinePaymentMethod] ?? receipt.method

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md print:shadow-none print:border-none">
        <DialogHeader className="text-center pb-2 border-b">
          <div className="mx-auto size-12 rounded-full bg-emerald-100 dark:bg-emerald-950 flex items-center justify-center text-emerald-600 dark:text-emerald-400 mb-2">
            <CheckCircle2 className="size-6" />
          </div>
          <DialogTitle className="text-xl font-bold uppercase tracking-wide">
            Biên Nhận Thu Tiền Phạt
          </DialogTitle>
          <DialogDescription className="text-xs text-muted-foreground">
            Hệ thống Quản lý Thư viện UTH · Phiếu thu điện tử
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4 py-3 text-sm">
          {/* Mã biên lai & Thời gian */}
          <div className="flex justify-between items-center text-xs border-b pb-2">
            <div>
              <span className="text-muted-foreground block">Mã biên nhận:</span>
              <span className="font-mono font-semibold">{receipt.id.slice(0, 13).toUpperCase()}</span>
            </div>
            <div className="text-right">
              <span className="text-muted-foreground block">Thời gian thu:</span>
              <span>{formatDate(receipt.paidAtUtc)}</span>
            </div>
          </div>

          {/* Thông tin độc giả */}
          <div className="rounded-lg bg-muted/50 p-3 space-y-1.5 text-xs">
            <div className="flex justify-between">
              <span className="text-muted-foreground">Độc giả:</span>
              <span className="font-medium text-foreground">{receipt.memberName}</span>
            </div>
            {receipt.memberCode && (
              <div className="flex justify-between">
                <span className="text-muted-foreground">Mã độc giả / thẻ:</span>
                <span className="font-mono font-medium">{receipt.memberCode}</span>
              </div>
            )}
            <div className="flex justify-between">
              <span className="text-muted-foreground">Lý do thu phạt:</span>
              <span className="font-medium">
                {violationTypeLabels[receipt.violationType] ?? receipt.violationType}
              </span>
            </div>
            {receipt.bookTitle && (
              <div className="flex justify-between">
                <span className="text-muted-foreground">Tên tài liệu / sách:</span>
                <span className="font-medium truncate max-w-[220px]" title={receipt.bookTitle}>
                  {receipt.bookTitle}
                </span>
              </div>
            )}
          </div>

          {/* Số tiền thu & biến động số dư */}
          <div className="rounded-lg border-2 border-emerald-500/20 bg-emerald-50/50 dark:bg-emerald-950/20 p-4 text-center space-y-2">
            <span className="text-xs text-muted-foreground block uppercase tracking-wider font-semibold">
              Số tiền đã thu
            </span>
            <div className="text-3xl font-extrabold text-emerald-600 dark:text-emerald-400 font-mono">
              {formatMoney(receipt.amount)}
            </div>
            <div className="pt-2 border-t border-emerald-500/20 flex justify-between items-center text-xs">
              <span className="text-muted-foreground">Số dư nợ còn lại:</span>
              {receipt.remainingBalance > 0 ? (
                <span className="font-mono font-bold text-rose-600 dark:text-rose-400">
                  {formatMoney(receipt.remainingBalance)}
                </span>
              ) : (
                <Badge variant="secondary" className="bg-emerald-600 text-white font-normal">
                  Đã thanh toán đủ (0 ₫)
                </Badge>
              )}
            </div>
          </div>

          {/* Thông tin giao dịch & người thu */}
          <div className="space-y-1.5 text-xs text-muted-foreground pt-1">
            <div className="flex justify-between">
              <span>Phương thức:</span>
              <span className="font-medium text-foreground">{methodLabel}</span>
            </div>
            <div className="flex justify-between">
              <span>Mã tham chiếu:</span>
              <span className="font-mono font-medium text-foreground">{receipt.reference}</span>
            </div>
            <div className="flex justify-between">
              <span>Người thu tiền:</span>
              <span className="font-medium text-foreground">
                {receipt.receivedByUserName || 'Thủ thư / Hệ thống'}
              </span>
            </div>
            <div className="flex justify-between">
              <span>Trạng thái vi phạm:</span>
              <Badge variant={receipt.isFullyPaid ? 'secondary' : 'default'} className="text-[11px]">
                {receipt.isFullyPaid ? 'Đã hoàn tất thanh toán' : 'Thanh toán một phần'}
              </Badge>
            </div>
          </div>
        </div>

        <DialogFooter className="print:hidden flex sm:justify-between items-center gap-2 border-t pt-3">
          <Button variant="outline" size="sm" onClick={handlePrint} className="gap-1.5">
            <Printer className="size-4" /> In biên nhận
          </Button>
          <Button size="sm" onClick={() => onOpenChange(false)}>
            Xong
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
