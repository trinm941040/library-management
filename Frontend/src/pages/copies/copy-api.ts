import { z } from 'zod'
import { authenticatedFetch, guidSchema, readResponse } from '@/auth/auth-api'
import { downloadResponse } from '@/shared/data/table-contracts'

const URL = '/api/v1/copies'
export const copyStatusSchema = z.enum([
  'Available',
  'Borrowed',
  'Reserved',
  'InTransit',
  'Lost',
  'Damaged',
  'Withdrawn',
])
export const copyConditionSchema = z.enum(['New', 'Good', 'Worn', 'Damaged', 'Lost'])
const copySchema = z.object({
  id: guidSchema,
  bookId: guidSchema,
  bookTitle: z.string(),
  barcode: z.string(),
  condition: copyConditionSchema,
  status: copyStatusSchema,
  acquiredAtUtc: z.string(),
  shelfId: guidSchema.nullable(),
  shelfCode: z.string().nullable(),
  branchId: guidSchema.nullable(),
  branchCode: z.string().nullable(),
  stockReceiptItemId: guidSchema.nullable(),
  concurrencyToken: guidSchema,
})
const pageSchema = z.object({
  items: z.array(copySchema),
  pageNumber: z.number().int().positive(),
  pageSize: z.number().int().positive(),
  totalCount: z.number().int().nonnegative(),
  totalPages: z.number().int().nonnegative(),
})
const shelfSchema = z.object({
  id: guidSchema,
  code: z.string(),
  label: z.string(),
  areaId: guidSchema,
  areaCode: z.string(),
  areaName: z.string(),
  branchId: guidSchema,
  branchCode: z.string(),
  branchName: z.string(),
})
export type BookCopy = z.infer<typeof copySchema>
export type CopyStatus = z.infer<typeof copyStatusSchema>
export type CopyCondition = z.infer<typeof copyConditionSchema>
export type ActiveShelf = z.infer<typeof shelfSchema>
export const copyStatuses: CopyStatus[] = [
  'Available',
  'Borrowed',
  'Reserved',
  'InTransit',
  'Lost',
  'Damaged',
  'Withdrawn',
]
export const copyStatusLabels: Record<CopyStatus, string> = {
  Available: 'Sẵn sàng',
  Borrowed: 'Đang mượn',
  Reserved: 'Đã giữ chỗ',
  InTransit: 'Đang chuyển',
  Lost: 'Mất',
  Damaged: 'Hư hỏng',
  Withdrawn: 'Thanh lý',
}
export const copyConditionLabels: Record<CopyCondition, string> = {
  New: 'Mới',
  Good: 'Tốt',
  Worn: 'Đã qua sử dụng',
  Damaged: 'Hư hỏng',
  Lost: 'Mất',
}
export type CopyFilters = {
  search?: string
  bookId?: string
  branchId?: string
  shelfId?: string
  condition?: CopyCondition
  status?: CopyStatus
  pageNumber?: number
  pageSize?: number
}
export type CreateCopyInput = {
  bookId: string
  barcode: string
  condition: CopyCondition
  shelfId: string
}
export type CopyBulkOperation = 'relocate' | 'status' | 'condition' | 'withdraw'
export type CopyBulkRow = {
  copyId: string
  concurrencyToken: string
  status?: CopyStatus
  condition?: CopyCondition
  shelfId?: string
  reason?: string
}
export type CopyImportRow = {
  barcode: string
  isbn: string
  shelfCode: string
  condition: CopyCondition
}
const bulkResultSchema = z.array(
  z.object({
    copyId: guidSchema,
    succeeded: z.boolean(),
    error: z.string().nullable(),
    copy: copySchema.nullable(),
  }),
)
const importPreviewSchema = z.array(
  z.object({
    rowNumber: z.number().int().positive(),
    barcode: z.string(),
    valid: z.boolean(),
    error: z.string().nullable(),
  }),
)
export type CopyBulkResult = z.infer<typeof bulkResultSchema>[number]
export type CopyImportPreview = z.infer<typeof importPreviewSchema>[number]

export async function getCopies(filters: CopyFilters, signal?: AbortSignal) {
  const query = new URLSearchParams()
  Object.entries(filters).forEach(([key, value]) => {
    if (value !== undefined && value !== '') query.set(key, String(value))
  })
  return readResponse(await authenticatedFetch(`${URL}?${query}`, { signal }), pageSchema)
}

export async function getAllCopiesByBook(bookId: string, signal?: AbortSignal) {
  const firstPage = await getCopies({ bookId, pageNumber: 1, pageSize: 100 }, signal)
  if (firstPage.totalPages <= 1) return firstPage.items
  const remainingPages = await Promise.all(
    Array.from({ length: firstPage.totalPages - 1 }, (_, index) =>
      getCopies({ bookId, pageNumber: index + 2, pageSize: 100 }, signal),
    ),
  )
  return [firstPage, ...remainingPages].flatMap((page) => page.items)
}

export function copyLocationLabel(copy: BookCopy, shelves: readonly ActiveShelf[]) {
  const shelf = shelves.find((item) => item.id === copy.shelfId)
  if (!shelf) return copy.shelfCode ? `Kệ ${copy.shelfCode}` : 'Chưa xếp vị trí'
  return `${shelf.branchName} · ${shelf.areaName} · Kệ ${shelf.code}`
}

export async function getCopyByBarcode(barcode: string, signal?: AbortSignal) {
  return readResponse(
    await authenticatedFetch(`${URL}/barcode/${encodeURIComponent(barcode)}`, { signal }),
    copySchema,
  )
}
export async function getCopy(id: string, signal?: AbortSignal) {
  return readResponse(await authenticatedFetch(`${URL}/${id}`, { signal }), copySchema)
}
export async function getActiveShelves(signal?: AbortSignal) {
  return readResponse(
    await authenticatedFetch('/api/v1/locations/active-shelves', { signal }),
    z.array(shelfSchema),
  )
}
export async function createCopy(input: CreateCopyInput) {
  return readResponse(
    await authenticatedFetch(URL, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(input),
    }),
    copySchema,
  )
}
export async function updateCopyStatus(copy: BookCopy, status: CopyStatus) {
  return readResponse(
    await authenticatedFetch(`${URL}/${copy.id}/status`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ status, concurrencyToken: copy.concurrencyToken }),
    }),
    copySchema,
  )
}
export async function relocateCopy(copy: BookCopy, shelfId: string) {
  return readResponse(
    await authenticatedFetch(`${URL}/${copy.id}/relocate`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ shelfId, concurrencyToken: copy.concurrencyToken }),
    }),
    copySchema,
  )
}

export async function runCopyBulk(operation: CopyBulkOperation, rows: CopyBulkRow[]) {
  return readResponse(
    await authenticatedFetch(`${URL}/bulk/${operation}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ rows }),
    }),
    bulkResultSchema,
  )
}

export async function previewCopyImport(rows: CopyImportRow[]) {
  return readResponse(
    await authenticatedFetch(`${URL}/import/preview`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ rows }),
    }),
    importPreviewSchema,
  )
}

export async function confirmCopyImport(rows: CopyImportRow[]) {
  return readResponse(
    await authenticatedFetch(`${URL}/import/confirm`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ rows }),
    }),
    z.array(copySchema),
  )
}

export async function exportCopies(filters: CopyFilters) {
  const query = new URLSearchParams()
  Object.entries(filters).forEach(([key, value]) => {
    if (value !== undefined && value !== '') query.set(key, String(value))
  })
  await downloadResponse(await authenticatedFetch(`${URL}/export?${query}`), 'book-copies.csv')
}
