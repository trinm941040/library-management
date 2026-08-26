import { useEffect, useState, type FormEvent } from 'react'
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
import type { SystemUser } from '../user-api'

export type UserFormData = {
  displayName: string
  email: string
  password: string
}

type UserFormDialogProps = {
  open: boolean
  user: SystemUser | null
  onOpenChange: (open: boolean) => void
  onSave: (data: UserFormData) => Promise<string | null>
}

const emptyForm: UserFormData = {
  displayName: '',
  email: '',
  password: '',
}

export function UserFormDialog({ open, user, onOpenChange, onSave }: UserFormDialogProps) {
  const [form, setForm] = useState<UserFormData>(emptyForm)
  const [error, setError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  useEffect(() => {
    setForm(
      user
        ? { displayName: user.displayName, email: user.email, password: '' }
        : emptyForm,
    )
    setError('')
  }, [open, user])

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError('')
    setIsSubmitting(true)

    try {
      const saveError = await onSave(form)
      if (saveError) {
        setError(saveError)
        return
      }
      onOpenChange(false)
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={(nextOpen) => !isSubmitting && onOpenChange(nextOpen)}>
      <DialogContent>
        <form onSubmit={handleSubmit}>
          <DialogHeader>
            <DialogTitle>{user ? 'Chỉnh sửa user' : 'Thêm user mới'}</DialogTitle>
            <DialogDescription>
              {user
                ? 'Cập nhật tên hiển thị và email của tài khoản.'
                : 'Tạo tài khoản mới. Hệ thống sẽ tự gán vai trò User.'}
            </DialogDescription>
          </DialogHeader>

          <div className="grid gap-5 py-6">
            <div className="grid gap-2">
              <Label htmlFor="user-display-name">Họ và tên</Label>
              <Input
                id="user-display-name"
                value={form.displayName}
                onChange={(event) => setForm({ ...form, displayName: event.target.value })}
                placeholder="Nguyễn Văn A"
                minLength={2}
                maxLength={100}
                autoComplete="name"
                disabled={isSubmitting}
                required
              />
            </div>

            <div className="grid gap-2">
              <Label htmlFor="user-email">Email</Label>
              <Input
                id="user-email"
                type="email"
                value={form.email}
                onChange={(event) => setForm({ ...form, email: event.target.value })}
                placeholder="user@example.com"
                maxLength={256}
                autoComplete="email"
                disabled={isSubmitting}
                required
              />
              {user && form.email.trim().toLowerCase() !== user.email.toLowerCase() ? (
                <p className="text-xs text-muted-foreground">
                  Đổi email sẽ hủy trạng thái xác nhận email và thu hồi các phiên đăng nhập hiện tại.
                </p>
              ) : null}
            </div>

            {!user ? (
              <div className="grid gap-2">
                <Label htmlFor="user-password">Mật khẩu tạm thời</Label>
                <Input
                  id="user-password"
                  type="password"
                  value={form.password}
                  onChange={(event) => setForm({ ...form, password: event.target.value })}
                  minLength={8}
                  maxLength={256}
                  autoComplete="new-password"
                  disabled={isSubmitting}
                  required
                />
                <p className="text-xs text-muted-foreground">Mật khẩu phải có ít nhất 8 ký tự.</p>
              </div>
            ) : null}

            {error ? (
              <p className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive" role="alert">
                {error}
              </p>
            ) : null}
          </div>

          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              disabled={isSubmitting}
              onClick={() => onOpenChange(false)}
            >
              Hủy
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? 'Đang lưu...' : user ? 'Lưu thay đổi' : 'Thêm user'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
