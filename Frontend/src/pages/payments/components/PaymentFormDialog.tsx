import { useEffect, useState } from 'react'
import { AlertCircle, CreditCard, Loader2, ShieldCheck } from 'lucide-react'
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
import { Input } from '@/common/components/ui/input'
import { Label } from '@/common/components/ui/label'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/common/components/ui/select'
import {
  createPayment,
  getPaymentPreview,
  PAYMENT_METHOD_LABELS,
  type FinePaymentMethod,
  type FinePaymentPreview,
  type FinePaymentReceipt,
} from '../payment-api'

const violationTypeLabels: Record<string, string> = {
  overdue: 'Quá hạn sách',
  damage: 'Hư hỏng sách',
  lost: 'Làm mất sách',
  other: 'Khác',
}

type PaymentFormDialogProps = {
  violationId: string | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onSuccess: (receipt: FinePaymentReceipt) => void
}

export function PaymentFormDialog({
  violationId,
  open,
  onOpenChange,
  onSuccess,
}: PaymentFormDialogProps) {
  const [preview, setPreview] = useState<FinePaymentPreview | null>(null)
  const [isLoadingPreview, setIsLoadingPreview] = useState(false)
  const [previewError, setPreviewError] = useState('')

  const [paymentOption, setPaymentOption] = useState<'full' | 'partial'>('full')
  const [amountInput, setAmountInput] = useState('')
  const [method, setMethod] = useState<FinePaymentMethod>('Cash')
  const [reference, setReference] = useState('')
  const [idempotencyKey, setIdempotencyKey] = useState('')

  const [isSubmitting, setIsSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState('')

  // Khi mở dialog, lấy preview và sinh idempotency key mới
  useEffect(() => {
    if (!open || !violationId) {
      setPreview(null)
      setPreviewError('')
      setSubmitError('')
      setAmountInput('')
      setPaymentOption('full')
      setReference('')
      return
    }

    // Sinh idempotency key duy nhất cho phiên thanh toán
    setIdempotencyKey(crypto.randomUUID ? crypto.randomUUID() : `IDEMP-${Date.now()}-${Math.random().toString(36).substring(2, 8)}`)

    const controller = new AbortController()
    setIsLoadingPreview(true)
    setPreviewError('')

    getPaymentPreview(violationId, controller.signal)
      .then((data) => {
        setPreview(data)
        setAmountInput(String(data.balance))
      })
      .catch((err: unknown) => {
        if (err instanceof DOMException && err.name === 'AbortError') return
        setPreviewError(err instanceof Error ? err.message : 'Không thể tải thông tin số dư.')
      })
      .finally(() => {
        if (!controller.signal.aborted) setIsLoadingPreview(false)
      })

    return () => controller.abort()
  }, [open, violationId])

  const balance = preview?.balance ?? 0
  const numericAmount = Number(amountInput) || 0
  const remainingAfterPayment = Math.max(0, balance - numericAmount)

  // Validation
  let validationError = ''
  if (numericAmount <= 0) {
    validationError = 'Số tiền thanh toán phải lớn hơn 0 ₫'
  } else if (numericAmount > balance) {
    validationError = `Số tiền thanh toán không được vượt quá số dư (${balance.toLocaleString('vi-VN')} ₫)`
  }

  const formatMoney = (val?: number) =>
    (val ?? 0).toLocaleString('vi-VN') + ' ₫'

  const handleSelectFull = () => {
    setPaymentOption('full')
    setAmountInput(String(balance))
  }

  const handleSelectPartial = () => {
    setPaymentOption('partial')
    if (balance > 10000) {
      setAmountInput(String(Math.floor(balance / 2)))
    }
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!violationId || validationError || isSubmitting) return

    setIsSubmitting(true)
    setSubmitError('')

    try {
      const receipt = await createPayment({
        violationId,
        amount: numericAmount,
        method,
        reference: reference.trim() || undefined,
        idempotencyKey,
      })

      onOpenChange(false)
      onSuccess(receipt)
    } catch (err: unknown) {
      setSubmitError(err instanceof Error ? err.message : 'Thanh toán thất bại, vui lòng thử lại.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={(val) => !isSubmitting && onOpenChange(val)}>
      <DialogContent className="max-w-md">
        <form onSubmit={handleSubmit} className="space-y-4">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2 text-lg">
              <CreditCard className="size-5 text-primary" /> Thu Tiền Phạt
            </DialogTitle>
            <DialogDescription>
              Ghi nhận giao dịch nộp tiền phạt cho bản ghi vi phạm.
            </DialogDescription>
          </DialogHeader>

          {isLoadingPreview ? (
            <div className="py-8 flex flex-col items-center justify-center gap-2 text-muted-foreground text-sm">
              <Loader2 className="size-6 animate-spin text-primary" />
              Đang tính toán số dư vi phạm...
            </div>
          ) : previewError ? (
            <div className="rounded-lg border border-destructive/30 bg-destructive/10 p-3 text-xs text-destructive flex items-center gap-2">
              <AlertCircle className="size-4 shrink-0" />
              {previewError}
            </div>
          ) : preview ? (
            <div className="space-y-4 text-xs">
              {/* Tóm tắt vi phạm & Độc giả */}
              <div className="rounded-lg bg-muted/60 p-3 space-y-2">
                <div className="flex justify-between items-start">
                  <div>
                    <span className="font-semibold text-sm text-foreground block">
                      {preview.memberName}
                    </span>
                    <span className="text-muted-foreground">
                      {preview.memberCode ? `Mã thẻ: ${preview.memberCode} · ` : ''}
                      {preview.borrowerEmail}
                    </span>
                  </div>
                  <Badge variant="outline">
                    {violationTypeLabels[preview.violationType] ?? preview.violationType}
                  </Badge>
                </div>

                {preview.bookTitle && (
                  <div className="pt-1 border-t text-muted-foreground">
                    Tài liệu: <span className="text-foreground font-medium">{preview.bookTitle}</span>
                  </div>
                )}
              </div>

              {/* Thẻ hiển thị số dư hiện tại */}
              <div className="rounded-lg border bg-card p-3 flex justify-between items-center">
                <div>
                  <span className="text-muted-foreground block text-[11px] uppercase tracking-wider">
                    Số dư còn phải nộp
                  </span>
                  <span className="text-2xl font-extrabold font-mono text-rose-600 dark:text-rose-400">
                    {formatMoney(preview.balance)}
                  </span>
                </div>
                <div className="text-right text-muted-foreground">
                  <div>Gốc: {formatMoney(preview.fineAmount)}</div>
                  {preview.totalPaid > 0 && (
                    <div className="text-emerald-600">Đã nộp: {formatMoney(preview.totalPaid)}</div>
                  )}
                </div>
              </div>

              {/* Tùy chọn nộp đủ hoặc nộp một phần */}
              <div className="space-y-1.5">
                <Label className="text-xs">Hình thức thanh toán:</Label>
                <div className="grid grid-cols-2 gap-2">
                  <Button
                    type="button"
                    variant={paymentOption === 'full' ? 'default' : 'outline'}
                    size="sm"
                    className="h-9 justify-center"
                    onClick={handleSelectFull}
                    disabled={isSubmitting}
                  >
                    Nộp toàn bộ ({formatMoney(balance)})
                  </Button>
                  <Button
                    type="button"
                    variant={paymentOption === 'partial' ? 'default' : 'outline'}
                    size="sm"
                    className="h-9 justify-center"
                    onClick={handleSelectPartial}
                    disabled={isSubmitting}
                  >
                    Nộp một phần
                  </Button>
                </div>
              </div>

              {/* Ô nhập số tiền */}
              <div className="space-y-1.5">
                <Label htmlFor="amount" className="text-xs flex justify-between">
                  <span>Số tiền thu (₫):</span>
                  <span className="text-muted-foreground font-normal">
                    {numericAmount > 0 ? formatMoney(numericAmount) : ''}
                  </span>
                </Label>
                <Input
                  id="amount"
                  type="number"
                  min={1000}
                  max={balance}
                  step={1000}
                  value={amountInput}
                  onChange={(e) => {
                    setAmountInput(e.target.value)
                    setPaymentOption('partial')
                  }}
                  disabled={isSubmitting}
                  placeholder="Nhập số tiền cần thu..."
                  className="font-mono text-base font-semibold"
                />
                {validationError && (
                  <p className="text-[11px] text-destructive flex items-center gap-1">
                    <AlertCircle className="size-3 shrink-0" /> {validationError}
                  </p>
                )}
              </div>

              {/* Phương thức thanh toán */}
              <div className="space-y-1.5">
                <Label htmlFor="method" className="text-xs">
                  Phương thức thanh toán:
                </Label>
                <Select
                  value={method}
                  onValueChange={(val) => setMethod(val as FinePaymentMethod)}
                  disabled={isSubmitting}
                >
                  <SelectTrigger id="method">
                    <SelectValue placeholder="Chọn phương thức" />
                  </SelectTrigger>
                  <SelectContent>
                    {Object.entries(PAYMENT_METHOD_LABELS).map(([val, label]) => (
                      <SelectItem key={val} value={val}>
                        {label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              {/* Mã tham chiếu / Ghi chú */}
              <div className="space-y-1.5">
                <Label htmlFor="reference" className="text-xs">
                  Mã tham chiếu / Số hóa đơn / Biên lai (tùy chọn):
                </Label>
                <Input
                  id="reference"
                  value={reference}
                  onChange={(e) => setReference(e.target.value)}
                  disabled={isSubmitting}
                  placeholder="Ví dụ: MB-982312, POS-01..."
                />
              </div>

              {/* Tóm tắt sau thanh toán */}
              <div className="rounded-lg border bg-muted/40 p-2.5 space-y-1 text-xs">
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Còn nợ sau khi thu:</span>
                  <span className="font-mono font-semibold text-foreground">
                    {formatMoney(remainingAfterPayment)}
                  </span>
                </div>
                <div className="flex justify-between items-center">
                  <span className="text-muted-foreground">Trạng thái vi phạm:</span>
                  <Badge variant={remainingAfterPayment === 0 ? 'secondary' : 'default'} className="text-[10px]">
                    {remainingAfterPayment === 0 ? 'Hoàn tất (Đã thanh toán)' : 'Thanh toán một phần'}
                  </Badge>
                </div>
              </div>

              {submitError && (
                <div className="rounded-lg border border-destructive/30 bg-destructive/10 p-2.5 text-xs text-destructive flex items-center gap-2">
                  <AlertCircle className="size-4 shrink-0" />
                  {submitError}
                </div>
              )}
            </div>
          ) : null}

          <DialogFooter className="gap-2 sm:justify-end">
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => onOpenChange(false)}
              disabled={isSubmitting}
            >
              Hủy
            </Button>
            <Button
              type="submit"
              size="sm"
              disabled={isSubmitting || isLoadingPreview || !!previewError || !!validationError || !preview}
              className="gap-1.5 min-w-[120px]"
            >
              {isSubmitting ? (
                <>
                  <Loader2 className="size-4 animate-spin" /> Đang ghi nhận...
                </>
              ) : (
                <>
                  <ShieldCheck className="size-4" /> Xác nhận thu
                </>
              )}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
