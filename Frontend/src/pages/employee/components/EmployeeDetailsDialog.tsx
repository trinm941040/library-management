import { useEffect, useState, type FormEvent } from 'react'
import { KeyRound, Pencil, ShieldCheck, UserRoundCheck, UserRoundX } from 'lucide-react'
import { Link } from 'react-router-dom'
import { createUser, getUserById, getUsers, type SystemUser } from '@/pages/users/user-api'
import { Badge } from '@/common/components/ui/badge'
import { Button } from '@/common/components/ui/button'
import { formatDate, formatDateTime } from '@/common/formatters'
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
import { employmentStatusLabels, getEmployeeById, type Employee } from '../employee-api'
import { EmploymentStatusBadge } from './EmploymentStatusBadge'

type EmployeeDetailsDialogProps = {
  open: boolean
  employee: Employee | null
  onOpenChange: (open: boolean) => void
  onEdit: (employee: Employee) => void
}

export function EmployeeDetailsDialog({
  open,
  employee,
  onOpenChange,
  onEdit,
}: EmployeeDetailsDialogProps) {
  const [details, setDetails] = useState<Employee | null>(employee)
  const [account, setAccount] = useState<SystemUser | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [detailsError, setDetailsError] = useState('')
  const [accountLookupComplete, setAccountLookupComplete] = useState(false)
  const [accountError, setAccountError] = useState('')
  const [showAccountForm, setShowAccountForm] = useState(false)
  const [password, setPassword] = useState('')
  const [isCreatingAccount, setIsCreatingAccount] = useState(false)

  useEffect(() => {
    if (!open || !employee) return

    const controller = new AbortController()
    setDetails(employee)
    setAccount(null)
    setDetailsError('')
    setAccountError('')
    setAccountLookupComplete(false)
    setShowAccountForm(false)
    setPassword('')
    setIsLoading(true)

    getEmployeeById(employee.id, controller.signal)
      .then(setDetails)
      .catch((error: unknown) => {
        if (!(error instanceof DOMException && error.name === 'AbortError')) {
          setDetailsError(error instanceof Error ? error.message : 'Không thể tải hồ sơ nhân viên.')
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) setIsLoading(false)
      })

    const accountRequest = employee.userId
      ? getUserById(employee.userId, controller.signal).then((user) => ({ items: [user] }))
      : getUsers({ search: employee.email, pageNumber: 1, pageSize: 20 }, controller.signal)

    accountRequest
      .then((response) => {
        const normalizedEmail = employee.email.toLowerCase()
        setAccount(
          response.items.find((user) => user.email.toLowerCase() === normalizedEmail) ?? null,
        )
        setAccountLookupComplete(true)
      })
      .catch((error: unknown) => {
        if (!(error instanceof DOMException && error.name === 'AbortError')) {
          setAccountError(
            error instanceof Error ? error.message : 'Không thể kiểm tra tài khoản truy cập.',
          )
          setAccountLookupComplete(true)
        }
      })

    return () => controller.abort()
  }, [employee, open])

  const handleCreateAccount = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!details) return
    setAccountError('')
    setIsCreatingAccount(true)

    try {
      const createdAccount = await createUser({
        displayName: details.fullName,
        email: details.email,
        password,
        employeeId: details.id,
      })
      setAccount(createdAccount)
      setPassword('')
      setShowAccountForm(false)
    } catch (error) {
      setAccountError(error instanceof Error ? error.message : 'Không thể cấp tài khoản truy cập.')
    } finally {
      setIsCreatingAccount(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={(nextOpen) => !isCreatingAccount && onOpenChange(nextOpen)}>
      <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-3xl">
        <DialogHeader>
          <DialogTitle>Hồ sơ nhân viên</DialogTitle>
          <DialogDescription>
            Thông tin nhân sự, việc làm và quyền truy cập hệ thống.
          </DialogDescription>
        </DialogHeader>

        {detailsError ? (
          <p
            className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive"
            role="alert"
          >
            {detailsError}
          </p>
        ) : null}

        {details ? (
          <div className={isLoading ? 'grid gap-6 opacity-60' : 'grid gap-6'}>
            <div className="flex flex-col gap-4 rounded-xl border bg-muted/30 p-5 sm:flex-row sm:items-center sm:justify-between">
              <div className="flex items-center gap-4">
                <span className="grid size-12 shrink-0 place-items-center rounded-full bg-primary/10 text-sm font-bold text-primary">
                  {getInitials(details.fullName)}
                </span>
                <div>
                  <div className="flex flex-wrap items-center gap-2">
                    <h2 className="text-xl font-semibold">{details.fullName}</h2>
                    <EmploymentStatusBadge status={details.status} />
                  </div>
                  <p className="mt-1 text-sm text-muted-foreground">
                    {details.employeeCode} · {details.position}
                  </p>
                </div>
              </div>
              <Button variant="outline" onClick={() => onEdit(details)}>
                <Pencil /> Cập nhật hồ sơ
              </Button>
            </div>

            <section aria-labelledby="employee-personal-title">
              <h3 id="employee-personal-title" className="mb-3 font-semibold">
                Thông tin cá nhân
              </h3>
              <dl className="grid gap-x-8 gap-y-4 rounded-xl border p-5 sm:grid-cols-2">
                <Detail label="Email" value={details.email} />
                <Detail label="Số điện thoại" value={details.phoneNumber || 'Chưa cập nhật'} />
                <Detail label="Ngày sinh" value={formatDate(details.dateOfBirth)} />
                <Detail label="Địa chỉ" value={details.address || 'Chưa cập nhật'} />
              </dl>
            </section>

            <section aria-labelledby="employee-employment-title">
              <h3 id="employee-employment-title" className="mb-3 font-semibold">
                Thông tin việc làm
              </h3>
              <dl className="grid gap-x-8 gap-y-4 rounded-xl border p-5 sm:grid-cols-2">
                <Detail label="Chức vụ" value={details.position} />
                <Detail label="Chi nhánh" value={details.branchName || details.department} />
                <Detail label="Đơn vị" value={details.department} />
                <Detail label="Ngày bắt đầu công tác" value={formatDate(details.hireDate)} />
                <Detail
                  label="Trạng thái việc làm"
                  value={employmentStatusLabels[details.status]}
                />
                <Detail label="Tạo hồ sơ" value={formatDateTime(details.createdAtUtc)} />
                <Detail label="Cập nhật gần nhất" value={formatDateTime(details.updatedAtUtc)} />
              </dl>
            </section>

            <section aria-labelledby="employee-account-title">
              <div className="mb-3 flex items-center justify-between gap-3">
                <h3 id="employee-account-title" className="font-semibold">
                  Tài khoản truy cập
                </h3>
                <Button asChild variant="ghost" size="sm">
                  <Link to="/users">Quản lý tài khoản</Link>
                </Button>
              </div>
              <div className="rounded-xl border p-5">
                {!accountLookupComplete ? (
                  <p className="text-sm text-muted-foreground">Đang kiểm tra tài khoản...</p>
                ) : null}
                {account ? (
                  <div className="grid gap-4">
                    <div className="flex flex-wrap items-center justify-between gap-3">
                      <span className="flex items-center gap-2 font-medium">
                        {account.isActive ? (
                          <UserRoundCheck className="size-4 text-emerald-600" />
                        ) : (
                          <UserRoundX className="size-4 text-destructive" />
                        )}
                        {account.email}
                      </span>
                      <Badge variant={account.isActive ? 'secondary' : 'destructive'}>
                        {account.isActive ? 'Được phép đăng nhập' : 'Đã khóa'}
                      </Badge>
                    </div>
                    <div className="flex flex-wrap gap-2">
                      {account.roles.length ? (
                        account.roles.map((role) => (
                          <Badge key={role} variant="outline">
                            <ShieldCheck /> {role}
                          </Badge>
                        ))
                      ) : (
                        <span className="text-sm text-muted-foreground">
                          Chưa được gán vai trò.
                        </span>
                      )}
                    </div>
                    <p className="text-xs text-muted-foreground">
                      Đăng nhập gần nhất:{' '}
                      {account.lastLoginAtUtc
                        ? formatDateTime(account.lastLoginAtUtc)
                        : 'Chưa đăng nhập'}
                    </p>
                    <Button asChild variant="outline" size="sm" className="w-fit">
                      <Link to="/roles">
                        <KeyRound /> Quản lý vai trò và quyền
                      </Link>
                    </Button>
                  </div>
                ) : null}
                {accountLookupComplete && !account && !accountError ? (
                  showAccountForm ? (
                    <form className="grid gap-4" onSubmit={handleCreateAccount}>
                      <div>
                        <p className="font-medium">Cấp tài khoản cho {details.fullName}</p>
                        <p className="mt-1 text-sm text-muted-foreground">
                          Tài khoản dùng email hồ sơ và được quản lý độc lập với trạng thái việc
                          làm.
                        </p>
                      </div>
                      <div className="grid gap-2">
                        <Label htmlFor="employee-account-password">Mật khẩu tạm thời</Label>
                        <Input
                          id="employee-account-password"
                          type="password"
                          value={password}
                          minLength={8}
                          maxLength={256}
                          autoComplete="new-password"
                          disabled={isCreatingAccount}
                          onChange={(event) => setPassword(event.target.value)}
                          required
                        />
                      </div>
                      <div className="flex gap-2">
                        <Button type="submit" disabled={isCreatingAccount}>
                          {isCreatingAccount ? 'Đang tạo...' : 'Tạo tài khoản'}
                        </Button>
                        <Button
                          type="button"
                          variant="outline"
                          disabled={isCreatingAccount}
                          onClick={() => setShowAccountForm(false)}
                        >
                          Hủy
                        </Button>
                      </div>
                    </form>
                  ) : (
                    <div className="flex flex-col items-start gap-3">
                      <div>
                        <p className="font-medium">Chưa có tài khoản truy cập</p>
                        <p className="mt-1 text-sm text-muted-foreground">
                          Hồ sơ nhân viên vẫn hoạt động bình thường khi chưa được cấp tài khoản.
                        </p>
                      </div>
                      <Button onClick={() => setShowAccountForm(true)}>
                        <KeyRound /> Cấp tài khoản
                      </Button>
                    </div>
                  )
                ) : null}
                {accountError ? (
                  <p
                    className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive"
                    role="alert"
                  >
                    {accountError}
                  </p>
                ) : null}
              </div>
            </section>
          </div>
        ) : null}

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Đóng
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

function Detail({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="text-xs font-medium tracking-wide text-muted-foreground uppercase">{label}</dt>
      <dd className="mt-1 text-sm">{value}</dd>
    </div>
  )
}

function getInitials(name: string) {
  return name
    .trim()
    .split(/\s+/)
    .map((part) => part[0])
    .join('')
    .slice(0, 2)
    .toUpperCase()
}
