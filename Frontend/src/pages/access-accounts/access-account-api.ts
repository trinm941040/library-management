import { z } from 'zod'
import { authenticatedFetch, authorizationChanged, guidSchema, readResponse } from '@/auth/auth-api'

const employeeSchema = z.object({
  id: guidSchema,
  employeeCode: z.string(),
  fullName: z.string(),
  email: z.string(),
  employmentStatus: z.string(),
})
const accountSchema = z.object({
  id: guidSchema,
  email: z.string(),
  displayName: z.string(),
  isActive: z.boolean(),
  emailConfirmed: z.boolean(),
  createdAtUtc: z.string(),
  lastLoginAtUtc: z.string().nullable(),
  roles: z.array(z.string()),
  employee: employeeSchema,
  isProtected: z.boolean(),
})
const accountPageSchema = z.object({
  items: z.array(accountSchema),
  pageNumber: z.number().int().positive(),
  pageSize: z.number().int().positive(),
  totalCount: z.number().int().nonnegative(),
  totalPages: z.number().int().nonnegative(),
})
const eligibleEmployeeSchema = employeeSchema.omit({ employmentStatus: true })
const sessionSchema = z.object({
  id: guidSchema,
  createdAtUtc: z.string(),
  expiresAtUtc: z.string(),
  usedAtUtc: z.string().nullable(),
  revokedAtUtc: z.string().nullable(),
  createdByIp: z.string().nullable(),
  userAgent: z.string().nullable(),
  revocationReason: z.string().nullable(),
  isActive: z.boolean(),
})

export type AccessAccount = z.infer<typeof accountSchema>
export type AccessAccountPage = z.infer<typeof accountPageSchema>
export type EligibleEmployee = z.infer<typeof eligibleEmployeeSchema>
export type AccountSession = z.infer<typeof sessionSchema>

const baseUrl = '/api/v1/access-accounts'
const mutation = async <T>(response: Response, schema?: z.ZodType<T>) => {
  const value = await readResponse(response, schema)
  await authorizationChanged()
  return value
}

export async function getAccessAccounts(
  filters: { search?: string; isActive?: boolean; pageNumber: number; pageSize: number },
  signal?: AbortSignal,
) {
  const query = new URLSearchParams()
  if (filters.search) query.set('search', filters.search)
  if (filters.isActive !== undefined) query.set('isActive', String(filters.isActive))
  query.set('pageNumber', String(filters.pageNumber))
  query.set('pageSize', String(filters.pageSize))
  return readResponse(await authenticatedFetch(`${baseUrl}?${query}`, { signal }), accountPageSchema)
}

export async function getAccessAccount(id: string, signal?: AbortSignal) {
  return readResponse(await authenticatedFetch(`${baseUrl}/${id}`, { signal }), accountSchema)
}

export async function getEligibleEmployees(search?: string, signal?: AbortSignal) {
  const query = search ? `?search=${encodeURIComponent(search)}` : ''
  return readResponse(
    await authenticatedFetch(`${baseUrl}/eligible-employees${query}`, { signal }),
    z.array(eligibleEmployeeSchema),
  )
}

export async function createAccessAccount(input: {
  employeeId: string
  email: string
  displayName: string
  password: string
  roleIds: string[]
}) {
  return mutation(await authenticatedFetch(baseUrl, {
    method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(input),
  }), accountSchema)
}

export async function setAccessAccountStatus(id: string, isActive: boolean) {
  return mutation(await authenticatedFetch(`${baseUrl}/${id}/status`, {
    method: 'PATCH', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ isActive }),
  }), accountSchema)
}

export async function resetAccessAccountPassword(id: string, temporaryPassword: string) {
  await mutation(await authenticatedFetch(`${baseUrl}/${id}/reset-password`, {
    method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ temporaryPassword }),
  }))
}

export async function getAccessAccountSessions(id: string, signal?: AbortSignal) {
  return readResponse(await authenticatedFetch(`${baseUrl}/${id}/sessions`, { signal }), z.array(sessionSchema))
}

export async function revokeAccessAccountSession(id: string, sessionId: string) {
  await mutation(await authenticatedFetch(`${baseUrl}/${id}/sessions/${sessionId}`, { method: 'DELETE' }))
}

export async function replaceAccessAccountRoles(id: string, roleIds: string[]) {
  await mutation(await authenticatedFetch(`${baseUrl}/${id}/roles`, {
    method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ roleIds }),
  }))
}
