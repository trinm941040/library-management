import { useEffect, useState, type ReactNode } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  CircleAlert,
  KeyRound,
  Layers3,
  Pencil,
  Plus,
  RefreshCw,
  Search,
  ShieldCheck,
  ShieldPlus,
  Trash2,
  type LucideIcon,
} from 'lucide-react'
import { Badge } from '@/common/components/ui/badge'
import { Button } from '@/common/components/ui/button'
import { PermissionBoundary } from '@/shared/auth/PermissionBoundary'
import { useAuth } from '@/auth/AuthProvider'
import { canAll } from '@/shared/auth/permissions'
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
import {
  PermissionAssignmentDialog,
  PermissionFormDialog,
  RoleFormDialog,
} from './components/RolePermissionDialogs'
import {
  createPermission,
  createRole,
  deletePermission,
  deleteRole,
  getAllPermissions,
  getPermissionModules,
  getPermissions,
  getRoles,
  isSystemPermission,
  replaceRolePermissions,
  updatePermission,
  updateRole,
  type Permission,
  type PermissionInput,
  type Role,
  type RoleInput,
} from './role-permission-api'

export type RolePermissionView = 'roles' | 'permissions'
type DeleteTarget = { type: 'role'; value: Role } | { type: 'permission'; value: Permission }

export function RolePermissionPage({ initialView }: { initialView: RolePermissionView }) {
  const view = initialView
  const navigate = useNavigate()
  const { user } = useAuth()
  const canManageMatrix = canAll(user?.permissions ?? [], ['roles.assign', 'permissions.read'])
  const [roles, setRoles] = useState<Role[]>([])
  const [permissions, setPermissions] = useState<Permission[]>([])
  const [permissionCatalog, setPermissionCatalog] = useState<Permission[]>([])
  const [modules, setModules] = useState<string[]>([])
  const [rolePage, setRolePage] = useState(1)
  const [rolePageSize, setRolePageSize] = useState(10)
  const [roleTotalCount, setRoleTotalCount] = useState(0)
  const [roleTotalPages, setRoleTotalPages] = useState(0)
  const [permissionPage, setPermissionPage] = useState(1)
  const [permissionPageSize, setPermissionPageSize] = useState(10)
  const [permissionTotalCount, setPermissionTotalCount] = useState(0)
  const [permissionTotalPages, setPermissionTotalPages] = useState(0)
  const [roleSearchInput, setRoleSearchInput] = useState('')
  const [roleSearch, setRoleSearch] = useState('')
  const [permissionSearchInput, setPermissionSearchInput] = useState('')
  const [permissionSearch, setPermissionSearch] = useState('')
  const [moduleFilter, setModuleFilter] = useState('all')
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [reloadKey, setReloadKey] = useState(0)
  const [roleFormOpen, setRoleFormOpen] = useState(false)
  const [editingRole, setEditingRole] = useState<Role | null>(null)
  const [permissionFormOpen, setPermissionFormOpen] = useState(false)
  const [editingPermission, setEditingPermission] = useState<Permission | null>(null)
  const [permissionRole, setPermissionRole] = useState<Role | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<DeleteTarget | null>(null)
  const [deleteError, setDeleteError] = useState('')
  const [isDeleting, setIsDeleting] = useState(false)

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      setRolePage(1)
      setRoleSearch(roleSearchInput.trim())
    }, 300)
    return () => window.clearTimeout(timeout)
  }, [roleSearchInput])

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      setPermissionPage(1)
      setPermissionSearch(permissionSearchInput.trim())
    }, 300)
    return () => window.clearTimeout(timeout)
  }, [permissionSearchInput])

  useEffect(() => {
    const controller = new AbortController()
    setIsLoading(true)
    setError('')

    const roleRequest =
      view === 'roles'
        ? getRoles(
            { search: roleSearch || undefined, pageNumber: rolePage, pageSize: rolePageSize },
            controller.signal,
          )
        : Promise.resolve(null)
    const permissionRequest =
      view === 'permissions'
        ? getPermissions(
            {
              search: permissionSearch || undefined,
              module: moduleFilter === 'all' ? undefined : moduleFilter,
              pageNumber: permissionPage,
              pageSize: permissionPageSize,
            },
            controller.signal,
          )
        : Promise.resolve(null)

    Promise.all([
      roleRequest,
      permissionRequest,
      canManageMatrix ? getAllPermissions(controller.signal) : Promise.resolve([]),
      view === 'permissions' ? getPermissionModules(controller.signal) : Promise.resolve([]),
    ])
      .then(([roleResult, permissionResult, allPermissions, moduleItems]) => {
        if (roleResult) {
          setRoles(roleResult.items)
          setRoleTotalCount(roleResult.totalCount)
          setRoleTotalPages(roleResult.totalPages)
          if (roleResult.totalPages > 0 && rolePage > roleResult.totalPages)
            setRolePage(roleResult.totalPages)
        }
        if (permissionResult) {
          setPermissions(permissionResult.items)
          setPermissionTotalCount(permissionResult.totalCount)
          setPermissionTotalPages(permissionResult.totalPages)
          if (permissionResult.totalPages > 0 && permissionPage > permissionResult.totalPages)
            setPermissionPage(permissionResult.totalPages)
        }
        setPermissionCatalog(allPermissions)
        if (view === 'permissions') setModules(moduleItems)
      })
      .catch((loadError: unknown) => {
        if (loadError instanceof DOMException && loadError.name === 'AbortError') return
        setError(
          loadError instanceof Error
            ? loadError.message
            : 'Không thể tải dữ liệu vai trò và quyền hạn.',
        )
      })
      .finally(() => {
        if (!controller.signal.aborted) setIsLoading(false)
      })

    return () => controller.abort()
  }, [
    canManageMatrix,
    moduleFilter,
    permissionPage,
    permissionPageSize,
    permissionSearch,
    reloadKey,
    rolePage,
    rolePageSize,
    roleSearch,
    view,
  ])

  const refresh = (message?: string) => {
    if (message) setNotice(message)
    setReloadKey((value) => value + 1)
  }

  const saveRole = async (input: RoleInput) => {
    try {
      if (editingRole) {
        await updateRole(editingRole.id, input)
        refresh('Đã cập nhật vai trò.')
      } else {
        await createRole(input)
        refresh('Đã tạo vai trò mới.')
      }
      return null
    } catch (saveError) {
      return saveError instanceof Error ? saveError.message : 'Không thể lưu vai trò.'
    }
  }

  const savePermission = async (input: PermissionInput) => {
    try {
      if (editingPermission) {
        await updatePermission(editingPermission.id, input)
        refresh('Đã cập nhật quyền hạn.')
      } else {
        await createPermission(input)
        refresh('Đã tạo quyền hạn mới.')
      }
      return null
    } catch (saveError) {
      return saveError instanceof Error ? saveError.message : 'Không thể lưu quyền hạn.'
    }
  }

  const saveRolePermissions = async (permissionIds: string[]) => {
    if (!permissionRole) return 'Không tìm thấy vai trò cần cập nhật.'
    try {
      await replaceRolePermissions(permissionRole.id, permissionIds)
      setPermissionRole(null)
      refresh(`Đã cập nhật quyền cho vai trò ${permissionRole.name}.`)
      return null
    } catch (saveError) {
      return saveError instanceof Error ? saveError.message : 'Không thể cập nhật quyền.'
    }
  }

  const confirmDelete = async () => {
    if (!deleteTarget) return
    setDeleteError('')
    setIsDeleting(true)
    try {
      if (deleteTarget.type === 'role') {
        await deleteRole(deleteTarget.value.id)
        refresh('Đã xóa vai trò.')
      } else {
        await deletePermission(deleteTarget.value.id)
        refresh('Đã xóa quyền hạn.')
      }
      setDeleteTarget(null)
    } catch (deleteFailure) {
      setDeleteError(
        deleteFailure instanceof Error ? deleteFailure.message : 'Không thể xóa dữ liệu.',
      )
    } finally {
      setIsDeleting(false)
    }
  }

  return (
    <>
      <div className="mx-auto w-full max-w-7xl px-5 py-10 md:px-12">
        <div className="mb-8 flex flex-col justify-between gap-4 xl:flex-row xl:items-end">
          <div>
            <p className="mb-2 text-xs font-bold tracking-widest text-primary uppercase">
              kiểm soát truy cập
            </p>
            <h1 className="text-3xl font-bold tracking-tight">Vai trò và quyền hạn</h1>
            <p className="mt-2 text-sm text-muted-foreground">
              Thiết lập vai trò và các quyền chi tiết được cấp trong hệ thống.
            </p>
          </div>
          <div className="flex gap-2">
            <Button variant="outline" disabled={isLoading} loading={isLoading && (roles.length > 0 || permissions.length > 0)} loadingLabel="Đang tải lại vai trò và quyền" onClick={() => refresh()}>
              <RefreshCw /> Làm mới
            </Button>
            <PermissionBoundary
              requiredPermissions={[view === 'roles' ? 'roles.create' : 'permissions.create']}
            >
              <Button
                onClick={() => {
                  if (view === 'roles') {
                    setEditingRole(null)
                    setRoleFormOpen(true)
                  } else {
                    setEditingPermission(null)
                    setPermissionFormOpen(true)
                  }
                }}
              >
                <Plus /> {view === 'roles' ? 'Tạo vai trò' : 'Tạo quyền'}
              </Button>
            </PermissionBoundary>
          </div>
        </div>

        {notice ? (
          <div
            className="mb-6 flex items-center justify-between gap-3 rounded-lg border border-primary/20 bg-primary/5 px-4 py-3 text-sm"
            role="status"
          >
            <span className="flex items-center gap-2">
              <ShieldCheck className="size-4 text-primary" />
              {notice}
            </span>
            <Button variant="ghost" size="xs" onClick={() => setNotice('')}>
              Đóng
            </Button>
          </div>
        ) : null}

        <div className="mb-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <SummaryCard
            title="Tổng vai trò"
            value={view === 'roles' ? roleTotalCount : '—'}
            icon={ShieldCheck}
          />
          <SummaryCard
            title="Vai trò hệ thống trên trang"
            value={roles.filter((role) => role.isSystemRole).length}
            icon={ShieldPlus}
          />
          <SummaryCard
            title="Tổng quyền hạn"
            value={view === 'permissions' ? permissionTotalCount : permissionCatalog.length}
            icon={KeyRound}
          />
          <SummaryCard title="Phân hệ" value={modules.length} icon={Layers3} />
        </div>

        <div
          className="mb-5 inline-flex rounded-lg border bg-muted/30 p-1"
          aria-label="Chọn dữ liệu phân quyền"
        >
          <PermissionBoundary requiredPermissions={['roles.read']}>
            <Button
              variant={view === 'roles' ? 'default' : 'ghost'}
              size="sm"
              onClick={() => navigate('/roles')}
            >
              <ShieldCheck /> Vai trò
            </Button>
          </PermissionBoundary>
          <PermissionBoundary requiredPermissions={['permissions.read']}>
            <Button
              variant={view === 'permissions' ? 'default' : 'ghost'}
              size="sm"
              onClick={() => navigate('/permissions')}
            >
              <KeyRound /> Quyền hạn
            </Button>
          </PermissionBoundary>
        </div>

        {error ? <LoadError message={error} onRetry={() => refresh()} /> : null}
        {view === 'roles' ? (
          <RolesTable
            roles={roles}
            search={roleSearchInput}
            isLoading={isLoading}
            onSearchChange={setRoleSearchInput}
            currentPage={rolePage}
            pageSize={rolePageSize}
            totalCount={roleTotalCount}
            totalPages={roleTotalPages}
            onPageChange={setRolePage}
            onPageSizeChange={(size) => {
              setRolePage(1)
              setRolePageSize(size)
            }}
            onEdit={(role) => {
              setEditingRole(role)
              setRoleFormOpen(true)
            }}
            onPermissions={setPermissionRole}
            onDelete={(role) => {
              setDeleteError('')
              setDeleteTarget({ type: 'role', value: role })
            }}
          />
        ) : (
          <PermissionsTable
            permissions={permissions}
            modules={modules}
            search={permissionSearchInput}
            moduleFilter={moduleFilter}
            isLoading={isLoading}
            onSearchChange={setPermissionSearchInput}
            currentPage={permissionPage}
            pageSize={permissionPageSize}
            totalCount={permissionTotalCount}
            totalPages={permissionTotalPages}
            onPageChange={setPermissionPage}
            onPageSizeChange={(size) => {
              setPermissionPage(1)
              setPermissionPageSize(size)
            }}
            onModuleChange={(module) => {
              setPermissionPage(1)
              setModuleFilter(module)
            }}
            onEdit={(permission) => {
              setEditingPermission(permission)
              setPermissionFormOpen(true)
            }}
            onDelete={(permission) => {
              setDeleteError('')
              setDeleteTarget({ type: 'permission', value: permission })
            }}
          />
        )}
      </div>

      <PermissionBoundary requiredPermissions={[editingRole ? 'roles.update' : 'roles.create']}>
        <RoleFormDialog
          open={roleFormOpen}
          role={editingRole}
          onOpenChange={setRoleFormOpen}
          onSave={saveRole}
        />
      </PermissionBoundary>
      <PermissionBoundary
        requiredPermissions={[editingPermission ? 'permissions.update' : 'permissions.create']}
      >
        <PermissionFormDialog
          open={permissionFormOpen}
          permission={editingPermission}
          isSystemPermission={editingPermission ? isSystemPermission(editingPermission) : false}
          onOpenChange={setPermissionFormOpen}
          onSave={savePermission}
        />
      </PermissionBoundary>
      <PermissionBoundary requiredPermissions={['roles.assign', 'permissions.read']}>
        <PermissionAssignmentDialog
          open={!!permissionRole}
          role={permissionRole}
          permissions={permissionCatalog}
          onOpenChange={(open) => !open && setPermissionRole(null)}
          onSave={saveRolePermissions}
        />
      </PermissionBoundary>
      <PermissionBoundary
        requiredPermissions={[
          deleteTarget?.type === 'role' ? 'roles.delete' : 'permissions.delete',
        ]}
      >
        <DeleteDialog
          target={deleteTarget}
          error={deleteError}
          isDeleting={isDeleting}
          onClose={() => setDeleteTarget(null)}
          onConfirm={confirmDelete}
        />
      </PermissionBoundary>
    </>
  )
}

type RolesTableProps = {
  roles: Role[]
  search: string
  isLoading: boolean
  onSearchChange: (value: string) => void
  currentPage: number
  pageSize: number
  totalCount: number
  totalPages: number
  onPageChange: (page: number) => void
  onPageSizeChange: (pageSize: number) => void
  onEdit: (role: Role) => void
  onPermissions: (role: Role) => void
  onDelete: (role: Role) => void
}

function RolesTable({
  roles,
  search,
  isLoading,
  onSearchChange,
  currentPage,
  pageSize,
  totalCount,
  totalPages,
  onPageChange,
  onPageSizeChange,
  onEdit,
  onPermissions,
  onDelete,
}: RolesTableProps) {
  return (
    <Card>
      <CardHeader className="gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <CardTitle>Danh sách vai trò</CardTitle>
          <p className="mt-1 text-sm text-muted-foreground">{totalCount} vai trò được tìm thấy.</p>
        </div>
        <SearchBox value={search} placeholder="Tìm tên hoặc mô tả..." onChange={onSearchChange} />
      </CardHeader>
      <CardContent>
        <div className="overflow-x-auto rounded-md border">
          <Table aria-busy={isLoading}>
            <TableHeader>
              <TableRow>
                <TableHead>Vai trò</TableHead>
                <TableHead>Loại</TableHead>
                <TableHead>Trạng thái</TableHead>
                <TableHead>Quyền hạn</TableHead>
                <TableHead>Ngày tạo</TableHead>
                <TableHead className="text-right">Thao tác</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {isLoading && roles.length === 0 ? <LoadingRows columns={6} /> : null}
              {!isLoading && roles.length === 0 ? (
                <EmptyRow columns={6} message="Không tìm thấy vai trò phù hợp." />
              ) : null}
              {roles.map((role) => (
                <TableRow key={role.id} className={isLoading ? 'opacity-60' : undefined}>
                  <TableCell>
                    <div className="grid min-w-56 gap-1">
                      <strong>{role.name}</strong>
                      <small className="max-w-md whitespace-normal text-muted-foreground">
                        {role.description || 'Chưa có mô tả.'}
                      </small>
                    </div>
                  </TableCell>
                  <TableCell>
                    <Badge variant={role.isSystemRole ? 'secondary' : 'outline'}>
                      {role.isSystemRole ? 'Hệ thống' : 'Tùy chỉnh'}
                    </Badge>
                  </TableCell>
                  <TableCell>
                    <Badge variant={role.isActive ? 'secondary' : 'destructive'}>
                      {role.isActive ? 'Đang sử dụng' : 'Ngừng sử dụng'}
                    </Badge>
                  </TableCell>
                  <TableCell>
                    <PermissionBoundary
                      requiredPermissions={['roles.assign']}
                      fallback={<span>{role.permissions.length} quyền</span>}
                    >
                      <Button variant="outline" size="sm" onClick={() => onPermissions(role)}>
                        <KeyRound /> {role.permissions.length} quyền
                      </Button>
                    </PermissionBoundary>
                  </TableCell>
                  <TableCell>{formatDate(role.createdAtUtc)}</TableCell>
                  <TableCell>
                    <div className="flex justify-end gap-1">
                      <PermissionBoundary requiredPermissions={['roles.update']}>
                        <Button
                          variant="ghost"
                          size="icon-sm"
                          aria-label={`Chỉnh sửa ${role.name}`}
                          title="Chỉnh sửa"
                          onClick={() => onEdit(role)}
                        >
                          <Pencil />
                        </Button>
                      </PermissionBoundary>
                      <PermissionBoundary requiredPermissions={['roles.delete']}>
                        <Button
                          variant="ghost"
                          size="icon-sm"
                          className="text-destructive hover:text-destructive"
                          aria-label={`Xóa ${role.name}`}
                          title={
                            role.isSystemRole ? 'Không thể xóa vai trò hệ thống' : 'Xóa vai trò'
                          }
                          disabled={role.isSystemRole}
                          onClick={() => onDelete(role)}
                        >
                          <Trash2 />
                        </Button>
                      </PermissionBoundary>
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
        {totalCount > 0 ? (
          <Pagination
            className="mt-4"
            currentPage={currentPage}
            totalPages={totalPages}
            pageSize={pageSize}
            onPageChange={onPageChange}
            onPageSizeChange={onPageSizeChange}
          />
        ) : null}
      </CardContent>
    </Card>
  )
}

type PermissionsTableProps = {
  permissions: Permission[]
  modules: string[]
  search: string
  moduleFilter: string
  isLoading: boolean
  onSearchChange: (value: string) => void
  onModuleChange: (value: string) => void
  currentPage: number
  pageSize: number
  totalCount: number
  totalPages: number
  onPageChange: (page: number) => void
  onPageSizeChange: (pageSize: number) => void
  onEdit: (permission: Permission) => void
  onDelete: (permission: Permission) => void
}

function PermissionsTable({
  permissions,
  modules,
  search,
  moduleFilter,
  isLoading,
  onSearchChange,
  onModuleChange,
  currentPage,
  pageSize,
  totalCount,
  totalPages,
  onPageChange,
  onPageSizeChange,
  onEdit,
  onDelete,
}: PermissionsTableProps) {
  return (
    <Card>
      <CardHeader className="gap-4">
        <div className="flex flex-col justify-between gap-4 lg:flex-row lg:items-center">
          <div>
            <CardTitle>Danh sách quyền hạn</CardTitle>
            <p className="mt-1 text-sm text-muted-foreground">{totalCount} quyền được tìm thấy.</p>
          </div>
          <SearchBox
            value={search}
            placeholder="Tìm tên hoặc mô tả quyền..."
            onChange={onSearchChange}
          />
        </div>
        <Select value={moduleFilter} onValueChange={onModuleChange}>
          <SelectTrigger className="w-full sm:w-56" aria-label="Lọc theo phân hệ">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Tất cả phân hệ</SelectItem>
            {modules.map((module) => (
              <SelectItem key={module} value={module}>
                {module}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </CardHeader>
      <CardContent>
        <div className="overflow-x-auto rounded-md border">
          <Table aria-busy={isLoading}>
            <TableHeader>
              <TableRow>
                <TableHead>Tên quyền</TableHead>
                <TableHead>Phân hệ</TableHead>
                <TableHead>Loại</TableHead>
                <TableHead>Mô tả</TableHead>
                <TableHead>Ngày tạo</TableHead>
                <TableHead className="text-right">Thao tác</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {isLoading && permissions.length === 0 ? <LoadingRows columns={6} /> : null}
              {!isLoading && permissions.length === 0 ? (
                <EmptyRow columns={6} message="Không tìm thấy quyền hạn phù hợp." />
              ) : null}
              {permissions.map((permission) => {
                const systemPermission = isSystemPermission(permission)
                return (
                  <TableRow key={permission.id} className={isLoading ? 'opacity-60' : undefined}>
                    <TableCell>
                      <strong className="font-mono text-xs">{permission.name}</strong>
                    </TableCell>
                    <TableCell>
                      <Badge variant="outline" className="capitalize">
                        {permission.module}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      <Badge variant={systemPermission ? 'secondary' : 'outline'}>
                        {systemPermission ? 'Hệ thống' : 'Tùy chỉnh'}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      <span className="block max-w-sm whitespace-normal text-muted-foreground">
                        {permission.description || 'Chưa có mô tả.'}
                      </span>
                    </TableCell>
                    <TableCell>{formatDate(permission.createdAtUtc)}</TableCell>
                    <TableCell>
                      <div className="flex justify-end gap-1">
                        <PermissionBoundary requiredPermissions={['permissions.update']}>
                          <Button
                            variant="ghost"
                            size="icon-sm"
                            aria-label={`Chỉnh sửa ${permission.name}`}
                            title="Chỉnh sửa"
                            onClick={() => onEdit(permission)}
                          >
                            <Pencil />
                          </Button>
                        </PermissionBoundary>
                        <PermissionBoundary requiredPermissions={['permissions.delete']}>
                          <Button
                            variant="ghost"
                            size="icon-sm"
                            className="text-destructive hover:text-destructive"
                            aria-label={`Xóa ${permission.name}`}
                            title={systemPermission ? 'Không thể xóa quyền hệ thống' : 'Xóa quyền'}
                            disabled={systemPermission}
                            onClick={() => onDelete(permission)}
                          >
                            <Trash2 />
                          </Button>
                        </PermissionBoundary>
                      </div>
                    </TableCell>
                  </TableRow>
                )
              })}
            </TableBody>
          </Table>
        </div>
        {totalCount > 0 ? (
          <Pagination
            className="mt-4"
            currentPage={currentPage}
            totalPages={totalPages}
            pageSize={pageSize}
            onPageChange={onPageChange}
            onPageSizeChange={onPageSizeChange}
          />
        ) : null}
      </CardContent>
    </Card>
  )
}

function SearchBox({
  value,
  placeholder,
  onChange,
}: {
  value: string
  placeholder: string
  onChange: (value: string) => void
}) {
  return (
    <div className="relative w-full lg:w-80">
      <Search className="absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
      <Input
        className="pl-9"
        value={value}
        placeholder={placeholder}
        onChange={(event) => onChange(event.target.value)}
      />
    </div>
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
function LoadError({ message, onRetry }: { message: string; onRetry: () => void }) {
  return (
    <div
      className="mb-5 flex flex-col items-center gap-3 rounded-lg border border-destructive/30 bg-destructive/5 p-6 text-center"
      role="alert"
    >
      <CircleAlert className="size-6 text-destructive" />
      <div>
        <p className="font-medium">Không thể tải dữ liệu phân quyền</p>
        <p className="mt-1 text-sm text-muted-foreground">{message}</p>
      </div>
      <Button variant="outline" size="sm" onClick={onRetry}>
        Thử lại
      </Button>
    </div>
  )
}

function LoadingRows({ columns }: { columns: number }) {
  return Array.from({ length: 5 }, (_, index) => (
    <TableRow key={index}>
      <TableCell colSpan={columns}>
        <div className="h-9 rounded-md bg-muted motion-safe:animate-pulse" />
      </TableCell>
    </TableRow>
  ))
}

function EmptyRow({ columns, message }: { columns: number; message: string }) {
  return (
    <TableRow>
      <TableCell colSpan={columns} className="h-32 text-center text-muted-foreground">
        {message}
      </TableCell>
    </TableRow>
  )
}

function DeleteDialog({
  target,
  error,
  isDeleting,
  onClose,
  onConfirm,
}: {
  target: DeleteTarget | null
  error: string
  isDeleting: boolean
  onClose: () => void
  onConfirm: () => void
}) {
  const name = target?.value.name
  return (
    <Dialog open={!!target} onOpenChange={(open) => !open && !isDeleting && onClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Xóa {target?.type === 'role' ? 'vai trò' : 'quyền hạn'}?</DialogTitle>
          <DialogDescription>
            <strong>{name}</strong> sẽ bị xóa vĩnh viễn. Thao tác sẽ thất bại nếu tài nguyên đang
            được bảo vệ hoặc vai trò còn được gán cho người dùng.
          </DialogDescription>
        </DialogHeader>
        {error ? (
          <p
            className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive"
            role="alert"
          >
            {error}
          </p>
        ) : null}
        <DialogFooter>
          <Button variant="outline" disabled={isDeleting} onClick={onClose}>
            Hủy
          </Button>
          <Button variant="destructive" disabled={isDeleting} onClick={onConfirm}>
            {isDeleting ? 'Đang xóa...' : 'Xóa'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('vi-VN', { dateStyle: 'short' }).format(new Date(value))
}
