import { z } from 'zod'

const environmentSchema = z.object({
  VITE_API_BASE_URL: z.string().trim().url().or(z.literal('')).default(''),
  VITE_API_TIMEOUT_MS: z.coerce.number().int().positive().default(15000),
})
const environment = environmentSchema.parse(import.meta.env)
const apiUrl = (path: string) => `${environment.VITE_API_BASE_URL}${path}`
const AUTH_URL = '/api/v1/auth'
const guidSchema = z
  .string()
  .regex(/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i, 'GUID không hợp lệ')

const userSchema = z
  .object({
    userId: guidSchema,
    employeeId: guidSchema.nullable(),
    displayName: z.string(),
    loginIdentifier: z.string(),
    lastLoginAtUtc: z.string().nullable(),
    employeeCode: z.string().nullable(),
    fullName: z.string().nullable(),
    phoneNumber: z.string().nullable(),
    dateOfBirth: z.string().nullable(),
    address: z.string().nullable(),
    position: z.string().nullable(),
    department: z.string().nullable(),
    employmentStatus: z.string().nullable(),
    branch: z.object({ id: guidSchema, code: z.string(), name: z.string() }).nullable(),
    roles: z.array(z.string()),
    permissions: z.array(z.string()),
    rowVersion: guidSchema.nullable(),
  })
  .transform((value) => ({ ...value, id: value.userId }))
export type User = z.infer<typeof userSchema>
const sessionSchema = z.object({
  accessToken: z.string().min(1),
  accessTokenExpiresAtUtc: z.iso.datetime({ offset: true }),
  currentUser: userSchema,
})
export const loginSchema = z.object({
  email: z.string().trim().email('Email không hợp lệ.').max(256),
  password: z.string().min(1, 'Vui lòng nhập mật khẩu.').max(1024, 'Mật khẩu quá dài.'),
})

export class ApiError extends Error {
  readonly status: number
  readonly fieldErrors: Record<string, string[]>

  constructor(message: string, status: number, fieldErrors: Record<string, string[]> = {}) {
    super(message)
    this.status = status
    this.fieldErrors = fieldErrors
  }
}

async function request(input: RequestInfo | URL, init: RequestInit = {}) {
  init.signal?.throwIfAborted()
  const controller = new AbortController()
  const timeout = window.setTimeout(
    () => controller.abort(new DOMException('Request timed out', 'TimeoutError')),
    environment.VITE_API_TIMEOUT_MS,
  )
  const abort = () => controller.abort(init.signal?.reason)
  init.signal?.addEventListener('abort', abort, { once: true })
  const headers = new Headers(init.headers)
  headers.set('X-Requested-With', 'XMLHttpRequest')
  try {
    return await fetch(typeof input === 'string' && input.startsWith('/') ? apiUrl(input) : input, {
      ...init,
      headers,
      cache: 'no-store',
      signal: controller.signal,
    })
  } catch (error) {
    if (controller.signal.aborted && !init.signal?.aborted)
      throw new ApiError('Yêu cầu đã quá thời gian chờ.', 408)
    throw error
  } finally {
    window.clearTimeout(timeout)
    init.signal?.removeEventListener('abort', abort)
  }
}

let accessToken: string | null = null
let refreshPromise: Promise<User> | null = null
let profilePromise: Promise<User> | null = null
let sessionGeneration = 0
const sessionListeners = new Set<(user: User | null) => void>()

export function subscribeSession(listener: (user: User | null) => void) {
  sessionListeners.add(listener)
  return () => {
    sessionListeners.delete(listener)
  }
}

function publishSession(user: User | null) {
  sessionListeners.forEach((listener) => listener(user))
}

function assertGeneration(generation: number) {
  if (generation !== sessionGeneration) throw new DOMException('Phiên đã thay đổi.', 'AbortError')
}

async function withSessionLock<T>(operation: () => Promise<T>): Promise<T> {
  // Tabs share the HttpOnly cookie. Serialize its rotation without sharing tokens.
  return navigator.locks ? navigator.locks.request('uth-session-cookie', operation) : operation()
}

export async function readResponse<T>(response: Response, schema?: z.ZodType<T>): Promise<T> {
  if (response.ok) {
    const data = (await response.json()) as unknown
    if (!schema) return data as T
    const parsed = schema.safeParse(data)
    if (!parsed.success) throw new ApiError('Phản hồi máy chủ không hợp lệ.', 502)
    return parsed.data
  }
  const problem = (await response.json().catch(() => null)) as {
    title?: string
    detail?: string
    errors?: Record<string, string[]>
  } | null
  throw new ApiError(
    problem?.detail ?? problem?.title ?? 'Không thể kết nối đến máy chủ.',
    response.status,
    problem?.errors,
  )
}

async function saveSession(response: Response, generation: number) {
  const session = await readResponse(response, sessionSchema)
  assertGeneration(generation)
  accessToken = session.accessToken
  publishSession(session.currentUser)
  return session.currentUser
}

function refreshAccessToken() {
  if (!refreshPromise) {
    const generation = sessionGeneration
    refreshPromise = withSessionLock(async () => {
      assertGeneration(generation)
      return saveSession(
        await request(`${AUTH_URL}/refresh`, { method: 'POST', credentials: 'include' }),
        generation,
      )
    })
      .catch((error: unknown) => {
        if (generation === sessionGeneration && error instanceof ApiError && error.status === 401)
          clearLocalSession()
        throw error
      })
      .finally(() => {
        refreshPromise = null
      })
  }
  return refreshPromise
}

export async function authenticatedFetch(input: RequestInfo | URL, init?: RequestInit) {
  const generation = sessionGeneration
  init?.signal?.throwIfAborted()
  const initialToken = accessToken
  const send = () => {
    assertGeneration(generation)
    const headers = new Headers(init?.headers)
    if (accessToken) headers.set('Authorization', `Bearer ${accessToken}`)
    return request(input, { ...init, headers, credentials: 'include' })
  }
  let response = await send()
  assertGeneration(generation)
  if (response.status === 403) window.dispatchEvent(new Event('auth:authorization-stale'))
  if (response.status !== 401) return response
  init?.signal?.throwIfAborted()
  // A late 401 may belong to the token already replaced by another request.
  if (accessToken === initialToken) await refreshAccessToken()
  init?.signal?.throwIfAborted()
  response = await send()
  assertGeneration(generation)
  if (response.status === 403) window.dispatchEvent(new Event('auth:authorization-stale'))
  if (response.status === 401) {
    clearLocalSession()
  }
  return response
}

export function getProfile() {
  if (!profilePromise) {
    const generation = sessionGeneration
    profilePromise = (async () => {
      const user = await readResponse(await authenticatedFetch('/api/v1/me'), userSchema)
      assertGeneration(generation)
      publishSession(user)
      return user
    })().finally(() => {
      profilePromise = null
    })
  }
  return profilePromise
}

export async function authorizationChanged() {
  window.dispatchEvent(new Event('auth:authorization-changed'))
  if (profilePromise) await profilePromise.catch(() => undefined)
  try {
    await getProfile()
  } catch {
    clearLocalSession()
  }
}

export async function login(email: string, password: string) {
  const input = loginSchema.parse({ email, password })
  clearLocalSession()
  const generation = sessionGeneration
  await refreshPromise?.catch(() => undefined)
  return withSessionLock(async () => {
    assertGeneration(generation)
    const response = await request(`${AUTH_URL}/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify(input),
    })
    return saveSession(response, generation)
  })
}
export function restoreSession() {
  return refreshAccessToken()
}
export function clearLocalSession() {
  sessionGeneration += 1
  accessToken = null
  publishSession(null)
}
export async function logout() {
  clearLocalSession()
  await refreshPromise?.catch(() => undefined)
  await withSessionLock(async () => {
    const response = await request(`${AUTH_URL}/logout`, { method: 'POST', credentials: 'include' })
    if (!response.ok) await readResponse(response)
  })
}
export { guidSchema, userSchema }
