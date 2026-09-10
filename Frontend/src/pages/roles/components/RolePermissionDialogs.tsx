import { useEffect, useMemo, useRef, useState, type FormEvent } from 'react'
import { ChevronDown, ChevronRight, FolderTree, Search, ShieldCheck } from 'lucide-react'
import { Badge } from '@/common/components/ui/badge'
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
import type { Permission, PermissionInput, Role, RoleInput } from '../role-permission-api'

type AsyncSave<T> = (data: T) => Promise<string | null>

type RoleFormDialogProps = {
  open: boolean
  role: Role | null
  onOpenChange: (open: boolean) => void
  onSave: AsyncSave<RoleInput>
}

export function RoleFormDialog({ open, role, onOpenChange, onSave }: RoleFormDialogProps) {
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [error, setError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  useEffect(() => {
    setName(role?.name ?? '')
    setDescription(role?.description ?? '')
    setError('')
  }, [open, role])

  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError('')
    setIsSubmitting(true)
    try {
      const saveError = await onSave({ name: name.trim(), description: description.trim() })
      if (saveError) return setError(saveError)
      onOpenChange(false)
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={(value) => !isSubmitting && onOpenChange(value)}>
      <DialogContent>
        <form onSubmit={submit}>
          <DialogHeader>
            <DialogTitle>{role ? 'Chỉnh sửa vai trò' : 'Tạo vai trò mới'}</DialogTitle>
            <DialogDescription>
              {role?.isSystemRole
                ? 'Vai trò hệ thống chỉ cho phép thay đổi mô tả.'
                : 'Đặt tên và mô tả rõ phạm vi trách nhiệm của vai trò.'}
            </DialogDescription>
          </DialogHeader>
          <div className="grid gap-5 py-6">
            <div className="grid gap-2">
              <Label htmlFor="role-name">Tên vai trò</Label>
              <Input
                id="role-name"
                value={name}
                onChange={(event) => setName(event.target.value)}
                minLength={2}
                maxLength={100}
                disabled={isSubmitting || role?.isSystemRole}
                required
              />
            </div>
            <div className="grid gap-2">
              <Label htmlFor="role-description">Mô tả</Label>
              <textarea
                id="role-description"
                value={description}
                onChange={(event) => setDescription(event.target.value)}
                maxLength={500}
                rows={4}
                disabled={isSubmitting}
                className="w-full resize-y rounded-md border border-input bg-transparent px-3 py-2 text-sm outline-none focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50 disabled:opacity-50"
              />
            </div>
            {error ? <DialogError>{error}</DialogError> : null}
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
              {isSubmitting ? 'Đang lưu...' : role ? 'Lưu thay đổi' : 'Tạo vai trò'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}

type PermissionFormDialogProps = {
  open: boolean
  permission: Permission | null
  isSystemPermission: boolean
  onOpenChange: (open: boolean) => void
  onSave: AsyncSave<PermissionInput>
}

export function PermissionFormDialog({
  open,
  permission,
  isSystemPermission,
  onOpenChange,
  onSave,
}: PermissionFormDialogProps) {
  const [name, setName] = useState('')
  const [module, setModule] = useState('')
  const [description, setDescription] = useState('')
  const [error, setError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  useEffect(() => {
    setName(permission?.name ?? '')
    setModule(permission?.module ?? '')
    setDescription(permission?.description ?? '')
    setError('')
  }, [open, permission])

  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError('')
    setIsSubmitting(true)
    try {
      const saveError = await onSave({
        name: name.trim().toLowerCase(),
        module: module.trim().toLowerCase(),
        description: description.trim(),
      })
      if (saveError) return setError(saveError)
      onOpenChange(false)
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={(value) => !isSubmitting && onOpenChange(value)}>
      <DialogContent>
        <form onSubmit={submit}>
          <DialogHeader>
            <DialogTitle>{permission ? 'Chỉnh sửa quyền hạn' : 'Tạo quyền hạn mới'}</DialogTitle>
            <DialogDescription>
              Tên quyền dùng định dạng <code>module.action</code>, chữ thường và không có khoảng
              trắng.
            </DialogDescription>
          </DialogHeader>
          <div className="grid gap-5 py-6">
            <div className="grid gap-2">
              <Label htmlFor="permission-name">Tên quyền</Label>
              <Input
                id="permission-name"
                value={name}
                onChange={(event) => setName(event.target.value)}
                placeholder="catalog.update"
                pattern="[a-z][a-z0-9-]*\.[a-z][a-z0-9-]*"
                minLength={3}
                maxLength={100}
                disabled={isSubmitting || isSystemPermission}
                required
              />
            </div>
            <div className="grid gap-2">
              <Label htmlFor="permission-module">Phân hệ</Label>
              <Input
                id="permission-module"
                value={module}
                onChange={(event) => setModule(event.target.value)}
                placeholder="catalog"
                maxLength={100}
                disabled={isSubmitting || isSystemPermission}
                required
              />
            </div>
            <div className="grid gap-2">
              <Label htmlFor="permission-description">Mô tả</Label>
              <textarea
                id="permission-description"
                value={description}
                onChange={(event) => setDescription(event.target.value)}
                maxLength={500}
                rows={3}
                disabled={isSubmitting}
                className="w-full resize-y rounded-md border border-input bg-transparent px-3 py-2 text-sm outline-none focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50 disabled:opacity-50"
              />
            </div>
            {isSystemPermission ? (
              <p className="text-xs text-muted-foreground">
                Quyền hệ thống chỉ cho phép cập nhật mô tả.
              </p>
            ) : null}
            {error ? <DialogError>{error}</DialogError> : null}
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
              {isSubmitting ? 'Đang lưu...' : permission ? 'Lưu thay đổi' : 'Tạo quyền'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}

type PermissionAssignmentDialogProps = {
  open: boolean
  role: Role | null
  permissions: Permission[]
  onOpenChange: (open: boolean) => void
  onSave: (permissionIds: string[]) => Promise<string | null>
}

export function PermissionAssignmentDialog({
  open,
  role,
  permissions,
  onOpenChange,
  onSave,
}: PermissionAssignmentDialogProps) {
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set())
  const [expandedModules, setExpandedModules] = useState<Set<string>>(new Set())
  const [search, setSearch] = useState('')
  const [error, setError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  useEffect(() => {
    setSelectedIds(new Set(role?.permissions.map((permission) => permission.id) ?? []))
    setExpandedModules(new Set(permissions.map((permission) => permission.module)))
    setSearch('')
    setError('')
  }, [open, permissions, role])

  const groups = useMemo(() => {
    const keyword = search.trim().toLowerCase()
    const filtered = keyword
      ? permissions.filter((permission) =>
          `${permission.name} ${permission.description} ${permission.module}`
            .toLowerCase()
            .includes(keyword),
        )
      : permissions

    const grouped = filtered.reduce<Record<string, Permission[]>>((result, permission) => {
      result[permission.module] ??= []
      result[permission.module].push(permission)
      return result
    }, {})

    return Object.entries(grouped).sort(([first], [second]) => first.localeCompare(second))
  }, [permissions, search])

  const togglePermission = (permissionId: string) => {
    setSelectedIds((current) => {
      const next = new Set(current)
      if (next.has(permissionId)) next.delete(permissionId)
      else next.add(permissionId)
      return next
    })
  }

  const toggleModule = (module: string) => {
    setExpandedModules((current) => {
      const next = new Set(current)
      if (next.has(module)) next.delete(module)
      else next.add(module)
      return next
    })
  }

  const toggleModulePermissions = (modulePermissions: Permission[]) => {
    setSelectedIds((current) => {
      const next = new Set(current)
      const allSelected = modulePermissions.every((permission) => next.has(permission.id))
      modulePermissions.forEach((permission) => {
        if (allSelected) next.delete(permission.id)
        else next.add(permission.id)
      })
      return next
    })
  }

  const save = async () => {
    setError('')
    setIsSubmitting(true)
    try {
      const saveError = await onSave([...selectedIds])
      if (saveError) return setError(saveError)
      onOpenChange(false)
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={(value) => !isSubmitting && onOpenChange(value)}>
      <DialogContent className="sm:max-w-3xl">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <ShieldCheck className="size-5 text-primary" />
            Quyền của {role?.name}
          </DialogTitle>
          <DialogDescription>
            Cây quyền được tổ chức theo phân hệ. Chọn nút cha để bật hoặc tắt toàn bộ quyền con.
          </DialogDescription>
        </DialogHeader>
        <div className="flex flex-col gap-2 sm:flex-row">
          <div className="relative flex-1">
            <Search className="absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              className="pl-9"
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Tìm quyền hoặc phân hệ..."
            />
          </div>
          <div className="flex gap-2">
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => setExpandedModules(new Set(groups.map(([module]) => module)))}
            >
              Mở tất cả
            </Button>
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => setExpandedModules(new Set())}
            >
              Thu gọn
            </Button>
          </div>
        </div>
        <div
          className="max-h-[55vh] overflow-y-auto rounded-lg border p-2"
          role="tree"
          aria-label={`Cây quyền của vai trò ${role?.name ?? ''}`}
        >
          {groups.map(([module, modulePermissions]) => {
            const expanded = search.trim() ? true : expandedModules.has(module)
            const selectedCount = modulePermissions.filter((permission) =>
              selectedIds.has(permission.id),
            ).length
            const allSelected =
              modulePermissions.length > 0 && selectedCount === modulePermissions.length
            const partiallySelected = selectedCount > 0 && !allSelected

            return (
              <div
                key={module}
                role="treeitem"
                aria-expanded={expanded}
                aria-selected={allSelected}
                className="not-last:border-b"
              >
                <div className="flex min-h-12 items-center gap-2 rounded-md px-2 hover:bg-muted/60">
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon-xs"
                    aria-label={`${expanded ? 'Thu gọn' : 'Mở'} phân hệ ${module}`}
                    onClick={() => toggleModule(module)}
                  >
                    {expanded ? <ChevronDown /> : <ChevronRight />}
                  </Button>
                  <TreeCheckbox
                    label={`Chọn toàn bộ quyền của phân hệ ${module}`}
                    checked={allSelected}
                    indeterminate={partiallySelected}
                    disabled={isSubmitting}
                    onChange={() => toggleModulePermissions(modulePermissions)}
                  />
                  <FolderTree className="size-4 text-primary" aria-hidden="true" />
                  <button
                    type="button"
                    className="min-w-0 flex-1 text-left font-semibold capitalize"
                    onClick={() => toggleModule(module)}
                  >
                    {module}
                  </button>
                  <Badge variant={allSelected ? 'secondary' : 'outline'}>
                    {selectedCount}/{modulePermissions.length}
                  </Badge>
                </div>

                {expanded ? (
                  <div role="group" className="relative ml-7 border-l py-1 pl-4">
                    {modulePermissions.map((permission) => (
                      <div
                        key={permission.id}
                        role="treeitem"
                        aria-selected={selectedIds.has(permission.id)}
                        className="relative flex items-start gap-3 rounded-md px-3 py-2.5 hover:bg-muted/60"
                      >
                        <span className="absolute top-5 -left-4 h-px w-3 bg-border" />
                        <TreeCheckbox
                          label={`Chọn quyền ${permission.name}`}
                          checked={selectedIds.has(permission.id)}
                          disabled={isSubmitting}
                          onChange={() => togglePermission(permission.id)}
                        />
                        <span className="min-w-0">
                          <strong className="block font-mono text-xs">{permission.name}</strong>
                          <small className="mt-1 block whitespace-normal text-muted-foreground">
                            {permission.description || 'Chưa có mô tả.'}
                          </small>
                        </span>
                      </div>
                    ))}
                  </div>
                ) : null}
              </div>
            )
          })}
          {groups.length === 0 ? (
            <p className="py-8 text-center text-sm text-muted-foreground">
              Không tìm thấy quyền phù hợp.
            </p>
          ) : null}
        </div>
        {error ? <DialogError>{error}</DialogError> : null}
        <DialogFooter>
          <Button
            type="button"
            variant="outline"
            disabled={isSubmitting}
            onClick={() => onOpenChange(false)}
          >
            Hủy
          </Button>
          <Button type="button" disabled={isSubmitting} onClick={save}>
            {isSubmitting ? 'Đang lưu...' : `Lưu ${selectedIds.size} quyền`}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

function TreeCheckbox({
  label,
  checked,
  indeterminate = false,
  disabled,
  onChange,
}: {
  label: string
  checked: boolean
  indeterminate?: boolean
  disabled: boolean
  onChange: () => void
}) {
  const ref = useRef<HTMLInputElement>(null)

  useEffect(() => {
    if (ref.current) ref.current.indeterminate = indeterminate
  }, [indeterminate])

  return (
    <input
      ref={ref}
      type="checkbox"
      className="mt-0.5 size-4 shrink-0 accent-primary"
      aria-label={label}
      aria-checked={indeterminate ? 'mixed' : checked}
      checked={checked}
      disabled={disabled}
      onChange={onChange}
    />
  )
}

function DialogError({ children }: { children: string }) {
  return (
    <p className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive" role="alert">
      {children}
    </p>
  )
}
