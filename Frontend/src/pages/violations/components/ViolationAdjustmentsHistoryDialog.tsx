import { useEffect, useState } from 'react'
import { AlertCircle, History, MinusCircle, PlusCircle, ShieldAlert } from 'lucide-react'
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
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/common/components/ui/table'
import { getViolationAdjustments, type FineAdjustmentItem } from '../adjustment-api'
import type { LibraryViolation } from '../violation-api'

type Props = {
  violation: LibraryViolation | null
  open: boolean
  onOpenChange: (open: boolean) => void
}

function formatMoney(amount: number): string {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(amount)
}

function formatDate(iso: string): string {
  if (!iso) return '—'
  return new Intl.DateTimeFormat('vi-VN', {
    dateStyle: 'short',
    timeStyle: 'short',
  }).format(new Date(iso))
}

export function ViolationAdjustmentsHistoryDialog({ violation, open, onOpenChange }: Props) {
  const [items, setItems] = useState<FineAdjustmentItem[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    if (open && violation) {
      setIsLoading(true)
      setError('')
      getViolationAdjustments(violation.id)
        .then(setItems)
        .catch((err) => {
          setError(err instanceof Error ? err.message : 'Không thể tải lịch sử điều chỉnh.')
        })
        .finally(() => setIsLoading(false))
    }
  }, [open, violation])

  if (!violation) return null

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <History className="size-5 text-primary" />
            Lịch sử điều chỉnh tiền phạt
          </DialogTitle>
          <DialogDescription>
            Độc giả: <strong>{violation.borrowerName}</strong> — Số tiền gốc: {formatMoney(violation.fineAmount)}
          </DialogDescription>
        </DialogHeader>

        {error && (
          <div className="flex items-center gap-2 rounded-md border border-destructive/30 bg-destructive/10 p-3 text-sm text-destructive">
            <AlertCircle className="size-4 shrink-0" />
            <span>{error}</span>
          </div>
        )}

        <div className="max-h-[350px] overflow-y-auto rounded-md border">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Thời gian</TableHead>
                <TableHead>Loại</TableHead>
                <TableHead className="text-right">Mức điều chỉnh</TableHead>
                <TableHead>Lý do</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {isLoading ? (
                <TableRow>
                  <TableCell colSpan={4} className="h-24 text-center text-muted-foreground">
                    Đang tải dữ liệu...
                  </TableCell>
                </TableRow>
              ) : items.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={4} className="h-24 text-center text-muted-foreground">
                    Chưa có lần điều chỉnh nào cho vi phạm này.
                  </TableCell>
                </TableRow>
              ) : (
                items.map((item) => (
                  <TableRow key={item.id}>
                    <TableCell className="text-xs whitespace-nowrap">
                      {formatDate(item.adjustedAtUtc)}
                    </TableCell>
                    <TableCell>
                      {item.amountDelta < 0 ? (
                        <Badge variant="secondary" className="gap-1 text-emerald-600 dark:text-emerald-400">
                          <MinusCircle className="size-3" />
                          Giảm phạt
                        </Badge>
                      ) : (
                        <Badge variant="secondary" className="gap-1 text-amber-600 dark:text-amber-400">
                          <PlusCircle className="size-3" />
                          Tăng phạt
                        </Badge>
                      )}
                    </TableCell>
                    <TableCell className="text-right font-medium whitespace-nowrap">
                      <span className={item.amountDelta < 0 ? 'text-emerald-600 dark:text-emerald-400' : 'text-amber-600 dark:text-amber-400'}>
                        {item.amountDelta > 0 ? `+${formatMoney(item.amountDelta)}` : formatMoney(item.amountDelta)}
                      </span>
                    </TableCell>
                    <TableCell className="text-sm max-w-xs break-words">
                      {item.reason}
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Đóng
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
