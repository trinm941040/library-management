import { useEffect, useState } from 'react'
import { BookOpen, Calendar, Clock, Info, ListOrdered, ShieldCheck, User } from 'lucide-react'
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
import {
  getReservationDetail,
  type LibraryReservation,
  type ReservationDetailResponse,
} from '../reservation-api'

const statusLabels: Record<string, string> = {
  waiting: 'Chờ sách',
  ready: 'Sẵn sàng nhận',
  expired: 'Hết hạn',
  fulfilled: 'Đã nhận sách',
  cancelled: 'Đã hủy',
}

const statusVariants: Record<string, 'default' | 'secondary' | 'destructive' | 'outline'> = {
  waiting: 'outline',
  ready: 'default',
  expired: 'destructive',
  fulfilled: 'secondary',
  cancelled: 'outline',
}

type ReservationDetailDialogProps = {
  reservation: LibraryReservation | null
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function ReservationDetailDialog({
  reservation,
  open,
  onOpenChange,
}: ReservationDetailDialogProps) {
  const [detail, setDetail] = useState<ReservationDetailResponse | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    if (!open || !reservation) {
      setDetail(null)
      setError('')
      return
    }

    const controller = new AbortController()
    setIsLoading(true)
    setError('')

    getReservationDetail(reservation.id, controller.signal)
      .then((data) => setDetail(data))
      .catch((err: unknown) => {
        if (err instanceof DOMException && err.name === 'AbortError') return
        setError(err instanceof Error ? err.message : 'Không thể tải chi tiết phiếu đặt trước.')
      })
      .finally(() => {
        if (!controller.signal.aborted) setIsLoading(false)
      })

    return () => controller.abort()
  }, [open, reservation])

  const formatDate = (value?: string | null) => {
    if (!value) return '—'
    return new Date(value).toLocaleString('vi-VN', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    })
  }

  const target = detail?.reservation ?? reservation

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <div className="flex items-center justify-between gap-3 pr-6">
            <DialogTitle className="text-xl font-bold flex items-center gap-2">
              <Info className="size-5 text-primary" />
              Chi tiết phiếu đặt trước
            </DialogTitle>
            {target ? (
              <Badge variant={statusVariants[target.status] ?? 'secondary'}>
                {statusLabels[target.status] ?? target.status}
              </Badge>
            ) : null}
          </div>
          <DialogDescription>
            Xem thứ tự hàng đợi, thông tin đầu sách, độc giả và chính sách áp dụng.
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
            {/* Hàng đợi & Trạng thái */}
            {target.queuePosition && target.queuePosition > 0 && target.status === 'waiting' ? (
              <div className="flex items-center gap-3 rounded-lg border border-amber-500/30 bg-amber-500/10 p-3.5 text-sm text-amber-900 dark:text-amber-200">
                <ListOrdered className="size-5 shrink-0 text-amber-600" />
                <div>
                  <p className="font-semibold">Vị trí trong hàng đợi: Thứ #{target.queuePosition}</p>
                  <p className="text-xs text-muted-foreground">
                    Sách sẽ chuyển sang trạng thái "Sẵn sàng nhận" khi các lượt đặt trước trước đó hoàn tất hoặc có sách mới trong kho.
                  </p>
                </div>
              </div>
            ) : target.status === 'ready' ? (
              <div className="flex items-center gap-3 rounded-lg border border-emerald-500/30 bg-emerald-500/10 p-3.5 text-sm text-emerald-900 dark:text-emerald-200">
                <ShieldCheck className="size-5 shrink-0 text-emerald-600" />
                <div>
                  <p className="font-semibold">Đã có sách sẵn sàng nhận (Ưu tiên #{target.queuePosition || 1})</p>
                  <p className="text-xs text-muted-foreground">
                    Độc giả có thể đến quầy thư viện để nhận sách trước hạn chót giữ sách.
                  </p>
                </div>
              </div>
            ) : null}

            {/* Thông tin đầu sách */}
            <div className="rounded-lg border p-4 space-y-3">
              <h4 className="text-sm font-semibold flex items-center gap-2 text-muted-foreground uppercase tracking-wider">
                <BookOpen className="size-4 text-primary" /> Thông tin sách
              </h4>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 text-sm">
                <div>
                  <span className="text-muted-foreground text-xs block">Tên sách:</span>
                  <span className="font-medium">{target.bookTitle}</span>
                </div>
                {target.bookAuthor ? (
                  <div>
                    <span className="text-muted-foreground text-xs block">Tác giả:</span>
                    <span className="font-medium">{target.bookAuthor}</span>
                  </div>
                ) : null}
                {detail?.bookIsbn ? (
                  <div>
                    <span className="text-muted-foreground text-xs block">Mã ISBN:</span>
                    <span className="font-mono">{detail.bookIsbn}</span>
                  </div>
                ) : null}
                {target.bookCategory ? (
                  <div>
                    <span className="text-muted-foreground text-xs block">Thể loại:</span>
                    <span>{target.bookCategory}</span>
                  </div>
                ) : null}
                {detail ? (
                  <div>
                    <span className="text-muted-foreground text-xs block">Bản sao trong kho:</span>
                    <span className="font-medium">{detail.availableCopiesCount} bản</span>
                  </div>
                ) : null}
              </div>
            </div>

            {/* Thông tin độc giả */}
            <div className="rounded-lg border p-4 space-y-3">
              <h4 className="text-sm font-semibold flex items-center gap-2 text-muted-foreground uppercase tracking-wider">
                <User className="size-4 text-primary" /> Thông tin người đặt
              </h4>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 text-sm">
                <div>
                  <span className="text-muted-foreground text-xs block">Họ và tên:</span>
                  <span className="font-medium">{target.reserverName}</span>
                </div>
                <div>
                  <span className="text-muted-foreground text-xs block">Email liên hệ:</span>
                  <span>{target.reserverEmail}</span>
                </div>
                {target.reserverMemberCode ? (
                  <div>
                    <span className="text-muted-foreground text-xs block">Mã độc giả:</span>
                    <span className="font-mono">{target.reserverMemberCode}</span>
                  </div>
                ) : null}
                {target.reserverCardNumber ? (
                  <div>
                    <span className="text-muted-foreground text-xs block">Số thẻ thư viện:</span>
                    <span className="font-mono">{target.reserverCardNumber}</span>
                  </div>
                ) : null}
                {detail?.reserverGroup ? (
                  <div>
                    <span className="text-muted-foreground text-xs block">Nhóm độc giả:</span>
                    <span>{detail.reserverGroup}</span>
                  </div>
                ) : null}
              </div>
            </div>

            {/* Mốc thời gian & Chính sách */}
            <div className="rounded-lg border p-4 space-y-3">
              <h4 className="text-sm font-semibold flex items-center gap-2 text-muted-foreground uppercase tracking-wider">
                <Calendar className="size-4 text-primary" /> Mốc thời gian & Chính sách
              </h4>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 text-sm">
                <div>
                  <span className="text-muted-foreground text-xs block">Thời điểm đặt trước:</span>
                  <span>{formatDate(target.reservedAtUtc)}</span>
                </div>
                <div>
                  <span className="text-muted-foreground text-xs block">Hạn chót giữ sách:</span>
                  <span className="font-medium text-amber-600 dark:text-amber-400">
                    {formatDate(target.expiresAtUtc)}
                  </span>
                </div>
                {target.fulfilledAtUtc ? (
                  <div>
                    <span className="text-muted-foreground text-xs block">Thời điểm nhận sách:</span>
                    <span className="text-emerald-600 font-medium">
                      {formatDate(target.fulfilledAtUtc)}
                    </span>
                  </div>
                ) : null}
                {target.cancelledAtUtc ? (
                  <div>
                    <span className="text-muted-foreground text-xs block">Thời điểm hủy:</span>
                    <span className="text-destructive font-medium">
                      {formatDate(target.cancelledAtUtc)}
                    </span>
                  </div>
                ) : null}
                {detail ? (
                  <div>
                    <span className="text-muted-foreground text-xs block">Số ngày giữ chỗ theo chính sách:</span>
                    <span>{detail.holdDays} ngày</span>
                  </div>
                ) : null}
              </div>
            </div>

            {/* Hàng đợi toàn bộ của cuốn sách này */}
            {detail && detail.bookQueue && detail.bookQueue.length > 0 ? (
              <div className="rounded-lg border p-4 space-y-3">
                <h4 className="text-sm font-semibold flex items-center justify-between text-muted-foreground uppercase tracking-wider">
                  <span className="flex items-center gap-2">
                    <Clock className="size-4 text-primary" /> Hàng đợi của cuốn sách này ({detail.bookQueue.length})
                  </span>
                </h4>
                <div className="space-y-2">
                  {detail.bookQueue.map((item, index) => {
                    const isCurrent = item.id === target.id
                    return (
                      <div
                        key={item.id}
                        className={`flex items-center justify-between p-2.5 rounded-md text-xs border ${
                          isCurrent
                            ? 'bg-primary/10 border-primary/40 font-medium'
                            : 'bg-muted/40 border-border'
                        }`}
                      >
                        <div className="flex items-center gap-2.5">
                          <span className="inline-flex size-5 items-center justify-center rounded-full bg-primary/20 text-[11px] font-bold">
                            #{item.queuePosition || index + 1}
                          </span>
                          <span>{item.reserverName}</span>
                          {isCurrent ? (
                            <Badge variant="outline" className="text-[10px] h-4 py-0">
                              Phiếu này
                            </Badge>
                          ) : null}
                        </div>
                        <div className="flex items-center gap-3 text-muted-foreground">
                          <span>Đặt: {new Date(item.reservedAtUtc).toLocaleDateString('vi-VN')}</span>
                          <Badge variant={statusVariants[item.status] ?? 'outline'} className="text-[10px]">
                            {statusLabels[item.status] ?? item.status}
                          </Badge>
                        </div>
                      </div>
                    )
                  })}
                </div>
              </div>
            ) : null}
          </div>
        ) : null}

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Đóng
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
