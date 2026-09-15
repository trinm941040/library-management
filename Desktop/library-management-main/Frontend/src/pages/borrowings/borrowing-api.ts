import { authenticatedFetch } from '@/auth/auth-api'

const BORROWINGS_URL = '/api/v1/borrowings'

export type LibraryBorrowing = {
  id: string
  bookId: string
  bookTitle: string
  borrowerId: string
  borrowerName: string
  borrowerEmail: string
  borrowedAtUtc: string
  dueAtUtc: string
  returnedAtUtc: string | null
  status: 'borrowed' | 'overdue' | 'returned' | string
}

export type BorrowingPageResponse = {
  items: LibraryBorrowing[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export type BorrowingFilters = {
  search?: string
  status?: string
  pageNumber?: number
  pageSize?: number
}

export type CreateBorrowingInput = {
  bookId: string
  borrowerId: string
  loanDays: number
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

export async function getBorrowings(
  filters: BorrowingFilters,
  signal?: AbortSignal,
): Promise<BorrowingPageResponse> {
  const query = new URLSearchParams()
  if (filters.search) query.set('search', filters.search)
  if (filters.status) query.set('status', filters.status)
  query.set('pageNumber', String(filters.pageNumber ?? 1))
  query.set('pageSize', String(filters.pageSize ?? 20))

  const response = await authenticatedFetch(`${BORROWINGS_URL}?${query}`, { signal })
  return readResponse<BorrowingPageResponse>(response)
}

export async function createBorrowing(input: CreateBorrowingInput): Promise<LibraryBorrowing> {
  const response = await authenticatedFetch(BORROWINGS_URL, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  return readResponse<LibraryBorrowing>(response)
}

export async function returnBorrowing(id: string): Promise<LibraryBorrowing> {
  const response = await authenticatedFetch(`${BORROWINGS_URL}/${id}/return`, { method: 'POST' })
  return readResponse<LibraryBorrowing>(response)
}
