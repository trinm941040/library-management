import { useCallback, useEffect, useState, type ReactNode } from 'react'
import {
  Ban,
  CircleAlert,
  Pencil,
  Plus,
  RefreshCw,
  Search,
  ShieldCheck,
  UserCheck,
  Users,
  UserX,
  type LucideIcon,
} from 'lucide-react'
import { Badge } from '@/common/components/ui/badge'
import { useAuth } from '@/auth/AuthProvider'
import { Button } from '@/common/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/common/components/ui/card'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/common/components/ui/dialog'
import { Input } from '@/common/components/ui/input'
import { Pagination } from '@/common/components/ui/pagination'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/common/components/ui/select'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/common/components/ui/table'
import { UserFormDialog, type UserFormData } from './components/UserFormDialog'
import {
  createUser,
  deactivateUser,
  getUsers,
  updateUser,
  type SystemUser,
  type UserPageResponse,
} from './user-api'

const USERS_PER_PAGE = 20

const roleLabels: Record<string, string> = {
  Administrator: 'Quản trị viên',
  User: 'Người dùng',
}

type StatusFilter = 'all' | 'active' | 'inactive'
type RoleFilter = 'all' | 'Administrator' | 'User'

type UserSummary = {
  total: number
  active: number
  inactive: number
}

export function UserPage() {
  const { user: currentUser } = useAuth()
  const [page, setPage] = useState<UserPageResponse | null>(null)
  const [summary, setSummary] = useState<UserSummary | null>(null)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState<StatusFilter>('all')
  const [role, setRole] = useState<RoleFilter>('all')
  const [currentPage, setCurrentPage] = useState(1)
  const [reloadKey, setReloadKey] = useState(0)
  const [isLoading, setIsLoading] = useState(true)
  const [pageError, setPageError] = useState('')
  const [notice, setNotice] = useState('')
  const [formOpen, setFormOpen] = useState(false)
  const [editingUser, setEditingUser] = useState<SystemUser | null>(null)
  const [deactivatingUser, setDeactivatingUser] = useState<SystemUser | null>(null)
  const [deactivateError, setDeactivateError] = useState('')
  const [isDeactivating, setIsDeactivating] = useState(false)

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      setSearch(searchInput.trim())
      setCurrentPage(1)
    }, 350)

    return () => window.clearTimeout(timeout)
  }, [searchInput])

  useEffect(() => {
    const controller = new AbortController()
    setIsLoading(true)
    setPageError('')

    getUsers(
      {
        search: search || undefined,
        isActive: status === 'all' ? undefined : status === 'active',
        role: role === 'all' ? undefined : role,
        pageNumber: currentPage,
        pageSize: USERS_PER_PAGE,
      },
      controller.signal,
    )
      .then((response) => {
        setPage(response)
        if (response.totalPages > 0 && currentPage > response.totalPages) {
          setCurrentPage(response.totalPages)
        }
      })
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === 'AbortError') return
        setPageError(error instanceof Error ? error.message : 'Không thể tải danh sách user.')
      })
      .finally(() => {
        if (!controller.signal.aborted) setIsLoading(false)
      })

    return () => controller.abort()
  }, [currentPage, reloadKey, role, search, status])

  useEffect(() => {
    const controller = new AbortController()

    Promise.all([
      getUsers({ isActive: true, pageNumber: 1, pageSize: 1 }, controller.signal),
      getUsers({ isActive: false, pageNumber: 1, pageSize: 1 }, controller.signal),
    ])
      .then(([activeUsers, inactiveUsers]) => {
        setSummary({
          active: activeUsers.totalCount,
          inactive: inactiveUsers.totalCount,
          total: activeUsers.totalCount + inactiveUsers.totalCount,
        })
      })
      .catch((error: unknown) => {
        if (!(error instanceof DOMException && error.name === 'AbortError')) setSummary(null)
      })

    return () => controller.abort()
  }, [reloadKey])

  const refresh = useCallback((message?: string) => {
    if (message) setNotice(message)
    setReloadKey((value) => value + 1)
  }, [])

  const openCreateForm = () => {
    setEditingUser(null)
    setFormOpen(true)
  }

  const openEditForm = (user: SystemUser) => {
    if (user.isProtected && user.id !== currentUser?.id) return
    setEditingUser(user)
    setFormOpen(true)
  }

  const handleSave = async (data: UserFormData) => {
    if (editingUser?.isProtected && editingUser.id !== currentUser?.id) {
      return 'Không thể thay đổi thông tin của tài khoản Administrator khác.'
    }

    try {
      const input = {
        displayName: data.displayName.trim(),
        email: data.email.trim().toLowerCase(),
      }

      if (editingUser) {
        await updateUser(editingUser.id, input)
        refresh('Đã cập nhật tài khoản thành công.')
      } else {
        await createUser({ ...input, password: data.password })
        setCurrentPage(1)
        refresh('Đã tạo tài khoản thành công.')
      }

      return null
    } catch (error) {
      return error instanceof Error ? error.message : 'Không thể lưu tài khoản.'
    }
  }

  const confirmDeactivate = async () => {
    if (!deactivatingUser) return
    if (deactivatingUser.isProtected) {
      setDeactivateError('Không thể vô hiệu hóa tài khoản Administrator.')
      return
    }
    setDeactivateError('')
    setIsDeactivating(true)

    try {
      await deactivateUser(deactivatingUser.id)
      setDeactivatingUser(null)
      refresh('Đã vô hiệu hóa tài khoản.')
    } catch (error) {
      setDeactivateError(
        error instanceof Error ? error.message : 'Không thể vô hiệu hóa tài khoản.',
      )
    } finally {
      setIsDeactivating(false)
    }
  }

  const displayedFrom = page && page.totalCount > 0 ? (page.pageNumber - 1) * page.pageSize + 1 : 0
  const displayedTo = page ? Math.min(page.pageNumber * page.pageSize, page.totalCount) : 0

  return (
    <>
      <div className="mx-auto w-full max-w-7xl px-5 py-10 md:px-12">
        <div className="mb-8 flex flex-col justify-between gap-4 sm:flex-row sm:items-end">
          <div>
            <p className="mb-2 text-xs font-bold tracking-widest text-primary uppercase">
              quản lý người dùng
            </p>
            <h1 className="text-3xl font-bold tracking-tight">Quản lý tài khoản</h1>
            <p className="mt-2 text-sm text-muted-foreground">
              Theo dõi, tạo mới, cập nhật và vô hiệu hóa tài khoản hệ thống.
            </p>
          </div>
          <div className="flex gap-2">
            <Button
              variant="outline"
              aria-label="Tải lại danh sách user"
              disabled={isLoading}
              onClick={() => refresh()}
            >
              <RefreshCw className={isLoading ? 'animate-spin' : ''} />
              Làm mới
            </Button>
            <Button onClick={openCreateForm}>
              <Plus /> Thêm user
            </Button>
          </div>
        </div>

        {notice ? (
          <div
            className="mb-6 flex items-center justify-between gap-3 rounded-lg border border-primary/20 bg-primary/5 px-4 py-3 text-sm"
            role="status"
          >
            <span className="flex items-center gap-2">
              <UserCheck className="size-4 text-primary" />
              {notice}
            </span>
            <Button variant="ghost" size="xs" onClick={() => setNotice('')}>
              Đóng
            </Button>
          </div>
        ) : null}

        <div className="mb-6 grid gap-4 sm:grid-cols-3">
          <SummaryCard title="Tổng user" value={summary?.total ?? '—'} icon={Users} />
          <SummaryCard title="Đang hoạt động" value={summary?.active ?? '—'} icon={UserCheck} />
          <SummaryCard title="Đã vô hiệu hóa" value={summary?.inactive ?? '—'} icon={UserX} />
        </div>

        <Card>
          <CardHeader className="gap-4">
            <div className="flex flex-col justify-between gap-4 lg:flex-row lg:items-center">
              <div>
                <CardTitle>Danh sách user</CardTitle>
                <p className="mt-1 text-sm text-muted-foreground">
                  {page ? `${page.totalCount} tài khoản phù hợp với bộ lọc.` : 'Đang tải dữ liệu.'}
                </p>
              </div>
              <div className="relative w-full lg:w-80">
                <Search className="absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  className="pl-9"
                  placeholder="Tìm theo tên hoặc email..."
                  value={searchInput}
                  onChange={(event) => setSearchInput(event.target.value)}
                />
              </div>
            </div>
            <div className="flex flex-col gap-3 sm:flex-row">
              <Select
                value={status}
                onValueChange={(value) => {
                  setStatus(value as StatusFilter)
                  setCurrentPage(1)
                }}
              >
                <SelectTrigger className="w-full sm:w-48" aria-label="Lọc theo trạng thái">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">Tất cả trạng thái</SelectItem>
                  <SelectItem value="active">Đang hoạt động</SelectItem>
                  <SelectItem value="inactive">Đã vô hiệu hóa</SelectItem>
                </SelectContent>
              </Select>
              <Select
                value={role}
                onValueChange={(value) => {
                  setRole(value as RoleFilter)
                  setCurrentPage(1)
                }}
              >
                <SelectTrigger className="w-full sm:w-48" aria-label="Lọc theo vai trò">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">Tất cả vai trò</SelectItem>
                  <SelectItem value="Administrator">Quản trị viên</SelectItem>
                  <SelectItem value="User">Người dùng</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </CardHeader>
          <CardContent>
            {pageError ? (
              <div
                className="mb-4 flex flex-col items-center gap-3 rounded-lg border border-destructive/30 bg-destructive/5 p-6 text-center"
                role="alert"
              >
                <CircleAlert className="size-6 text-destructive" />
                <div>
                  <p className="font-medium">Không thể tải danh sách user</p>
                  <p className="mt-1 text-sm text-muted-foreground">{pageError}</p>
                </div>
                <Button variant="outline" size="sm" onClick={() => refresh()}>
                  Thử lại
                </Button>
              </div>
            ) : null}

            <div className="overflow-x-auto rounded-md border">
              <Table aria-busy={isLoading}>
                <TableHeader>
                  <TableRow>
                    <TableHead>User</TableHead>
                    <TableHead>Vai trò</TableHead>
                    <TableHead>Trạng thái</TableHead>
                    <TableHead>Đăng nhập cuối</TableHead>
                    <TableHead>Ngày tạo</TableHead>
                    <TableHead className="text-right">Thao tác</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {isLoading && !page ? <LoadingRows /> : null}
                  {!isLoading && !pageError && page?.items.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={6} className="h-32 text-center text-muted-foreground">
                        Không tìm thấy user phù hợp.
                      </TableCell>
                    </TableRow>
                  ) : null}
                  {page?.items.map((user) => {
                    const isCurrentUser = user.id === currentUser?.id
                    const canEdit = !user.isProtected || isCurrentUser
                    const canDeactivate = !user.isProtected && user.isActive
                    const editTitle = canEdit
                      ? isCurrentUser && user.isProtected
                        ? 'Chỉnh sửa thông tin cá nhân'
                        : 'Chỉnh sửa'
                      : 'Không thể thay đổi Administrator khác'
                    const deactivateTitle = user.isProtected
                      ? 'Không thể vô hiệu hóa tài khoản Administrator'
                      : user.isActive
                        ? 'Vô hiệu hóa user'
                        : 'User đã bị vô hiệu hóa'

                    return (
                      <TableRow key={user.id} className={isLoading ? 'opacity-60' : undefined}>
                        <TableCell>
                          <div className="flex min-w-60 items-center gap-3">
                            <span className="grid size-9 shrink-0 place-items-center rounded-full bg-primary/10 text-xs font-bold text-primary">
                              {getInitials(user.displayName)}
                            </span>
                            <span className="grid gap-0.5">
                              <span className="flex items-center gap-2">
                                <strong>{user.displayName}</strong>
                                {isCurrentUser ? <Badge variant="secondary">Bạn</Badge> : null}
                              </span>
                              <small className="text-muted-foreground">{user.email}</small>
                              <small className="text-muted-foreground">
                                {user.emailConfirmed ? 'Email đã xác nhận' : 'Email chưa xác nhận'}
                              </small>
                            </span>
                          </div>
                        </TableCell>
                        <TableCell>
                          <div className="flex max-w-52 flex-wrap gap-1.5">
                            {user.roles.length > 0 ? (
                              user.roles.map((userRole) => (
                                <Badge key={userRole} variant="outline">
                                  {userRole === 'Administrator' ? <ShieldCheck /> : null}
                                  {roleLabels[userRole] ?? userRole}
                                </Badge>
                              ))
                            ) : (
                              <span className="text-sm text-muted-foreground">Chưa có vai trò</span>
                            )}
                          </div>
                        </TableCell>
                        <TableCell>
                          <Badge variant={user.isActive ? 'secondary' : 'destructive'}>
                            {user.isActive ? 'Đang hoạt động' : 'Đã vô hiệu hóa'}
                          </Badge>
                        </TableCell>
                        <TableCell>{formatDateTime(user.lastLoginAtUtc)}</TableCell>
                        <TableCell>{formatDateTime(user.createdAtUtc)}</TableCell>
                        <TableCell>
                          <div className="flex justify-end gap-1">
                            <Button
                              variant="ghost"
                              size="icon-sm"
                              aria-label={`Chỉnh sửa ${user.displayName}`}
                              title={editTitle}
                              disabled={!canEdit}
                              onClick={() => openEditForm(user)}
                            >
                              <Pencil />
                            </Button>
                            <Button
                              variant="ghost"
                              size="icon-sm"
                              className="text-destructive hover:text-destructive"
                              aria-label={`Vô hiệu hóa ${user.displayName}`}
                              title={deactivateTitle}
                              disabled={!canDeactivate}
                              onClick={() => {
                                setDeactivateError('')
                                setDeactivatingUser(user)
                              }}
                            >
                              <Ban />
                            </Button>
                          </div>
                        </TableCell>
                      </TableRow>
                    )
                  })}
                </TableBody>
              </Table>
            </div>

            <div className="mt-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <p className="text-sm text-muted-foreground">
                {page?.totalCount
                  ? `Hiển thị ${displayedFrom}-${displayedTo} trong ${page.totalCount} user.`
                  : 'Không có user để hiển thị.'}
              </p>
              {page && page.totalPages > 0 ? (
                <Pagination
                  currentPage={page.pageNumber}
                  totalPages={page.totalPages}
                  onPageChange={setCurrentPage}
                />
              ) : null}
            </div>
          </CardContent>
        </Card>
      </div>

      <UserFormDialog
        open={formOpen}
        user={editingUser}
        onOpenChange={setFormOpen}
        onSave={handleSave}
      />

      <Dialog
        open={!!deactivatingUser}
        onOpenChange={(open) => !open && !isDeactivating && setDeactivatingUser(null)}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Vô hiệu hóa tài khoản?</DialogTitle>
            <DialogDescription>
              Tài khoản <strong>{deactivatingUser?.displayName}</strong> sẽ không thể đăng nhập và
              tất cả phiên hiện tại sẽ bị thu hồi. API hiện chưa hỗ trợ kích hoạt lại tài khoản.
            </DialogDescription>
          </DialogHeader>
          {deactivateError ? (
            <p
              className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive"
              role="alert"
            >
              {deactivateError}
            </p>
          ) : null}
          <DialogFooter>
            <Button
              variant="outline"
              disabled={isDeactivating}
              onClick={() => setDeactivatingUser(null)}
            >
              Hủy
            </Button>
            <Button variant="destructive" disabled={isDeactivating} onClick={confirmDeactivate}>
              {isDeactivating ? 'Đang xử lý...' : 'Vô hiệu hóa'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  )
}

function SummaryCard({
  title,
  value,
  icon: Icon,
}: {
  title: string
  value: ReactNode
  icon: LucideIcon
}) {
  return (
    <Card>
      <CardContent className="flex items-center gap-4 pt-6">
        <span className="grid size-11 place-items-center rounded-lg bg-primary/10 text-primary">
          <Icon className="size-5" />
        </span>
        <span>
          <p className="text-sm text-muted-foreground">{title}</p>
          <strong className="text-2xl">{value}</strong>
        </span>
      </CardContent>
    </Card>
  )
}

function LoadingRows() {
  return Array.from({ length: 5 }, (_, index) => (
    <TableRow key={index}>
      <TableCell colSpan={6}>
        <div className="h-9 animate-pulse rounded-md bg-muted" />
      </TableCell>
    </TableRow>
  ))
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

function formatDateTime(value: string | null) {
  if (!value) return 'Chưa đăng nhập'

  return new Intl.DateTimeFormat('vi-VN', {
    dateStyle: 'short',
    timeStyle: 'short',
  }).format(new Date(value))
}
