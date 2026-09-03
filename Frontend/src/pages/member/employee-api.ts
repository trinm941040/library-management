import { authenticatedFetch } from '@/auth/auth-api'

const EMPLOYEES_URL = '/api/v1/employees'

export const employmentStatuses = ['Active', 'OnLeave', 'Inactive', 'Terminated'] as const

export type EmploymentStatus = (typeof employmentStatuses)[number]

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
  hireDate: string
  status: EmploymentStatus
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

export async function deleteEmployee(id: string): Promise<void> {
  const response = await authenticatedFetch(`${EMPLOYEES_URL}/${id}`, { method: 'DELETE' })
  await readResponse<void>(response)
}
