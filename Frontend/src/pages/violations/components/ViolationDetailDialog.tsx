import { useEffect, useState } from 'react'
import { AlertCircle, BookOpen, Calculator, Calendar, CreditCard, DollarSign, Eye, History, Info, QrCode, User } from 'lucide-react'
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
import { PermissionBoundary } from '@/shared/auth/PermissionBoundary'
import {
  getViolationDetail,
  type LibraryViolation,
  type ViolationDetailResponse,
} from '../violation-api'
import { PaymentReceiptDialog } from '@/pages/payments/components/PaymentReceiptDialog'
import { getPaymentReceipt, type FinePaymentReceipt } from '@/pages/payments/payment-api'

const typeLabels: Record<string, string> = {
  overdue: 'Quá hạn',
  damage: 'Hư hỏng',
  lost: 'Mất sách',
  other: 'Khác',
}

const statusLabels: Record<string, string> = {
  open: 'Chưa thanh toán',
  partially_paid: 'Thanh toán một phần',
  paid: 'Đã thanh toán',
  waived: 'Đã miễn giảm',
}

const statusVariants: Record<string, 'default' | 'secondary' | 'destructive' | 'outline'> = {
  open: 'destructive',
  partially_paid: 'default',
  paid: 'secondary',
  waived: 'outline',
}

type ViolationDetailDialogProps = {
  violation: LibraryViolation | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onOpenPayment?: (violationId: string) => void
}

export function ViolationDetailDialog({
  violation,
  open,
  onOpenChange,
  onOpenPayment,
}: ViolationDetailDialogProps) {
  const [detail, setDetail] = useState<ViolationDetailResponse | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState('')
  const [receiptToShow, setReceiptToShow] = useState<FinePaymentReceipt | null>(null)
  const [receiptOpen, setReceiptOpen] = useState(false)

  useEffect(() => {
    if (!open || !violation) {
      setDetail(null)
      setError('')
      return
    }

    const controller = new AbortController()
    setIsLoading(true)
    setError('')

    getViolationDetail(violation.id, controller.signal)
      .then((data) => setDetail(data))
      .catch((err: unknown) => {
        if (err instanceof DOMException && err.name === 'AbortError') return
        setError(err instanceof Error ? err.message : 'Không thể tải chi tiết vi phạm.')
      })
      .finally(() => {
        if (!controller.signal.aborted) setIsLoading(false)
      })

    return () => controller.abort()
  }, [open, violation])

  const target = detail?.violation ?? violation

  const formatCurrency = (val?: number) =>
    (val ?? 0).toLocaleString('vi-VN') + ' đ'

  const formatDate = (val?: string | null) => {
    if (!val) return '—'
    return new Date(val).toLocaleString('vi-VN', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    })
  }

  // Parse calculation basis if json
  let parsedBasis: Record<string, unknown> | null = null
  if (detail?.calculationBasis) {
    try {
      parsedBasis = JSON.parse(detail.calculationBasis) as Record<string, unknown>
    } catch {
      // not json, treat as plain text
    }
  }

  const balance = target?.balance ?? target?.fineAmount ?? 0
  const isPaid = target?.status === 'paid' || balance === 0

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <div className="flex items-center justify-between gap-3 pr-6">
            <DialogTitle className="text-xl font-bold flex items-center gap-2">
              <Info className="size-5 text-primary" />
              Chi tiết vi phạm & Tiền phạt
            </DialogTitle>
            {target ? (
              <Badge variant={statusVariants[target.status] ?? 'outline'}>
                {statusLabels[target.status] ?? target.status}
              </Badge>
            ) : null}
          </div>
          <DialogDescription>
            Xem cấu phần tính phạt, thông tin độc giả, nguồn phát sinh và lịch sử giao dịch.
          </DialogDescription>
        </DialogHeader>

        {isLoading ? (
          <div className="py-12 text-center text-sm text-muted-foreground">
            Đang tải dữ liệu chi tiết...
          </div>
        ) : error ? (
          <div className="rounded-lg border border-destructive/30 bg-destructive/5 p-4 text-sm text-destructive">
            {error}
          </div>
        ) : target ? (
          <div className="space-y-5 py-2">
            {/* Tổng quan tài chính & Số dư */}
            <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 rounded-lg border bg-muted/40 p-3.5 text-center">
              <div>
                <span className="text-xs text-muted-foreground block">Tiền phạt gốc</span>
                <span className="text-sm font-semibold">{formatCurrency(target.fineAmount)}</span>
              </div>
              <div>
                <span className="text-xs text-muted-foreground block">Đã điều chỉnh</span>
                <span className="text-sm font-semibold text-blue-600 dark:text-blue-400">
                  {formatCurrency(target.totalAdjusted)}
                </span>
              </div>
              <div>
                <span className="text-xs text-muted-foreground block">Đã thanh toán</span>
                <span className="text-sm font-semibold text-emerald-600 dark:text-emerald-400">
                  {formatCurrency(target.totalPaid)}
                </span>
              </div>
              <div>
                <span className="text-xs text-muted-foreground block">Số dư còn nợ</span>
                <span className={`text-base font-bold ${isPaid ? 'text-emerald-600' : 'text-destructive'}`}>
                  {formatCurrency(balance)}
                </span>
              </div>
            </div>

            {/* Thông tin độc giả */}
            <div className="rounded-lg border p-4 space-y-3">
              <h4 className="text-sm font-semibold flex items-center gap-2 text-muted-foreground uppercase tracking-wider">
                <User className="size-4 text-primary" /> Thông tin độc giả
              </h4>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 text-sm">
                <div>
                  <span className="text-muted-foreground text-xs block">Họ và tên:</span>
                  <span className="font-medium">{target.borrowerName}</span>
                </div>
                <div>
                  <span className="text-muted-foreground text-xs block">Email liên hệ:</span>
                  <span>{target.borrowerEmail}</span>
                </div>
                {target.borrowerMemberCode ? (
                  <div>
                    <span className="text-muted-foreground text-xs block">Mã độc giả:</span>
                    <span className="font-mono">{target.borrowerMemberCode}</span>
                  </div>
                ) : null}
                {target.borrowerCardNumber ? (
                  <div>
                    <span className="text-muted-foreground text-xs block">Số thẻ thư viện:</span>
                    <span className="font-mono">{target.borrowerCardNumber}</span>
                  </div>
                ) : null}
              </div>
            </div>

            {/* Nguồn phát sinh & Sách */}
            <div className="rounded-lg border p-4 space-y-3">
              <h4 className="text-sm font-semibold flex items-center gap-2 text-muted-foreground uppercase tracking-wider">
                <BookOpen className="size-4 text-primary" /> Tài liệu & Nguồn phát sinh
              </h4>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 text-sm">
                <div>
                  <span className="text-muted-foreground text-xs block">Tên sách:</span>
                  <span className="font-medium">{target.bookTitle || 'Không xác định'}</span>
                </div>
                {detail?.bookAuthor ? (
                  <div>
                    <span className="text-muted-foreground text-xs block">Tác giả:</span>
                    <span>{detail.bookAuthor}</span>
                  </div>
                ) : null}
                {detail?.bookIsbn ? (
                  <div>
                    <span className="text-muted-foreground text-xs block">Mã ISBN:</span>
                    <span className="font-mono">{detail.bookIsbn}</span>
                  </div>
                ) : null}
                {target.bookCopyBarcode ? (
                  <div>
                    <span className="text-muted-foreground text-xs block">Mã vạch bản sao:</span>
                    <span className="font-mono font-medium flex items-center gap-1">
                      <QrCode className="size-3.5 text-primary" /> {target.bookCopyBarcode}
                    </span>
                  </div>
                ) : null}
                <div>
                  <span className="text-muted-foreground text-xs block">Hành vi vi phạm:</span>
                  <Badge variant="outline">{typeLabels[target.type] ?? target.type}</Badge>
                </div>
                <div>
                  <span className="text-muted-foreground text-xs block">Thời điểm ghi nhận:</span>
                  <span>{formatDate(target.recordedAtUtc)}</span>
                </div>
              </div>
              {target.note ? (
                <div className="pt-1 text-xs">
                  <span className="text-muted-foreground block">Nội dung ghi chú:</span>
                  <p className="mt-0.5 rounded bg-muted/50 p-2 font-medium">{target.note}</p>
                </div>
              ) : null}
            </div>

            {/* Căn cứ tính toán & Công thức tiền phạt */}
            <div className="rounded-lg border p-4 space-y-3">
              <h4 className="text-sm font-semibold flex items-center gap-2 text-muted-foreground uppercase tracking-wider">
                <Calculator className="size-4 text-primary" /> Căn cứ & Công thức tính phạt
              </h4>
              <div className="space-y-2 text-xs">
                {parsedBasis ? (
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 bg-muted/40 p-3 rounded">
                    {parsedBasis.Formula ? (
                      <div className="col-span-full">
                        <span className="text-muted-foreground block">Công thức áp dụng:</span>
                        <span className="font-semibold text-primary">{String(parsedBasis.Formula)}</span>
                      </div>
                    ) : null}
                    {parsedBasis.DailyRate ? (
                      <div>
                        <span className="text-muted-foreground block">Đơn giá phạt mỗi ngày:</span>
                        <span>{formatCurrency(Number(parsedBasis.DailyRate))}</span>
                      </div>
                    ) : null}
                    {parsedBasis.OverdueDays ? (
                      <div>
                        <span className="text-muted-foreground block">Số ngày quá hạn:</span>
                        <span>{String(parsedBasis.OverdueDays)} ngày</span>
                      </div>
                    ) : null}
                    {parsedBasis.MaxFine ? (
                      <div>
                        <span className="text-muted-foreground block">Mức phạt tối đa (trần):</span>
                        <span>{formatCurrency(Number(parsedBasis.MaxFine))}</span>
                      </div>
                    ) : null}
                    {parsedBasis.LostRatio ? (
                      <div>
                        <span className="text-muted-foreground block">Tỷ lệ bồi thường mất sách:</span>
                        <span>{String(parsedBasis.LostRatio)}%</span>
                      </div>
                    ) : null}
                  </div>
                ) : (
                  <p className="text-muted-foreground">Áp dụng theo quy định của chính sách lưu thông thư viện.</p>
                )}
              </div>
            </div>

            {/* Lịch sử thanh toán & điều chỉnh */}
            {detail && ((detail.payments && detail.payments.length > 0) || (detail.adjustments && detail.adjustments.length > 0)) ? (
              <div className="rounded-lg border p-4 space-y-3">
                <h4 className="text-sm font-semibold flex items-center gap-2 text-muted-foreground uppercase tracking-wider">
                  <History className="size-4 text-primary" /> Lịch sử thanh toán & điều chỉnh
                </h4>
                <div className="space-y-2 text-xs">
                  {detail.payments.map((pmt) => (
                    <div key={pmt.id} className="flex justify-between items-center p-2 rounded bg-emerald-500/10 border border-emerald-500/20">
                      <div>
                        <span className="font-semibold text-emerald-700 dark:text-emerald-300">
                          Thanh toán: +{formatCurrency(pmt.amount)}
                        </span>
                        <span className="text-muted-foreground block">
                          Phương thức: {pmt.method} {pmt.reference ? `· Tham chiếu: ${pmt.reference}` : ''}
                        </span>
                      </div>
                      <div className="flex items-center gap-2">
                        <span className="text-muted-foreground">{formatDate(pmt.paidAtUtc)}</span>
                        <Button
                          variant="ghost"
                          size="sm"
                          className="h-7 px-2 text-xs"
                          title="Xem biên nhận"
                          onClick={() => {
                            getPaymentReceipt(pmt.id)
                              .then((r) => {
                                setReceiptToShow(r)
                                setReceiptOpen(true)
                              })
                              .catch(() => {
                                setReceiptToShow({
                                  id: pmt.id,
                                  violationId: target?.id ?? '',
                                  violationType: target?.type ?? 'other',
                                  bookTitle: target?.bookTitle ?? '',
                                  memberId: target?.borrowerId ?? '',
                                  memberName: target?.borrowerName ?? '',
                                  memberCode: target?.borrowerMemberCode ?? null,
                                  amount: pmt.amount,
                                  previousBalance: (target?.balance ?? 0) + pmt.amount,
                                  remainingBalance: target?.balance ?? 0,
                                  method: pmt.method,
                                  reference: pmt.reference,
                                  paidAtUtc: pmt.paidAtUtc,
                                  receivedByUserId: pmt.receivedByUserId,
                                  receivedByUserName: 'Thủ thư',
                                  isFullyPaid: (target?.balance ?? 0) <= 0,
                                })
                                setReceiptOpen(true)
                              })
                          }}
                        >
                          <Eye className="size-3.5 mr-1" /> Biên nhận
                        </Button>
                      </div>
                    </div>
                  ))}
                  {detail.adjustments.map((adj) => (
                    <div key={adj.id} className="flex justify-between items-center p-2 rounded bg-blue-500/10 border border-blue-500/20">
                      <div>
                        <span className="font-semibold text-blue-700 dark:text-blue-300">
                          Điều chỉnh: {adj.amountDelta > 0 ? `+${formatCurrency(adj.amountDelta)}` : formatCurrency(adj.amountDelta)}
                        </span>
                        <span className="text-muted-foreground block">Lý do: {adj.reason}</span>
                      </div>
                      <span className="text-muted-foreground">{formatDate(adj.adjustedAtUtc)}</span>
                    </div>
                  ))}
                </div>
              </div>
            ) : null}
          </div>
        ) : null}

        <DialogFooter className="flex sm:justify-between items-center gap-2">
          <div>
            {target && (target.balance ?? 0) > 0 && target.status !== 'waived' && onOpenPayment ? (
              <PermissionBoundary requiredPermissions={['violations.resolve']}>
                <Button
                  size="sm"
                  className="gap-1.5"
                  onClick={() => {
                    onOpenChange(false)
                    onOpenPayment(target.id)
                  }}
                >
                  <CreditCard className="size-4" /> Thu tiền phạt ({formatCurrency(target.balance)})
                </Button>
              </PermissionBoundary>
            ) : null}
          </div>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Đóng
          </Button>
        </DialogFooter>
      </DialogContent>

      <PaymentReceiptDialog
        receipt={receiptToShow}
        open={receiptOpen}
        onOpenChange={setReceiptOpen}
      />
    </Dialog>
  )
}
