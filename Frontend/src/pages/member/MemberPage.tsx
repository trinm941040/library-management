import { useCallback, useEffect, useMemo, useState } from 'react'
import {
  BriefcaseBusiness,
  Building2,
  CalendarDays,
  CircleAlert,
  Pencil,
  Plus,
  RefreshCw,
  Search,
  Trash2,
  UserRoundCheck,
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
import { EmployeeFormDialog } from './components/EmployeeFormDialog'
import {
  createEmployee,
  deleteEmployee,
  getEmployees,
  updateEmployee,
  type Employee,
  type EmployeePageResponse,
  type EmploymentStatus,
  type SaveEmployeeInput,
} from './employee-api'

const EMPLOYEES_PER_PAGE = 20

const statusOptions: { value: EmploymentStatus; label: string }[] = [
  { value: 'Active', label: 'Đang làm việc' },
  { value: 'OnLeave', label: 'Đang nghỉ phép' },
  { value: 'Inactive', label: 'Tạm ngưng' },
  { value: 'Terminated', label: 'Đã nghỉ việc' },
]

const statusLabels = Object.fromEntries(
  statusOptions.map(({ value, label }) => [value, label]),
) as Record<EmploymentStatus, string>

type StatusFilter = 'all' | EmploymentStatus

export function MemberPage() {
  const [page, setPage] = useState<EmployeePageResponse | null>(null)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [departmentInput, setDepartmentInput] = useState('')
  const [department, setDepartment] = useState('')
  const [positionInput, setPositionInput] = useState('')
  const [position, setPosition] = useState('')
  const [status, setStatus] = useState<StatusFilter>('all')
  const [currentPage, setCurrentPage] = useState(1)
  const [reloadKey, setReloadKey] = useState(0)
  const [isLoading, setIsLoading] = useState(true)
  const [pageError, setPageError] = useState('')
  const [notice, setNotice] = useState('')
  const [formOpen, setFormOpen] = useState(false)
  const [editingEmployee, setEditingEmployee] = useState<Employee | null>(null)
  const [deletingEmployee, setDeletingEmployee] = useState<Employee | null>(null)
  const [deleteError, setDeleteError] = useState('')
  const [isDeleting, setIsDeleting] = useState(false)

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      setSearch(searchInput.trim())
      setDepartment(departmentInput.trim())
      setPosition(positionInput.trim())
      setCurrentPage(1)
    }, 350)
    return () => window.clearTimeout(timeout)
  }, [departmentInput, positionInput, searchInput])

  useEffect(() => {
    const controller = new AbortController()
    setIsLoading(true)
    setPageError('')

    getEmployees(
      {
        search: search || undefined,
        department: department || undefined,
        position: position || undefined,
        status: status === 'all' ? undefined : status,
        pageNumber: currentPage,
        pageSize: EMPLOYEES_PER_PAGE,
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
        setPageError(error instanceof Error ? error.message : 'Không thể tải danh sách nhân viên.')
      })
      .finally(() => {
        if (!controller.signal.aborted) setIsLoading(false)
      })

    return () => controller.abort()
  }, [currentPage, department, position, reloadKey, search, status])

  const refresh = useCallback((message?: string) => {
    if (message) setNotice(message)
    setReloadKey((value) => value + 1)
  }, [])

  const departmentsOnPage = useMemo(
    () => new Set(page?.items.map((employee) => employee.department) ?? []).size,
    [page],
  )
  const activeOnPage = page?.items.filter((employee) => employee.status === 'Active').length ?? 0
  const onLeaveOnPage = page?.items.filter((employee) => employee.status === 'OnLeave').length ?? 0

  const handleSave = async (data: SaveEmployeeInput) => {
    try {
      if (editingEmployee) {
        await updateEmployee(editingEmployee.id, data)
        refresh('Đã cập nhật hồ sơ nhân viên.')
      } else {
        await createEmployee(data)
        setCurrentPage(1)
        refresh('Đã thêm nhân viên mới.')
      }
      return null
    } catch (error) {
      return error instanceof Error ? error.message : 'Không thể lưu hồ sơ nhân viên.'
    }
  }

  const confirmDelete = async () => {
    if (!deletingEmployee) return
    setDeleteError('')
    setIsDeleting(true)

    try {
      await deleteEmployee(deletingEmployee.id)
      setDeletingEmployee(null)
      refresh('Đã xóa nhân viên khỏi hệ thống.')
    } catch (error) {
      setDeleteError(error instanceof Error ? error.message : 'Không thể xóa nhân viên.')
    } finally {
      setIsDeleting(false)
    }
  }

  const resetFilters = () => {
    setSearchInput('')
    setDepartmentInput('')
    setPositionInput('')
    setStatus('all')
    setCurrentPage(1)
  }

  const displayedFrom = page && page.totalCount > 0 ? (page.pageNumber - 1) * page.pageSize + 1 : 0
  const displayedTo = page ? Math.min(page.pageNumber * page.pageSize, page.totalCount) : 0
  const hasFilters = Boolean(searchInput || departmentInput || positionInput || status !== 'all')

  return (
    <>
      <div className="mx-auto w-full max-w-7xl px-5 py-10 md:px-12">
        <div className="mb-8 flex flex-col justify-between gap-4 sm:flex-row sm:items-end">
          <div>
            <p className="mb-2 text-xs font-bold tracking-widest text-primary uppercase">
              quản lý người dùng
            </p>
            <h1 className="text-3xl font-bold tracking-tight">Quản lý nhân viên</h1>
            <p className="mt-2 text-sm text-muted-foreground">
              Quản lý hồ sơ, vị trí công tác và trạng thái làm việc của nhân viên thư viện.
            </p>
          </div>
          <div className="flex gap-2">
            <Button variant="outline" disabled={isLoading} onClick={() => refresh()}>
              <RefreshCw className={isLoading ? 'animate-spin' : ''} /> Làm mới
            </Button>
            <Button
              onClick={() => {
                setEditingEmployee(null)
                setFormOpen(true)
              }}
            >
              <Plus /> Thêm nhân viên
            </Button>
          </div>
        </div>

        {notice ? (
          <div
            className="mb-6 flex items-center justify-between gap-3 rounded-lg border border-primary/20 bg-primary/5 px-4 py-3 text-sm"
            role="status"
          >
            <span className="flex items-center gap-2">
              <UserRoundCheck className="size-4 text-primary" /> {notice}
            </span>
            <Button variant="ghost" size="xs" onClick={() => setNotice('')}>
              Đóng
            </Button>
          </div>
        ) : null}

        <div className="mb-6 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <SummaryCard
            title="Tổng kết quả"
            value={page?.totalCount ?? '—'}
            icon={BriefcaseBusiness}
          />
          <SummaryCard title="Đang làm / trang" value={activeOnPage} icon={UserRoundCheck} />
          <SummaryCard title="Nghỉ phép / trang" value={onLeaveOnPage} icon={CalendarDays} />
          <SummaryCard title="Phòng ban / trang" value={departmentsOnPage} icon={Building2} />
        </div>

        <Card>
          <CardHeader>
            <CardTitle>Danh sách nhân viên</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="mb-5 grid gap-3 lg:grid-cols-[minmax(15rem,1fr)_13rem_13rem_12rem_auto]">
              <div className="relative">
                <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  className="pl-9"
                  aria-label="Tìm nhân viên"
                  value={searchInput}
                  onChange={(event) => setSearchInput(event.target.value)}
                  placeholder="Mã, tên, email, số điện thoại..."
                />
              </div>
              <Input
                value={departmentInput}
                onChange={(event) => setDepartmentInput(event.target.value)}
                placeholder="Lọc phòng ban"
                aria-label="Lọc theo phòng ban"
              />
              <Input
                value={positionInput}
                onChange={(event) => setPositionInput(event.target.value)}
                placeholder="Lọc chức vụ"
                aria-label="Lọc theo chức vụ"
              />
              <Select
                value={status}
                onValueChange={(value) => {
                  setStatus(value as StatusFilter)
                  setCurrentPage(1)
                }}
              >
                <SelectTrigger className="w-full" aria-label="Lọc theo trạng thái">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">Tất cả trạng thái</SelectItem>
                  {statusOptions.map((option) => (
                    <SelectItem key={option.value} value={option.value}>
                      {option.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <Button variant="outline" disabled={!hasFilters} onClick={resetFilters}>
                Xóa lọc
              </Button>
            </div>

            {pageError ? (
              <div
                className="mb-5 flex items-start gap-3 rounded-lg border border-destructive/30 bg-destructive/5 p-4 text-sm text-destructive"
                role="alert"
              >
                <CircleAlert className="mt-0.5 size-4 shrink-0" />
                <span className="flex-1">{pageError}</span>
                <Button variant="outline" size="sm" onClick={() => refresh()}>
                  Thử lại
                </Button>
              </div>
            ) : null}

            <div className="overflow-hidden rounded-lg border">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Nhân viên</TableHead>
                    <TableHead>Đơn vị công tác</TableHead>
                    <TableHead>Liên hệ</TableHead>
                    <TableHead>Ngày vào làm</TableHead>
                    <TableHead>Trạng thái</TableHead>
                    <TableHead>
                      <span className="sr-only">Thao tác</span>
                    </TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {isLoading && !page ? <LoadingRows /> : null}
                  {!isLoading && page?.items.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={6} className="h-32 text-center text-muted-foreground">
                        Không tìm thấy nhân viên phù hợp.
                      </TableCell>
                    </TableRow>
                  ) : null}
                  {page?.items.map((employee) => (
                    <TableRow key={employee.id} className={isLoading ? 'opacity-60' : undefined}>
                      <TableCell>
                        <div className="flex min-w-56 items-center gap-3">
                          <span className="grid size-9 shrink-0 place-items-center rounded-full bg-primary/10 text-xs font-bold text-primary">
                            {getInitials(employee.fullName)}
                          </span>
                          <span className="grid gap-0.5">
                            <strong>{employee.fullName}</strong>
                            <small className="font-medium text-primary">
                              {employee.employeeCode}
                            </small>
                          </span>
                        </div>
                      </TableCell>
                      <TableCell>
                        <div className="grid min-w-40 gap-0.5">
                          <span>{employee.position}</span>
                          <small className="text-muted-foreground">{employee.department}</small>
                        </div>
                      </TableCell>
                      <TableCell>
                        <div className="grid min-w-52 gap-0.5">
                          <span>{employee.email}</span>
                          <small className="text-muted-foreground">
                            {employee.phoneNumber || 'Chưa có số điện thoại'}
                          </small>
                        </div>
                      </TableCell>
                      <TableCell>{formatDate(employee.hireDate)}</TableCell>
                      <TableCell>
                        <StatusBadge status={employee.status} />
                      </TableCell>
                      <TableCell>
                        <div className="flex justify-end gap-1">
                          <Button
                            variant="ghost"
                            size="icon-sm"
                            aria-label={`Chỉnh sửa ${employee.fullName}`}
                            onClick={() => {
                              setEditingEmployee(employee)
                              setFormOpen(true)
                            }}
                          >
                            <Pencil />
                          </Button>
                          <Button
                            variant="ghost"
                            size="icon-sm"
                            className="text-destructive hover:text-destructive"
                            aria-label={`Xóa ${employee.fullName}`}
                            onClick={() => {
                              setDeleteError('')
                              setDeletingEmployee(employee)
                            }}
                          >
                            <Trash2 />
                          </Button>
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>

            <div className="mt-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <p className="text-sm text-muted-foreground">
                {page?.totalCount
                  ? `Hiển thị ${displayedFrom}-${displayedTo} trong ${page.totalCount} nhân viên.`
                  : 'Không có nhân viên để hiển thị.'}
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

      <EmployeeFormDialog
        open={formOpen}
        employee={editingEmployee}
        onOpenChange={setFormOpen}
        onSave={handleSave}
      />

      <Dialog
        open={!!deletingEmployee}
        onOpenChange={(open) => !open && !isDeleting && setDeletingEmployee(null)}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Xóa nhân viên?</DialogTitle>
            <DialogDescription>
              Hồ sơ <strong>{deletingEmployee?.fullName}</strong> ({deletingEmployee?.employeeCode})
              sẽ bị xóa vĩnh viễn. Thao tác này không thể hoàn tác.
            </DialogDescription>
          </DialogHeader>
          {deleteError ? (
            <p
              className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive"
              role="alert"
            >
              {deleteError}
            </p>
          ) : null}
          <DialogFooter>
            <Button
              variant="outline"
              disabled={isDeleting}
              onClick={() => setDeletingEmployee(null)}
            >
              Hủy
            </Button>
            <Button variant="destructive" disabled={isDeleting} onClick={confirmDelete}>
              {isDeleting ? 'Đang xóa...' : 'Xóa nhân viên'}
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
  value: string | number
  icon: typeof BriefcaseBusiness
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

function StatusBadge({ status }: { status: EmploymentStatus }) {
  const classes: Record<EmploymentStatus, string> = {
    Active: 'border-emerald-200 bg-emerald-50 text-emerald-700',
    OnLeave: 'border-amber-200 bg-amber-50 text-amber-700',
    Inactive: 'border-slate-200 bg-slate-100 text-slate-700',
    Terminated: 'border-red-200 bg-red-50 text-red-700',
  }
  return (
    <Badge variant="outline" className={classes[status]}>
      {statusLabels[status]}
    </Badge>
  )
}

function LoadingRows() {
  return Array.from({ length: 5 }, (_, index) => (
    <TableRow key={index}>
      <TableCell colSpan={6}>
        <div className="h-10 animate-pulse rounded-md bg-muted" />
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

function formatDate(value: string) {
  return new Intl.DateTimeFormat('vi-VN').format(new Date(`${value}T00:00:00`))
}
