import { useState } from 'react'
import { Bookmark } from 'lucide-react'
import { Spinner } from '@/common/components/atoms/Spinner'
import { Button } from '@/common/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/common/components/ui/dialog'
import { Input } from '@/common/components/ui/input'
import { Label } from '@/common/components/ui/label'

type SaveFilterDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSave: (name: string) => Promise<void>
}

export function SaveFilterDialog({ open, onOpenChange, onSave }: SaveFilterDialogProps) {
  const [name, setName] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!name.trim()) {
      setError('Vui lòng nhập tên bộ lọc.')
      return
    }

    setSaving(true)
    setError(null)
    try {
      await onSave(name.trim())
      setName('')
      onOpenChange(false)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Không thể lưu bộ lọc.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <form onSubmit={handleSubmit}>
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <Bookmark className="w-5 h-5 text-primary" /> Lưu bộ lọc hiện tại
            </DialogTitle>
            <DialogDescription>
              Đặt tên để nhanh chóng áp dụng lại các tiêu chí lọc này trong tương lai.
            </DialogDescription>
          </DialogHeader>

          <div className="py-4 space-y-3">
            <div className="space-y-1.5">
              <Label htmlFor="filter-name">Tên bộ lọc <span className="text-destructive">*</span></Label>
              <Input
                id="filter-name"
                placeholder="Ví dụ: Khoản mượn tháng này chi nhánh chính..."
                value={name}
                onChange={(e) => setName(e.target.value)}
                maxLength={150}
              />
            </div>
            {error && <p className="text-xs text-destructive">{error}</p>}
          </div>

          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={() => onOpenChange(false)}
              disabled={saving}
            >
              Hủy
            </Button>
            <Button type="submit" disabled={saving || !name.trim()}>
              {saving && <Spinner size="sm" decorative />}
              Lưu bộ lọc
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
