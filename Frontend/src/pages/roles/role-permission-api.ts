import { authenticatedFetch, authorizationChanged } from '@/auth/auth-api'

const ROLES_URL = '/api/v1/roles'
const PERMISSIONS_URL = '/api/v1/permissions'

export type Permission = {
  id: string
  name: string
  description: string
  module: string
  createdAtUtc: string
}

export type PermissionSummary = Omit<Permission, 'createdAtUtc'>

export type Role = {
  id: string
  name: string
  description: string
  isSystemRole: boolean
  isActive: boolean
  createdAtUtc: string
  permissions: PermissionSummary[]
}

export type RoleInput = {
  name: string
  description: string
}

export type PermissionInput = {
  name: string
  description: string
  module: string
}

type ProblemDetails = {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

export class RolePermissionApiError extends Error {
  readonly status: number

  constructor(message: string, status: number) {
    super(message)
    this.name = 'RolePermissionApiError'
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

  throw new RolePermissionApiError(
    validationMessage ?? problem?.detail ?? problem?.title ?? 'Không thể xử lý yêu cầu.',
    response.status,
  )
}

async function readMutation<T>(response: Response): Promise<T> {
  const result = await readResponse<T>(response)
  await authorizationChanged()
  return result
}

function withQuery(url: string, values: Record<string, string | undefined>) {
  const query = new URLSearchParams()
  Object.entries(values).forEach(([key, value]) => {
    if (value) query.set(key, value)
  })
  const suffix = query.toString()
  return suffix ? `${url}?${suffix}` : url
}

export async function getRoles(search?: string, signal?: AbortSignal): Promise<Role[]> {
  const response = await authenticatedFetch(withQuery(ROLES_URL, { search }), { signal })
  return readResponse<Role[]>(response)
}

export async function createRole(input: RoleInput): Promise<Role> {
  const response = await authenticatedFetch(ROLES_URL, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  return readResponse<Role>(response)
}

export async function updateRole(id: string, input: RoleInput): Promise<Role> {
  const response = await authenticatedFetch(`${ROLES_URL}/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  return readMutation<Role>(response)
}

export async function deleteRole(id: string): Promise<void> {
  const response = await authenticatedFetch(`${ROLES_URL}/${id}`, { method: 'DELETE' })
  await readMutation<void>(response)
}

export async function replaceRolePermissions(
  roleId: string,
  permissionIds: string[],
): Promise<Role> {
  const response = await authenticatedFetch(`${ROLES_URL}/${roleId}/permissions`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ permissionIds }),
  })
  return readMutation<Role>(response)
}

export async function replaceUserRoles(userId: string, roleIds: string[]): Promise<void> {
  const response = await authenticatedFetch(`/api/v1/users/${userId}/roles`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ roleIds }),
  })
  await readMutation<void>(response)
}

export async function getPermissions(
  filters: { search?: string; module?: string } = {},
  signal?: AbortSignal,
): Promise<Permission[]> {
  const response = await authenticatedFetch(withQuery(PERMISSIONS_URL, filters), { signal })
  return readResponse<Permission[]>(response)
}

export async function createPermission(input: PermissionInput): Promise<Permission> {
  const response = await authenticatedFetch(PERMISSIONS_URL, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  return readResponse<Permission>(response)
}

export async function updatePermission(id: string, input: PermissionInput): Promise<Permission> {
  const response = await authenticatedFetch(`${PERMISSIONS_URL}/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  return readMutation<Permission>(response)
}

export async function deletePermission(id: string): Promise<void> {
  const response = await authenticatedFetch(`${PERMISSIONS_URL}/${id}`, { method: 'DELETE' })
  await readMutation<void>(response)
}

const systemPermissionNames = new Set([
  'users.read',
  'users.create',
  'users.update',
  'users.deactivate',
  'roles.read',
  'roles.create',
  'roles.update',
  'roles.delete',
  'roles.assign',
  'permissions.read',
  'permissions.create',
  'permissions.update',
  'permissions.delete',
  'todos.read',
  'todos.create',
  'todos.update',
  'todos.delete',
])

export function isSystemPermission(permission: Permission) {
  return systemPermissionNames.has(permission.name)
}
