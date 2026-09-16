import { zodResolver } from '@hookform/resolvers/zod'
import { ArrowLeft, Eye, EyeOff, KeyRound, ShieldCheck } from 'lucide-react'
import { useState } from 'react'
import { useForm, type UseFormReturn } from 'react-hook-form'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '@/auth/AuthProvider'
import { Button } from '@/common/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/common/components/ui/card'
import { Input } from '@/common/components/ui/input'
import { Label } from '@/common/components/ui/label'
import { changePassword, passwordFormSchema, type PasswordFormValues } from './profile-api'

export function ChangePasswordPage() {
  const navigate = useNavigate()
  const { clearSession } = useAuth()
  const [error, setError] = useState('')
  const [visible, setVisible] = useState<Record<keyof PasswordFormValues, boolean>>({ currentPassword: false, newPassword: false, confirmPassword: false })
  const form = useForm<PasswordFormValues>({ resolver: zodResolver(passwordFormSchema), defaultValues: { currentPassword: '', newPassword: '', confirmPassword: '' } })

  const submit = form.handleSubmit(async (values) => {
    setError('')
    try {
      await changePassword(values)
      form.reset(); clearSession()
      navigate('/login', { replace: true, state: { notice: 'Đổi mật khẩu thành công. Vui lòng đăng nhập lại.' } })
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Không thể đổi mật khẩu.')
      form.reset()
    }
  })

  return <div className="content-wrap max-w-4xl">
    <Link to="/profile" className="mb-5 inline-flex items-center gap-2 text-sm font-medium text-muted-foreground hover:text-primary"><ArrowLeft className="size-4" /> Quay lại thông tin cá nhân</Link>
    <div className="mb-7"><p className="eyebrow">BẢO MẬT TÀI KHOẢN</p><h1 className="text-3xl font-bold">Đổi mật khẩu</h1><p className="subheading">Cập nhật mật khẩu đăng nhập và thu hồi các phiên đang hoạt động.</p></div>
    <div className="grid gap-6 md:grid-cols-[minmax(0,1fr)_17rem]">
      <Card>
        <CardHeader><CardTitle className="flex items-center gap-2"><KeyRound className="size-5 text-primary" /> Mật khẩu mới</CardTitle><CardDescription>Không chia sẻ mật khẩu với bất kỳ ai.</CardDescription></CardHeader>
        <CardContent><form className="grid gap-5" onSubmit={submit}>
          <PasswordField label="Mật khẩu hiện tại" name="currentPassword" autoComplete="current-password" form={form} visible={visible.currentPassword} toggle={() => setVisible((value) => ({ ...value, currentPassword: !value.currentPassword }))} />
          <PasswordField label="Mật khẩu mới" name="newPassword" autoComplete="new-password" form={form} visible={visible.newPassword} toggle={() => setVisible((value) => ({ ...value, newPassword: !value.newPassword }))} />
          <PasswordField label="Xác nhận mật khẩu mới" name="confirmPassword" autoComplete="new-password" form={form} visible={visible.confirmPassword} toggle={() => setVisible((value) => ({ ...value, confirmPassword: !value.confirmPassword }))} />
          {error ? <p role="alert" className="rounded-md border border-destructive/30 bg-destructive/5 p-3 text-sm text-destructive">{error}</p> : null}
          <div className="flex justify-end gap-2"><Button type="button" variant="outline" asChild><Link to="/profile">Hủy</Link></Button><Button type="submit" loading={form.formState.isSubmitting} loadingLabel="Đang cập nhật mật khẩu">Cập nhật mật khẩu</Button></div>
        </form></CardContent>
      </Card>
      <Card className="h-fit bg-muted/20">
        <CardHeader><CardTitle className="flex items-center gap-2 text-base"><ShieldCheck className="size-5 text-primary" /> Yêu cầu bảo mật</CardTitle></CardHeader>
        <CardContent><ul className="grid gap-3 text-sm text-muted-foreground"><li>Ít nhất 8 ký tự.</li><li>Nên kết hợp chữ hoa, chữ thường, số và ký tự đặc biệt.</li><li>Không sử dụng lại mật khẩu cũ hoặc thông tin dễ đoán.</li><li className="rounded-md bg-primary/5 p-3 text-foreground">Sau khi đổi thành công, bạn cần đăng nhập lại trên tất cả thiết bị.</li></ul></CardContent>
      </Card>
    </div>
  </div>
}

function PasswordField({ label, name, visible, toggle, form, autoComplete }: { label: string; name: keyof PasswordFormValues; visible: boolean; toggle: () => void; form: UseFormReturn<PasswordFormValues>; autoComplete: string }) {
  const message = form.formState.errors[name]?.message
  return <div className="grid gap-2"><Label htmlFor={name}>{label}</Label><div className="relative"><Input id={name} type={visible ? 'text' : 'password'} autoComplete={autoComplete} className="pr-10" aria-invalid={!!message} {...form.register(name)} /><Button type="button" variant="ghost" size="icon-sm" className="absolute top-0.5 right-1" aria-label={visible ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'} onClick={toggle}>{visible ? <EyeOff /> : <Eye />}</Button></div>{message ? <p className="text-sm text-destructive" role="alert">{message}</p> : null}</div>
}
