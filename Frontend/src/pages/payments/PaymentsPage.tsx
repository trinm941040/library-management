import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import {
  AlertCircle,
  CreditCard,
  Eye,
  Filter,
  Receipt,
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
import {
  getPayments,
  PAYMENT_METHOD_LABELS,
  type FinePaymentMethod,
  type FinePaymentReceipt,
  type PaymentPageResponse,
} from './payment-api'
import { PaymentReceiptDialog } from './components/PaymentReceiptDialog'

const violationTypeLabels: Record<string, string> = {
  overdue: 'Quá hạn',
  damage: 'Hư hỏng',
  lost: 'Mất sách',
  other: 'Khác',
}

export function PaymentsPage() {
  const [searchParams, setSearchParams] = useSearchParams()

  const urlSearch = searchParams.get('search') ?? ''
  const urlMethod = searchParams.get('method') ?? 'all'
  const urlPage = Number(searchParams.get('page')) || 1
  const urlPageSize = Number(searchParams.get('pageSize')) || 20

  const [page, setPage] = useState<PaymentPageResponse | null>(null)
  const [searchInput, setSearchInput] = useState(urlSearch)
  const [search, setSearch] = useState(urlSearch)
  const [method, setMethod] = useState(urlMethod)
  const [currentPage, setCurrentPage] = useState(urlPage)
  const [pageSize, setPageSize] = useState(urlPageSize)

  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState('')
  const [reloadKey, setReloadKey] = useState(0)

  const [selectedReceipt, setSelectedReceipt] = useState<FinePaymentReceipt | null>(null)
  const [receiptOpen, setReceiptOpen] = useState(false)

  // Debounce search
  useEffect(() => {
    const timer = window.setTimeout(() => {
      setSearch(searchInput.trim())
      setCurrentPage(1)
    }, 350)
    return () => window.clearTimeout(timer)
  }, [searchInput])

  // Sync URL params
  useEffect(() => {
    const params = new URLSearchParams()
    if (search) params.set('search', search)
    if (method !== 'all') params.set('method', method)
    if (currentPage > 1) params.set('page', String(currentPage))
    if (pageSize !== 20) params.set('pageSize', String(pageSize))
    setSearchParams(params, { replace: true })
  }, [search, method, currentPage, pageSize, setSearchParams])

  // Fetch payments
  useEffect(() => {
    const controller = new AbortController()
    setIsLoading(true)
    setError('')

    getPayments(
      {
        search: search || undefined,
        method: method !== 'all' ? method : undefined,
        pageNumber: currentPage,
        pageSize,
      },
      controller.signal,
    )
      .then((data) => setPage(data))
      .catch((err: unknown) => {
        if (err instanceof DOMException && err.name === 'AbortError') return
        setError(err instanceof Error ? err.message : 'Không thể tải danh sách giao dịch.')
      })
      .finally(() => {
        if (!controller.signal.aborted) setIsLoading(false)
      })

    return () => controller.abort()
  }, [search, method, currentPage, pageSize, reloadKey])

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
    })
  }

  const handleOpenReceipt = (receipt: FinePaymentReceipt) => {
    setSelectedReceipt(receipt)
    setReceiptOpen(true)
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight flex items-center gap-2">
            <CreditCard className="size-6 text-primary" /> Thu Tiền Phạt
          </h1>
          <p className="text-muted-foreground text-sm">
            Quản lý và tra cứu lịch sử các giao dịch thu tiền phạt vi phạm độc giả.
          </p>
        </div>

        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={() => setReloadKey((k) => k + 1)}
            disabled={isLoading}
            className="gap-1.5"
          >
            <RefreshCw className={`size-4 ${isLoading ? 'animate-spin' : ''}`} />
            Làm mới
          </Button>

          <Button size="sm" asChild className="gap-1.5">
            <Link to="/violations?hasBalance=true">
              <Receipt className="size-4" /> Thu phạt từ Vi phạm
            </Link>
          </Button>
        </div>
      </div>

      {/* Bộ lọc & Tìm kiếm */}
      <Card>
        <CardContent className="pt-6">
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <div className="relative sm:col-span-2">
              <Search className="absolute left-3 top-2.5 size-4 text-muted-foreground" />
              <Input
                placeholder="Tìm theo tên độc giả, mã thẻ, mã tham chiếu..."
                value={searchInput}
                onChange={(e) => setSearchInput(e.target.value)}
                className="pl-9"
              />
            </div>

            <div>
              <Select
                value={method}
                onValueChange={(val) => {
                  setMethod(val)
                  setCurrentPage(1)
                }}
              >
                <SelectTrigger>
                  <Filter className="size-4 mr-2 text-muted-foreground" />
                  <SelectValue placeholder="Phương thức thanh toán" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">Tất cả phương thức</SelectItem>
                  {Object.entries(PAYMENT_METHOD_LABELS).map(([val, label]) => (
                    <SelectItem key={val} value={val}>
                      {label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Lỗi tải */}
      {error && (
        <div className="rounded-lg border border-destructive/30 bg-destructive/10 p-4 text-destructive text-sm flex items-center gap-2">
          <AlertCircle className="size-5 shrink-0" />
          {error}
        </div>
      )}

      {/* Bảng giao dịch */}
      <Card>
        <CardHeader className="pb-3">
          <div className="flex justify-between items-center">
            <CardTitle className="text-base font-semibold">
              Danh sách giao dịch thu ({page?.totalCount ?? 0})
            </CardTitle>
          </div>
        </CardHeader>

        <CardContent>
          <div className="rounded-md border overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-[120px]">Mã biên nhận</TableHead>
                  <TableHead>Độc giả</TableHead>
                  <TableHead>Tài liệu & Lý do</TableHead>
                  <TableHead className="text-right">Số tiền thu</TableHead>
                  <TableHead>Phương thức</TableHead>
                  <TableHead>Mã tham chiếu</TableHead>
                  <TableHead>Thời gian</TableHead>
                  <TableHead>Người thu</TableHead>
                  <TableHead className="text-right w-[90px]">Thao tác</TableHead>
                </TableRow>
              </TableHeader>

              <TableBody>
                {isLoading && (!page || page.items.length === 0) ? (
                  <TableRow>
                    <TableCell colSpan={9} className="h-32 text-center text-muted-foreground text-sm">
                      Đang tải danh sách giao dịch...
                    </TableCell>
                  </TableRow>
                ) : !page || page.items.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={9} className="h-32 text-center text-muted-foreground text-sm">
                      Chưa có giao dịch thu tiền phạt nào.
                    </TableCell>
                  </TableRow>
                ) : (
                  page.items.map((item) => (
                    <TableRow key={item.id}>
                      <TableCell className="font-mono text-xs font-medium">
                        {item.id.slice(0, 8).toUpperCase()}
                      </TableCell>

                      <TableCell>
                        <div className="font-medium text-foreground">{item.memberName}</div>
                        {item.memberCode && (
                          <div className="font-mono text-xs text-muted-foreground">
                            {item.memberCode}
                          </div>
                        )}
                      </TableCell>

                      <TableCell className="text-xs">
                        <div className="font-medium text-foreground truncate max-w-[200px]" title={item.bookTitle}>
                          {item.bookTitle || 'Không rõ'}
                        </div>
                        <Badge variant="outline" className="text-[10px] mt-0.5">
                          {violationTypeLabels[item.violationType] ?? item.violationType}
                        </Badge>
                      </TableCell>

                      <TableCell className="text-right font-mono font-bold text-emerald-600 dark:text-emerald-400">
                        {formatMoney(item.amount)}
                      </TableCell>

                      <TableCell className="text-xs">
                        <Badge variant="secondary">
                          {PAYMENT_METHOD_LABELS[item.method as FinePaymentMethod] ?? item.method}
                        </Badge>
                      </TableCell>

                      <TableCell className="font-mono text-xs text-muted-foreground">
                        {item.reference || '—'}
                      </TableCell>

                      <TableCell className="text-xs text-muted-foreground whitespace-nowrap">
                        {formatDate(item.paidAtUtc)}
                      </TableCell>

                      <TableCell className="text-xs text-muted-foreground">
                        {item.receivedByUserName || 'Thủ thư'}
                      </TableCell>

                      <TableCell className="text-right">
                        <Button
                          variant="ghost"
                          size="sm"
                          title="Xem biên nhận"
                          onClick={() => handleOpenReceipt(item)}
                          className="h-8 w-8 p-0"
                        >
                          <Eye className="size-4" />
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </div>

          {page && page.totalCount > 0 && (
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
          )}
        </CardContent>
      </Card>

      {/* Modal Biên nhận */}
      <PaymentReceiptDialog
        receipt={selectedReceipt}
        open={receiptOpen}
        onOpenChange={setReceiptOpen}
      />
    </div>
  )
}
