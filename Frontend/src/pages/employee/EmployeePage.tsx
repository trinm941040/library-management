import { useCallback, useEffect, useState, type ReactNode } from 'react'
import { useSearchParams } from 'react-router-dom'
import {
  BriefcaseBusiness,
  Building2,
  CalendarDays,
  CircleAlert,
  Eye,
  Pencil,
  Plus,
  RefreshCw,
  Search,
  UserRoundCheck,
  UserRoundX,
  type LucideIcon,
} from 'lucide-react'
import { Button } from '@/common/components/ui/button'
import { formatDate } from '@/common/formatters'
import { Card, CardContent, CardHeader, CardTitle } from '@/common/components/ui/card'
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
import { EmployeeDetailsDialog } from './components/EmployeeDetailsDialog'
import { EmploymentStatusBadge } from './components/EmploymentStatusBadge'
import { EmployeeFormDialog } from './components/EmployeeFormDialog'
import { PermissionBoundary } from '@/shared/auth/PermissionBoundary'
import {
  createEmployee,
  employmentStatusLabels,
  employmentStatuses,
  getEmployees,
  getEmployeeBranches,
  getEmployeeSummary,
  updateEmployee,
  type Employee,
  type EmployeeBranch,
  type EmployeePageResponse,
  type EmployeeSummary,
  type EmploymentStatus,
  type SaveEmployeeInput,
} from './employee-api'

type StatusFilter = 'all' | EmploymentStatus

export function EmployeePage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const search = searchParams.get('search')?.trim() ?? ''
  const department = searchParams.get('department')?.trim() ?? ''
  const position = searchParams.get('position')?.trim() ?? ''
  const branchId = searchParams.get('branchId') ?? 'all'
  const statusParam = searchParams.get('status')
  const status: StatusFilter = employmentStatuses.includes(statusParam as EmploymentStatus)
    ? (statusParam as EmploymentStatus)
    : 'all'
  const requestedPage = Number(searchParams.get('page'))
  const currentPage = Number.isInteger(requestedPage) && requestedPage > 0 ? requestedPage : 1
  const requestedPageSize = Number(searchParams.get('pageSize'))
  const pageSize = [10, 20, 50, 100].includes(requestedPageSize) ? requestedPageSize : 20
  const [page, setPage] = useState<EmployeePageResponse | null>(null)
  const [summary, setSummary] = useState<EmployeeSummary | null>(null)
  const [branches, setBranches] = useState<EmployeeBranch[]>([])
  const [searchInput, setSearchInput] = useState(search)
  const [departmentInput, setDepartmentInput] = useState(department)
  const [positionInput, setPositionInput] = useState(position)
  const [reloadKey, setReloadKey] = useState(0)
  const [isLoading, setIsLoading] = useState(true)
  const [pageError, setPageError] = useState('')
  const [notice, setNotice] = useState('')
  const [formOpen, setFormOpen] = useState(false)
  const [detailsOpen, setDetailsOpen] = useState(false)
  const [selectedEmployee, setSelectedEmployee] = useState<Employee | null>(null)
  const [editingEmployee, setEditingEmployee] = useState<Employee | null>(null)

  const updateUrlFilters = useCallback(
    (changes: Record<string, string | undefined>) => {
      setSearchParams(
        (current) => {
          const next = new URLSearchParams(current)
          Object.entries(changes).forEach(([key, value]) => {
            if (!value || value === 'all') next.delete(key)
            else next.set(key, value)
          })
          return next
        },
        { replace: true },
      )
    },
    [setSearchParams],
  )

  useEffect(() => {
    setSearchInput(search)
    setDepartmentInput(department)
    setPositionInput(position)
  }, [department, position, search])

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      const nextSearch = searchInput.trim()
      const nextDepartment = departmentInput.trim()
      const nextPosition = positionInput.trim()
      if (nextSearch !== search || nextDepartment !== department || nextPosition !== position)
        updateUrlFilters({
          search: nextSearch || undefined,
          department: nextDepartment || undefined,
          position: nextPosition || undefined,
          page: undefined,
        })
    }, 350)
    return () => window.clearTimeout(timeout)
  }, [department, departmentInput, position, positionInput, search, searchInput, updateUrlFilters])

  useEffect(() => {
    const controller = new AbortController()
    getEmployeeBranches(controller.signal)
      .then(setBranches)
      .catch((error: unknown) => {
        if (!(error instanceof DOMException && error.name === 'AbortError')) setBranches([])
      })
    return () => controller.abort()
  }, [reloadKey])

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
        branchId: branchId === 'all' ? undefined : branchId,
        pageNumber: currentPage,
        pageSize,
      },
      controller.signal,
    )
      .then((response) => {
        setPage(response)
        if (response.totalPages > 0 && currentPage > response.totalPages)
          updateUrlFilters({ page: String(response.totalPages) })
      })
      .catch((error: unknown) => {
        if (!(error instanceof DOMException && error.name === 'AbortError'))
          setPageError(
            error instanceof Error ? error.message : 'Không thể tải danh sách nhân viên.',
          )
      })
      .finally(() => {
        if (!controller.signal.aborted) setIsLoading(false)
      })
    return () => controller.abort()
  }, [branchId, currentPage, department, pageSize, position, reloadKey, search, status, updateUrlFilters])

  useEffect(() => {
    const controller = new AbortController()
    getEmployeeSummary(controller.signal)
      .then(setSummary)
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
    setEditingEmployee(null)
    setFormOpen(true)
  }
  const openEditForm = (employee: Employee) => {
    setDetailsOpen(false)
    setPageError('')
    setEditingEmployee({ ...employee })
    setFormOpen(true)
  }
  const openDetails = (employee: Employee) => {
    setSelectedEmployee(employee)
    setDetailsOpen(true)
  }

  const handleSave = async (data: SaveEmployeeInput) => {
    try {
      if (editingEmployee) {
        const updated = await updateEmployee(editingEmployee.id, {
          ...data,
          concurrencyToken: editingEmployee.concurrencyToken,
        })
        setSelectedEmployee((current) => (current?.id === updated.id ? updated : current))
        refresh('Đã cập nhật hồ sơ và thông tin việc làm.')
      } else {
        const created = await createEmployee(data)
        updateUrlFilters({ page: undefined })
        setPage((current) => current ? {
          ...current,
          items: [created, ...current.items.filter((item) => item.id !== created.id)].slice(0, current.pageSize),
          totalCount: current.totalCount + 1,
          totalPages: Math.ceil((current.totalCount + 1) / current.pageSize),
        } : current)
        setSummary((current) => current ? {
          ...current,
          total: current.total + 1,
          active: current.active + (created.status === 'Active' ? 1 : 0),
          onLeave: current.onLeave + (created.status === 'OnLeave' ? 1 : 0),
          stopped: current.stopped + (created.status === 'Inactive' || created.status === 'Terminated' ? 1 : 0),
        } : current)
        refresh('Đã tạo hồ sơ nhân viên.')
      }
      return null
    } catch (error) {
      return error instanceof Error ? error.message : 'Không thể lưu hồ sơ nhân viên.'
    }
  }

  const resetFilters = () => {
    setSearchInput('')
    setDepartmentInput('')
    setPositionInput('')
    updateUrlFilters({
      search: undefined,
      department: undefined,
      position: undefined,
      branchId: undefined,
      status: undefined,
      page: undefined,
    })
  }
  const displayedFrom = page && page.totalCount > 0 ? (page.pageNumber - 1) * page.pageSize + 1 : 0
  const displayedTo = page ? Math.min(page.pageNumber * page.pageSize, page.totalCount) : 0
  const hasFilters = Boolean(
    searchInput || departmentInput || positionInput || status !== 'all' || branchId !== 'all',
  )

  return (
    <>
      <div className="mx-auto w-full max-w-7xl px-5 py-10 md:px-12">
        <div className="mb-8 flex flex-col justify-between gap-4 xl:flex-row xl:items-end">
          <div>
            <p className="mb-2 text-xs font-bold tracking-widest text-primary uppercase">
              nhân sự và quyền truy cập
            </p>
            <h1 className="text-3xl font-bold tracking-tight">Hồ sơ nhân viên</h1>
            <p className="mt-2 text-sm text-muted-foreground">
              Staff là hồ sơ nhân sự, khác Access Account dùng đăng nhập và Member là độc giả.
            </p>
          </div>
          <div className="flex gap-2">
            <Button variant="outline" disabled={isLoading} loading={isLoading && Boolean(page)} loadingLabel="Đang tải lại nhân viên" onClick={() => refresh()}>
              <RefreshCw /> Làm mới
            </Button>
            <PermissionBoundary requiredPermissions={['employees.create']}>
              <Button onClick={openCreateForm}>
                <Plus /> Tạo hồ sơ
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
              <UserRoundCheck className="size-4 text-primary" /> {notice}
            </span>
            <Button variant="ghost" size="xs" onClick={() => setNotice('')}>
              Đóng
            </Button>
          </div>
        ) : null}

        <div className="mb-6 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <SummaryCard
            title="Tổng nhân viên"
            value={summary?.total ?? '—'}
            icon={BriefcaseBusiness}
          />
          <SummaryCard title="Đang làm việc" value={summary?.active ?? '—'} icon={UserRoundCheck} />
          <SummaryCard title="Đang nghỉ phép" value={summary?.onLeave ?? '—'} icon={CalendarDays} />
          <SummaryCard
            title="Tạm ngưng / nghỉ việc"
            value={summary?.stopped ?? '—'}
            icon={UserRoundX}
          />
        </div>

        <Card>
          <CardHeader>
            <CardTitle>Danh sách hồ sơ nhân viên</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="mb-5 grid min-w-0 grid-cols-1 gap-3 md:grid-cols-2 xl:grid-cols-3">
              <div className="relative min-w-0">
                <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  className="w-full min-w-0 pl-9"
                  aria-label="Tìm nhân viên"
                  value={searchInput}
                  onChange={(event) => setSearchInput(event.target.value)}
                  placeholder="Mã, tên, email, số điện thoại..."
                />
              </div>
              <Input
                className="w-full min-w-0"
                value={departmentInput}
                onChange={(event) => setDepartmentInput(event.target.value)}
                placeholder="Đơn vị"
                aria-label="Lọc theo đơn vị"
              />
              <Select
                value={branchId}
                onValueChange={(value) => updateUrlFilters({ branchId: value, page: undefined })}
              >
                <SelectTrigger className="w-full" aria-label="Lọc theo chi nhánh">
                  <SelectValue placeholder="Tất cả chi nhánh" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">Tất cả chi nhánh</SelectItem>
                  {branches.map((branch) => (
                    <SelectItem key={branch.id} value={branch.id}>
                      {branch.code} - {branch.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <Input
                className="w-full min-w-0"
                value={positionInput}
                onChange={(event) => setPositionInput(event.target.value)}
                placeholder="Chức vụ"
                aria-label="Lọc theo chức vụ"
              />
              <Select
                value={status}
                onValueChange={(value) => {
                  updateUrlFilters({ status: value, page: undefined })
                }}
              >
                <SelectTrigger className="w-full" aria-label="Lọc theo trạng thái">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">Tất cả trạng thái</SelectItem>
                  {employmentStatuses.map((employeeStatus) => (
                    <SelectItem key={employeeStatus} value={employeeStatus}>
                      {employmentStatusLabels[employeeStatus]}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <Button
                className="w-full"
                variant="outline"
                disabled={!hasFilters}
                onClick={resetFilters}
              >
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

            <div className="max-h-[60vh] overflow-auto rounded-lg border">
              <Table>
                <TableHeader className="sticky top-0 z-10 bg-background">
                  <TableRow>
                    <TableHead>Nhân viên</TableHead>
                    <TableHead>Việc làm</TableHead>
                    <TableHead>Liên hệ</TableHead>
                    <TableHead>Bắt đầu công tác</TableHead>
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
                        Không tìm thấy hồ sơ nhân viên phù hợp.
                      </TableCell>
                    </TableRow>
                  ) : null}
                  {page?.items.map((employee) => (
                    <TableRow
                      key={employee.id}
                      className={isLoading ? 'opacity-60' : 'cursor-pointer'}
                      onDoubleClick={() => openDetails(employee)}
                    >
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
                        <div className="grid min-w-44 gap-0.5">
                          <span>{employee.position}</span>
                          <small className="flex items-center gap-1 text-muted-foreground">
                            <Building2 className="size-3" />{' '}
                            {employee.branchName || employee.department}
                          </small>
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
                        <EmploymentStatusBadge status={employee.status} />
                      </TableCell>
                      <TableCell>
                        <div className="flex justify-end gap-1">
                          <Button
                            variant="ghost"
                            size="icon-sm"
                            aria-label={`Xem hồ sơ ${employee.fullName}`}
                            onClick={() => openDetails(employee)}
                          >
                            <Eye />
                          </Button>
                          <PermissionBoundary requiredPermissions={['employees.update']}>
                            <Button
                              variant="ghost"
                              size="icon-sm"
                              aria-label={`Cập nhật ${employee.fullName}`}
                              onClick={() => openEditForm(employee)}
                            >
                              <Pencil />
                            </Button>
                          </PermissionBoundary>
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
                  : 'Không có hồ sơ để hiển thị.'}
              </p>
              {page && page.totalPages > 0 ? (
                <Pagination
                  currentPage={page.pageNumber}
                  totalPages={page.totalPages}
                  totalCount={page.totalCount}
                  itemCount={page.items.length}
                  loading={isLoading}
                  onPageChange={(nextPage) => updateUrlFilters({ page: String(nextPage) })}
                  pageSize={pageSize}
                  onPageSizeChange={(size) => {
                    updateUrlFilters({ page: undefined, pageSize: String(size) })
                  }}
                />
              ) : null}
            </div>
          </CardContent>
        </Card>
      </div>

      <PermissionBoundary
        requiredPermissions={[editingEmployee ? 'employees.update' : 'employees.create']}
      >
        <EmployeeFormDialog
          key={editingEmployee
            ? `${editingEmployee.id}:${editingEmployee.email}:${editingEmployee.phoneNumber ?? ''}`
            : 'create-employee'}
          open={formOpen}
          employee={editingEmployee}
          onOpenChange={setFormOpen}
          onSave={handleSave}
        />
      </PermissionBoundary>
      <EmployeeDetailsDialog
        open={detailsOpen}
        employee={selectedEmployee}
        onOpenChange={setDetailsOpen}
        onEdit={openEditForm}
      />
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
        <div className="h-10 rounded-md bg-muted motion-safe:animate-pulse" />
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
