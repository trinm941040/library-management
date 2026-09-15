import type { ReactNode } from 'react'
import type { z } from 'zod'
import { Button } from '@/common/components/ui/button'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/common/components/ui/dialog'
import { importFieldErrorSchema } from '@/shared/data/table-contracts'

type ImportError = z.infer<typeof importFieldErrorSchema>

export function ImportPreviewDialog({
  open, title, description, file, errors, canConfirm, pending, children,
  onOpenChange, onFileChange, onPreview, onConfirm,
}: {
  open: boolean
  title: string
  description: string
  file: File | null
  errors: readonly ImportError[]
  canConfirm: boolean
  pending?: boolean
  children?: ReactNode
  onOpenChange: (open: boolean) => void
  onFileChange: (file: File | null) => void
  onPreview: () => void
  onConfirm: () => void
}) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-4xl">
        <DialogHeader><DialogTitle>{title}</DialogTitle><DialogDescription>{description}</DialogDescription></DialogHeader>
        <input type="file" accept=".csv,text/csv" aria-label="Chọn tệp CSV" disabled={pending}
          onChange={(event) => onFileChange(event.target.files?.[0] ?? null)} />
        {errors.length > 0 ? (
          <div className="max-h-48 overflow-auto rounded-md border border-destructive/40 p-3" role="alert">
            <p className="mb-2 font-medium text-destructive">Có {errors.length} lỗi cần sửa</p>
            <ul className="list-disc pl-5 text-sm">{errors.map((error, index) => (
              <li key={`${error.rowNumber}-${error.field}-${index}`}>Dòng {error.rowNumber}, {error.field}: {error.message}</li>
            ))}</ul>
          </div>
        ) : null}
        {children}
        <DialogFooter>
          <Button variant="outline" disabled={!file || pending} onClick={onPreview}>Xem trước</Button>
          <Button disabled={!canConfirm || pending} onClick={onConfirm}>Xác nhận nhập</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
