import { authenticatedFetch } from '@/auth/auth-api'

const BOOKS_URL = '/api/v1/books'

export type LibraryBook = {
  id: string
  title: string
  author: string
  isbn: string
  category: string
  quantity: number
  createdAtUtc: string
  updatedAtUtc: string | null
  authors?: BookReference[] | null
  categories?: BookReference[] | null
  publisher?: BookReference | null
  availableCopyCount?: number | null
  status?: 'Active' | 'Inactive'
  concurrencyToken?: string
  description?: string | null
  editionStatement?: string | null
  publicationYear?: number | null
}

export type BookReference = { id: string; name: string }

export type BookPageResponse = {
  items: LibraryBook[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export type BookFilters = {
  search?: string
  category?: string
  pageNumber?: number
  pageSize?: number
  authorIds?: string[]
  categoryIds?: string[]
  publisherId?: string
  status?: 'Active' | 'Inactive'
  sortBy?: string
  sortDirection?: 'asc' | 'desc'
}

export type BookInput = {
  title: string
  author: string
  isbn: string
  category: string
  quantity: number
  authorIds?: string[]
  categoryIds?: string[]
  publisherId?: string
  description?: string | null
  editionStatement?: string | null
  publicationYear?: number | null
  concurrencyToken?: string
}

type ProblemDetails = {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

export class BookApiError extends Error {
  readonly status: number

  constructor(message: string, status: number) {
    super(message)
    this.name = 'BookApiError'
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

  throw new BookApiError(
    validationMessage ?? problem?.detail ?? problem?.title ?? 'Không thể xử lý yêu cầu.',
    response.status,
  )
}

export async function getBooks(
  filters: BookFilters,
  signal?: AbortSignal,
): Promise<BookPageResponse> {
  const query = new URLSearchParams()
  if (filters.search) query.set('search', filters.search)
  if (filters.category) query.set('category', filters.category)
  query.set('pageNumber', String(filters.pageNumber ?? 1))
  query.set('pageSize', String(filters.pageSize ?? 20))
  if (filters.authorIds?.length) filters.authorIds.forEach((id) => query.append('authorIds', id))
  if (filters.categoryIds?.length) filters.categoryIds.forEach((id) => query.append('categoryIds', id))
  if (filters.publisherId) query.set('publisherId', filters.publisherId)
  if (filters.status) query.set('status', filters.status)
  query.set('sortBy', filters.sortBy ?? 'title')
  query.set('sortDirection', filters.sortDirection ?? 'asc')

  const response = await authenticatedFetch(`${BOOKS_URL}?${query}`, { signal })
  return readResponse<BookPageResponse>(response)
}

export async function getBook(id: string): Promise<LibraryBook> {
  const response = await authenticatedFetch(`${BOOKS_URL}/${id}`)
  return readResponse<LibraryBook>(response)
}

export async function createBook(input: BookInput): Promise<LibraryBook> {
  const response = await authenticatedFetch(BOOKS_URL, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  return readResponse<LibraryBook>(response)
}

export async function updateBook(id: string, input: BookInput): Promise<LibraryBook> {
  const response = await authenticatedFetch(`${BOOKS_URL}/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  return readResponse<LibraryBook>(response)
}

export async function deleteBook(id: string): Promise<void> {
  const response = await authenticatedFetch(`${BOOKS_URL}/${id}`, { method: 'DELETE' })
  await readResponse<void>(response)
}
