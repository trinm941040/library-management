import { useEffect, useMemo, useState } from 'react'
import { BookOpen, ExternalLink, MapPin, X } from 'lucide-react'
import { Link } from 'react-router-dom'
import { DataTable, ScreenState, StatusBadge, type DataTableColumn } from '@/common/components'
import { Badge } from '@/common/components/ui/badge'
import { Button } from '@/common/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/common/components/ui/card'
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/common/components/ui/dialog'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/common/components/ui/select'
import {
  copyConditionLabels,
  copyLocationLabel,
  copyStatusLabels,
  copyStatuses,
  getAllCopiesByBook,
  getActiveShelves,
  type ActiveShelf,
  type BookCopy,
  type CopyStatus,
} from '@/pages/copies/copy-api'
import { getBook, type LibraryBook, type SemanticBookSearchItem } from '../book-api'
import { AiSparklesIcon } from './AiSparklesIcon'

type StatusFilter = 'All' | CopyStatus
type InventoryDetails = {
  book: LibraryBook
  copies: BookCopy[]
  shelves: ActiveShelf[]
  totalCopies: number
}

function statusTone(status: CopyStatus) {
  if (status === 'Available') return 'success' as const
  if (status === 'Lost' || status === 'Damaged' || status === 'Withdrawn') return 'danger' as const
  if (status === 'Reserved' || status === 'InTransit') return 'warning' as const
  return 'neutral' as const
}

export function AiBookInventoryDialog({
  book,
  query,
  open,
  onOpenChange,
}: {
  book: SemanticBookSearchItem | null
  query: string
  open: boolean
  onOpenChange: (open: boolean) => void
}) {
  const [status, setStatus] = useState<StatusFilter>('All')
  const [shelfId, setShelfId] = useState('all')
  const [details, setDetails] = useState<InventoryDetails | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [retryKey, setRetryKey] = useState(0)

  useEffect(() => {
    if (!open || !book) return
    const controller = new AbortController()
    setLoading(true)
    setError('')
    setDetails(null)
    Promise.all([
      getBook(book.id, controller.signal),
      getAllCopiesByBook(book.id, controller.signal),
      getActiveShelves(controller.signal),
    ])
      .then(([bookDetails, copies, shelves]) => {
        setDetails({
          book: bookDetails,
          copies,
          shelves,
          totalCopies: copies.length,
        })
      })
      .catch((reason: unknown) => {
        if (!(reason instanceof DOMException && reason.name === 'AbortError'))
          setError('Không thể tải thông tin tồn kho của sách. Vui lòng thử lại.')
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false)
      })
    return () => controller.abort()
  }, [book, open, retryKey])

  const filteredCopies = useMemo(() => {
    if (!details) return []
    return details.copies.filter(
      (copy) =>
        (status === 'All' || copy.status === status) &&
        (shelfId === 'all' || copy.shelfId === shelfId),
    )
  }, [details, shelfId, status])

  const availableLocations = useMemo(() => {
    if (!details) return []
    const groups = new Map<string, { label: string; count: number }>()
    for (const copy of details.copies.filter((item) => item.status === 'Available')) {
      const key = copy.shelfId ?? 'unassigned'
      const current = groups.get(key)
      groups.set(key, {
        label: copyLocationLabel(copy, details.shelves),
        count: (current?.count ?? 0) + 1,
      })
    }
    return [...groups.values()].sort((left, right) => right.count - left.count)
  }, [details])

  if (!book) return null

  const copies = details?.copies ?? []
  const shelves = details?.shelves ?? []
  const availableCopies = copies.filter((copy) => copy.status === 'Available').length
  const borrowedCopies = copies.filter((copy) => copy.status === 'Borrowed').length
  const reservedCopies = copies.filter((copy) => copy.status === 'Reserved').length
  const unavailableCopies = Math.max(
    0,
    (details?.totalCopies ?? 0) - availableCopies - borrowedCopies - reservedCopies,
  )
  const columns: DataTableColumn<BookCopy>[] = [
    {
      id: 'id',
      header: 'Mã bản sao',
      cell: (copy) => <span className="font-mono text-xs">{copy.id.slice(0, 8)}</span>,
    },
    {
      id: 'barcode',
      header: 'Mã vạch',
      cell: (copy) => <span className="font-mono">{copy.barcode}</span>,
    },
    {
      id: 'status',
      header: 'Trạng thái',
      cell: (copy) => (
        <StatusBadge label={copyStatusLabels[copy.status]} tone={statusTone(copy.status)} />
      ),
    },
    { id: 'location', header: 'Vị trí', cell: (copy) => copyLocationLabel(copy, shelves) },
    { id: 'condition', header: 'Tình trạng', cell: (copy) => copyConditionLabels[copy.condition] },
    {
      id: 'acquired',
      header: 'Ngày nhập',
      cell: (copy) => new Date(copy.acquiredAtUtc).toLocaleDateString('vi-VN'),
    },
  ]

  return (
    <Dialog
      open={open}
      onOpenChange={(next) => {
        onOpenChange(next)
        if (!next) {
          setStatus('All')
          setShelfId('all')
          setDetails(null)
          setError('')
        }
      }}
    >
      <DialogContent
        showCloseButton={false}
        viewportClassName="gap-0"
        className="max-h-[92dvh] p-0 sm:max-w-6xl"
      >
        <DialogHeader className="border-b px-4 py-4 text-left sm:px-6">
          <div className="flex items-start justify-between gap-4">
            <div className="min-w-0 space-y-2">
              <div className="flex flex-wrap items-center gap-2">
                <DialogTitle className="break-words">{book.title}</DialogTitle>
                <Badge variant="outline" className="shrink-0 gap-1">
                  <AiSparklesIcon className="size-3.5" /> Độ liên quan{' '}
                  {Math.round(book.similarity * 100)}%
                </Badge>
              </div>
              <DialogDescription>
                Kết quả liên quan đến truy vấn: &ldquo;{query}&rdquo;. Điểm này chỉ thể hiện mức
                liên quan với truy vấn.
              </DialogDescription>
            </div>
            <DialogClose asChild>
              <Button
                type="button"
                variant="ghost"
                size="icon"
                className="shrink-0"
                aria-label="Đóng"
              >
                <X />
              </Button>
            </DialogClose>
          </div>
        </DialogHeader>

        <div className="grid gap-5 px-4 py-5 sm:px-6">
          {loading ? (
            <ScreenState kind="loading" title="Đang tải thông tin tồn kho..." />
          ) : error ? (
            <ScreenState
              kind="error"
              title="Không thể tải tồn kho"
              description={error}
              actionLabel="Thử lại"
              onAction={() => setRetryKey((value) => value + 1)}
            />
          ) : details ? (
            <>
              <Card className="gap-4 py-4">
                <CardContent className="grid gap-4 sm:grid-cols-[96px_minmax(0,1fr)]">
                  <div className="grid h-32 w-24 place-items-center rounded-lg border bg-muted text-muted-foreground">
                    <BookOpen className="size-9" />
                  </div>
                  <div className="grid gap-3">
                    <div>
                      <h3 className="text-lg font-semibold">{details.book.title}</h3>
                      <p className="text-muted-foreground">{details.book.author}</p>
                    </div>
                    <div className="grid gap-x-6 gap-y-2 text-sm sm:grid-cols-2 lg:grid-cols-4">
                      <Info label="ISBN" value={details.book.isbn} />
                      <Info label="Thể loại" value={details.book.category} />
                      <Info label="Nhà xuất bản" value={details.book.publisher?.name} />
                      <Info
                        label="Năm xuất bản"
                        value={
                          details.book.publicationYear ? String(details.book.publicationYear) : null
                        }
                      />
                      <Info label="Ấn bản" value={details.book.editionStatement} />
                      <Info label="Ngôn ngữ" value={details.book.language} />
                    </div>
                    {details.book.description ? (
                      <p className="line-clamp-3 text-sm text-muted-foreground">
                        {details.book.description}
                      </p>
                    ) : null}
                  </div>
                </CardContent>
              </Card>

              <section className="grid gap-3" aria-labelledby="inventory-summary-title">
                <h3 id="inventory-summary-title" className="font-semibold">
                  Tổng quan kho
                </h3>
                <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-5">
                  <Metric label="Tổng bản sao" value={details.totalCopies} />
                  <Metric label="Sẵn sàng" value={availableCopies} tone="success" />
                  <Metric label="Đang mượn" value={borrowedCopies} />
                  <Metric label="Đã giữ chỗ" value={reservedCopies} tone="warning" />
                  <Metric
                    label="Trạng thái khác"
                    value={unavailableCopies}
                    tone={unavailableCopies ? 'danger' : undefined}
                  />
                </div>
              </section>

              <Card className="gap-3 border-emerald-600/30 bg-emerald-600/5 py-4">
                <CardHeader>
                  <CardTitle className="text-base text-emerald-700 dark:text-emerald-300">
                    ✓ {availableCopies} bản sao sẵn sàng
                  </CardTitle>
                </CardHeader>
                <CardContent className="grid gap-2">
                  {availableLocations.length ? (
                    availableLocations.map((location) => (
                      <div
                        key={location.label}
                        className="flex items-start justify-between gap-3 rounded-md border bg-background/80 p-3 text-sm"
                      >
                        <span className="flex min-w-0 items-start gap-2">
                          <MapPin className="mt-0.5 size-4 shrink-0 text-primary" />
                          {location.label}
                        </span>
                        <strong className="shrink-0">{location.count} bản</strong>
                      </div>
                    ))
                  ) : (
                    <p className="text-sm text-muted-foreground">Hiện không có bản sao sẵn sàng.</p>
                  )}
                </CardContent>
              </Card>

              <section className="grid gap-3" aria-labelledby="copy-list-title">
                <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
                  <div>
                    <h3 id="copy-list-title" className="font-semibold">
                      Bản sao vật lý
                    </h3>
                    <p className="text-sm text-muted-foreground">
                      {filteredCopies.length} bản sao phù hợp bộ lọc.
                    </p>
                  </div>
                  <div className="grid gap-2 sm:grid-cols-2">
                    <Select
                      value={status}
                      onValueChange={(value) => setStatus(value as StatusFilter)}
                    >
                      <SelectTrigger aria-label="Lọc trạng thái bản sao">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="All">Mọi trạng thái</SelectItem>
                        {copyStatuses.map((item) => (
                          <SelectItem key={item} value={item}>
                            {copyStatusLabels[item]}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    <Select value={shelfId} onValueChange={setShelfId}>
                      <SelectTrigger aria-label="Lọc vị trí bản sao">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="all">Mọi vị trí</SelectItem>
                        {shelves
                          .filter((shelf) => copies.some((copy) => copy.shelfId === shelf.id))
                          .map((shelf) => (
                            <SelectItem key={shelf.id} value={shelf.id}>
                              {shelf.branchCode} / {shelf.areaCode} / {shelf.code}
                            </SelectItem>
                          ))}
                      </SelectContent>
                    </Select>
                  </div>
                </div>
                <div className="hidden sm:block">
                  <DataTable
                    caption={`Bản sao của ${details.book.title}`}
                    rows={filteredCopies}
                    columns={columns}
                    getRowId={(copy) => copy.id}
                    emptyTitle="Không có bản sao phù hợp"
                  />
                </div>
                <div className="grid gap-3 sm:hidden">
                  {filteredCopies.map((copy) => (
                    <Card key={copy.id} className="gap-3 py-4">
                      <CardContent className="grid gap-3">
                        <div className="flex items-start justify-between gap-2">
                          <div>
                            <p className="font-mono font-semibold">{copy.barcode}</p>
                            <p className="font-mono text-xs text-muted-foreground">
                              {copy.id.slice(0, 8)}
                            </p>
                          </div>
                          <StatusBadge
                            label={copyStatusLabels[copy.status]}
                            tone={statusTone(copy.status)}
                          />
                        </div>
                        <p className="flex items-start gap-2 text-sm">
                          <MapPin className="mt-0.5 size-4 shrink-0 text-primary" />
                          {copyLocationLabel(copy, shelves)}
                        </p>
                        <div className="grid grid-cols-2 gap-2 text-sm">
                          <Info label="Tình trạng" value={copyConditionLabels[copy.condition]} />
                          <Info
                            label="Ngày nhập"
                            value={new Date(copy.acquiredAtUtc).toLocaleDateString('vi-VN')}
                          />
                        </div>
                      </CardContent>
                    </Card>
                  ))}
                </div>
              </section>
            </>
          ) : null}
        </div>

        <DialogFooter showCloseButton className="border-t bg-background px-4 py-4 sm:px-6">
          <Button asChild>
            <Link to={`/catalog/${book.id}`}>
              <ExternalLink /> Mở trang biên mục
            </Link>
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

function Info({ label, value }: { label: string; value?: string | null }) {
  if (!value) return null
  return (
    <div>
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className="mt-0.5 font-medium">{value}</p>
    </div>
  )
}

function Metric({
  label,
  value,
  tone,
}: {
  label: string
  value: number
  tone?: 'success' | 'warning' | 'danger'
}) {
  const colors =
    tone === 'success'
      ? 'text-emerald-700 dark:text-emerald-300'
      : tone === 'warning'
        ? 'text-amber-700 dark:text-amber-300'
        : tone === 'danger'
          ? 'text-destructive'
          : ''
  return (
    <div className="rounded-lg border bg-card p-3">
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className={`mt-1 text-2xl font-bold ${colors}`}>{value}</p>
    </div>
  )
}
