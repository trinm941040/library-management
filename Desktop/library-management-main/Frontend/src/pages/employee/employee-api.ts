import { authenticatedFetch } from '@/auth/auth-api'

const EMPLOYEES_URL = '/api/v1/employees'

export const employmentStatuses = ['Active', 'OnLeave', 'Inactive', 'Terminated'] as const

export type EmploymentStatus = (typeof employmentStatuses)[number]

export const employmentStatusLabels: Record<EmploymentStatus, string> = {
  Active: 'Đang làm việc',
  OnLeave: 'Đang nghỉ phép',
  Inactive: 'Tạm ngưng',
  Terminated: 'Đã nghỉ việc',
}

export type Employee = {
  id: string
  employeeCode: string
  fullName: string
  email: string
  phoneNumber: string | null
  dateOfBirth: string | null
  address: string | null
  position: string
  department: string
  branchId: string
  branchCode: string
  branchName: string
  userId: string | null
  hireDate: string
  status: EmploymentStatus
  concurrencyToken: string
  createdAtUtc: string
  updatedAtUtc: string
}

export type EmployeePageResponse = {
  items: Employee[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export type EmployeeSummary = {
  total: number
  active: number
  onLeave: number
  stopped: number
}

export type EmployeeBranch = {
  id: string
  code: string
  name: string
}

export type EmployeeFilters = {
  search?: string
  department?: string
  position?: string
  status?: EmploymentStatus
  pageNumber?: number
  pageSize?: number
}

export type SaveEmployeeInput = {
  employeeCode: string
  fullName: string
  email: string
  phoneNumber: string | null
  dateOfBirth: string | null
  address: string | null
  position: string
  department: string
  hireDate: string
  status: EmploymentStatus
  branchId?: string
  concurrencyToken?: string
}

type ProblemDetails = {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

export class EmployeeApiError extends Error {
  readonly status: number

  constructor(message: string, status: number) {
    super(message)
    this.name = 'EmployeeApiError'
    this.status = status
  }
}

async function readResponse<T>(response: Response): Promise<T> {
  if (response.ok) {
    if (response.status === 204) return undefined as T
    return response.json() as Promise<T>
  }

  const problem = (await response.json().catch(() => null)) as ProblemDetails | null
  const validationMessage = problem?.errors
    ? Object.values(problem.errors).flat().find(Boolean)
    : undefined

  throw new EmployeeApiError(
    validationMessage ?? problem?.detail ?? problem?.title ?? 'Không thể xử lý yêu cầu.',
    response.status,
  )
}

export async function getEmployees(
  filters: EmployeeFilters,
  signal?: AbortSignal,
): Promise<EmployeePageResponse> {
  const query = new URLSearchParams()
  if (filters.search) query.set('search', filters.search)
  if (filters.department) query.set('department', filters.department)
  if (filters.position) query.set('position', filters.position)
  if (filters.status) query.set('status', filters.status)
  query.set('pageNumber', String(filters.pageNumber ?? 1))
  query.set('pageSize', String(filters.pageSize ?? 20))

  const response = await authenticatedFetch(`${EMPLOYEES_URL}?${query}`, { signal })
  return readResponse<EmployeePageResponse>(response)
}

export async function getEmployeeById(id: string, signal?: AbortSignal): Promise<Employee> {
  const response = await authenticatedFetch(`${EMPLOYEES_URL}/${id}`, { signal })
  return readResponse<Employee>(response)
}

export async function getEmployeeBranches(signal?: AbortSignal): Promise<EmployeeBranch[]> {
  const response = await authenticatedFetch(`${EMPLOYEES_URL}/branches`, { signal })
  return readResponse<EmployeeBranch[]>(response)
}

export async function getEmployeeSummary(signal?: AbortSignal): Promise<EmployeeSummary> {
  const [all, active, onLeave, inactive, terminated] = await Promise.all([
    getEmployees({ pageNumber: 1, pageSize: 1 }, signal),
    getEmployees({ status: 'Active', pageNumber: 1, pageSize: 1 }, signal),
    getEmployees({ status: 'OnLeave', pageNumber: 1, pageSize: 1 }, signal),
    getEmployees({ status: 'Inactive', pageNumber: 1, pageSize: 1 }, signal),
    getEmployees({ status: 'Terminated', pageNumber: 1, pageSize: 1 }, signal),
  ])

  return {
    total: all.totalCount,
    active: active.totalCount,
    onLeave: onLeave.totalCount,
    stopped: inactive.totalCount + terminated.totalCount,
  }
}

export async function createEmployee(input: SaveEmployeeInput): Promise<Employee> {
  const response = await authenticatedFetch(EMPLOYEES_URL, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  return readResponse<Employee>(response)
}

export async function updateEmployee(id: string, input: SaveEmployeeInput): Promise<Employee> {
  const response = await authenticatedFetch(`${EMPLOYEES_URL}/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  return readResponse<Employee>(response)
}
