import { z } from 'zod'
import { authenticatedFetch, guidSchema, readResponse } from '@/auth/auth-api'
import { bulkResultSchema, importFieldErrorSchema } from '@/shared/data/table-contracts'

const BOOKS_URL = '/api/v1/books'
const referenceSchema = z.object({ id: guidSchema, name: z.string() })
const bookSchema = z.object({
  id: guidSchema, title: z.string(), author: z.string(), isbn: z.string(), category: z.string(),
  createdAtUtc: z.string(), updatedAtUtc: z.string().nullable(),
  authors: z.array(referenceSchema).nullish(), categories: z.array(referenceSchema).nullish(),
  publisher: referenceSchema.nullish(), availableCopyCount: z.number().int().nonnegative().nullish(),
  status: z.enum(['Active', 'Inactive']), concurrencyToken: guidSchema,
  description: z.string().nullable(), editionStatement: z.string().nullable(),
  publicationYear: z.number().int().nullable(), language: z.string().nullable(),
  pageCount: z.number().int().positive().nullable(),
})
const bookPageSchema = z.object({
  items: z.array(bookSchema), pageNumber: z.number().int().positive(),
  pageSize: z.number().int().positive().max(100), totalCount: z.number().int().nonnegative(),
  totalPages: z.number().int().nonnegative(),
})
const importRowSchema = z.object({
  rowNumber: z.number().int().positive(), title: z.string(), author: z.string(),
  isbn: z.string(), category: z.string(), publisher: z.string().nullable().optional(),
  description: z.string().nullable().optional(), editionStatement: z.string().nullable().optional(),
  publicationYear: z.number().int().nullable().optional(), language: z.string().nullable().optional(),
  pageCount: z.number().int().positive().nullable().optional(),
})
const importPreviewSchema = z.object({
  rows: z.array(importRowSchema), errors: z.array(importFieldErrorSchema),
  checksum: z.string().length(64), canConfirm: z.boolean(),
})
const importResultSchema = z.object({
  importedCount: z.number().int().nonnegative(), errors: z.array(importFieldErrorSchema),
  correlationId: z.string(),
})

export type LibraryBook = z.infer<typeof bookSchema>
export type BookPageResponse = z.infer<typeof bookPageSchema>
export type BookImportPreview = z.infer<typeof importPreviewSchema>
export type BookImportResult = z.infer<typeof importResultSchema>
export type BookReference = z.infer<typeof referenceSchema>
export type BookFilters = {
  search?: string
  category?: string
  pageNumber?: number
  pageSize?: number
  sortBy?: 'title' | 'author' | 'isbn' | 'category' | 'createdAtUtc'
  sortDirection?: 'asc' | 'desc'
  status?: 'Active' | 'Inactive'
}
export type BookInput = {
  title: string; author: string; isbn: string; category: string
  publisherName?: string | null; description?: string | null; editionStatement?: string | null
  publicationYear?: number | null; language?: string | null; pageCount?: number | null
  concurrencyToken?: string
}

function queryString(filters: BookFilters) {
  const query = new URLSearchParams()
  Object.entries(filters).forEach(([key, value]) => {
    if (value !== undefined && value !== '') query.set(key, String(value))
  })
  query.set('pageNumber', String(filters.pageNumber ?? 1))
  query.set('pageSize', String(filters.pageSize ?? 20))
  return query.toString()
}

export async function getBooks(filters: BookFilters, signal?: AbortSignal) {
  return readResponse(await authenticatedFetch(`${BOOKS_URL}?${queryString(filters)}`, { signal }), bookPageSchema)
}
export async function getBook(id: string, signal?: AbortSignal) {
  return readResponse(await authenticatedFetch(`${BOOKS_URL}/${id}`, { signal }), bookSchema)
}
export async function getCatalogReferences(type: 'authors' | 'publishers' | 'categories', signal?: AbortSignal) {
  return readResponse(
    await authenticatedFetch(`${BOOKS_URL}/catalog-references?type=${type}`, { signal }),
    z.array(referenceSchema),
  )
}
export async function createBook(input: BookInput) {
  return readResponse(await authenticatedFetch(BOOKS_URL, {
    method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(input),
  }), bookSchema)
}
export async function updateBook(id: string, input: BookInput) {
  return readResponse(await authenticatedFetch(`${BOOKS_URL}/${id}`, {
    method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(input),
  }), bookSchema)
}
export async function deleteBook(id: string) {
  await readResponse(await authenticatedFetch(`${BOOKS_URL}/${id}`, { method: 'DELETE' }))
}
export async function bulkDeleteBooks(ids: readonly string[]) {
  return readResponse(await authenticatedFetch(`${BOOKS_URL}/bulk-delete`, {
    method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ ids }),
  }), bulkResultSchema)
}
export async function previewBookImport(file: File) {
  const body = new FormData()
  body.append('file', file)
  return readResponse(await authenticatedFetch(`${BOOKS_URL}/import/preview`, { method: 'POST', body }), importPreviewSchema)
}
export async function confirmBookImport(preview: BookImportPreview) {
  return readResponse(await authenticatedFetch(`${BOOKS_URL}/import/confirm`, {
    method: 'POST', headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ rows: preview.rows, checksum: preview.checksum }),
  }), importResultSchema)
}
export function exportBooks(filters: BookFilters, signal?: AbortSignal) {
  return authenticatedFetch(`${BOOKS_URL}/export?${queryString(filters)}`, { signal })
}

export function downloadBookImportTemplate() {
  const content = [
    'Title,Author,ISBN,Category,Publisher,Description,EditionStatement,PublicationYear,Language,PageCount',
    'Dế Mèn phiêu lưu ký,Tô Hoài,9786044832814,Văn học,Nhà xuất bản Kim Đồng,,Tái bản,2024,Tiếng Việt,160',
  ].join('\r\n')
  const url = URL.createObjectURL(new Blob([`\uFEFF${content}`], { type: 'text/csv;charset=utf-8' }))
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = 'mau-nhap-bieu-ghi-sach.csv'
  anchor.click()
  URL.revokeObjectURL(url)
}
