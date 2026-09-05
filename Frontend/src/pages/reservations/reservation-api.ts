import { authenticatedFetch } from '@/auth/auth-api'

const RESERVATIONS_URL = '/api/v1/reservations'

export type LibraryReservation = {
  id: string
  bookId: string
  bookTitle: string
  reserverId: string
  reserverName: string
  reserverEmail: string
  reservedAtUtc: string
  expiresAtUtc: string
  fulfilledAtUtc: string | null
  cancelledAtUtc: string | null
  status: 'waiting' | 'ready' | 'expired' | 'fulfilled' | 'cancelled' | string
}

export type ReservationPageResponse = {
  items: LibraryReservation[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export type ReservationFilters = {
  search?: string
  status?: string
  pageNumber?: number
  pageSize?: number
}

export type CreateReservationInput = {
  bookId: string
  reserverId: string
  holdDays: number
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

export async function getReservations(
  filters: ReservationFilters,
  signal?: AbortSignal,
): Promise<ReservationPageResponse> {
  const query = new URLSearchParams()
  if (filters.search) query.set('search', filters.search)
  if (filters.status) query.set('status', filters.status)
  query.set('pageNumber', String(filters.pageNumber ?? 1))
  query.set('pageSize', String(filters.pageSize ?? 20))

  const response = await authenticatedFetch(`${RESERVATIONS_URL}?${query}`, { signal })
  return readResponse<ReservationPageResponse>(response)
}

export async function createReservation(input: CreateReservationInput): Promise<LibraryReservation> {
  const response = await authenticatedFetch(RESERVATIONS_URL, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  return readResponse<LibraryReservation>(response)
}

export async function cancelReservation(id: string): Promise<LibraryReservation> {
  const response = await authenticatedFetch(`${RESERVATIONS_URL}/${id}/cancel`, { method: 'POST' })
  return readResponse<LibraryReservation>(response)
}

export async function fulfillReservation(id: string): Promise<LibraryReservation> {
  const response = await authenticatedFetch(`${RESERVATIONS_URL}/${id}/fulfill`, { method: 'POST' })
  return readResponse<LibraryReservation>(response)
}
