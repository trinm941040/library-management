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
  bookCopyId?: string | null
  bookCopyBarcode?: string | null
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

export type MemberCheckoutLookup = {
  memberId: string
  memberCode: string
  fullName: string
  email: string
  memberGroup: string
  cardNumber: string | null
  cardExpiresOn: string | null
  cardStatus: string | null
  memberStatus: string
  activeBorrowingsCount: number
  borrowingLimit: number
  overdueLoansCount: number
  isEligible: boolean
  ineligibilityReasons: string[]
}

export type BookCopyCheckoutLookup = {
  copyId: string
  bookId: string
  barcode: string
  title: string
  author: string
  isbn: string
  category: string
  condition: string
  status: string
  isAvailable: boolean
  ineligibilityReason: string | null
  policyName: string | null
  loanPeriodDays: number
  sampleDueAtUtc: string | null
}

export type CheckoutWithBarcodePayload = {
  memberCardOrCode: string
  bookBarcode: string
  loanDaysOverride?: number
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

export async function lookupMemberForCheckout(
  cardOrCode: string,
  signal?: AbortSignal,
): Promise<MemberCheckoutLookup> {
  const query = new URLSearchParams({ cardOrCode: cardOrCode.trim() })
  const response = await authenticatedFetch(`${BORROWINGS_URL}/checkout/lookup-member?${query}`, { signal })
  return readResponse<MemberCheckoutLookup>(response)
}

export async function lookupBookCopyForCheckout(
  barcode: string,
  memberId?: string,
  signal?: AbortSignal,
): Promise<BookCopyCheckoutLookup> {
  const query = new URLSearchParams({ barcode: barcode.trim() })
  if (memberId) query.set('memberId', memberId)
  const response = await authenticatedFetch(`${BORROWINGS_URL}/checkout/lookup-copy?${query}`, { signal })
  return readResponse<BookCopyCheckoutLookup>(response)
}

export async function checkoutWithBarcode(payload: CheckoutWithBarcodePayload): Promise<LibraryBorrowing> {
  const response = await authenticatedFetch(`${BORROWINGS_URL}/checkout`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  })
  return readResponse<LibraryBorrowing>(response)
}

export type BookCopyReturnLookup = {
  borrowingId: string
  bookId: string
  title: string
  author: string
  copyId: string
  barcode: string
  condition: string
  borrowerId: string
  borrowerName: string
  borrowerEmail: string
  memberCode: string | null
  cardNumber: string | null
  borrowedAtUtc: string
  dueAtUtc: string
  isOverdue: boolean
  overdueDays: number
  finePerDay: number
  estimatedOverdueFine: number
  fixedDamageFine: number
  estimatedLostFine: number
  policyName: string | null
  concurrencyToken: string
}

export type ConfirmReturnPayload = {
  barcode: string
  condition: string
  note?: string | null
  customDamageFine?: number | null
  customLostFine?: number | null
  concurrencyToken: string
}

export type ViolationSummary = {
  id: string
  type: string
  note: string
  fineAmount: number
  recordedAtUtc: string
}

export type ReturnExecutionResult = {
  borrowing: LibraryBorrowing
  returnedAtUtc: string
  condition: string
  status: string
  violations: ViolationSummary[]
  totalFine: number
  hasWaitingReservation: boolean
}

export async function lookupBookCopyForReturn(
  barcode: string,
  signal?: AbortSignal,
): Promise<BookCopyReturnLookup> {
  const query = new URLSearchParams({ barcode: barcode.trim() })
  const response = await authenticatedFetch(`${BORROWINGS_URL}/return/lookup-copy?${query}`, { signal })
  return readResponse<BookCopyReturnLookup>(response)
}

export async function confirmReturn(payload: ConfirmReturnPayload): Promise<ReturnExecutionResult> {
  const response = await authenticatedFetch(`${BORROWINGS_URL}/return/confirm`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  })
  return readResponse<ReturnExecutionResult>(response)
}
