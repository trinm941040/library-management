import { authenticatedFetch, authorizationChanged, guidSchema } from '@/auth/auth-api'
import { z } from 'zod'

const ROLES_URL = '/api/v1/roles'
const PERMISSIONS_URL = '/api/v1/permissions'

export type Permission = {
  id: string
  name: string
  description: string
  module: string
  createdAtUtc: string
  isSystem: boolean
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
  isActive?: boolean
}

export type PermissionInput = {
  name: string
  description: string
  module: string
}

export type PageResult<T> = {
  items: T[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
}

const permissionSummarySchema = z.object({
  id: guidSchema,
  name: z.string(),
  description: z.string(),
  module: z.string(),
  isSystem: z.boolean(),
})
const permissionSchema = permissionSummarySchema.extend({ createdAtUtc: z.string() })
const roleSchema = z.object({
  id: guidSchema,
  name: z.string(),
  description: z.string(),
  isSystemRole: z.boolean(),
  isActive: z.boolean(),
  createdAtUtc: z.string(),
  permissions: z.array(permissionSummarySchema),
})
const createPageSchema = <T extends z.ZodTypeAny>(itemSchema: T) =>
  z.object({
    items: z.array(itemSchema),
    pageNumber: z.number().int().positive(),
    pageSize: z.number().int().positive(),
    totalCount: z.number().int().nonnegative(),
    totalPages: z.number().int().nonnegative(),
  })
const rolePageSchema = createPageSchema(roleSchema)
const permissionPageSchema = createPageSchema(permissionSchema)

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

async function readResponse<T>(response: Response, schema?: z.ZodType<T>): Promise<T> {
  if (response.ok) {
    if (response.status === 204) return undefined as T
    const payload = (await response.json()) as unknown
    if (!schema) return payload as T
    const parsed = schema.safeParse(payload)
    if (!parsed.success) {
      throw new RolePermissionApiError('Phản hồi máy chủ không hợp lệ.', 502)
    }
    return parsed.data
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

async function readMutation<T>(response: Response, schema?: z.ZodType<T>): Promise<T> {
  const result = await readResponse<T>(response, schema)
  await authorizationChanged()
  return result
}

function withQuery(url: string, values: Record<string, string | number | undefined>) {
  const query = new URLSearchParams()
  Object.entries(values).forEach(([key, value]) => {
    if (value !== undefined && value !== '') query.set(key, String(value))
  })
  const suffix = query.toString()
  return suffix ? `${url}?${suffix}` : url
}

export async function getRoles(
  filters: { search?: string; pageNumber?: number; pageSize?: number } = {},
  signal?: AbortSignal,
): Promise<PageResult<Role>> {
  const response = await authenticatedFetch(withQuery(ROLES_URL, filters), { signal })
  return readResponse<PageResult<Role>>(response, rolePageSchema)
}

export async function getAllRoles(signal?: AbortSignal): Promise<Role[]> {
  const firstPage = await getRoles({ pageNumber: 1, pageSize: 100 }, signal)
  const roles = [...firstPage.items]
  for (let pageNumber = 2; pageNumber <= firstPage.totalPages; pageNumber += 1) {
    const page = await getRoles({ pageNumber, pageSize: 100 }, signal)
    roles.push(...page.items)
  }
  return roles
}

export async function createRole(input: RoleInput): Promise<Role> {
  const response = await authenticatedFetch(ROLES_URL, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  return readMutation<Role>(response, roleSchema)
}

export async function updateRole(id: string, input: RoleInput): Promise<Role> {
  const response = await authenticatedFetch(`${ROLES_URL}/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  return readMutation<Role>(response, roleSchema)
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
  return readMutation<Role>(response, roleSchema)
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
  filters: { search?: string; module?: string; pageNumber?: number; pageSize?: number } = {},
  signal?: AbortSignal,
): Promise<PageResult<Permission>> {
  const response = await authenticatedFetch(withQuery(PERMISSIONS_URL, filters), { signal })
  return readResponse<PageResult<Permission>>(response, permissionPageSchema)
}

export async function getAllPermissions(signal?: AbortSignal): Promise<Permission[]> {
  const firstPage = await getPermissions({ pageNumber: 1, pageSize: 100 }, signal)
  const permissions = [...firstPage.items]
  for (let pageNumber = 2; pageNumber <= firstPage.totalPages; pageNumber += 1) {
    const page = await getPermissions({ pageNumber, pageSize: 100 }, signal)
    permissions.push(...page.items)
  }
  return permissions
}

export async function getPermissionModules(signal?: AbortSignal): Promise<string[]> {
  const response = await authenticatedFetch(`${PERMISSIONS_URL}/modules`, { signal })
  return readResponse<string[]>(response, z.array(z.string()))
}

export async function createPermission(input: PermissionInput): Promise<Permission> {
  const response = await authenticatedFetch(PERMISSIONS_URL, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  return readMutation<Permission>(response, permissionSchema)
}

export async function updatePermission(id: string, input: PermissionInput): Promise<Permission> {
  const response = await authenticatedFetch(`${PERMISSIONS_URL}/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  return readMutation<Permission>(response, permissionSchema)
}

export async function deletePermission(id: string): Promise<void> {
  const response = await authenticatedFetch(`${PERMISSIONS_URL}/${id}`, { method: 'DELETE' })
  await readMutation<void>(response)
}

export function isSystemPermission(permission: Permission) {
  return permission.isSystem
}
