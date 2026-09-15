import { authenticatedFetch } from '@/auth/auth-api'
import { z } from 'zod'

const POLICIES_URL = '/api/v1/circulation-policies'
const idSchema = z.string().regex(/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i)

const circulationPolicySchema = z.object({
  id: idSchema, name: z.string(), description: z.string().nullish(), version: z.number().int(),
  isActive: z.boolean(), memberGroup: z.string().nullish(), documentType: z.string().nullish(),
  branchId: idSchema.nullish(), effectiveFrom: z.string(), effectiveTo: z.string().nullish(),
  maxLoanBooks: z.number(), loanPeriodDays: z.number(), maxRenewals: z.number(),
  renewalPeriodDays: z.number(), holdDays: z.number(), blockIfOverdue: z.boolean(),
  finePerDay: z.number(), fixedFineAmount: z.number(), maxFineAmount: z.number(),
  lostBookPenaltyRatio: z.number(), createdAtUtc: z.string(), updatedAtUtc: z.string().nullish(),
  createdByUserId: idSchema.nullish(), concurrencyToken: idSchema,
})

const circulationPolicyPageSchema = z.object({
  items: z.array(circulationPolicySchema), pageNumber: z.number(), pageSize: z.number(),
  totalCount: z.number(), totalPages: z.number(),
})

const resolvedPolicySchema = circulationPolicySchema.pick({
  memberGroup: true, documentType: true, branchId: true, maxLoanBooks: true,
  loanPeriodDays: true, maxRenewals: true, renewalPeriodDays: true, holdDays: true,
  blockIfOverdue: true, finePerDay: true, fixedFineAmount: true, maxFineAmount: true,
  lostBookPenaltyRatio: true,
}).extend({
  policyId: idSchema.nullish(), policyName: z.string(), version: z.number(),
  isDefaultFallback: z.boolean(), matchScore: z.number(),
})

const policyPreviewSchema = z.object({
  policy: resolvedPolicySchema, calculatedOverdueFine: z.number(), calculatedLostPenalty: z.number(),
  sampleDueAtUtc: z.string(), sampleHoldExpiresAtUtc: z.string(),
})

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
  concurrencyToken: string
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

export type UpdateCirculationPolicyInput = Omit<CreateCirculationPolicyInput, 'isActive'> & {
  concurrencyToken: string
}

export type CreatePolicyVersionInput = Partial<Omit<UpdateCirculationPolicyInput, 'concurrencyToken'>> & {
  concurrencyToken: string
}

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
  return circulationPolicyPageSchema.parse(await readResponse<unknown>(response))
}

export async function getCirculationPolicyById(id: string): Promise<CirculationPolicy> {
  const response = await authenticatedFetch(`${POLICIES_URL}/${id}`)
  return circulationPolicySchema.parse(await readResponse<unknown>(response))
}

export async function createCirculationPolicy(
  input: CreateCirculationPolicyInput,
): Promise<CirculationPolicy> {
  const response = await authenticatedFetch(POLICIES_URL, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  return circulationPolicySchema.parse(await readResponse<unknown>(response))
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
  return circulationPolicySchema.parse(await readResponse<unknown>(response))
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
  return circulationPolicySchema.parse(await readResponse<unknown>(response))
}

export async function activateCirculationPolicy(
  id: string,
  concurrencyToken: string,
): Promise<CirculationPolicy> {
  const response = await authenticatedFetch(`${POLICIES_URL}/${id}/activate`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ concurrencyToken }),
  })
  return circulationPolicySchema.parse(await readResponse<unknown>(response))
}

export async function deactivateCirculationPolicy(
  id: string,
  concurrencyToken: string,
): Promise<CirculationPolicy> {
  const response = await authenticatedFetch(`${POLICIES_URL}/${id}/deactivate`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ concurrencyToken }),
  })
  return circulationPolicySchema.parse(await readResponse<unknown>(response))
}

export async function previewCirculationPolicy(
  input: PolicyPreviewInput,
): Promise<PolicyPreviewResponse> {
  const response = await authenticatedFetch(`${POLICIES_URL}/preview`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  return policyPreviewSchema.parse(await readResponse<unknown>(response))
}
