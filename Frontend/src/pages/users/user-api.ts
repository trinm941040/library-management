import { authenticatedFetch } from '@/auth/auth-api'

const USERS_URL = '/api/v1/users'

export type SystemUser = {
  id: string
  email: string
  displayName: string
  isActive: boolean
  emailConfirmed: boolean
  createdAtUtc: string
  lastLoginAtUtc: string | null
  roles: string[]
}

export type UserPageResponse = {
  items: SystemUser[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export type UserFilters = {
  search?: string
  isActive?: boolean
  role?: string
  pageNumber?: number
  pageSize?: number
}

export type CreateUserInput = {
  email: string
  password: string
  displayName: string
}

export type UpdateUserInput = {
  email: string
  displayName: string
}

type ProblemDetails = {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

export class UserApiError extends Error {
  readonly status: number

  constructor(message: string, status: number) {
    super(message)
    this.name = 'UserApiError'
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

  throw new UserApiError(
    validationMessage ?? problem?.detail ?? problem?.title ?? 'Không thể xử lý yêu cầu.',
    response.status,
  )
}

export async function getUsers(
  filters: UserFilters,
  signal?: AbortSignal,
): Promise<UserPageResponse> {
  const query = new URLSearchParams()
  if (filters.search) query.set('search', filters.search)
  if (filters.isActive !== undefined) query.set('isActive', String(filters.isActive))
  if (filters.role) query.set('role', filters.role)
  query.set('pageNumber', String(filters.pageNumber ?? 1))
  query.set('pageSize', String(filters.pageSize ?? 20))

  const response = await authenticatedFetch(`${USERS_URL}?${query}`, { signal })
  return readResponse<UserPageResponse>(response)
}

export async function createUser(input: CreateUserInput): Promise<SystemUser> {
  const response = await authenticatedFetch(USERS_URL, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  return readResponse<SystemUser>(response)
}

export async function updateUser(id: string, input: UpdateUserInput): Promise<SystemUser> {
  const response = await authenticatedFetch(`${USERS_URL}/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  return readResponse<SystemUser>(response)
}

export async function deactivateUser(id: string): Promise<void> {
  const response = await authenticatedFetch(`${USERS_URL}/${id}`, { method: 'DELETE' })
  await readResponse<void>(response)
}
