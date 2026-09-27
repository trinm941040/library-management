import type { ReactNode } from 'react'
import type { z } from 'zod'
import { FileText, Upload } from 'lucide-react'
import { Button } from '@/common/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/common/components/ui/dialog'
import { importFieldErrorSchema } from '@/shared/data/table-contracts'

type ImportError = z.infer<typeof importFieldErrorSchema>

export function ImportPreviewDialog({
  open,
  title,
  description,
  file,
  errors,
  canConfirm,
  pendingAction,
  children,
  onOpenChange,
  onFileChange,
  onPreview,
  onConfirm,
}: {
  open: boolean
  title: string
  description: string
  file: File | null
  errors: readonly ImportError[]
  canConfirm: boolean
  pendingAction?: 'preview' | 'confirm' | null
  children?: ReactNode
  onOpenChange: (open: boolean) => void
  onFileChange: (file: File | null) => void
  onPreview: () => void
  onConfirm: () => void
}) {
  return (
    <Dialog
      open={open}
      onOpenChange={(nextOpen) => !pendingAction && onOpenChange(nextOpen)}
    >
      <DialogContent className="sm:max-w-4xl">
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
          <DialogDescription>{description}</DialogDescription>
        </DialogHeader>
        <div className="grid min-w-0 gap-2">
          <label
            className={`flex min-h-11 w-fit max-w-full cursor-pointer items-center gap-2 rounded-md border border-input bg-background px-4 py-2 text-sm font-medium shadow-sm transition-colors hover:border-primary hover:bg-accent hover:text-accent-foreground focus-within:outline-none focus-within:ring-2 focus-within:ring-ring focus-within:ring-offset-2 ${pendingAction ? 'pointer-events-none opacity-50' : ''}`}
          >
            <Upload className="size-4 shrink-0" aria-hidden="true" />
            <span>Chọn tệp CSV</span>
            <input
              type="file"
              className="sr-only"
              accept=".csv,text/csv"
              aria-label="Chọn tệp CSV"
              disabled={Boolean(pendingAction)}
              onChange={(event) => onFileChange(event.target.files?.[0] ?? null)}
            />
          </label>
          <p className="flex min-w-0 items-center gap-2 text-sm text-muted-foreground" aria-live="polite">
            <FileText className="size-4 shrink-0" aria-hidden="true" />
            <span className="truncate">{file?.name ?? 'Chưa chọn tệp nào'}</span>
          </p>
        </div>
        {errors.length > 0 ? (
          <div
            className="max-h-48 overflow-auto rounded-md border border-destructive/40 p-3"
            role="alert"
          >
            <p className="mb-2 font-medium text-destructive">Có {errors.length} lỗi cần sửa</p>
            <ul className="list-disc pl-5 text-sm">
              {errors.map((error, index) => (
                <li key={`${error.rowNumber}-${error.field}-${index}`}>
                  Dòng {error.rowNumber}, {error.field}: {error.message}
                </li>
              ))}
            </ul>
          </div>
        ) : null}
        {children}
        <DialogFooter>
          <Button
            variant="outline"
            disabled={!file || Boolean(pendingAction)}
            loading={pendingAction === 'preview'}
            loadingLabel="Đang tạo bản xem trước"
            onClick={onPreview}
          >
            Xem trước
          </Button>
          <Button
            disabled={!canConfirm || Boolean(pendingAction)}
            loading={pendingAction === 'confirm'}
            loadingLabel="Đang nhập dữ liệu"
            onClick={onConfirm}
          >
            Xác nhận nhập
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
