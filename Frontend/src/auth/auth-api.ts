const AUTH_URL = '/api/v1/auth'

type TokenResponse = {
  accessToken: string
  accessTokenExpiresAtUtc: string
}

export type User = {
  id: string
  email: string
  displayName: string
  roles: string[]
}

let accessToken: string | null = null

async function readResponse<T>(response: Response): Promise<T> {
  if (response.ok) {
    return response.json() as Promise<T>
  }

  const problem = (await response.json().catch(() => null)) as {
    title?: string
    detail?: string
  } | null
  throw new Error(problem?.title ?? problem?.detail ?? 'Không thể kết nối đến máy chủ.')
}

async function saveToken(response: Response) {
  const data = await readResponse<TokenResponse>(response)
  accessToken = data.accessToken
}

async function getProfile(): Promise<User> {
  const response = await fetch(`${AUTH_URL}/me`, {
    headers: { Authorization: `Bearer ${accessToken}` },
    credentials: 'include',
  })
  return readResponse<User>(response)
}

export async function login(email: string, password: string): Promise<User> {
  const response = await fetch(`${AUTH_URL}/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify({ email, password }),
  })

  await saveToken(response)
  return getProfile()
}

export async function restoreSession(): Promise<User> {
  const response = await fetch(`${AUTH_URL}/refresh`, {
    method: 'POST',
    credentials: 'include',
  })

  await saveToken(response)
  return getProfile()
}

export async function logout(): Promise<void> {
  await fetch(`${AUTH_URL}/logout`, {
    method: 'POST',
    credentials: 'include',
  }).catch(() => undefined)

  accessToken = null
}
