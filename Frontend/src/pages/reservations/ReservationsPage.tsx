import { useCallback, useEffect, useState } from 'react'
import { BookOpen, CircleAlert, Clock, Eye, ListOrdered, Plus, RefreshCw, Search, X } from 'lucide-react'
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
import { ReservationFormDialog, type ReservationFormData } from './components/ReservationFormDialog'
import { ReservationDetailDialog } from './components/ReservationDetailDialog'
import { FulfillReservationDialog } from './components/FulfillReservationDialog'
import { CancelReservationDialog } from './components/CancelReservationDialog'
import { PermissionBoundary } from '@/shared/auth/PermissionBoundary'
import {
  cancelReservation,
  createReservation,
  fulfillReservation,
  getReservations,
  type LibraryReservation,
  type ReservationPageResponse,
} from './reservation-api'

const statusLabels: Record<string, string> = {
  waiting: 'Chờ sách',
  ready: 'Sẵn sàng nhận',
  expired: 'Hết hạn',
  fulfilled: 'Đã nhận',
  cancelled: 'Đã hủy',
}

const statusVariants: Record<string, 'default' | 'secondary' | 'destructive' | 'outline'> = {
  waiting: 'outline',
  ready: 'default',
  expired: 'destructive',
  fulfilled: 'secondary',
  cancelled: 'outline',
}

export function ReservationsPage() {
  const [page, setPage] = useState<ReservationPageResponse | null>(null)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState('all')
  const [currentPage, setCurrentPage] = useState(1)
  const [pageSize, setPageSize] = useState(20)
  const [reloadKey, setReloadKey] = useState(0)
  const [isLoading, setIsLoading] = useState(true)
  const [pageError, setPageError] = useState('')
  const [notice, setNotice] = useState('')
  const [formOpen, setFormOpen] = useState(false)

  // Dialog state
  const [selectedDetail, setSelectedDetail] = useState<LibraryReservation | null>(null)
  const [detailOpen, setDetailOpen] = useState(false)
  const [selectedFulfill, setSelectedFulfill] = useState<LibraryReservation | null>(null)
  const [fulfillOpen, setFulfillOpen] = useState(false)
  const [selectedCancel, setSelectedCancel] = useState<LibraryReservation | null>(null)
  const [cancelOpen, setCancelOpen] = useState(false)

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      setSearch(searchInput.trim())
      setCurrentPage(1)
    }, 350)

    return () => window.clearTimeout(timeout)
  }, [searchInput])

  useEffect(() => {
    const controller = new AbortController()
    setIsLoading(true)
    setPageError('')

    getReservations(
      {
        search: search || undefined,
        status: status === 'all' ? undefined : status,
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
        setPageError(error instanceof Error ? error.message : 'Không thể tải phiếu đặt trước.')
      })
      .finally(() => {
        if (!controller.signal.aborted) setIsLoading(false)
      })

    return () => controller.abort()
  }, [currentPage, pageSize, reloadKey, search, status])

  const refresh = useCallback((message?: string) => {
    if (message) setNotice(message)
    setReloadKey((value) => value + 1)
  }, [])

  const handleSave = async (data: ReservationFormData) => {
    try {
      const holdDays = Number(data.holdDays)
      if (!Number.isInteger(holdDays) || holdDays < 1) return 'Số ngày giữ chỗ phải từ 1 trở lên.'

      await createReservation({
        bookId: data.bookId,
        reserverId: data.reserverId,
        holdDays,
      })
      setCurrentPage(1)
      refresh('Đã tạo phiếu đặt trước thành công.')
      return null
    } catch (error) {
      return error instanceof Error ? error.message : 'Không thể tạo phiếu đặt trước.'
    }
  }

  const handleFulfill = async (id: string, barcode?: string, concurrencyToken?: string) => {
    try {
      await fulfillReservation(id, {
        bookCopyBarcode: barcode,
        concurrencyToken: concurrencyToken ?? '',
      })
      refresh('Đã hoàn tất nhận sách và tạo khoản mượn cho độc giả.')
      return null
    } catch (error) {
      const msg = error instanceof Error ? error.message : 'Không thể xử lý nhận sách.'
      refresh() // reload page to sync latest data
      return msg
    }
  }

  const handleCancel = async (id: string, reason?: string, concurrencyToken?: string) => {
    try {
      await cancelReservation(id, {
        reason,
        concurrencyToken: concurrencyToken ?? '',
      })
      refresh('Đã hủy phiếu đặt trước và chuyển thứ tự ưu tiên cho độc giả tiếp theo.')
      return null
    } catch (error) {
      const msg = error instanceof Error ? error.message : 'Không thể hủy phiếu đặt trước.'
      refresh() // reload page to sync latest data
      return msg
    }
  }

  const formatDate = (value: string) =>
    new Date(value).toLocaleDateString('vi-VN', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
    })

  return (
    <>
      <div className="mx-auto w-full max-w-7xl px-5 py-10 md:px-12">
        <div className="mb-8 flex flex-col justify-between gap-4 sm:flex-row sm:items-end">
          <div>
            <p className="mb-2 text-xs font-bold tracking-widest text-primary uppercase">
              quản lý lưu thông
            </p>
            <h1 className="text-3xl font-bold tracking-tight">Đặt trước sách</h1>
            <p className="mt-2 text-sm text-muted-foreground">
              Quản lý hàng đợi đặt trước, theo dõi hạn nhận sách và xử lý trao sách cho độc giả.
            </p>
          </div>
          <div className="flex gap-2">
            <Button variant="outline" disabled={isLoading} onClick={() => refresh()}>
              <RefreshCw className={isLoading ? 'animate-spin' : ''} />
              Làm mới
            </Button>
            <PermissionBoundary requiredPermissions={['reservations.create']}>
              <Button onClick={() => setFormOpen(true)}>
                <Plus /> Tạo phiếu đặt trước
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
              <Clock className="size-4 text-primary" />
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
                <CardTitle>Danh sách phiếu đặt trước</CardTitle>
                <p className="mt-1 text-sm text-muted-foreground">
                  {page ? `${page.totalCount} phiếu phù hợp với bộ lọc.` : 'Đang tải dữ liệu.'}
                </p>
              </div>
              <div className="flex w-full flex-col gap-3 sm:flex-row lg:w-auto">
                <div className="relative w-full lg:w-72">
                  <Search className="absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
                  <Input
                    className="pl-9"
                    placeholder="Tìm theo tên hoặc email độc giả..."
                    value={searchInput}
                    onChange={(event) => setSearchInput(event.target.value)}
                  />
                </div>
                <Select
                  value={status}
                  onValueChange={(value) => {
                    setStatus(value)
                    setCurrentPage(1)
                  }}
                >
                  <SelectTrigger className="w-full sm:w-44" aria-label="Lọc theo trạng thái">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">Tất cả trạng thái</SelectItem>
                    <SelectItem value="waiting">Chờ sách</SelectItem>
                    <SelectItem value="ready">Sẵn sàng nhận</SelectItem>
                    <SelectItem value="expired">Hết hạn</SelectItem>
                    <SelectItem value="fulfilled">Đã nhận</SelectItem>
                    <SelectItem value="cancelled">Đã hủy</SelectItem>
                  </SelectContent>
                </Select>
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
                    <TableHead>Sách</TableHead>
                    <TableHead>Độc giả</TableHead>
                    <TableHead className="text-center">Hàng đợi</TableHead>
                    <TableHead>Ngày đặt</TableHead>
                    <TableHead>Hạn nhận</TableHead>
                    <TableHead>Trạng thái</TableHead>
                    <TableHead className="text-right">Thao tác</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {isLoading && !page ? (
                    <TableRow>
                      <TableCell colSpan={7} className="h-32 text-center text-muted-foreground">
                        Đang tải dữ liệu...
                      </TableCell>
                    </TableRow>
                  ) : null}
                  {!isLoading && !pageError && page?.items.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={7} className="h-32 text-center text-muted-foreground">
                        Chưa có phiếu đặt trước nào.
                      </TableCell>
                    </TableRow>
                  ) : null}
                  {page?.items.map((item) => (
                    <TableRow key={item.id}>
                      <TableCell>
                        <div className="font-semibold">{item.bookTitle}</div>
                        {item.bookAuthor ? (
                          <div className="text-xs text-muted-foreground">{item.bookAuthor}</div>
                        ) : null}
                      </TableCell>
                      <TableCell>
                        <div className="grid gap-0.5">
                          <span className="font-medium">{item.reserverName}</span>
                          <span className="text-xs text-muted-foreground">{item.reserverEmail}</span>
                          {item.reserverMemberCode ? (
                            <span className="text-[11px] font-mono text-muted-foreground">
                              Mã ĐG: {item.reserverMemberCode}
                            </span>
                          ) : null}
                        </div>
                      </TableCell>
                      <TableCell className="text-center">
                        {item.queuePosition && item.queuePosition > 0 && item.status === 'waiting' ? (
                          <span className="inline-flex items-center gap-1 rounded-full bg-amber-500/15 px-2.5 py-0.5 text-xs font-semibold text-amber-700 dark:text-amber-300">
                            <ListOrdered className="size-3" />
                            Thứ #{item.queuePosition}
                          </span>
                        ) : item.status === 'ready' ? (
                          <span className="inline-flex items-center gap-1 rounded-full bg-emerald-500/15 px-2.5 py-0.5 text-xs font-semibold text-emerald-700 dark:text-emerald-300">
                            Ưu tiên #{item.queuePosition || 1}
                          </span>
                        ) : (
                          <span className="text-xs text-muted-foreground">—</span>
                        )}
                      </TableCell>
                      <TableCell className="text-xs">{formatDate(item.reservedAtUtc)}</TableCell>
                      <TableCell className="text-xs font-medium">
                        <span className={item.status === 'expired' ? 'text-destructive' : ''}>
                          {formatDate(item.expiresAtUtc)}
                        </span>
                      </TableCell>
                      <TableCell>
                        <Badge variant={statusVariants[item.status] ?? 'secondary'}>
                          {statusLabels[item.status] ?? item.status}
                        </Badge>
                      </TableCell>
                      <TableCell className="text-right">
                        <div className="flex justify-end gap-1.5">
                          {/* Nút xem chi tiết */}
                          <Button
                            variant="ghost"
                            size="sm"
                            title="Xem chi tiết"
                            onClick={() => {
                              setSelectedDetail(item)
                              setDetailOpen(true)
                            }}
                          >
                            <Eye className="size-4" />
                            <span className="sr-only sm:not-sr-only sm:ml-1 text-xs">Chi tiết</span>
                          </Button>

                          {/* Nút nhận sách */}
                          {item.status === 'ready' ? (
                            <PermissionBoundary requiredPermissions={['reservations.fulfill']}>
                              <Button
                                size="sm"
                                className="bg-emerald-600 hover:bg-emerald-700 text-white"
                                onClick={() => {
                                  setSelectedFulfill(item)
                                  setFulfillOpen(true)
                                }}
                              >
                                <BookOpen className="size-4" />
                                <span className="text-xs">Nhận sách</span>
                              </Button>
                            </PermissionBoundary>
                          ) : null}

                          {/* Nút hủy đặt trước */}
                          {item.status === 'waiting' ||
                          item.status === 'ready' ||
                          item.status === 'expired' ? (
                            <PermissionBoundary requiredPermissions={['reservations.cancel']}>
                              <Button
                                variant="outline"
                                size="sm"
                                onClick={() => {
                                  setSelectedCancel(item)
                                  setCancelOpen(true)
                                }}
                              >
                                <X className="size-4" />
                                <span className="text-xs">Hủy</span>
                              </Button>
                            </PermissionBoundary>
                          ) : null}
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
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

      <PermissionBoundary requiredPermissions={['reservations.create']}>
        <ReservationFormDialog open={formOpen} onOpenChange={setFormOpen} onSave={handleSave} />
      </PermissionBoundary>

      {/* Dialog chi tiết đặt trước */}
      <ReservationDetailDialog
        reservation={selectedDetail}
        open={detailOpen}
        onOpenChange={setDetailOpen}
      />

      {/* Dialog nhận sách */}
      <FulfillReservationDialog
        reservation={selectedFulfill}
        open={fulfillOpen}
        onOpenChange={setFulfillOpen}
        onFulfill={handleFulfill}
      />

      {/* Dialog hủy đặt trước */}
      <CancelReservationDialog
        reservation={selectedCancel}
        open={cancelOpen}
        onOpenChange={setCancelOpen}
        onCancelReservation={handleCancel}
      />
    </>
  )
}
