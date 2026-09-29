import { useEffect, useState } from 'react'
import { BookOpen, MapPin } from 'lucide-react'
import { ScreenState } from '@/common/components'
import { Badge } from '@/common/components/ui/badge'
import { Button } from '@/common/components/ui/button'
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/common/components/ui/dialog'
import { getKioskBook, type KioskBookDetail } from './kiosk-api'

export type KioskBookSelection = {
  id: string
  title: string
  similarity?: number
}

export function KioskBookDetailDialog({
  selection,
  open,
  onOpenChange,
}: {
  selection: KioskBookSelection | null
  open: boolean
  onOpenChange: (open: boolean) => void
}) {
  const [details, setDetails] = useState<KioskBookDetail | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [retryKey, setRetryKey] = useState(0)

  useEffect(() => {
    if (!open || !selection) return
    const controller = new AbortController()
    setLoading(true)
    setError('')
    setDetails(null)
    getKioskBook(selection.id, controller.signal)
      .then(setDetails)
      .catch((reason: unknown) => {
        if (!(reason instanceof DOMException && reason.name === 'AbortError'))
          setError('Không thể tải vị trí sách lúc này. Vui lòng thử lại.')
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false)
      })
    return () => controller.abort()
  }, [open, retryKey, selection])

  if (!selection) return null
  const availableCount = details?.availableCopies ?? 0

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-3xl">
        <DialogHeader>
          <DialogTitle className="pr-8 text-xl">{selection.title}</DialogTitle>
          <DialogDescription>Thông tin sách và vị trí bản sao đang có sẵn.</DialogDescription>
        </DialogHeader>

        <div className="grid gap-5 px-1">
          {loading ? (
            <ScreenState kind="loading" title="Đang tải vị trí sách..." />
          ) : error ? (
            <ScreenState
              kind="error"
              title="Không thể tải chi tiết"
              description={error}
              actionLabel="Thử lại"
              onAction={() => setRetryKey((value) => value + 1)}
            />
          ) : details ? (
            <>
              <section className="grid gap-4 rounded-xl border bg-card p-4 sm:grid-cols-[6rem_minmax(0,1fr)]">
                <div className="grid h-32 w-24 place-items-center rounded-lg bg-muted text-muted-foreground">
                  <BookOpen className="size-10" aria-hidden="true" />
                </div>
                <div className="grid gap-2">
                  <div>
                    <h3 className="text-lg font-semibold">{details.title}</h3>
                    <p className="text-muted-foreground">{details.author}</p>
                  </div>
                  <div className="flex flex-wrap gap-2">
                    <Badge variant="secondary">{details.category}</Badge>
                    {selection.similarity !== undefined ? (
                      <Badge variant="outline">
                        Độ liên quan {Math.round(selection.similarity * 100)}%
                      </Badge>
                    ) : null}
                  </div>
                  <p className="font-mono text-sm">ISBN: {details.isbn}</p>
                  {details.description ? (
                    <p className="text-sm text-muted-foreground">{details.description}</p>
                  ) : null}
                </div>
              </section>

              <section className="grid gap-3 rounded-xl border border-emerald-600/30 bg-emerald-600/5 p-4">
                <h3 className="text-lg font-semibold text-emerald-700 dark:text-emerald-300">
                  {availableCount > 0
                    ? `✓ ${availableCount} bản đang có sẵn`
                    : 'Hiện không có bản sao sẵn sàng'}
                </h3>
                {details.locations.map((location) => (
                  <div
                    key={location.label}
                    className="flex items-start justify-between gap-4 rounded-lg border bg-background p-4"
                  >
                    <span className="flex items-start gap-2">
                      <MapPin className="mt-0.5 size-5 shrink-0 text-primary" aria-hidden="true" />
                      <strong>{location.label}</strong>
                    </span>
                    <span className="shrink-0">{location.availableCopies} bản</span>
                  </div>
                ))}
              </section>
            </>
          ) : null}
        </div>

        <DialogFooter>
          <DialogClose asChild>
            <Button type="button" size="lg">Hoàn tất</Button>
          </DialogClose>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
