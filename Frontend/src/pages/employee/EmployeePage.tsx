import { useCallback, useEffect, useState, type ReactNode } from 'react'
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
import {
  createEmployee,
  employmentStatusLabels,
  employmentStatuses,
  getEmployees,
  getEmployeeSummary,
  updateEmployee,
  type Employee,
  type EmployeePageResponse,
  type EmployeeSummary,
  type EmploymentStatus,
  type SaveEmployeeInput,
} from './employee-api'

const EMPLOYEES_PER_PAGE = 20
type StatusFilter = 'all' | EmploymentStatus

export function EmployeePage() {
  const [page, setPage] = useState<EmployeePageResponse | null>(null)
  const [summary, setSummary] = useState<EmployeeSummary | null>(null)
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
  const [detailsOpen, setDetailsOpen] = useState(false)
  const [selectedEmployee, setSelectedEmployee] = useState<Employee | null>(null)
  const [editingEmployee, setEditingEmployee] = useState<Employee | null>(null)

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
        if (response.totalPages > 0 && currentPage > response.totalPages)
          setCurrentPage(response.totalPages)
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
  }, [currentPage, department, position, reloadKey, search, status])

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
    setEditingEmployee(employee)
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
          branchId: editingEmployee.branchId,
          concurrencyToken: editingEmployee.concurrencyToken,
        })
        setSelectedEmployee((current) => (current?.id === updated.id ? updated : current))
        refresh('Đã cập nhật hồ sơ và thông tin việc làm.')
      } else {
        await createEmployee(data)
        setCurrentPage(1)
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
              nhân sự và quyền truy cập
            </p>
            <h1 className="text-3xl font-bold tracking-tight">Quản lý nhân viên</h1>
            <p className="mt-2 text-sm text-muted-foreground">
              Quản lý hồ sơ, đơn vị công tác, trạng thái việc làm và tài khoản truy cập.
            </p>
          </div>
          <div className="flex gap-2">
            <Button variant="outline" disabled={isLoading} onClick={() => refresh()}>
              <RefreshCw className={isLoading ? 'animate-spin' : ''} /> Làm mới
            </Button>
            <Button onClick={openCreateForm}>
              <Plus /> Tạo hồ sơ
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
                placeholder="Đơn vị hoặc chi nhánh"
                aria-label="Lọc theo đơn vị hoặc chi nhánh"
              />
              <Input
                value={positionInput}
                onChange={(event) => setPositionInput(event.target.value)}
                placeholder="Chức vụ"
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
                  {employmentStatuses.map((employeeStatus) => (
                    <SelectItem key={employeeStatus} value={employeeStatus}>
                      {employmentStatusLabels[employeeStatus]}
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
                          <Button
                            variant="ghost"
                            size="icon-sm"
                            aria-label={`Cập nhật ${employee.fullName}`}
                            onClick={() => openEditForm(employee)}
                          >
                            <Pencil />
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
                  : 'Không có hồ sơ để hiển thị.'}
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
