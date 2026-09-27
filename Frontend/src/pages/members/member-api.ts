import { z } from 'zod'
import { authenticatedFetch, guidSchema, readResponse } from '@/auth/auth-api'

const URL = '/api/v1/members'
export const memberStatuses = ['Active', 'Expired', 'Suspended', 'Discontinued'] as const
export const cardStatuses = ['Active', 'Expired', 'Suspended', 'Revoked'] as const
export const restrictionTypes = ['Borrowing', 'Reservation', 'AllTransactions'] as const
export const historyCategories = [
  'Borrowings', 'Returns', 'Renewals', 'Reservations', 'Violations', 'Payments',
] as const
export type MemberStatus = (typeof memberStatuses)[number]
export type CardStatus = (typeof cardStatuses)[number]
export type RestrictionType = (typeof restrictionTypes)[number]
export type HistoryCategory = (typeof historyCategories)[number]
export const statusLabels: Record<MemberStatus, string> = {
  Active: 'Đang hoạt động', Expired: 'Hết hạn', Suspended: 'Tạm đình chỉ', Discontinued: 'Ngừng sử dụng',
}

const cardSchema = z.object({
  id: guidSchema, cardNumber: z.string(), issuedOn: z.string(), expiresOn: z.string(), status: z.enum(cardStatuses),
})
const restrictionSchema = z.object({
  id: guidSchema, type: z.enum(restrictionTypes), reason: z.string(), startsAtUtc: z.string(),
  endsAtUtc: z.string().nullable(), removedAtUtc: z.string().nullable(),
  removalReason: z.string().nullable(), isActive: z.boolean(),
})
const borrowingSchema = z.object({
  id: guidSchema, bookId: guidSchema, borrowedAtUtc: z.string(), dueAtUtc: z.string(), returnedAtUtc: z.string().nullable(),
})
const reservationSchema = z.object({
  id: guidSchema, bookId: guidSchema, reservedAtUtc: z.string(), expiresAtUtc: z.string(),
  fulfilledAtUtc: z.string().nullable(), cancelledAtUtc: z.string().nullable(),
})
const fineSchema = z.object({
  id: guidSchema, type: z.string(), bookTitle: z.string(), note: z.string(), originalAmount: z.number(),
  adjustmentTotal: z.number(), paymentTotal: z.number(), balance: z.number(), recordedAtUtc: z.string(), status: z.string(),
})
const memberSchema = z.object({
  id: guidSchema, memberCode: z.string(), fullName: z.string(), email: z.string(),
  phoneNumber: z.string().nullable(), dateOfBirth: z.string().nullable(), address: z.string().nullable(),
  memberGroup: z.string(), status: z.enum(memberStatuses), borrowingLimit: z.number().int(),
  loanPeriodDays: z.number().int(), concurrencyToken: guidSchema, createdAtUtc: z.string(), updatedAtUtc: z.string(),
  card: cardSchema.nullable(), restrictions: z.array(restrictionSchema), borrowings: z.array(borrowingSchema),
  reservations: z.array(reservationSchema), fines: z.array(fineSchema),
})
const memberPageSchema = z.object({
  items: z.array(memberSchema), pageNumber: z.number().int().positive(), pageSize: z.number().int().positive(),
  totalCount: z.number().int().nonnegative(), totalPages: z.number().int().nonnegative(),
})
const historyPageSchema = z.object({
  items: z.array(z.object({
    id: guidSchema, type: z.string(), occurredAtUtc: z.string(), title: z.string(),
    description: z.string(), amount: z.number().nullable(),
  })),
  pageNumber: z.number().int().positive(), pageSize: z.number().int().positive(),
  totalCount: z.number().int().nonnegative(), totalPages: z.number().int().nonnegative(),
})

export type Member = z.infer<typeof memberSchema>
export type MemberPageResponse = z.infer<typeof memberPageSchema>
export type MemberHistoryPage = z.infer<typeof historyPageSchema>
export type SaveMemberInput = Pick<Member,
  'memberCode' | 'fullName' | 'email' | 'phoneNumber' | 'dateOfBirth' | 'address' |
  'memberGroup' | 'status' | 'borrowingLimit' | 'loanPeriodDays'> & { concurrencyToken?: string }

async function send(path: string, method: string, body: unknown) {
  return readResponse(await authenticatedFetch(`${URL}${path}`, {
    method, headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body),
  }), memberSchema)
}
export async function getMembers(filters: {
  search?: string; status?: MemberStatus; memberGroup?: string; pageNumber: number; pageSize: number
}, signal?: AbortSignal) {
  const query = new URLSearchParams()
  Object.entries(filters).forEach(([key, value]) => {
    if (value !== undefined && value !== '') query.set(key, String(value))
  })
  return readResponse(await authenticatedFetch(`${URL}?${query}`, { signal }), memberPageSchema)
}
export async function getMember(id: string, signal?: AbortSignal) {
  return readResponse(await authenticatedFetch(`${URL}/${id}`, { signal }), memberSchema)
}
export async function getMemberHistory(id: string, category: HistoryCategory, pageNumber: number,
  pageSize: number, signal?: AbortSignal) {
  const query = new URLSearchParams({ category, pageNumber: String(pageNumber), pageSize: String(pageSize) })
  return readResponse(await authenticatedFetch(`${URL}/${id}/history?${query}`, { signal }), historyPageSchema)
}
export const createMember = (body: SaveMemberInput) => send('', 'POST', body)
export const updateMember = (id: string, body: SaveMemberInput) => send(`/${id}`, 'PUT', body)
export async function issueCard(id: string, body: {
  cardNumber: string; issuedOn: string; expiresOn: string; concurrencyToken: string
}) {
  const response = await authenticatedFetch(`${URL}/${id}/card`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
    body: JSON.stringify({
      cardNumber: body.cardNumber.trim(),
      issuedOn: body.issuedOn,
      expiresOn: body.expiresOn,
      concurrencyToken: body.concurrencyToken,
    }),
  })
  return readResponse(response, memberSchema)
}
export const renewCard = (id: string, expiresOn: string, concurrencyToken: string) =>
  send(`/${id}/card/renew`, 'POST', { expiresOn, concurrencyToken })
export const changeCardStatus = (id: string, status: CardStatus, concurrencyToken: string) =>
  send(`/${id}/card/status`, 'PATCH', { status, concurrencyToken })
export const addRestriction = (id: string, body: {
  type: RestrictionType; reason: string; startsAtUtc: string; endsAtUtc: string | null; concurrencyToken: string
}) => send(`/${id}/restrictions`, 'POST', body)
export const removeRestriction = (id: string, restrictionId: string, reason: string, concurrencyToken: string) =>
  send(`/${id}/restrictions/${restrictionId}/remove`, 'POST', { reason, concurrencyToken })
export const addPayment = (id: string, violationId: string, body: {
  amount: number; method: 'Cash' | 'BankTransfer' | 'Card' | 'Other'; reference: string | null
}) => send(`/${id}/violations/${violationId}/payments`, 'POST', body)
export const addAdjustment = (id: string, violationId: string, body: { amountDelta: number; reason: string }) =>
  send(`/${id}/violations/${violationId}/adjustments`, 'POST', body)
