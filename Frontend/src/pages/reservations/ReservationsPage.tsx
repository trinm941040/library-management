import { useCallback, useEffect, useState } from 'react'
import { BookOpen, CircleAlert, Clock, Plus, RefreshCw, Search, X } from 'lucide-react'
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
import {
  cancelReservation,
  createReservation,
  fulfillReservation,
  getReservations,
  type ReservationPageResponse,
} from './reservation-api'

const ITEMS_PER_PAGE = 20

const statusLabels: Record<string, string> = {
  waiting: 'Chờ sách',
  ready: 'Sẵn sàng nhận',
  expired: 'Hết hạn',
  fulfilled: 'Đã nhận',
  cancelled: 'Đã hủy',
}

export function ReservationsPage() {
  const [page, setPage] = useState<ReservationPageResponse | null>(null)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState('all')
  const [currentPage, setCurrentPage] = useState(1)
  const [reloadKey, setReloadKey] = useState(0)
  const [isLoading, setIsLoading] = useState(true)
  const [pageError, setPageError] = useState('')
  const [notice, setNotice] = useState('')
  const [formOpen, setFormOpen] = useState(false)
  const [busyId, setBusyId] = useState<string | null>(null)

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
        pageSize: ITEMS_PER_PAGE,
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
  }, [currentPage, reloadKey, search, status])

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
      refresh('Đã tạo phiếu đặt trước.')
      return null
    } catch (error) {
      return error instanceof Error ? error.message : 'Không thể tạo phiếu đặt trước.'
    }
  }

  const runAction = async (id: string, action: () => Promise<unknown>, successMessage: string) => {
    setBusyId(id)
    try {
      await action()
      refresh(successMessage)
    } catch (error) {
      setPageError(error instanceof Error ? error.message : 'Không thể cập nhật phiếu đặt trước.')
    } finally {
      setBusyId(null)
    }
  }

  const formatDate = (value: string) =>
    new Date(value).toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' })

  return (
    <>
      <div className="mx-auto w-full max-w-7xl px-5 py-10 md:px-12">
        <div className="mb-8 flex flex-col justify-between gap-4 sm:flex-row sm:items-end">
          <div>
            <p className="mb-2 text-xs font-bold tracking-widest text-primary uppercase">
              quản lý tác vụ
            </p>
            <h1 className="text-3xl font-bold tracking-tight">Đặt trước</h1>
            <p className="mt-2 text-sm text-muted-foreground">
              Giữ chỗ sách, theo dõi hạn nhận và chuyển thành phiếu mượn khi sách có sẵn.
            </p>
          </div>
          <div className="flex gap-2">
            <Button variant="outline" disabled={isLoading} onClick={() => refresh()}>
              <RefreshCw className={isLoading ? 'animate-spin' : ''} />
              Làm mới
            </Button>
            <Button onClick={() => setFormOpen(true)}>
              <Plus /> Tạo phiếu đặt trước
            </Button>
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
                    <SelectItem value="all">Tất cả</SelectItem>
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
                    <TableHead>Ngày đặt</TableHead>
                    <TableHead>Hạn nhận</TableHead>
                    <TableHead>Trạng thái</TableHead>
                    <TableHead className="text-right">Thao tác</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {isLoading && !page ? (
                    <TableRow>
                      <TableCell colSpan={6} className="h-32 text-center text-muted-foreground">
                        Đang tải...
                      </TableCell>
                    </TableRow>
                  ) : null}
                  {!isLoading && !pageError && page?.items.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={6} className="h-32 text-center text-muted-foreground">
                        Chưa có phiếu đặt trước.
                      </TableCell>
                    </TableRow>
                  ) : null}
                  {page?.items.map((item) => (
                    <TableRow key={item.id}>
                      <TableCell>
                        <strong>{item.bookTitle}</strong>
                      </TableCell>
                      <TableCell>
                        <span className="grid gap-0.5">
                          <strong>{item.reserverName}</strong>
                          <small className="text-muted-foreground">{item.reserverEmail}</small>
                        </span>
                      </TableCell>
                      <TableCell>{formatDate(item.reservedAtUtc)}</TableCell>
                      <TableCell>{formatDate(item.expiresAtUtc)}</TableCell>
                      <TableCell>
                        <Badge variant={item.status === 'expired' ? 'destructive' : 'secondary'}>
                          {statusLabels[item.status] ?? item.status}
                        </Badge>
                      </TableCell>
                      <TableCell className="text-right">
                        <div className="flex justify-end gap-2">
                          {item.status === 'ready' ? (
                            <Button
                              size="sm"
                              disabled={busyId === item.id}
                              onClick={() =>
                                runAction(
                                  item.id,
                                  () => fulfillReservation(item.id),
                                  'Đã chuyển đặt trước thành phiếu mượn.',
                                )
                              }
                            >
                              <BookOpen />
                              Nhận sách
                            </Button>
                          ) : null}
                          {item.status === 'waiting' || item.status === 'ready' || item.status === 'expired' ? (
                            <Button
                              variant="outline"
                              size="sm"
                              disabled={busyId === item.id}
                              onClick={() =>
                                runAction(item.id, () => cancelReservation(item.id), 'Đã hủy phiếu đặt trước.')
                              }
                            >
                              <X />
                              Hủy
                            </Button>
                          ) : null}
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>

            {page && page.totalPages > 1 ? (
              <div className="mt-4 flex justify-end">
                <Pagination
                  currentPage={page.pageNumber}
                  totalPages={page.totalPages}
                  onPageChange={setCurrentPage}
                />
              </div>
            ) : null}
          </CardContent>
        </Card>
      </div>

      <ReservationFormDialog open={formOpen} onOpenChange={setFormOpen} onSave={handleSave} />
    </>
  )
}
