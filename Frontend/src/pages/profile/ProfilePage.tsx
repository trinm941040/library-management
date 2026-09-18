import { zodResolver } from '@hookform/resolvers/zod'
import {
  BriefcaseBusiness,
  Building2,
  Check,
  KeyRound,
  Pencil,
  RefreshCw,
  ShieldCheck,
  UserRound,
} from 'lucide-react'
import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { ApiError, getProfile } from '@/auth/auth-api'
import { useAuth } from '@/auth/AuthProvider'
import { Button } from '@/common/components/ui/button'
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/common/components/ui/card'
import { Input } from '@/common/components/ui/input'
import { Label } from '@/common/components/ui/label'
import { LoadingBoundary } from '@/common/components/molecules/LoadingBoundary'
import { profileFormSchema, updateProfile, type ProfileFormValues } from './profile-api'

export function ProfilePage() {
  const { user, updateUser } = useAuth()
  const [editing, setEditing] = useState(false)
  const [notice, setNotice] = useState('')
  const [error, setError] = useState('')
  const [isReloading, setIsReloading] = useState(false)

  const profileForm = useForm<ProfileFormValues>({
    resolver: zodResolver(profileFormSchema),
    values: user?.rowVersion
      ? {
          fullName: user.fullName ?? user.displayName,
          phoneNumber: user.phoneNumber ?? '',
          dateOfBirth: user.dateOfBirth ?? '',
          address: user.address ?? '',
          rowVersion: user.rowVersion,
        }
      : undefined,
  })

  useEffect(() => {
    const warn = (event: BeforeUnloadEvent) => {
      if (!profileForm.formState.isDirty) return
      event.preventDefault()
      event.returnValue = ''
    }
    window.addEventListener('beforeunload', warn)
    return () => window.removeEventListener('beforeunload', warn)
  }, [profileForm.formState.isDirty])

  if (!user)
    return (
      <div className="content-wrap">
        <LoadingBoundary loading label="Đang tải hồ sơ" className="min-h-[40vh]" />
      </div>
    )

  const submitProfile = profileForm.handleSubmit(async (values) => {
    setError('')
    setNotice('')
    try {
      const updated = await updateProfile(values)
      updateUser(updated)
      profileForm.reset({
        fullName: updated.fullName ?? updated.displayName,
        phoneNumber: updated.phoneNumber ?? '',
        dateOfBirth: updated.dateOfBirth ?? '',
        address: updated.address ?? '',
        rowVersion: updated.rowVersion!,
      })
      setEditing(false)
      setNotice('Thông tin cá nhân đã được cập nhật.')
    } catch (caught) {
      const apiError = caught instanceof ApiError ? caught : null
      setError(
        apiError?.status === 409
          ? 'Hồ sơ vừa được cập nhật ở nơi khác. Hãy tải lại trang trước khi lưu lại.'
          : caught instanceof Error
            ? caught.message
            : 'Không thể cập nhật hồ sơ.',
      )
    }
  })

  const reloadProfile = async () => {
    setIsReloading(true)
    setError('')
    try {
      const latest = await getProfile()
      updateUser(latest)
      profileForm.reset({
        fullName: latest.fullName ?? latest.displayName,
        phoneNumber: latest.phoneNumber ?? '',
        dateOfBirth: latest.dateOfBirth ?? '',
        address: latest.address ?? '',
        rowVersion: latest.rowVersion!,
      })
      setEditing(false)
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Không thể tải lại hồ sơ.')
    } finally {
      setIsReloading(false)
    }
  }

  return (
    <div className="content-wrap max-w-6xl">
      <div className="mb-7 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <p className="eyebrow">TÀI KHOẢN CỦA TÔI</p>
          <h1 className="text-3xl font-bold">Thông tin cá nhân</h1>
          <p className="subheading">
            Xem thông tin công việc, quyền truy cập và cập nhật hồ sơ được phép.
          </p>
        </div>
        <div className="flex gap-2">
          <Button
            variant="outline"
            loading={isReloading}
            loadingLabel="Đang tải lại hồ sơ"
            onClick={reloadProfile}
          >
            <RefreshCw /> Tải lại
          </Button>
          {!editing && user.employeeId ? (
            <Button onClick={() => setEditing(true)}>
              <Pencil /> Chỉnh sửa
            </Button>
          ) : null}
        </div>
      </div>
      {notice ? (
        <p
          className="mb-5 rounded-lg border border-green-600/20 bg-green-600/10 p-3 text-sm text-green-700"
          role="status"
        >
          {notice}
        </p>
      ) : null}
      {error ? (
        <div
          className="mb-5 flex items-center justify-between gap-3 rounded-lg border border-destructive/30 bg-destructive/5 p-3 text-sm text-destructive"
          role="alert"
        >
          <span>{error}</span>
          <Button type="button" size="sm" variant="outline" onClick={reloadProfile}>
            Thử lại
          </Button>
        </div>
      ) : null}

      <div className="grid gap-6 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <UserRound className="size-5 text-primary" /> Thông tin cá nhân
            </CardTitle>
            <CardDescription>Các trường bạn có thể tự cập nhật.</CardDescription>
          </CardHeader>
          <CardContent>
            {user.employeeId && user.rowVersion ? (
              <form className="grid gap-4" onSubmit={submitProfile}>
                <EditableField
                  label="Họ và tên"
                  name="fullName"
                  disabled={!editing}
                  form={profileForm}
                />
                <EditableField
                  label="Số điện thoại"
                  name="phoneNumber"
                  disabled={!editing}
                  form={profileForm}
                  autoComplete="tel"
                />
                <EditableField
                  label="Ngày sinh"
                  name="dateOfBirth"
                  disabled={!editing}
                  form={profileForm}
                  type="date"
                />
                <div className="grid gap-2">
                  <Label htmlFor="address">Địa chỉ</Label>
                  <textarea
                    id="address"
                    className="min-h-24 rounded-md border border-input bg-transparent px-3 py-2 text-sm disabled:opacity-60"
                    disabled={!editing}
                    aria-invalid={!!profileForm.formState.errors.address}
                    {...profileForm.register('address')}
                  />
                  {profileForm.formState.errors.address ? (
                    <FieldError text={profileForm.formState.errors.address.message} />
                  ) : null}
                </div>
                {editing ? (
                  <div className="flex justify-end gap-2">
                    <Button
                      type="button"
                      variant="outline"
                      onClick={() => {
                        profileForm.reset()
                        setEditing(false)
                        setError('')
                      }}
                    >
                      Hủy
                    </Button>
                    <Button
                      type="submit"
                      loading={profileForm.formState.isSubmitting}
                      loadingLabel="Đang lưu hồ sơ"
                    >
                      Lưu thay đổi
                    </Button>
                  </div>
                ) : null}
              </form>
            ) : (
              <p className="text-sm text-muted-foreground">
                Tài khoản chưa được liên kết với hồ sơ nhân viên. Liên hệ quản trị viên để cập nhật.
              </p>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <BriefcaseBusiness className="size-5 text-primary" /> Công việc và chi nhánh
            </CardTitle>
            <CardDescription>Thông tin chỉ đọc, được quản lý bởi quản trị viên.</CardDescription>
          </CardHeader>
          <CardContent className="grid gap-4 sm:grid-cols-2">
            <ReadOnly label="Email / định danh" value={user.loginIdentifier} />
            <ReadOnly label="Mã nhân viên" value={user.employeeCode} />
            <ReadOnly label="Chức danh" value={user.position} />
            <ReadOnly label="Phòng ban" value={user.department} />
            <ReadOnly label="Trạng thái" value={user.employmentStatus} />
            <ReadOnly
              label="Chi nhánh"
              value={user.branch ? `${user.branch.name} (${user.branch.code})` : null}
              icon={<Building2 />}
            />
            <ReadOnly
              label="Lần đăng nhập gần nhất"
              value={
                user.lastLoginAtUtc ? new Date(user.lastLoginAtUtc).toLocaleString('vi-VN') : null
              }
            />
          </CardContent>
        </Card>

        <Card className="lg:col-span-2">
          <CardHeader className="border-b">
            <CardTitle className="flex items-center gap-2">
              <ShieldCheck className="size-5 text-primary" /> Quyền truy cập
            </CardTitle>
            <CardDescription>
              Các quyền hiệu lực được tổng hợp từ vai trò trong phiên đăng nhập hiện tại.
            </CardDescription>
          </CardHeader>
          <CardContent className="grid gap-7">
            <section aria-labelledby="roles-heading">
              <h3 id="roles-heading" className="mb-3 text-sm font-semibold">
                Vai trò được gán
              </h3>
              <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
                {user.roles.length ? (
                  user.roles.map((role) => (
                    <div
                      key={role}
                      className="flex items-center gap-3 rounded-lg border bg-muted/20 p-3"
                    >
                      <span className="grid size-9 place-items-center rounded-full bg-primary/10 text-primary">
                        <UserRound className="size-4" />
                      </span>
                      <span>
                        <strong className="block text-sm">{friendlyRole(role)}</strong>
                        <small className="text-muted-foreground">Vai trò đang hiệu lực</small>
                      </span>
                    </div>
                  ))
                ) : (
                  <p className="text-sm text-muted-foreground">Chưa được gán vai trò.</p>
                )}
              </div>
            </section>
            <section aria-labelledby="permissions-heading">
              <div className="mb-3 flex items-center justify-between gap-3">
                <h3 id="permissions-heading" className="text-sm font-semibold">
                  Quyền theo chức năng
                </h3>
                <span className="rounded-full bg-primary/10 px-2.5 py-1 text-xs font-semibold text-primary">
                  {user.permissions.length} quyền
                </span>
              </div>
              <PermissionGroups values={user.permissions} />
            </section>
            <div className="flex items-start gap-3 rounded-lg border border-blue-500/20 bg-blue-500/5 p-4 text-sm">
              <KeyRound className="mt-0.5 size-4 shrink-0 text-primary" />
              <p className="text-muted-foreground">
                Vai trò và quyền chỉ có thể thay đổi qua chức năng quản trị. Nếu cần thêm quyền, hãy
                liên hệ quản trị viên hệ thống.
              </p>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}

type FormHandle = ReturnType<typeof useForm<ProfileFormValues>>
function EditableField({
  label,
  name,
  form,
  ...props
}: { label: string; name: 'fullName' | 'phoneNumber' | 'dateOfBirth'; form: FormHandle } & Omit<
  React.ComponentProps<typeof Input>,
  'form' | 'name'
>) {
  const message = form.formState.errors[name]?.message
  return (
    <div className="grid gap-2">
      <Label htmlFor={name}>{label}</Label>
      <Input id={name} aria-invalid={!!message} {...props} {...form.register(name)} />
      {message ? <FieldError text={message} /> : null}
    </div>
  )
}
function FieldError({ text }: { text?: string }) {
  return (
    <p className="text-sm text-destructive" role="alert">
      {text}
    </p>
  )
}
function ReadOnly({
  label,
  value,
  icon,
}: {
  label: string
  value: string | null
  icon?: React.ReactNode
}) {
  return (
    <div>
      <p className="mb-1 text-xs font-medium text-muted-foreground">{label}</p>
      <p className="flex items-center gap-2 text-sm font-medium">
        {icon ? <span className="[&_svg]:size-4">{icon}</span> : null}
        {value || 'Chưa có thông tin'}
      </p>
    </div>
  )
}
const moduleLabels: Record<string, string> = {
  users: 'Tài khoản',
  roles: 'Vai trò',
  permissions: 'Phân quyền',
  employees: 'Nhân viên',
  members: 'Độc giả',
  books: 'Kho sách',
  borrowings: 'Mượn trả',
  reservations: 'Đặt trước',
  violations: 'Vi phạm',
  'audit-logs': 'Nhật ký hoạt động',
  settings: 'Cài đặt hệ thống',
  todos: 'Công việc',
}
const actionLabels: Record<string, string> = {
  read: 'Xem',
  create: 'Tạo mới',
  update: 'Cập nhật',
  delete: 'Xóa',
  deactivate: 'Ngưng kích hoạt',
  assign: 'Gán quyền',
  return: 'Trả sách',
  cancel: 'Hủy',
  fulfill: 'Hoàn tất',
  resolve: 'Xử lý',
  export: 'Xuất dữ liệu',
  'manage-cards': 'Quản lý thẻ',
  'manage-restrictions': 'Quản lý hạn chế',
  'manage-finances': 'Quản lý tài chính',
}
function friendlyRole(value: string) {
  return value
}
function PermissionGroups({ values }: { values: string[] }) {
  const groups = Object.entries(
    values.reduce<Record<string, string[]>>((result, permission) => {
      const [module, ...action] = permission.split('.')
      ;(result[module] ??= []).push(action.join('.'))
      return result
    }, {}),
  )
  if (!groups.length)
    return <p className="text-sm text-muted-foreground">Không có quyền hiệu lực.</p>
  return (
    <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
      {groups.map(([module, actions]) => (
        <div key={module} className="rounded-lg border p-4">
          <div className="mb-3 flex items-center justify-between">
            <strong className="text-sm">{moduleLabels[module] ?? module}</strong>
            <span className="text-xs text-muted-foreground">{actions.length}</span>
          </div>
          <ul className="grid gap-2">
            {actions.map((action) => (
              <li key={action} className="flex items-center gap-2 text-sm text-muted-foreground">
                <span className="grid size-5 place-items-center rounded-full bg-green-500/10 text-green-600">
                  <Check className="size-3" />
                </span>
                {actionLabels[action] ?? action}
              </li>
            ))}
          </ul>
        </div>
      ))}
    </div>
  )
}
