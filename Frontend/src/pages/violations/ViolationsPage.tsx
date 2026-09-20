import { useCallback, useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import {
  AlertTriangle,
  Calculator,
  CircleAlert,
  Eye,
  Filter,
  History,
  Plus,
  RefreshCw,
  Search,
} from 'lucide-react'
import { Badge } from '@/common/components/ui/badge'
import { Button } from '@/common/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/common/components/ui/card'
import { Input } from '@/common/components/ui/input'
import { Pagination } from '@/common/components/ui/pagination'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/common/components/ui/select'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/common/components/ui/table'
import { ViolationFormDialog, type ViolationFormData } from './components/ViolationFormDialog'
import { ViolationDetailDialog } from './components/ViolationDetailDialog'
import { ViolationAdjustmentDialog } from './components/ViolationAdjustmentDialog'
import { ViolationAdjustmentsHistoryDialog } from './components/ViolationAdjustmentsHistoryDialog'
import { PermissionBoundary } from '@/shared/auth/PermissionBoundary'
import { useAuth } from '@/auth/AuthProvider'
import {
  createViolation,
  getViolations,
  waiveViolation,
  type LibraryViolation,
  type ViolationPageResponse,
} from './violation-api'
import { PaymentFormDialog } from '@/pages/payments/components/PaymentFormDialog'
import { PaymentReceiptDialog } from '@/pages/payments/components/PaymentReceiptDialog'
import type { FinePaymentReceipt } from '@/pages/payments/payment-api'

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

const typeLabels: Record<string, string> = {
  overdue: 'Quá hạn',
  damage: 'Hư hỏng',
  lost: 'Mất sách',
  other: 'Khác',
}

export function ViolationsPage() {
  const { user } = useAuth()
  const canAdjust = Boolean(user?.permissions?.includes('violations.adjust') || user?.permissions?.includes('violations.resolve'))
  const canWaive = Boolean(user?.permissions?.includes('violations.waive') || user?.permissions?.includes('violations.resolve'))
  const canRead = Boolean(user?.permissions?.includes('violations.read'))

  const [adjustmentViolation, setAdjustmentViolation] = useState<LibraryViolation | null>(null)
  const [historyViolation, setHistoryViolation] = useState<LibraryViolation | null>(null)

  const [searchParams, setSearchParams] = useSearchParams()

  const urlSearch = searchParams.get('search') ?? ''
  const urlStatus = searchParams.get('status') ?? 'all'
  const urlType = searchParams.get('type') ?? 'all'
  const urlHasBalance = searchParams.get('hasBalance') === 'true'
  const urlPage = Number(searchParams.get('page')) || 1
  const urlPageSize = Number(searchParams.get('pageSize')) || 20
  const [page, setPage] = useState<ViolationPageResponse | null>(null)
  const [searchInput, setSearchInput] = useState(urlSearch)
  const [search, setSearch] = useState(urlSearch)
  const [status, setStatus] = useState(urlStatus)
  const [type, setType] = useState(urlType)
  const [hasBalanceOnly, setHasBalanceOnly] = useState(urlHasBalance)
  const [currentPage, setCurrentPage] = useState(urlPage)
  const [pageSize, setPageSize] = useState(urlPageSize)

  const [reloadKey, setReloadKey] = useState(0)
  const [isLoading, setIsLoading] = useState(true)
  const [pageError, setPageError] = useState('')
  const [notice, setNotice] = useState('')

  const [formOpen, setFormOpen] = useState(false)
  const [detailOpen, setDetailOpen] = useState(false)
  const [selectedViolation, setSelectedViolation] = useState<LibraryViolation | null>(null)
  const [busyId, setBusyId] = useState<string | null>(null)

  const [paymentFormOpen, setPaymentFormOpen] = useState(false)
  const [paymentReceiptOpen, setPaymentReceiptOpen] = useState(false)
  const [paymentViolationId, setPaymentViolationId] = useState<string | null>(null)
  const [currentReceipt, setCurrentReceipt] = useState<FinePaymentReceipt | null>(null)

  const handleOpenPayment = (violationId: string) => {
    setPaymentViolationId(violationId)
    setPaymentFormOpen(true)
  }

  const handlePaymentSuccess = (receipt: FinePaymentReceipt) => {
    setCurrentReceipt(receipt)
    setPaymentReceiptOpen(true)
    setNotice(
      receipt.isFullyPaid
        ? 'Đã ghi nhận thanh toán hoàn tất cho vi phạm.'
        : `Đã ghi nhận thanh toán một phần (${receipt.amount.toLocaleString('vi-VN')} ₫). Số dư còn lại: ${receipt.remainingBalance.toLocaleString('vi-VN')} ₫.`
    )
    setReloadKey((k) => k + 1)
  }

  // Đồng bộ search input debounce
  useEffect(() => {
    const timeout = window.setTimeout(() => {
      setSearch(searchInput.trim())
      setCurrentPage(1)
    }, 350)
    return () => window.clearTimeout(timeout)
  }, [searchInput])

  // Đồng bộ trạng thái bộ lọc lên URL query params
  useEffect(() => {
    const params = new URLSearchParams()
    if (search) params.set('search', search)
    if (status !== 'all') params.set('status', status)
    if (type !== 'all') params.set('type', type)
    if (hasBalanceOnly) params.set('hasBalance', 'true')
    if (currentPage > 1) params.set('page', String(currentPage))
    if (pageSize !== 20) params.set('pageSize', String(pageSize))
    setSearchParams(params, { replace: true })
  }, [search, status, type, hasBalanceOnly, currentPage, pageSize, setSearchParams])

  // Tải danh sách vi phạm
  useEffect(() => {
    const controller = new AbortController()
    setIsLoading(true)
    setPageError('')

    getViolations(
      {
        search: search || undefined,
        status: status === 'all' ? undefined : status,
        type: type === 'all' ? undefined : type,
        hasBalanceOnly: hasBalanceOnly || undefined,
        pageNumber: currentPage,
        pageSize,
      },
      controller.signal,
    )
      .then((response) => {
        setPage(response)
        if (response.totalPages > 0 && currentPage > response.totalPages) {
          setCurrentPage(response.totalPages)
        }
      })
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === 'AbortError') return
        setPageError(error instanceof Error ? error.message : 'Không thể tải danh sách vi phạm.')
      })
      .finally(() => {
        if (!controller.signal.aborted) setIsLoading(false)
      })

    return () => controller.abort()
  }, [currentPage, pageSize, reloadKey, search, status, type, hasBalanceOnly])

  const refresh = useCallback((message?: string) => {
    if (message) setNotice(message)
    setReloadKey((value) => value + 1)
  }, [])

  const handleSave = async (data: ViolationFormData) => {
    try {
      const fineAmount = Number(data.fineAmount)
      if (!Number.isFinite(fineAmount) || fineAmount < 0) return 'Số tiền phạt không hợp lệ.'

      await createViolation({
        borrowerId: data.borrowerId,
        bookId: data.bookId === 'none' ? null : data.bookId,
        type: data.type,
        note: data.note.trim(),
        fineAmount,
        overdueDays: data.overdueDays,
        bookPrice: data.bookPrice,
        damageLevel: data.damageLevel || undefined,
      })
      setCurrentPage(1)
      refresh('Đã ghi nhận vi phạm thành công.')
      return null
    } catch (error) {
      return error instanceof Error ? error.message : 'Không thể ghi nhận vi phạm.'
    }
  }

  const runAction = async (id: string, action: () => Promise<unknown>, successMessage: string) => {
    setBusyId(id)
    try {
      await action()
      refresh(successMessage)
    } catch (error) {
      setPageError(error instanceof Error ? error.message : 'Không thể cập nhật vi phạm.')
    } finally {
      setBusyId(null)
    }
  }

  const formatDate = (value: string) =>
    new Date(value).toLocaleDateString('vi-VN', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    })

  const formatMoney = (value?: number) =>
    new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(value ?? 0)

  const handleOpenDetail = (item: LibraryViolation) => {
    setSelectedViolation(item)
    setDetailOpen(true)
  }

  return (
    <>
      <div className="mx-auto w-full max-w-7xl px-5 py-10 md:px-12">
        <div className="mb-8 flex flex-col justify-between gap-4 sm:flex-row sm:items-end">
          <div>
            <p className="mb-2 text-xs font-bold tracking-widest text-primary uppercase">
              Quản lý lưu thông
            </p>
            <h1 className="text-3xl font-bold tracking-tight">Xử lý vi phạm & Phạt</h1>
            <p className="mt-2 text-sm text-muted-foreground">
              Ghi nhận vi phạm mượn trả, tự động tính phạt theo chính sách và theo dõi số dư còn nợ.
            </p>
          </div>
          <div className="flex gap-2">
            <Button variant="outline" disabled={isLoading} onClick={() => refresh()}>
              <RefreshCw className={isLoading ? 'animate-spin' : ''} />
              Làm mới
            </Button>
            <PermissionBoundary requiredPermissions={['violations.create']}>
              <Button onClick={() => setFormOpen(true)}>
                <Plus /> Ghi nhận vi phạm
              </Button>
            </PermissionBoundary>
          </div>
        </div>

        {notice ? (
          <div
            className="mb-6 flex items-center justify-between gap-3 rounded-lg border border-primary/20 bg-primary/5 px-4 py-3 text-sm"
            role="status"
          >
            <span className="flex items-center gap-2">
              <AlertTriangle className="size-4 text-primary" />
              {notice}
            </span>
            <Button variant="ghost" size="xs" onClick={() => setNotice('')}>
              Đóng
            </Button>
          </div>
        ) : null}

        <Card>
          <CardHeader className="gap-4">
            <div className="flex flex-col justify-between gap-4 lg:flex-row lg:items-center">
              <div>
                <CardTitle>Danh sách vi phạm</CardTitle>
                <p className="mt-1 text-sm text-muted-foreground">
                  {page ? `${page.totalCount} phiếu phù hợp với tiêu chí lọc.` : 'Đang tải dữ liệu...'}
                </p>
              </div>

              {/* Bộ lọc mở rộng */}
              <div className="flex flex-wrap items-center gap-3">
                <div className="relative min-w-[220px] flex-1 sm:w-64">
                  <Search className="absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
                  <Input
                    className="pl-9"
                    placeholder="Tìm độc giả, sách, mã vạch..."
                    value={searchInput}
                    onChange={(event) => setSearchInput(event.target.value)}
                  />
                </div>

                <Select
                  value={type}
                  onValueChange={(val) => {
                    setType(val)
                    setCurrentPage(1)
                  }}
                >
                  <SelectTrigger className="w-[150px]" aria-label="Lọc theo loại vi phạm">
                    <SelectValue placeholder="Loại vi phạm" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">Tất cả loại</SelectItem>
                    <SelectItem value="overdue">Quá hạn</SelectItem>
                    <SelectItem value="damage">Hư hỏng</SelectItem>
                    <SelectItem value="lost">Mất sách</SelectItem>
                    <SelectItem value="other">Khác</SelectItem>
                  </SelectContent>
                </Select>

                <Select
                  value={status}
                  onValueChange={(val) => {
                    setStatus(val)
                    setCurrentPage(1)
                  }}
                >
                  <SelectTrigger className="w-[170px]" aria-label="Lọc theo trạng thái">
                    <SelectValue placeholder="Trạng thái" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">Tất cả trạng thái</SelectItem>
                    <SelectItem value="open">Chưa thanh toán</SelectItem>
                    <SelectItem value="partially_paid">Thanh toán một phần</SelectItem>
                    <SelectItem value="paid">Đã thanh toán</SelectItem>
                    <SelectItem value="waived">Đã miễn giảm</SelectItem>
                  </SelectContent>
                </Select>

                <Button
                  variant={hasBalanceOnly ? 'default' : 'outline'}
                  size="sm"
                  onClick={() => {
                    setHasBalanceOnly((prev) => !prev)
                    setCurrentPage(1)
                  }}
                  className="gap-1.5"
                >
                  <Filter className="size-3.5" />
                  {hasBalanceOnly ? 'Đang lọc: Còn nợ' : 'Chỉ khoản còn nợ'}
                </Button>
              </div>
            </div>
          </CardHeader>
          <CardContent>
            {pageError ? (
              <div
                className="mb-4 flex flex-col items-center gap-3 rounded-lg border border-destructive/30 bg-destructive/5 p-6 text-center"
                role="alert"
              >
                <CircleAlert className="size-6 text-destructive" />
                <p className="font-medium">{pageError}</p>
                <Button variant="outline" size="sm" onClick={() => refresh()}>
                  Thử lại
                </Button>
              </div>
            ) : null}

            <div className="overflow-x-auto rounded-md border">
              <Table aria-busy={isLoading}>
                <TableHeader>
                  <TableRow>
                    <TableHead>Độc giả</TableHead>
                    <TableHead>Tài liệu / Bản sao</TableHead>
                    <TableHead>Loại vi phạm</TableHead>
                    <TableHead>Tiền phạt gốc</TableHead>
                    <TableHead>Số dư còn nợ</TableHead>
                    <TableHead>Ngày ghi nhận</TableHead>
                    <TableHead>Trạng thái</TableHead>
                    <TableHead className="text-right">Thao tác</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {isLoading && !page ? (
                    <TableRow>
                      <TableCell colSpan={8} className="h-32 text-center text-muted-foreground">
                        Đang tải danh sách vi phạm...
                      </TableCell>
                    </TableRow>
                  ) : null}
                  {!isLoading && !pageError && page?.items.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={8} className="h-32 text-center text-muted-foreground">
                        Không tìm thấy vi phạm nào phù hợp với điều kiện tìm kiếm.
                      </TableCell>
                    </TableRow>
                  ) : null}
                  {page?.items.map((item) => {
                    const balance = item.balance ?? (item.status === 'paid' || item.status === 'waived' ? 0 : item.fineAmount)
                    const isSettled = item.status === 'paid' || item.status === 'waived' || balance <= 0

                    return (
                      <TableRow key={item.id} className="hover:bg-muted/30">
                        <TableCell>
                          <div className="grid gap-0.5">
                            <span className="font-medium text-foreground">{item.borrowerName}</span>
                            <span className="text-xs text-muted-foreground">{item.borrowerEmail}</span>
                            {item.borrowerMemberCode && (
                              <span className="text-[11px] text-muted-foreground">Mã: {item.borrowerMemberCode}</span>
                            )}
                          </div>
                        </TableCell>
                        <TableCell>
                          <div className="grid gap-0.5 max-w-[220px]">
                            <span className="font-medium truncate" title={item.bookTitle || 'Không xác định'}>
                              {item.bookTitle || '—'}
                            </span>
                            {item.bookCopyBarcode && (
                              <span className="text-xs text-muted-foreground font-mono">
                                Mã vạch: {item.bookCopyBarcode}
                              </span>
                            )}
                          </div>
                        </TableCell>
                        <TableCell>
                          <Badge variant="outline" className="font-normal">
                            {typeLabels[item.type] ?? item.type}
                          </Badge>
                        </TableCell>
                        <TableCell className="font-mono text-sm">
                          {formatMoney(item.fineAmount)}
                        </TableCell>
                        <TableCell>
                          {balance > 0 ? (
                            <span className="font-mono font-semibold text-rose-600 dark:text-rose-400">
                              {formatMoney(balance)}
                            </span>
                          ) : (
                            <span className="font-mono text-emerald-600 dark:text-emerald-400 font-medium">
                              0 ₫
                            </span>
                          )}
                        </TableCell>
                        <TableCell className="text-xs text-muted-foreground whitespace-nowrap">
                          {formatDate(item.recordedAtUtc)}
                        </TableCell>
                        <TableCell>
                          <Badge variant={statusVariants[item.status] ?? 'secondary'}>
                            {statusLabels[item.status] ?? item.status}
                          </Badge>
                        </TableCell>
                        <TableCell className="text-right">
                          <div className="flex items-center justify-end gap-1.5">
                            <Button
                              variant="ghost"
                              size="sm"
                              title="Xem chi tiết vi phạm"
                              onClick={() => handleOpenDetail(item)}
                            >
                              <Eye className="size-4" />
                              <span className="hidden sm:inline ml-1">Chi tiết</span>
                            </Button>

                            {!isSettled && (canAdjust || canWaive) ? (
                              <Button
                                size="sm"
                                variant="outline"
                                className="gap-1 text-xs"
                                onClick={() => setAdjustmentViolation(item)}
                              >
                                <Calculator className="size-3.5" />
                                <span className="hidden sm:inline">Điều chỉnh</span>
                              </Button>
                            ) : null}

                            {canRead ? (
                              <Button
                                size="sm"
                                variant="ghost"
                                className="gap-1 text-xs text-muted-foreground hover:text-foreground"
                                onClick={() => setHistoryViolation(item)}
                              >
                                <History className="size-3.5" />
                                <span className="hidden sm:inline">Lịch sử</span>
                              </Button>
                            ) : null}

                            {!isSettled && (
                              <PermissionBoundary requiredPermissions={['violations.resolve']}>
                                <Button
                                  size="sm"
                                  disabled={busyId === item.id}
                                  onClick={() => handleOpenPayment(item.id)}
                                >
                                  Nộp phạt
                                </Button>
                                <Button
                                  variant="outline"
                                  size="sm"
                                  disabled={busyId === item.id}
                                  onClick={() =>
                                    runAction(
                                      item.id,
                                      () => waiveViolation(item.id),
                                      'Đã miễn giảm vi phạm thành công.',
                                    )
                                  }
                                >
                                  Miễn
                                </Button>
                              </PermissionBoundary>
                            )}
                          </div>
                        </TableCell>
                      </TableRow>
                    )
                  })}
                </TableBody>
              </Table>
            </div>

            {page && page.totalCount > 0 ? (
              <div className="mt-4 flex justify-end">
                <Pagination
                  currentPage={page.pageNumber}
                  totalPages={page.totalPages}
                  onPageChange={setCurrentPage}
                  pageSize={pageSize}
                  onPageSizeChange={(size) => {
                    setCurrentPage(1)
                    setPageSize(size)
                  }}
                />
              </div>
            ) : null}
          </CardContent>
        </Card>
      </div>

      <PermissionBoundary requiredPermissions={['violations.create']}>
        <ViolationFormDialog open={formOpen} onOpenChange={setFormOpen} onSave={handleSave} />
      </PermissionBoundary>

      <ViolationDetailDialog
        violation={selectedViolation}
        open={detailOpen}
        onOpenChange={setDetailOpen}
        onOpenPayment={handleOpenPayment}
      />

      <PaymentFormDialog
        violationId={paymentViolationId}
        open={paymentFormOpen}
        onOpenChange={setPaymentFormOpen}
        onSuccess={handlePaymentSuccess}
      />

      <PaymentReceiptDialog
        receipt={currentReceipt}
        open={paymentReceiptOpen}
        onOpenChange={setPaymentReceiptOpen}
      />

      <ViolationAdjustmentDialog
        violation={adjustmentViolation}
        open={Boolean(adjustmentViolation)}
        onOpenChange={(open) => !open && setAdjustmentViolation(null)}
        onSuccess={(msg) => {
          setNotice(msg)
          refresh()
        }}
        canAdjust={canAdjust}
        canWaive={canWaive}
      />

      <ViolationAdjustmentsHistoryDialog
        violation={historyViolation}
        open={Boolean(historyViolation)}
        onOpenChange={(open) => !open && setHistoryViolation(null)}
      />
    </>
  )
}
