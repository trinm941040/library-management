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
  status: 'open' | 'partially_paid' | 'paid' | 'waived' | string
  totalAdjusted?: number
  totalPaid?: number
  balance?: number
  borrowingId?: string | null
  bookCopyId?: string | null
  bookCopyBarcode?: string | null
  borrowerMemberCode?: string | null
  borrowerCardNumber?: string | null
  concurrencyToken?: string
}

export type PaymentHistoryItem = {
  id: string
  amount: number
  method: string
  reference: string
  paidAtUtc: string
  receivedByUserId: string | null
}

export type AdjustmentHistoryItem = {
  id: string
  amountDelta: number
  reason: string
  adjustedAtUtc: string
  adjustedByUserId: string | null
}

export type ViolationDetailResponse = {
  violation: LibraryViolation
  bookIsbn: string | null
  bookAuthor: string | null
  bookCategory: string | null
  calculationBasis: string | null
  payments: PaymentHistoryItem[]
  adjustments: AdjustmentHistoryItem[]
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
  borrowerId?: string
  type?: string
  status?: string
  fromDate?: string
  toDate?: string
  hasBalanceOnly?: boolean
  pageNumber?: number
  pageSize?: number
}

export type CreateViolationInput = {
  borrowerId: string
  bookId: string | null
  bookCopyId?: string | null
  borrowingId?: string | null
  type: string
  note: string
  fineAmount: number
  overdueDays?: number
  bookPrice?: number
  damageLevel?: string | null
}

export type FinePreviewRequest = {
  borrowerId: string
  bookId: string | null
  type: string
  overdueDays?: number
  bookPrice?: number
  damageLevel?: string | null
  customAmount?: number
}

export type FinePreviewResponse = {
  calculatedFine: number
  formula: string
  policyName: string
  dailyRate: number
  maxFine: number
  lostRatio: number
  policyId: string | null
  policyVersion: number
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
  if (filters.borrowerId) query.set('borrowerId', filters.borrowerId)
  if (filters.type && filters.type !== 'all') query.set('type', filters.type)
  if (filters.status && filters.status !== 'all') query.set('status', filters.status)
  if (filters.fromDate) query.set('fromDate', filters.fromDate)
  if (filters.toDate) query.set('toDate', filters.toDate)
  if (filters.hasBalanceOnly) query.set('hasBalanceOnly', 'true')
  query.set('pageNumber', String(filters.pageNumber ?? 1))
  query.set('pageSize', String(filters.pageSize ?? 20))

  const response = await authenticatedFetch(`${VIOLATIONS_URL}?${query}`, { signal })
  return readResponse<ViolationPageResponse>(response)
}

export async function getViolationDetail(
  id: string,
  signal?: AbortSignal,
): Promise<ViolationDetailResponse> {
  const response = await authenticatedFetch(`${VIOLATIONS_URL}/${id}`, { signal })
  return readResponse<ViolationDetailResponse>(response)
}

export async function previewFine(input: FinePreviewRequest): Promise<FinePreviewResponse> {
  const response = await authenticatedFetch(`${VIOLATIONS_URL}/preview`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  return readResponse<FinePreviewResponse>(response)
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
