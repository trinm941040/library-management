import { z } from 'zod'
import { ApiError, authenticatedFetch, guidSchema, readResponse } from '@/auth/auth-api'

const EMPLOYEES_URL = '/api/v1/employees'

export const employmentStatuses = ['Active', 'OnLeave', 'Inactive', 'Terminated'] as const

export type EmploymentStatus = (typeof employmentStatuses)[number]

export const employmentStatusLabels: Record<EmploymentStatus, string> = {
  Active: 'Đang làm việc',
  OnLeave: 'Đang nghỉ phép',
  Inactive: 'Tạm ngưng',
  Terminated: 'Đã nghỉ việc',
}

const employmentStatusSchema = z.enum(employmentStatuses)
const employeeSchema = z.object({
  id: guidSchema,
  employeeCode: z.string(),
  fullName: z.string(),
  email: z.string(),
  phoneNumber: z.string().nullable(),
  dateOfBirth: z.string().nullable(),
  address: z.string().nullable(),
  position: z.string(),
  department: z.string(),
  branchId: guidSchema,
  branchCode: z.string(),
  branchName: z.string(),
  userId: guidSchema.nullable(),
  hireDate: z.string(),
  status: employmentStatusSchema,
  concurrencyToken: guidSchema,
  createdAtUtc: z.string(),
  updatedAtUtc: z.string(),
})
const employeePageSchema = z.object({
  items: z.array(employeeSchema),
  pageNumber: z.number().int().positive(),
  pageSize: z.number().int().positive(),
  totalCount: z.number().int().nonnegative(),
  totalPages: z.number().int().nonnegative(),
})
const employeeBranchSchema = z.object({ id: guidSchema, code: z.string(), name: z.string() })

export type Employee = z.infer<typeof employeeSchema>
export type EmployeePageResponse = z.infer<typeof employeePageSchema>

export type EmployeeSummary = {
  total: number
  active: number
  onLeave: number
  stopped: number
}

export type EmployeeBranch = z.infer<typeof employeeBranchSchema>

export type EmployeeFilters = {
  search?: string
  department?: string
  position?: string
  status?: EmploymentStatus
  branchId?: string
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
  deactivateLinkedAccount?: boolean
}

export async function getEmployees(
  filters: EmployeeFilters,
  signal?: AbortSignal,
  retryTransient = true,
): Promise<EmployeePageResponse> {
  const query = new URLSearchParams()
  if (filters.search) query.set('search', filters.search)
  if (filters.department) query.set('department', filters.department)
  if (filters.position) query.set('position', filters.position)
  if (filters.status) query.set('status', filters.status)
  if (filters.branchId) query.set('branchId', filters.branchId)
  query.set('pageNumber', String(filters.pageNumber ?? 1))
  query.set('pageSize', String(filters.pageSize ?? 20))

  try {
    const response = await authenticatedFetch(`${EMPLOYEES_URL}?${query}`, { signal })
    return await readResponse(response, employeePageSchema)
  } catch (error) {
    if (retryTransient && error instanceof ApiError && (error.status === 408 || error.status >= 500))
      return getEmployees(filters, signal, false)
    throw error
  }
}

export async function getEmployeeById(id: string, signal?: AbortSignal): Promise<Employee> {
  const response = await authenticatedFetch(`${EMPLOYEES_URL}/${id}`, { signal })
  return readResponse(response, employeeSchema)
}

export async function getEmployeeBranches(signal?: AbortSignal): Promise<EmployeeBranch[]> {
  const response = await authenticatedFetch(`${EMPLOYEES_URL}/branches`, { signal })
  return readResponse(response, z.array(employeeBranchSchema))
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
  return readResponse(response, employeeSchema)
}

export async function updateEmployee(id: string, input: SaveEmployeeInput): Promise<Employee> {
  const response = await authenticatedFetch(`${EMPLOYEES_URL}/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  return readResponse(response, employeeSchema)
}

export async function updateEmployeeStatus(
  id: string,
  status: EmploymentStatus,
  concurrencyToken: string,
  deactivateLinkedAccount = false,
): Promise<Employee> {
  const response = await authenticatedFetch(`${EMPLOYEES_URL}/${id}/status`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ status, concurrencyToken, deactivateLinkedAccount }),
  })
  return readResponse(response, employeeSchema)
}
