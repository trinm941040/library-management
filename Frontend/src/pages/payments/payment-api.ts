import { authenticatedFetch } from '@/auth/auth-api'

const PAYMENTS_URL = '/api/v1/payments'

export type FinePaymentMethod = 'Cash' | 'BankTransfer' | 'Card' | 'Other'

export const PAYMENT_METHOD_LABELS: Record<FinePaymentMethod, string> = {
  Cash: 'Tiền mặt',
  BankTransfer: 'Chuyển khoản ngân hàng',
  Card: 'Thẻ thanh toán / POS',
  Other: 'Phương thức khác',
}

export type FinePaymentPreview = {
  violationId: string
  violationType: string
  bookTitle: string
  memberId: string
  memberName: string
  memberCode: string | null
  borrowerEmail: string
  fineAmount: number
  totalAdjusted: number
  totalPaid: number
  balance: number
  suggestedAmount: number
  isOpen: boolean
  status: string
}

export type FinePaymentReceipt = {
  id: string
  violationId: string
  violationType: string
  bookTitle: string
  memberId: string
  memberName: string
  memberCode: string | null
  amount: number
  previousBalance: number
  remainingBalance: number
  method: string
  reference: string
  paidAtUtc: string
  receivedByUserId: string | null
  receivedByUserName: string | null
  isFullyPaid: boolean
}

export type PaymentFilterRequest = {
  search?: string
  violationId?: string
  memberId?: string
  method?: FinePaymentMethod | string
  fromDate?: string
  toDate?: string
  pageNumber?: number
  pageSize?: number
}

export type PaymentPageResponse = {
  items: FinePaymentReceipt[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export type CreatePaymentRequest = {
  violationId: string
  amount: number
  method: FinePaymentMethod
  reference?: string
  idempotencyKey?: string
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

export async function getPayments(
  filters: PaymentFilterRequest,
  signal?: AbortSignal,
): Promise<PaymentPageResponse> {
  const query = new URLSearchParams()
  if (filters.search) query.set('search', filters.search)
  if (filters.violationId) query.set('violationId', filters.violationId)
  if (filters.memberId) query.set('memberId', filters.memberId)
  if (filters.method && filters.method !== 'all') query.set('method', filters.method)
  if (filters.fromDate) query.set('fromDate', filters.fromDate)
  if (filters.toDate) query.set('toDate', filters.toDate)
  query.set('pageNumber', String(filters.pageNumber ?? 1))
  query.set('pageSize', String(filters.pageSize ?? 20))

  const response = await authenticatedFetch(`${PAYMENTS_URL}?${query}`, { signal })
  return readResponse<PaymentPageResponse>(response)
}

export async function getPaymentReceipt(
  id: string,
  signal?: AbortSignal,
): Promise<FinePaymentReceipt> {
  const response = await authenticatedFetch(`${PAYMENTS_URL}/${id}`, { signal })
  return readResponse<FinePaymentReceipt>(response)
}

export async function getPaymentPreview(
  violationId: string,
  signal?: AbortSignal,
): Promise<FinePaymentPreview> {
  const response = await authenticatedFetch(`${PAYMENTS_URL}/preview`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ violationId }),
    signal,
  })
  return readResponse<FinePaymentPreview>(response)
}

export async function createPayment(input: CreatePaymentRequest): Promise<FinePaymentReceipt> {
  const response = await authenticatedFetch(PAYMENTS_URL, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  return readResponse<FinePaymentReceipt>(response)
}
