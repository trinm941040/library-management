import { useMemo, useState } from 'react'
import {
  LockKeyhole,
  LockOpen,
  Pencil,
  Plus,
  Search,
  ShieldCheck,
  Trash2,
  UserCheck,
  Users,
  UserX,
  type LucideIcon,
} from 'lucide-react'
import { Badge } from '@/common/components/ui/badge'
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
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/common/components/ui/table'
import { UserFormDialog, type UserFormData } from './components/UserFormDialog'
import { loadUsers, saveUsers, type SystemUser, type UserRole } from './user-store'

const roleLabels: Record<UserRole, string> = {
  Administrator: 'Quản trị viên',
  Librarian: 'Thủ thư',
  Member: 'Độc giả',
}

const USERS_PER_PAGE = 5

export function UserPage() {
  const [users, setUsers] = useState<SystemUser[]>(loadUsers)
  const [search, setSearch] = useState('')
  const [currentPage, setCurrentPage] = useState(1)
  const [formOpen, setFormOpen] = useState(false)
  const [editingUser, setEditingUser] = useState<SystemUser | null>(null)
  const [deletingUser, setDeletingUser] = useState<SystemUser | null>(null)

  const filteredUsers = useMemo(() => {
    const keyword = search.trim().toLowerCase()
    if (!keyword) return users

    return users.filter((user) =>
      `${user.name} ${user.email} ${roleLabels[user.role]}`.toLowerCase().includes(keyword),
    )
  }, [search, users])

  const totalPages = Math.max(1, Math.ceil(filteredUsers.length / USERS_PER_PAGE))
  const displayedPage = Math.min(currentPage, totalPages)
  const paginatedUsers = useMemo(() => {
    const firstUserIndex = (displayedPage - 1) * USERS_PER_PAGE
    return filteredUsers.slice(firstUserIndex, firstUserIndex + USERS_PER_PAGE)
  }, [displayedPage, filteredUsers])

  const commitUsers = (nextUsers: SystemUser[]) => {
    setUsers(nextUsers)
    saveUsers(nextUsers)
  }

  const openCreateForm = () => {
    setEditingUser(null)
    setFormOpen(true)
  }

  const openEditForm = (user: SystemUser) => {
    setEditingUser(user)
    setFormOpen(true)
  }

  const handleSave = (data: UserFormData) => {
    const normalizedEmail = data.email.trim().toLowerCase()
    const emailExists = users.some(
      (user) => user.email.toLowerCase() === normalizedEmail && user.id !== editingUser?.id,
    )

    if (emailExists) return 'Email này đã được sử dụng.'

    if (editingUser) {
      commitUsers(
        users.map((user) =>
          user.id === editingUser.id
            ? { ...user, ...data, name: data.name.trim(), email: normalizedEmail }
            : user,
        ),
      )
    } else {
      commitUsers([
        {
          id: crypto.randomUUID(),
          name: data.name.trim(),
          email: normalizedEmail,
          role: data.role,
          status: 'active',
          createdAt: new Date().toISOString().slice(0, 10),
        },
        ...users,
      ])
    }

    return null
  }

  const toggleUserLock = (targetUser: SystemUser) => {
    commitUsers(
      users.map((user) =>
        user.id === targetUser.id
          ? { ...user, status: user.status === 'active' ? 'locked' : 'active' }
          : user,
      ),
    )
  }

  const deleteUser = () => {
    if (!deletingUser) return
    commitUsers(users.filter((user) => user.id !== deletingUser.id))
    setDeletingUser(null)
  }

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
              Quản lý tài khoản người dùng của hệ thống.
            </p>
          </div>
          <Button onClick={openCreateForm}>
            <Plus /> Thêm user
          </Button>
        </div>

        <div className="mb-6 grid gap-4 sm:grid-cols-3">
          <SummaryCard title="Tổng user" value={users.length} icon={Users} />
          <SummaryCard
            title="Đang hoạt động"
            value={users.filter((user) => user.status === 'active').length}
            icon={UserCheck}
          />
          <SummaryCard
            title="Đã bị khóa"
            value={users.filter((user) => user.status === 'locked').length}
            icon={UserX}
          />
        </div>

        <Card>
          <CardHeader className="gap-4 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <CardTitle>Danh sách user</CardTitle>
              <p className="mt-1 text-sm text-muted-foreground">
                Hiển thị {filteredUsers.length} trong {users.length} user.
              </p>
            </div>
            <div className="relative w-full sm:w-80">
              <Search className="absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                className="pl-9"
                placeholder="Tìm theo tên, email hoặc vai trò..."
                value={search}
                onChange={(event) => {
                  setSearch(event.target.value)
                  setCurrentPage(1)
                }}
              />
            </div>
          </CardHeader>
          <CardContent>
            <div className="overflow-x-auto rounded-md border">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>User</TableHead>
                    <TableHead>Vai trò</TableHead>
                    <TableHead>Trạng thái</TableHead>
                    <TableHead>Ngày tạo</TableHead>
                    <TableHead className="text-right">Thao tác</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {paginatedUsers.map((user) => (
                    <TableRow key={user.id}>
                      <TableCell>
                        <div className="flex items-center gap-3">
                          <span className="grid size-9 place-items-center rounded-full bg-primary/10 text-xs font-bold text-primary">
                            {getInitials(user.name)}
                          </span>
                          <span className="grid gap-0.5">
                            <strong>{user.name}</strong>
                            <small className="text-muted-foreground">{user.email}</small>
                          </span>
                        </div>
                      </TableCell>
                      <TableCell>
                        <span className="inline-flex items-center gap-2">
                          {user.role === 'Administrator' ? (
                            <ShieldCheck className="size-4" />
                          ) : null}
                          {roleLabels[user.role]}
                        </span>
                      </TableCell>
                      <TableCell>
                        <Badge variant={user.status === 'active' ? 'secondary' : 'destructive'}>
                          {user.status === 'active' ? 'Đang hoạt động' : 'Đã khóa'}
                        </Badge>
                      </TableCell>
                      <TableCell>{formatDate(user.createdAt)}</TableCell>
                      <TableCell>
                        <div className="flex justify-end gap-1">
                          <Button
                            variant="ghost"
                            size="icon-sm"
                            aria-label={`Chỉnh sửa ${user.name}`}
                            title="Chỉnh sửa"
                            onClick={() => openEditForm(user)}
                          >
                            <Pencil />
                          </Button>
                          <Button
                            variant="ghost"
                            size="icon-sm"
                            aria-label={
                              user.status === 'active'
                                ? `Khóa ${user.name}`
                                : `Mở khóa ${user.name}`
                            }
                            title={user.status === 'active' ? 'Khóa user' : 'Mở khóa user'}
                            onClick={() => toggleUserLock(user)}
                          >
                            {user.status === 'active' ? <LockKeyhole /> : <LockOpen />}
                          </Button>
                          <Button
                            variant="ghost"
                            size="icon-sm"
                            className="text-destructive hover:text-destructive"
                            aria-label={`Xóa ${user.name}`}
                            title="Xóa"
                            onClick={() => setDeletingUser(user)}
                          >
                            <Trash2 />
                          </Button>
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
                  {filteredUsers.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={5} className="h-28 text-center text-muted-foreground">
                        Không tìm thấy user phù hợp.
                      </TableCell>
                    </TableRow>
                  ) : null}
                </TableBody>
              </Table>
            </div>
            <div className="mt-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <p className="text-sm text-muted-foreground">
                {filteredUsers.length === 0
                  ? 'Không có user để hiển thị.'
                  : `Hiển thị ${(displayedPage - 1) * USERS_PER_PAGE + 1}-${Math.min(
                      displayedPage * USERS_PER_PAGE,
                      filteredUsers.length,
                    )} trong ${filteredUsers.length} user.`}
              </p>
              <Pagination
                currentPage={displayedPage}
                totalPages={totalPages}
                onPageChange={setCurrentPage}
              />
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

      <Dialog open={!!deletingUser} onOpenChange={(open) => !open && setDeletingUser(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Xóa user?</DialogTitle>
            <DialogDescription>
              Tài khoản <strong>{deletingUser?.name}</strong> sẽ bị xóa khỏi danh sách. Thao tác này
              không thể hoàn tác.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDeletingUser(null)}>
              Hủy
            </Button>
            <Button variant="destructive" onClick={deleteUser}>
              Xóa user
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
  value: number
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

function getInitials(name: string) {
  return name
    .split(' ')
    .map((part) => part[0])
    .join('')
    .slice(0, 2)
    .toUpperCase()
}

function formatDate(date: string) {
  return new Intl.DateTimeFormat('vi-VN').format(new Date(date))
}
