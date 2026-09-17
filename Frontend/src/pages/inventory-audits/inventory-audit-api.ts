import { z } from 'zod'
import { authenticatedFetch, guidSchema, readResponse } from '@/auth/auth-api'
import { locationSchema } from '@/pages/branches/branch-api'

const URL = '/api/v1/inventory-audits'
const statusSchema = z.enum(['Draft', 'InProgress', 'Completed', 'Cancelled'])
const copyStatusSchema = z.enum(['Available', 'Borrowed', 'Reserved', 'InTransit', 'Lost', 'Damaged', 'Withdrawn'])
const conditionSchema = z.enum(['New', 'Good', 'Worn', 'Damaged', 'Lost'])
const resultSchema = z.enum(['Pending', 'Found', 'Misplaced', 'Missing', 'Damaged',
  'Unexpected', 'StatusMismatch', 'ConditionMismatch'])
const itemSchema = z.object({ id: guidSchema, bookCopyId: guidSchema,
  copyConcurrencyToken: guidSchema, barcode: z.string(),
  bookTitle: z.string(), isExpected: z.boolean(), expectedShelfId: guidSchema.nullable(),
  actualShelfId: guidSchema.nullable(), expectedStatus: copyStatusSchema,
  actualStatus: copyStatusSchema.nullable(), expectedCondition: conditionSchema,
  actualCondition: conditionSchema.nullable(), result: resultSchema,
  scannedAtUtc: z.string().nullable() })
const auditSchema = z.object({ id: guidSchema, branchId: guidSchema, areaId: guidSchema.nullable(),
  shelfId: guidSchema.nullable(), startedByUserId: guidSchema, status: statusSchema,
  startedAtUtc: z.string(), completedAtUtc: z.string().nullable(), notes: z.string().nullable(),
  concurrencyToken: guidSchema, expectedCount: z.number().int().nonnegative(),
  scannedCount: z.number().int().nonnegative(), pendingCount: z.number().int().nonnegative(),
  foundCount: z.number().int().nonnegative(), discrepancyCount: z.number().int().nonnegative(),
  items: z.array(itemSchema) })
const pageSchema = z.object({ items: z.array(auditSchema), pageNumber: z.number().int().positive(),
  pageSize: z.number().int().positive(), totalCount: z.number().int().nonnegative(),
  totalPages: z.number().int().nonnegative() })
export type InventoryAudit = z.infer<typeof auditSchema>
export type InventoryAuditItem = z.infer<typeof itemSchema>
export type CopyStatus = z.infer<typeof copyStatusSchema>
export type CopyCondition = z.infer<typeof conditionSchema>
export type AuditStatus = z.infer<typeof statusSchema>
export async function getAuditLocations(signal?: AbortSignal) {
  return readResponse(await authenticatedFetch(`${URL}/locations`, { signal }), z.array(locationSchema))
}
export async function getAudits(filters: { branchId?: string; status?: AuditStatus;
  pageNumber: number; pageSize: number }, signal?: AbortSignal) {
  const query = new URLSearchParams()
  Object.entries(filters).forEach(([key, value]) => { if (value !== undefined && value !== '') query.set(key, String(value)) })
  return readResponse(await authenticatedFetch(`${URL}?${query}`, { signal }), pageSchema)
}
export async function getAudit(id: string, signal?: AbortSignal) {
  return readResponse(await authenticatedFetch(`${URL}/${id}`, { signal }), auditSchema)
}
export async function createAudit(input: { branchId: string; areaId: string | null;
  shelfId: string | null; notes: string | null }) {
  return readResponse(await authenticatedFetch(URL, { method: 'POST',
    headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(input) }), auditSchema)
}
export async function scanAudit(id: string, input: { barcode: string; actualShelfId: string;
  actualStatus: CopyStatus; actualCondition: CopyCondition; concurrencyToken: string }) {
  return readResponse(await authenticatedFetch(`${URL}/${id}/scan`, { method: 'POST',
    headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(input) }), auditSchema)
}
export async function reconcileAudit(id: string, signal?: AbortSignal) {
  return readResponse(await authenticatedFetch(`${URL}/${id}/reconcile`, { signal }), auditSchema)
}
export async function completeAudit(id: string, input: { concurrencyToken: string;
  acknowledgeDiscrepancies: boolean }) {
  return readResponse(await authenticatedFetch(`${URL}/${id}/complete`, { method: 'POST',
    headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(input) }), auditSchema)
}
export async function exportAudit(id: string) {
  const response = await authenticatedFetch(`${URL}/${id}/export`)
  if (!response.ok) await readResponse(response)
  return response.blob()
}
export async function applyAuditCorrection(id: string, correction: { bookCopyId: string;
  concurrencyToken: string; shelfId: string | null; status: CopyStatus | null;
  condition: CopyCondition | null }) {
  return readResponse(await authenticatedFetch(`${URL}/${id}/apply`, { method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ corrections: [correction] }) }),
  z.object({ appliedCount: z.number().int().positive() }))
}
