import { authenticatedFetch } from '@/auth/auth-api'

const POLICIES_URL = '/api/v1/circulation-policies'

export type CirculationPolicy = {
  id: string
  name: string
  description?: string | null
  version: number
  isActive: boolean
  memberGroup?: string | null
  documentType?: string | null
  branchId?: string | null
  effectiveFrom: string
  effectiveTo?: string | null
  maxLoanBooks: number
  loanPeriodDays: number
  maxRenewals: number
  renewalPeriodDays: number
  holdDays: number
  blockIfOverdue: boolean
  finePerDay: number
  fixedFineAmount: number
  maxFineAmount: number
  lostBookPenaltyRatio: number
  createdAtUtc: string
  updatedAtUtc?: string | null
  createdByUserId?: string | null
}

export type CirculationPolicyPageResponse = {
  items: CirculationPolicy[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export type CirculationPolicyFilters = {
  search?: string
  isActive?: boolean
  memberGroup?: string
  branchId?: string
  pageNumber?: number
  pageSize?: number
}

export type CreateCirculationPolicyInput = {
  name: string
  description?: string
  memberGroup?: string
  documentType?: string
  branchId?: string
  effectiveFrom: string
  effectiveTo?: string
  maxLoanBooks: number
  loanPeriodDays: number
  maxRenewals: number
  renewalPeriodDays: number
  holdDays: number
  blockIfOverdue: boolean
  finePerDay: number
  fixedFineAmount: number
  maxFineAmount: number
  lostBookPenaltyRatio: number
  isActive: boolean
}

export type UpdateCirculationPolicyInput = Omit<CreateCirculationPolicyInput, 'isActive'>

export type CreatePolicyVersionInput = Partial<UpdateCirculationPolicyInput>

export type ResolvedPolicy = {
  policyId?: string | null
  policyName: string
  version: number
  isDefaultFallback: boolean
  memberGroup?: string | null
  documentType?: string | null
  branchId?: string | null
  maxLoanBooks: number
  loanPeriodDays: number
  maxRenewals: number
  renewalPeriodDays: number
  holdDays: number
  blockIfOverdue: boolean
  finePerDay: number
  fixedFineAmount: number
  maxFineAmount: number
  lostBookPenaltyRatio: number
  matchScore: number
}

export type PolicyPreviewInput = {
  memberGroup?: string
  documentType?: string
  branchId?: string
  effectiveAtUtc?: string
  testOverdueDays?: number
  testBookPrice?: number
  testIsLost?: boolean
}

export type PolicyPreviewResponse = {
  policy: ResolvedPolicy
  calculatedOverdueFine: number
  calculatedLostPenalty: number
  sampleDueAtUtc: string
  sampleHoldExpiresAtUtc: string
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

export async function getCirculationPolicies(
  filters: CirculationPolicyFilters = {},
  signal?: AbortSignal,
): Promise<CirculationPolicyPageResponse> {
  const query = new URLSearchParams()
  if (filters.search) query.set('search', filters.search)
  if (filters.isActive !== undefined) query.set('isActive', String(filters.isActive))
  if (filters.memberGroup) query.set('memberGroup', filters.memberGroup)
  if (filters.branchId) query.set('branchId', filters.branchId)
  query.set('pageNumber', String(filters.pageNumber ?? 1))
  query.set('pageSize', String(filters.pageSize ?? 20))

  const response = await authenticatedFetch(`${POLICIES_URL}?${query}`, { signal })
  return readResponse<CirculationPolicyPageResponse>(response)
}

export async function getCirculationPolicyById(id: string): Promise<CirculationPolicy> {
  const response = await authenticatedFetch(`${POLICIES_URL}/${id}`)
  return readResponse<CirculationPolicy>(response)
}

export async function createCirculationPolicy(
  input: CreateCirculationPolicyInput,
): Promise<CirculationPolicy> {
  const response = await authenticatedFetch(POLICIES_URL, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  return readResponse<CirculationPolicy>(response)
}

export async function updateCirculationPolicy(
  id: string,
  input: UpdateCirculationPolicyInput,
): Promise<CirculationPolicy> {
  const response = await authenticatedFetch(`${POLICIES_URL}/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  return readResponse<CirculationPolicy>(response)
}

export async function createPolicyVersion(
  id: string,
  input: CreatePolicyVersionInput,
): Promise<CirculationPolicy> {
  const response = await authenticatedFetch(`${POLICIES_URL}/${id}/versions`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  return readResponse<CirculationPolicy>(response)
}

export async function activateCirculationPolicy(id: string): Promise<CirculationPolicy> {
  const response = await authenticatedFetch(`${POLICIES_URL}/${id}/activate`, {
    method: 'POST',
  })
  return readResponse<CirculationPolicy>(response)
}

export async function deactivateCirculationPolicy(id: string): Promise<CirculationPolicy> {
  const response = await authenticatedFetch(`${POLICIES_URL}/${id}/deactivate`, {
    method: 'POST',
  })
  return readResponse<CirculationPolicy>(response)
}

export async function previewCirculationPolicy(
  input: PolicyPreviewInput,
): Promise<PolicyPreviewResponse> {
  const response = await authenticatedFetch(`${POLICIES_URL}/preview`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  return readResponse<PolicyPreviewResponse>(response)
}
