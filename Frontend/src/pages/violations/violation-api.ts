import { authenticatedFetch } from '@/auth/auth-api'

const VIOLATIONS_URL = '/api/v1/violations'

export type LibraryViolation = {
  id: string
  borrowerId: string
  borrowerName: string
  borrowerEmail: string
  bookId: string | null
  bookTitle: string
  type: 'overdue' | 'damage' | 'lost' | 'other' | string
  note: string
  fineAmount: number
  recordedAtUtc: string
  resolvedAtUtc: string | null
  status: 'open' | 'paid' | 'waived' | string
}

export type ViolationPageResponse = {
  items: LibraryViolation[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export type ViolationFilters = {
  search?: string
  status?: string
  pageNumber?: number
  pageSize?: number
}

export type CreateViolationInput = {
  borrowerId: string
  bookId: string | null
  type: string
  note: string
  fineAmount: number
}

type ProblemDetails = {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
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

  throw new Error(validationMessage ?? problem?.detail ?? problem?.title ?? 'Không thể xử lý yêu cầu.')
}

export async function getViolations(
  filters: ViolationFilters,
  signal?: AbortSignal,
): Promise<ViolationPageResponse> {
  const query = new URLSearchParams()
  if (filters.search) query.set('search', filters.search)
  if (filters.status) query.set('status', filters.status)
  query.set('pageNumber', String(filters.pageNumber ?? 1))
  query.set('pageSize', String(filters.pageSize ?? 20))

  const response = await authenticatedFetch(`${VIOLATIONS_URL}?${query}`, { signal })
  return readResponse<ViolationPageResponse>(response)
}

export async function createViolation(input: CreateViolationInput): Promise<LibraryViolation> {
  const response = await authenticatedFetch(VIOLATIONS_URL, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  return readResponse<LibraryViolation>(response)
}

export async function payViolation(id: string): Promise<LibraryViolation> {
  const response = await authenticatedFetch(`${VIOLATIONS_URL}/${id}/pay`, { method: 'POST' })
  return readResponse<LibraryViolation>(response)
}

export async function waiveViolation(id: string): Promise<LibraryViolation> {
  const response = await authenticatedFetch(`${VIOLATIONS_URL}/${id}/waive`, { method: 'POST' })
  return readResponse<LibraryViolation>(response)
}
