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
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/common/components/ui/select'
import type { SystemUser, UserRole } from '../user-store'

export type UserFormData = {
  name: string
  email: string
  role: UserRole
}

type UserFormDialogProps = {
  open: boolean
  user: SystemUser | null
  onOpenChange: (open: boolean) => void
  onSave: (data: UserFormData) => string | null
}

const emptyForm: UserFormData = {
  name: '',
  email: '',
  role: 'Member',
}

export function UserFormDialog({ open, user, onOpenChange, onSave }: UserFormDialogProps) {
  const [form, setForm] = useState<UserFormData>(emptyForm)
  const [error, setError] = useState('')

  useEffect(() => {
    setForm(user ? { name: user.name, email: user.email, role: user.role } : emptyForm)
    setError('')
  }, [open, user])

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const saveError = onSave(form)
    if (saveError) {
      setError(saveError)
      return
    }
    onOpenChange(false)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <form onSubmit={handleSubmit}>
          <DialogHeader>
            <DialogTitle>{user ? 'Chỉnh sửa user' : 'Thêm user mới'}</DialogTitle>
            <DialogDescription>
              {user
                ? 'Cập nhật thông tin và vai trò của user.'
                : 'Tạo một tài khoản mới cho hệ thống.'}
            </DialogDescription>
          </DialogHeader>

          <div className="grid gap-5 py-6">
            <div className="grid gap-2">
              <Label htmlFor="user-name">Họ và tên</Label>
              <Input
                id="user-name"
                value={form.name}
                onChange={(event) => setForm({ ...form, name: event.target.value })}
                placeholder="Nguyễn Văn A"
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
                required
              />
            </div>

            {!user ? (
              <div className="grid gap-2">
                <Label htmlFor="user-password">Mật khẩu tạm thời</Label>
                <Input id="user-password" type="password" minLength={8} required />
              </div>
            ) : null}

            <div className="grid gap-2">
              <Label htmlFor="user-role">Bắt buộc đổi mật khẩu</Label>
            </div>

            <div className="grid gap-2">
              <Label htmlFor="user-role">Vai trò</Label>
              <Select
                value={form.role}
                onValueChange={(role) => setForm({ ...form, role: role as UserRole })}
              >
                <SelectTrigger id="user-role" className="w-full">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Administrator">Quản trị viên</SelectItem>
                  <SelectItem value="Librarian">Thủ thư</SelectItem>
                  <SelectItem value="Member">Độc giả</SelectItem>
                </SelectContent>
              </Select>
            </div>

            {error ? (
              <p className="text-sm text-destructive" role="alert">
                {error}
              </p>
            ) : null}
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Hủy
            </Button>
            <Button type="submit">{user ? 'Lưu thay đổi' : 'Thêm user'}</Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
