import { z } from 'zod'

const AUTH_URL = '/api/v1/auth'
const guidSchema = z.string().regex(
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i,
  'GUID không hợp lệ',
)

const tokenSchema = z.object({ accessToken: z.string().min(1), accessTokenExpiresAtUtc: z.string() })
const userSchema = z.object({
  userId: guidSchema, employeeId: guidSchema.nullable(), displayName: z.string(),
  loginIdentifier: z.string(), lastLoginAtUtc: z.string().nullable(), employeeCode: z.string().nullable(),
  fullName: z.string().nullable(), phoneNumber: z.string().nullable(), dateOfBirth: z.string().nullable(),
  address: z.string().nullable(), position: z.string().nullable(), department: z.string().nullable(),
  employmentStatus: z.string().nullable(),
  branch: z.object({ id: guidSchema, code: z.string(), name: z.string() }).nullable(),
  roles: z.array(z.string()), permissions: z.array(z.string()), rowVersion: guidSchema.nullable(),
}).transform((value) => ({ ...value, id: value.userId }))
export type User = z.infer<typeof userSchema>

export class ApiError extends Error {
  readonly status: number
  readonly fieldErrors: Record<string, string[]>

  constructor(message: string, status: number, fieldErrors: Record<string, string[]> = {}) {
    super(message)
    this.status = status
    this.fieldErrors = fieldErrors
  }
}

let accessToken: string | null = null
let refreshPromise: Promise<void> | null = null

export async function readResponse<T>(response: Response, schema?: z.ZodType<T>): Promise<T> {
  if (response.ok) {
    const data = (await response.json()) as unknown
    return schema ? schema.parse(data) : (data as T)
  }
  const problem = (await response.json().catch(() => null)) as { title?: string; detail?: string; errors?: Record<string, string[]> } | null
  throw new ApiError(problem?.detail ?? problem?.title ?? 'Không thể kết nối đến máy chủ.', response.status, problem?.errors)
}

async function saveToken(response: Response) {
  accessToken = (await readResponse(response, tokenSchema)).accessToken
}
async function refreshAccessToken() {
  await saveToken(await fetch(`${AUTH_URL}/refresh`, { method: 'POST', credentials: 'include' }))
}

export async function authenticatedFetch(input: RequestInfo | URL, init?: RequestInit) {
  const send = () => {
    const headers = new Headers(init?.headers)
    if (accessToken) headers.set('Authorization', `Bearer ${accessToken}`)
    return fetch(input, { ...init, headers, credentials: 'include' })
  }
  let response = await send()
  if (response.status !== 401) return response
  try {
    refreshPromise ??= refreshAccessToken().finally(() => (refreshPromise = null))
    await refreshPromise
  } catch (error) {
    clearLocalSession()
    window.dispatchEvent(new Event('auth:expired'))
    throw error
  }
  response = await send()
  if (response.status === 401) {
    clearLocalSession()
    window.dispatchEvent(new Event('auth:expired'))
  }
  return response
}

export async function getProfile() { return readResponse(await authenticatedFetch('/api/v1/me'), userSchema) }
export async function login(email: string, password: string) {
  const response = await fetch(`${AUTH_URL}/login`, {
    method: 'POST', headers: { 'Content-Type': 'application/json' }, credentials: 'include',
    body: JSON.stringify({ email, password }),
  })
  await saveToken(response)
  return getProfile()
}
export async function restoreSession() { await refreshAccessToken(); return getProfile() }
export function clearLocalSession() { accessToken = null }
export async function logout() {
  try { await fetch(`${AUTH_URL}/logout`, { method: 'POST', credentials: 'include' }) }
  finally { clearLocalSession() }
}
export { guidSchema, userSchema }
