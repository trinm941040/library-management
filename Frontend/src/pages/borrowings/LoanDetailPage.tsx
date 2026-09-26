import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import {
  AlertCircle,
  AlertTriangle,
  ArrowLeft,
  BookOpen,
  Calendar,
  CheckCircle2,
  Clock,
  History,
  RefreshCw,
  Sparkles,
  User,
} from 'lucide-react'
import { useAuth } from '@/auth/AuthProvider'
import { can } from '@/shared/auth/permissions'
import { formatDateTime } from '@/common/formatters'
import { Badge } from '@/common/components/ui/badge'
import { Button } from '@/common/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/common/components/ui/card'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/common/components/ui/dialog'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/common/components/ui/table'
import {
  ApiError,
  getBorrowingDetail,
  renewBorrowing,
  type BorrowingDetail,
} from './borrowing-api'

const statusLabels: Record<string, string> = {
  borrowed: 'Đang mượn',
  overdue: 'Quá hạn',
  returned: 'Đã trả',
}

const groupLabels: Record<string, string> = {
  General: 'Thông thường',
  Student: 'Sinh viên',
  Faculty: 'Giảng viên',
  Lecturer: 'Giảng viên',
  Staff: 'Cán bộ / Nhân viên',
  Guest: 'Khách',
}

export function LoanDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { user } = useAuth()

  const [detail, setDetail] = useState<BorrowingDetail | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [pageError, setPageError] = useState<string | null>(null)
  const [isConfirmOpen, setIsConfirmOpen] = useState(false)
  const [isRenewing, setIsRenewing] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)
  const [isConflictError, setIsConflictError] = useState(false)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)

  const canRenew = can(user?.permissions ?? [], 'borrowings.renew')

  const fetchDetail = useCallback(
    async (signal?: AbortSignal) => {
      if (!id) return
      setIsLoading(true)
      setPageError(null)
      setActionError(null)
      setIsConflictError(false)

      try {
        const result = await getBorrowingDetail(id, signal)
        setDetail(result)
      } catch (err) {
        if (err instanceof DOMException && err.name === 'AbortError') return
        const message = err instanceof Error ? err.message : 'Không thể tải thông tin khoản mượn.'
        setPageError(message)
      } finally {
        setIsLoading(false)
      }
    },
    [id],
  )

  useEffect(() => {
    const controller = new AbortController()
    fetchDetail(controller.signal)
    return () => controller.abort()
  }, [fetchDetail])

  const handleRenew = async () => {
    if (!detail || !id) return
    setIsRenewing(true)
    setActionError(null)
    setIsConflictError(false)
    setSuccessMessage(null)

    try {
      await renewBorrowing(id, detail.renewalPreview.concurrencyToken)
      setIsConfirmOpen(false)
      setSuccessMessage('Gia hạn khoản mượn thành công! Hạn trả mới đã được cập nhật.')
      await fetchDetail()
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.status === 409) {
          setIsConflictError(true)
          setActionError('Khoản mượn đã bị thay đổi bởi thao tác khác (dữ liệu vừa được cập nhật từ nơi khác). Vui lòng làm mới để lấy thông tin mới nhất.')
        } else if (err.status === 403) {
          setActionError('Bạn không có quyền thực hiện gia hạn khoản mượn này.')
        } else {
          setActionError(err.message)
        }
      } else if (err instanceof Error) {
        setActionError(err.message)
      } else {
        setActionError('Có lỗi xảy ra khi thực hiện gia hạn.')
      }
    } finally {
      setIsRenewing(false)
    }
  }

  if (isLoading && !detail) {
    return (
      <div className="container mx-auto p-6 space-y-4">
        <div className="flex items-center gap-2 text-muted-foreground">
          <Button variant="ghost" size="sm" onClick={() => navigate('/borrowings')}>
            <ArrowLeft className="mr-1 size-4" /> Quay lại danh sách
          </Button>
        </div>
        <Card>
          <CardContent className="flex h-64 items-center justify-center text-muted-foreground">
            <RefreshCw className="mr-2 size-5 animate-spin" />
            Đang tải dữ liệu khoản mượn...
          </CardContent>
        </Card>
      </div>
    )
  }

  if (pageError && !detail) {
    return (
      <div className="container mx-auto p-6 space-y-4">
        <Button variant="ghost" size="sm" onClick={() => navigate('/borrowings')}>
          <ArrowLeft className="mr-1 size-4" /> Quay lại danh sách
        </Button>
        <div className="rounded-lg border border-destructive/30 bg-destructive/10 p-6 text-center">
          <AlertCircle className="mx-auto size-8 text-destructive mb-2" />
          <h3 className="text-lg font-semibold text-destructive">Không thể mở khoản mượn</h3>
          <p className="mt-1 text-sm text-muted-foreground">{pageError}</p>
          <Button className="mt-4" variant="outline" onClick={() => fetchDetail()}>
            <RefreshCw className="mr-2 size-4" /> Thử lại
          </Button>
        </div>
      </div>
    )
  }

  if (!detail) return null

  const { borrowing, renewalPreview, renewals } = detail
  const isOverdue = borrowing.status === 'overdue'
  const isReturned = borrowing.status === 'returned'

  return (
    <div className="container mx-auto max-w-6xl p-4 sm:p-6 space-y-6">
      {/* Thanh tiêu đề và điều hướng */}
      <div className="flex flex-col gap-4 xl:flex-row xl:items-center xl:justify-between">
        <div className="space-y-1">
          <div className="flex items-center gap-2">
            <Button variant="ghost" size="sm" asChild className="-ml-2 h-8 px-2">
              <Link to="/borrowings">
                <ArrowLeft className="mr-1 size-4" /> Phiếu mượn
              </Link>
            </Button>
            <span className="text-muted-foreground">/</span>
            <span className="text-sm font-medium">Chi tiết khoản mượn</span>
          </div>
          <h1 className="text-2xl font-bold tracking-tight">Chi tiết khoản mượn</h1>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={() => fetchDetail()}
            disabled={isLoading}
            title="Làm mới thông tin"
          >
            <RefreshCw className={`size-4 mr-1 ${isLoading ? 'animate-spin' : ''}`} />
            Làm mới
          </Button>

          {canRenew && !isReturned && (
            <Button
              size="sm"
              disabled={!renewalPreview.isEligible || isRenewing}
              onClick={() => {
                setActionError(null)
                setIsConflictError(false)
                setIsConfirmOpen(true)
              }}
            >
              <Calendar className="mr-1.5 size-4" />
              Gia hạn khoản mượn
            </Button>
          )}
        </div>
      </div>

      {/* Thông báo thao tác thành công */}
      {successMessage && (
        <div className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-emerald-500/30 bg-emerald-500/10 p-4 text-emerald-800 dark:text-emerald-300">
          <div className="flex items-center gap-2">
            <CheckCircle2 className="size-5 shrink-0 text-emerald-600 dark:text-emerald-400" />
            <p className="text-sm font-medium">{successMessage}</p>
          </div>
          <Button
            variant="ghost"
            size="sm"
            className="h-7 text-xs"
            onClick={() => setSuccessMessage(null)}
          >
            Đóng
          </Button>
        </div>
      )}

      {/* Thông báo lỗi hoặc xung đột dữ liệu */}
      {actionError && (
        <div
          className={`flex flex-col gap-2 rounded-lg border p-4 sm:flex-row sm:items-center sm:justify-between ${
            isConflictError
              ? 'border-amber-500/40 bg-amber-500/10 text-amber-900 dark:text-amber-200'
              : 'border-destructive/40 bg-destructive/10 text-destructive'
          }`}
          role="alert"
        >
          <div className="flex items-start gap-2">
            {isConflictError ? (
              <AlertTriangle className="size-5 shrink-0 text-amber-600 dark:text-amber-400 mt-0.5" />
            ) : (
              <AlertCircle className="size-5 shrink-0 text-destructive mt-0.5" />
            )}
            <div>
              <p className="text-sm font-semibold">
                {isConflictError ? 'Dữ liệu bị xung đột do có thay đổi mới' : 'Không thể thực hiện gia hạn'}
              </p>
              <p className="text-xs text-muted-foreground mt-0.5">{actionError}</p>
            </div>
          </div>
          {isConflictError && (
            <Button
              variant="outline"
              size="sm"
              className="shrink-0 bg-background"
              onClick={() => fetchDetail()}
            >
              <RefreshCw className="mr-1.5 size-3.5" /> Tải lại dữ liệu mới nhất
            </Button>
          )}
        </div>
      )}

      {/* Lưới thông tin: Tác phẩm, Độc giả và Điều kiện gia hạn */}
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
        {/* Cột trái: Thông tin tác phẩm & độc giả */}
        <div className="space-y-6 lg:col-span-2">
          {/* Thẻ thông tin tác phẩm */}
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-base flex items-center gap-2">
                <BookOpen className="size-4 text-primary" /> Thông tin tác phẩm & bản sao
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3 text-sm">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <span className="text-muted-foreground block text-xs">Tên sách:</span>
                  <span className="font-semibold text-base">{borrowing.bookTitle}</span>
                </div>
                <div>
                  <span className="text-muted-foreground block text-xs">Mã vạch bản sao:</span>
                  <span className="font-mono font-medium">{borrowing.bookCopyBarcode || 'Chưa gắn mã vạch'}</span>
                </div>
                <div>
                  <span className="text-muted-foreground block text-xs">Tác giả:</span>
                  <span>{detail.bookAuthor || 'Chưa cập nhật'}</span>
                </div>
                <div>
                  <span className="text-muted-foreground block text-xs">Thể loại / Danh mục:</span>
                  <span>{detail.bookCategory || 'Chưa phân loại'}</span>
                </div>
                <div>
                  <span className="text-muted-foreground block text-xs">Mã ISBN:</span>
                  <span className="font-mono">{detail.bookIsbn || 'Chưa có'}</span>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Thẻ thông tin độc giả */}
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-base flex items-center gap-2">
                <User className="size-4 text-primary" /> Thông tin độc giả
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3 text-sm">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <span className="text-muted-foreground block text-xs">Họ và tên:</span>
                  <span className="font-semibold">{borrowing.borrowerName}</span>
                </div>
                <div>
                  <span className="text-muted-foreground block text-xs">Địa chỉ email:</span>
                  <span className="text-muted-foreground">{borrowing.borrowerEmail}</span>
                </div>
                <div>
                  <span className="text-muted-foreground block text-xs">Mã độc giả:</span>
                  <span className="font-mono font-medium">{detail.borrowerMemberCode || 'Chưa có'}</span>
                </div>
                <div>
                  <span className="text-muted-foreground block text-xs">Số thẻ thư viện:</span>
                  <span className="font-mono">{detail.borrowerCardNumber || 'Chưa cấp thẻ'}</span>
                </div>
                <div>
                  <span className="text-muted-foreground block text-xs">Nhóm độc giả:</span>
                  <span>{groupLabels[detail.borrowerGroup ?? ''] ?? detail.borrowerGroup ?? 'Thông thường'}</span>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Thẻ tiến độ mượn trả */}
          <Card>
            <CardHeader className="pb-3">
              <div className="flex items-center justify-between">
                <CardTitle className="text-base flex items-center gap-2">
                  <Clock className="size-4 text-primary" /> Tiến độ mượn trả
                </CardTitle>
                <Badge
                  variant={
                    borrowing.status === 'overdue'
                      ? 'destructive'
                      : borrowing.status === 'returned'
                      ? 'secondary'
                      : 'default'
                  }
                >
                  {statusLabels[borrowing.status] ?? borrowing.status}
                </Badge>
              </div>
            </CardHeader>
            <CardContent className="space-y-3 text-sm">
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
                <div>
                  <span className="text-muted-foreground block text-xs">Ngày mượn:</span>
                  <span className="font-medium">{formatDateTime(borrowing.borrowedAtUtc)}</span>
                </div>
                <div>
                  <span className="text-muted-foreground block text-xs">Hạn trả hiện tại:</span>
                  <span
                    className={`font-semibold ${
                      isOverdue ? 'text-destructive' : 'text-foreground'
                    }`}
                  >
                    {formatDateTime(borrowing.dueAtUtc)}
                  </span>
                </div>
                <div>
                  <span className="text-muted-foreground block text-xs">Ngày hoàn trả:</span>
                  <span>{borrowing.returnedAtUtc ? formatDateTime(borrowing.returnedAtUtc) : 'Chưa trả'}</span>
                </div>
              </div>

              <div className="pt-2 border-t flex flex-wrap items-center gap-6 text-xs text-muted-foreground">
                <div>
                  Số lần đã gia hạn:{' '}
                  <strong className="text-foreground">
                    {borrowing.renewalCount} / {renewalPreview.maxRenewals}
                  </strong>
                </div>
                <div>
                  Chính sách lưu thông áp dụng:{' '}
                  <strong className="text-foreground">
                    {renewalPreview.policyName || 'Chính sách mặc định'}
                  </strong>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Cột phải: Điều kiện gia hạn & Xem trước */}
        <div className="space-y-6">
          <Card className={renewalPreview.isEligible ? 'border-primary/40' : 'border-destructive/30'}>
            <CardHeader className="pb-3">
              <div className="flex items-center justify-between">
                <CardTitle className="text-base flex items-center gap-2">
                  <Sparkles className="size-4 text-primary" /> Điều kiện gia hạn
                </CardTitle>
                <Badge
                  variant={renewalPreview.isEligible ? 'default' : 'destructive'}
                  className={renewalPreview.isEligible ? 'bg-emerald-600 hover:bg-emerald-700' : ''}
                >
                  {renewalPreview.isEligible ? 'Đủ điều kiện' : 'Không đủ điều kiện'}
                </Badge>
              </div>
              <CardDescription>
                Hệ thống tự động kiểm tra chính sách, hạn mượn, độc giả và sách chờ.
              </CardDescription>
            </CardHeader>

            <CardContent className="space-y-4 text-sm">
              {renewalPreview.isEligible ? (
                <div className="space-y-3 rounded-lg border border-emerald-500/20 bg-emerald-500/5 p-4">
                  <div className="flex items-center gap-2 text-emerald-700 dark:text-emerald-300 font-medium text-xs">
                    <CheckCircle2 className="size-4" /> Khoản mượn có thể gia hạn thêm
                  </div>

                  <div>
                    <span className="text-xs text-muted-foreground block">Hạn trả mới sau gia hạn:</span>
                    <span className="text-base font-bold text-primary">
                      {formatDateTime(renewalPreview.proposedDueAtUtc)}
                    </span>
                    <span className="text-xs text-muted-foreground block mt-0.5">
                      (+{renewalPreview.renewalPeriodDays} ngày kể từ hạn hiện tại)
                    </span>
                  </div>

                  <div className="pt-2 border-t border-emerald-500/10 grid grid-cols-2 gap-2 text-xs">
                    <div>
                      <span className="text-muted-foreground">Lần gia hạn:</span>
                      <p className="font-semibold">
                        Lần {borrowing.renewalCount + 1} / {renewalPreview.maxRenewals}
                      </p>
                    </div>
                    <div>
                      <span className="text-muted-foreground">Thời gian gia hạn:</span>
                      <p className="font-semibold">{renewalPreview.renewalPeriodDays} ngày</p>
                    </div>
                  </div>
                </div>
              ) : (
                <div className="space-y-2 rounded-lg border border-destructive/20 bg-destructive/5 p-4">
                  <div className="flex items-center gap-1.5 text-destructive font-medium text-xs">
                    <AlertCircle className="size-4 shrink-0" />
                    Lý do không đủ điều kiện gia hạn:
                  </div>
                  <ul className="list-inside list-disc space-y-1 text-xs text-destructive/90">
                    {renewalPreview.ineligibilityReasons.map((reason, idx) => (
                      <li key={idx}>{reason}</li>
                    ))}
                  </ul>
                </div>
              )}

              {/* Nút thực hiện gia hạn */}
              <div className="pt-2">
                {!canRenew ? (
                  <p className="text-xs text-muted-foreground text-center italic">
                    Tài khoản của bạn chưa được cấp quyền thực hiện gia hạn.
                  </p>
                ) : (
                  <Button
                    className="w-full"
                    disabled={!renewalPreview.isEligible || isReturned || isRenewing}
                    onClick={() => {
                      setActionError(null)
                      setIsConflictError(false)
                      setIsConfirmOpen(true)
                    }}
                  >
                    <Calendar className="mr-2 size-4" />
                    Gia hạn khoản mượn
                  </Button>
                )}
              </div>
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Bảng lịch sử các lần gia hạn */}
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base flex items-center gap-2">
            <History className="size-4 text-primary" /> Lịch sử gia hạn ({renewals.length})
          </CardTitle>
          <CardDescription>
            Danh sách các lần gia hạn của khoản mượn này cùng mốc thời gian và hạn trả cũ/mới.
          </CardDescription>
        </CardHeader>
        <CardContent>
          {renewals.length === 0 ? (
            <div className="py-8 text-center text-sm text-muted-foreground">
              Khoản mượn này chưa từng được gia hạn lần nào.
            </div>
          ) : (
            <div className="overflow-x-auto rounded-md border">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead className="w-16 text-center">Lần</TableHead>
                    <TableHead>Hạn trả cũ</TableHead>
                    <TableHead>Hạn trả mới</TableHead>
                    <TableHead>Thời điểm gia hạn</TableHead>
                    <TableHead>Người thực hiện</TableHead>
                    <TableHead>Chính sách áp dụng</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {renewals.map((item, index) => (
                    <TableRow key={item.id}>
                      <TableCell className="text-center font-medium">#{renewals.length - index}</TableCell>
                      <TableCell className="text-muted-foreground">{formatDateTime(item.previousDueAtUtc)}</TableCell>
                      <TableCell className="font-semibold text-primary">{formatDateTime(item.newDueAtUtc)}</TableCell>
                      <TableCell>{formatDateTime(item.renewedAtUtc)}</TableCell>
                      <TableCell className="text-xs">
                        {item.renewedByUserName || 'Thủ thư / Hệ thống'}
                      </TableCell>
                      <TableCell className="text-xs text-muted-foreground">
                        Phiên bản chính sách {item.appliedPolicyVersion}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Hộp thoại xác nhận gia hạn */}
      <Dialog open={isConfirmOpen} onOpenChange={setIsConfirmOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Xác nhận gia hạn khoản mượn</DialogTitle>
            <DialogDescription>
              Kiểm tra kỹ thông tin thời hạn trả trước khi tiến hành cập nhật vào hệ thống.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-3 py-2 text-sm">
            <div className="rounded-lg bg-muted/60 p-3 space-y-2">
              <div className="flex justify-between">
                <span className="text-muted-foreground">Tác phẩm:</span>
                <span className="max-w-[min(240px,60vw)] truncate text-right font-medium">{borrowing.bookTitle}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Độc giả:</span>
                <span className="font-medium">{borrowing.borrowerName}</span>
              </div>
              <div className="border-t pt-2 flex justify-between">
                <span className="text-muted-foreground">Hạn trả hiện tại:</span>
                <span>{formatDateTime(borrowing.dueAtUtc)}</span>
              </div>
              <div className="flex justify-between font-semibold text-primary">
                <span>Hạn trả mới dự kiến:</span>
                <span>{formatDateTime(renewalPreview.proposedDueAtUtc)}</span>
              </div>
              <div className="flex justify-between text-xs text-muted-foreground">
                <span>Số lần gia hạn sau thao tác:</span>
                <span>
                  {borrowing.renewalCount + 1} / {renewalPreview.maxRenewals}
                </span>
              </div>
            </div>
          </div>

          <DialogFooter className="gap-2 sm:gap-0">
            <Button
              variant="outline"
              disabled={isRenewing}
              onClick={() => setIsConfirmOpen(false)}
            >
              Hủy
            </Button>
            <Button disabled={isRenewing} onClick={handleRenew}>
              {isRenewing ? (
                <>
                  <RefreshCw className="mr-1.5 size-4 animate-spin" /> Đang gia hạn...
                </>
              ) : (
                'Xác nhận gia hạn'
              )}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
