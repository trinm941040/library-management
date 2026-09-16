import { authenticatedFetch } from '@/auth/auth-api'

const VIOLATIONS_URL = '/api/v1/violations'

export type FineAdjustmentItem = {
  id: string
  memberId: string
  violationId: string
  amountDelta: number
  reason: string
  adjustedAtUtc: string
  adjustedByUserId: string | null
}

export type FineAdjustmentPreview = {
  violationId: string
  originalFineAmount: number
  totalAdjusted: number
  totalPaid: number
  currentBalance: number
  adjustmentAmountDelta: number
  projectedBalance: number
  projectedStatus: string
  isAllowed: boolean
  validationMessage: string | null
}

export type FineAdjustmentResult = {
  succeeded: boolean
  adjustment: FineAdjustmentItem | null
  newBalance: number
  status: string
  errors: string[]
}

type ProblemDetails = {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

async function handleResponse<T>(response: Response): Promise<T> {
  if (response.ok) {
    if (response.status === 204) return undefined as T
    return response.json() as Promise<T>
  }

  const problem = (await response.json().catch(() => null)) as ProblemDetails | null
  const validationMessage = problem?.errors
    ? Object.values(problem.errors).flat().find(Boolean)
    : undefined

  throw new Error(validationMessage ?? problem?.detail ?? problem?.title ?? 'Không thể xử lý yêu cầu điều chỉnh.')
}

export async function getAdjustmentPreview(
  violationId: string,
  amountDelta: number,
): Promise<FineAdjustmentPreview> {
  const response = await authenticatedFetch(`${VIOLATIONS_URL}/${violationId}/adjustment-preview`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ amountDelta }),
  })
  return handleResponse<FineAdjustmentPreview>(response)
}

export async function adjustViolation(
  violationId: string,
  payload: { amountDelta: number; reason: string },
): Promise<FineAdjustmentResult> {
  const response = await authenticatedFetch(`${VIOLATIONS_URL}/${violationId}/adjust`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  })
  return handleResponse<FineAdjustmentResult>(response)
}

export async function waiveViolation(
  violationId: string,
  payload: { reason: string },
): Promise<FineAdjustmentResult> {
  const response = await authenticatedFetch(`${VIOLATIONS_URL}/${violationId}/waive`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  })
  return handleResponse<FineAdjustmentResult>(response)
}

export async function getViolationAdjustments(
  violationId: string,
): Promise<FineAdjustmentItem[]> {
  const response = await authenticatedFetch(`${VIOLATIONS_URL}/${violationId}/adjustments`)
  return handleResponse<FineAdjustmentItem[]>(response)
}
