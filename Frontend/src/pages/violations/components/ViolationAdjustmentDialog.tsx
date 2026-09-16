import { useEffect, useMemo, useState } from 'react'
import {
  AlertCircle,
  ArrowRight,
  Calculator,
  CheckCircle2,
  DollarSign,
  MinusCircle,
  PlusCircle,
  ShieldAlert,
} from 'lucide-react'
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
import { Textarea } from '@/common/components/ui/textarea'
import {
  adjustViolation,
  getAdjustmentPreview,
  waiveViolation,
  type FineAdjustmentPreview,
} from '../adjustment-api'
import type { LibraryViolation } from '../violation-api'

type Mode = 'decrease' | 'increase' | 'waive'

type Props = {
  violation: LibraryViolation | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onSuccess: (message: string) => void
  canAdjust?: boolean
  canWaive?: boolean
}

function formatMoney(amount: number): string {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(amount)
}

export function ViolationAdjustmentDialog({
  violation,
  open,
  onOpenChange,
  onSuccess,
  canAdjust = true,
  canWaive = true,
}: Props) {
  const [mode, setMode] = useState<Mode>('decrease')
  const [amountInput, setAmountInput] = useState('')
  const [reason, setReason] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState('')
  const [preview, setPreview] = useState<FineAdjustmentPreview | null>(null)
  const [isPreviewLoading, setIsPreviewLoading] = useState(false)

  const currentBalance = useMemo(() => {
    if (!violation) return 0
    if (typeof violation.balance === 'number') return violation.balance
    return violation.fineAmount
  }, [violation])

  // Reset form when dialog opens with violation
  useEffect(() => {
    if (open && violation) {
      const defaultMode = canAdjust ? 'decrease' : 'waive'
      setMode(defaultMode)
      setAmountInput('')
      setReason('')
      setError('')
      setPreview(null)
    }
  }, [open, violation, canAdjust])

  // Compute amountDelta based on mode and input
  const amountDelta = useMemo(() => {
    if (mode === 'waive') {
      return -currentBalance
    }
    const num = parseFloat(amountInput)
    if (isNaN(num) || num <= 0) return 0
    return mode === 'decrease' ? -num : num
  }, [mode, amountInput, currentBalance])

  // Projected balance local preview
  const projectedBalance = useMemo(() => {
    return Math.max(0, currentBalance + amountDelta)
  }, [currentBalance, amountDelta])

  // Call API preview on debounced change
  useEffect(() => {
    if (!open || !violation || amountDelta === 0) {
      setPreview(null)
      return
    }

    const timer = window.setTimeout(async () => {
      setIsPreviewLoading(true)
      try {
        const res = await getAdjustmentPreview(violation.id, amountDelta)
        setPreview(res)
      } catch {
        // Fallback to local calculation if preview fails
      } finally {
        setIsPreviewLoading(false)
      }
    }, 300)

    return () => window.clearTimeout(timer)
  }, [open, violation, amountDelta])

  // Validation
  const validationError = useMemo(() => {
    if (mode !== 'waive') {
      const num = parseFloat(amountInput)
      if (!amountInput.trim() || isNaN(num) || num <= 0) {
        return 'Vui lòng nhập số tiền điều chỉnh hợp lệ lớn hơn 0.'
      }
      if (mode === 'decrease' && num > currentBalance) {
        return `Số tiền giảm không được vượt quá số dư hiện tại (${formatMoney(currentBalance)}).`
      }
    }
    if (!reason.trim()) {
      return 'Lý do điều chỉnh / miễn là bắt buộc.'
    }
    return null
  }, [mode, amountInput, reason, currentBalance])

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault()
    if (!violation || validationError) return

    setIsSubmitting(true)
    setError('')

    try {
      if (mode === 'waive') {
        const result = await waiveViolation(violation.id, { reason: reason.trim() })
        if (result.succeeded) {
          onSuccess('Đã miễn toàn bộ số tiền phạt thành công.')
          onOpenChange(false)
        }
      } else {
        const result = await adjustViolation(violation.id, {
          amountDelta,
          reason: reason.trim(),
        })
        if (result.succeeded) {
          const actionText = mode === 'decrease' ? 'giảm' : 'tăng'
          onSuccess(`Đã điều chỉnh ${actionText} tiền phạt thành công. Số dư mới: ${formatMoney(result.newBalance)}`)
          onOpenChange(false)
        }
      }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Có lỗi xảy ra khi thực hiện điều chỉnh.'
      setError(message)
    } finally {
      setIsSubmitting(false)
    }
  }

  if (!violation) return null

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-lg">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Calculator className="size-5 text-primary" />
            Điều chỉnh / Miễn tiền phạt
          </DialogTitle>
          <DialogDescription>
            Độc giả: <strong>{violation.borrowerName}</strong> ({violation.borrowerEmail})
            {violation.bookTitle ? ` — Sách: ${violation.bookTitle}` : ''}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          {error && (
            <div className="flex items-center gap-2 rounded-md border border-destructive/30 bg-destructive/10 p-3 text-sm text-destructive">
              <AlertCircle className="size-4 shrink-0" />
              <span>{error}</span>
            </div>
          )}

          {/* Tab chọn hình thức điều chỉnh */}
          <div className="space-y-1.5">
            <Label>Hình thức điều chỉnh</Label>
            <div className="grid grid-cols-3 gap-2">
              {canAdjust && (
                <Button
                  type="button"
                  variant={mode === 'decrease' ? 'default' : 'outline'}
                  size="sm"
                  className="flex items-center justify-center gap-1.5"
                  onClick={() => setMode('decrease')}
                >
                  <MinusCircle className="size-4" />
                  Giảm phạt
                </Button>
              )}
              {canAdjust && (
                <Button
                  type="button"
                  variant={mode === 'increase' ? 'default' : 'outline'}
                  size="sm"
                  className="flex items-center justify-center gap-1.5"
                  onClick={() => setMode('increase')}
                >
                  <PlusCircle className="size-4" />
                  Tăng phạt
                </Button>
              )}
              {canWaive && (
                <Button
                  type="button"
                  variant={mode === 'waive' ? 'default' : 'outline'}
                  size="sm"
                  className="flex items-center justify-center gap-1.5"
                  onClick={() => setMode('waive')}
                >
                  <ShieldAlert className="size-4" />
                  Miễn toàn bộ
                </Button>
              )}
            </div>
          </div>

          {/* Ô nhập số tiền (chỉ hiện khi không phải waive) */}
          {mode !== 'waive' ? (
            <div className="space-y-1.5">
              <Label htmlFor="adjustment-amount">
                {mode === 'decrease' ? 'Số tiền muốn giảm (VNĐ)' : 'Số tiền muốn tăng thêm (VNĐ)'}
              </Label>
              <div className="relative">
                <Input
                  id="adjustment-amount"
                  type="number"
                  min="1"
                  step="1000"
                  max={mode === 'decrease' ? currentBalance : 100000000}
                  placeholder="Ví dụ: 20000"
                  value={amountInput}
                  onChange={(e) => setAmountInput(e.target.value)}
                  className="pr-16"
                  required
                />
                <span className="absolute right-3 top-1/2 -translate-y-1/2 text-xs font-semibold text-muted-foreground">
                  VNĐ
                </span>
              </div>
              {mode === 'decrease' && currentBalance > 0 && (
                <div className="flex gap-2 pt-1 text-xs text-muted-foreground">
                  <span>Gợi ý:</span>
                  <button
                    type="button"
                    className="underline hover:text-primary"
                    onClick={() => setAmountInput(Math.floor(currentBalance * 0.5).toString())}
                  >
                    50% ({formatMoney(Math.floor(currentBalance * 0.5))})
                  </button>
                  <button
                    type="button"
                    className="underline hover:text-primary"
                    onClick={() => setAmountInput(currentBalance.toString())}
                  >
                    100% ({formatMoney(currentBalance)})
                  </button>
                </div>
              )}
            </div>
          ) : (
            <div className="rounded-lg border border-blue-200 bg-blue-50/60 p-3 text-sm text-blue-900 dark:border-blue-900/40 dark:bg-blue-950/20 dark:text-blue-300">
              Hệ thống sẽ <strong>miễn toàn bộ số dư nợ còn lại</strong> ({formatMoney(currentBalance)}) và chuyển trạng thái vi phạm sang <strong>Đã miễn (waived)</strong>.
            </div>
          )}

          {/* Thẻ xem trước số dư trước/sau */}
          <div className="rounded-lg border bg-muted/40 p-3.5 space-y-2">
            <div className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
              Xem trước thay đổi số dư
            </div>
            <div className="flex items-center justify-between text-sm">
              <div className="space-y-0.5">
                <span className="text-xs text-muted-foreground">Số dư hiện tại</span>
                <div className="font-semibold">{formatMoney(currentBalance)}</div>
              </div>

              <div className="flex items-center gap-1 text-muted-foreground px-2">
                <ArrowRight className="size-4" />
                <span className="text-xs font-medium">
                  {amountDelta > 0 ? `+${formatMoney(amountDelta)}` : amountDelta < 0 ? formatMoney(amountDelta) : '0 ₫'}
                </span>
              </div>

              <div className="space-y-0.5 text-right">
                <span className="text-xs text-muted-foreground">Số dư sau điều chỉnh</span>
                <div className={`font-bold ${projectedBalance === 0 ? 'text-green-600 dark:text-green-400' : 'text-primary'}`}>
                  {formatMoney(projectedBalance)}
                </div>
              </div>
            </div>

            {preview?.validationMessage && (
              <div className="text-xs text-destructive font-medium pt-1">
                {preview.validationMessage}
              </div>
            )}
          </div>

          {/* Ô nhập lý do (bắt buộc) */}
          <div className="space-y-1.5">
            <Label htmlFor="adjustment-reason">
              Lý do điều chỉnh / miễn <span className="text-destructive">*</span>
            </Label>
            <Textarea
              id="adjustment-reason"
              rows={3}
              placeholder="Nhập lý do chi tiết (ví dụ: Độc giả hoàn cảnh khó khăn được duyệt miễn, hoặc phát hiện sách rách trang...)"
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              required
            />
          </div>

          <DialogFooter className="gap-2 sm:gap-0">
            <Button
              type="button"
              variant="outline"
              onClick={() => onOpenChange(false)}
              disabled={isSubmitting}
            >
              Hủy
            </Button>
            <Button
              type="submit"
              disabled={isSubmitting || Boolean(validationError) || isPreviewLoading}
              className="gap-1.5"
            >
              {isSubmitting ? (
                'Đang lưu...'
              ) : (
                <>
                  <CheckCircle2 className="size-4" />
                  Xác nhận điều chỉnh
                </>
              )}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
