import { z } from 'zod'
import { authenticatedFetch, guidSchema, readResponse } from '@/auth/auth-api'

const URL = '/api/v1/stock-receipts'
const itemSchema = z.object({
  id: guidSchema, bookId: guidSchema, bookTitle: z.string(), isbn: z.string(),
  expectedQuantity: z.number().int().nonnegative(), receivedQuantity: z.number().int().nonnegative(),
  damagedQuantity: z.number().int().nonnegative(), unitCost: z.number().nullable(),
  totalValue: z.number(), concurrencyToken: guidSchema,
})
const receiptSchema = z.object({
  id: guidSchema, receiptNumber: z.string(), supplierId: guidSchema, supplierName: z.string(),
  branchId: guidSchema, branchCode: z.string(), receivedByUserId: guidSchema,
  status: z.enum(['Draft', 'Received', 'Confirmed', 'Cancelled']), receivedAtUtc: z.string(),
  notes: z.string().nullable(), concurrencyToken: guidSchema,
  totalQuantity: z.number().nonnegative(), totalValue: z.number(), items: z.array(itemSchema),
})
const pageSchema = z.object({ items: z.array(receiptSchema), pageNumber: z.number().int().positive(),
  pageSize: z.number().int().positive(), totalCount: z.number().int().nonnegative(),
  totalPages: z.number().int().nonnegative() })
export type StockReceipt = z.infer<typeof receiptSchema>
export type ReceiptStatus = StockReceipt['status']
export type ReceiptInput = {
  supplierId: string; branchId: string; receivedAtUtc: string; notes: string | null;
  concurrencyToken?: string;
  items: Array<{ id?: string; bookId: string; expectedQuantity: number;
    receivedQuantity: number; damagedQuantity: number; unitCost: number | null }>
}
export type ReceiptFilters = { search?: string; fromUtc?: string; toUtc?: string;
  supplierId?: string; branchId?: string; status?: ReceiptStatus; pageNumber: number; pageSize: number }
export async function getReceipts(filters: ReceiptFilters, signal?: AbortSignal) {
  const query = new URLSearchParams()
  Object.entries(filters).forEach(([key, value]) => { if (value !== undefined && value !== '') query.set(key, String(value)) })
  return readResponse(await authenticatedFetch(`${URL}?${query}`, { signal }), pageSchema)
}
export async function getReceipt(id: string, signal?: AbortSignal) {
  return readResponse(await authenticatedFetch(`${URL}/${id}`, { signal }), receiptSchema)
}
export async function createReceipt(input: ReceiptInput) {
  return readResponse(await authenticatedFetch(URL, { method: 'POST',
    headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(input) }), receiptSchema)
}
export async function updateReceipt(id: string, input: ReceiptInput) {
  return readResponse(await authenticatedFetch(`${URL}/${id}`, { method: 'PUT',
    headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(input) }), receiptSchema)
}

const copySchema = z.object({ id: guidSchema, stockReceiptItemId: guidSchema, barcode: z.string(),
  shelfId: guidSchema, condition: z.enum(['New', 'Good', 'Worn', 'Damaged', 'Lost']),
  status: z.enum(['Available', 'Borrowed', 'Reserved', 'InTransit', 'Lost', 'Damaged', 'Withdrawn']) })
const discrepancySchema = z.object({ id: guidSchema, type: z.enum(['Missing', 'Excess', 'Damaged', 'Other']),
  expectedQuantity: z.number().int().nonnegative(), actualQuantity: z.number().int().nonnegative(),
  description: z.string(), createdAtUtc: z.string(), createdByUserId: guidSchema.nullable() })
const confirmationSchema = z.object({ receipt: receiptSchema, copies: z.array(copySchema),
  discrepancies: z.array(discrepancySchema) })
export type ReceiptConfirmation = z.infer<typeof confirmationSchema>
export type ConfirmReceiptInput = { concurrencyToken: string; items: Array<{ stockReceiptItemId: string;
  copies: Array<{ barcode: string; shelfId: string; condition: 'New' | 'Good' | 'Worn' | 'Damaged' }> }> }
export async function getReceiptConfirmation(id: string, signal?: AbortSignal) {
  return readResponse(await authenticatedFetch(`${URL}/${id}/confirmation`, { signal }), confirmationSchema)
}
export async function confirmReceipt(id: string, input: ConfirmReceiptInput) {
  return readResponse(await authenticatedFetch(`${URL}/${id}/confirm`, { method: 'POST',
    headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(input) }), confirmationSchema)
}
