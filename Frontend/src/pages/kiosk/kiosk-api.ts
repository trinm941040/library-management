import { z } from 'zod'
import { guidSchema, publicFetch, readResponse } from '@/auth/auth-api'

const KIOSK_URL = '/api/v1/kiosk'

const kioskBookSchema = z.object({
  id: guidSchema,
  title: z.string(),
  author: z.string(),
  isbn: z.string(),
  category: z.string(),
  description: z.string().nullable(),
  availableCopies: z.number().int().nonnegative(),
})

const kioskBookPageSchema = z.object({
  items: z.array(kioskBookSchema),
  pageNumber: z.number().int().positive(),
  pageSize: z.number().int().positive(),
  totalCount: z.number().int().nonnegative(),
  totalPages: z.number().int().nonnegative(),
})

const semanticBookSchema = kioskBookSchema.extend({
  similarity: z.number().min(0).max(1),
  totalCopies: z.number().int().nonnegative(),
})

const semanticResponseSchema = z.object({
  items: z.array(semanticBookSchema),
  scoreMeaning: z.string(),
})

const kioskBookDetailSchema = kioskBookSchema.extend({
  editionStatement: z.string().nullable(),
  publicationYear: z.number().int().nullable(),
  language: z.string().nullable(),
  pageCount: z.number().int().positive().nullable(),
  totalCopies: z.number().int().nonnegative(),
  locations: z.array(
    z.object({
      label: z.string(),
      availableCopies: z.number().int().positive(),
    }),
  ),
})

export type KioskBook = z.infer<typeof kioskBookSchema>
export type KioskSemanticBook = z.infer<typeof semanticBookSchema>
export type KioskBookDetail = z.infer<typeof kioskBookDetailSchema>

export async function searchKioskBooks(search: string, signal?: AbortSignal) {
  const query = new URLSearchParams({ search, pageNumber: '1', pageSize: '20' })
  return readResponse(
    await publicFetch(`${KIOSK_URL}/books?${query}`, { signal }),
    kioskBookPageSchema,
  )
}

export async function semanticSearchKioskBooks(query: string, signal?: AbortSignal) {
  return readResponse(
    await publicFetch(`${KIOSK_URL}/books/semantic-search`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'X-Background-Request': 'true' },
      body: JSON.stringify({ query, categoryId: null, availableOnly: false, topK: 10 }),
      signal,
    }),
    semanticResponseSchema,
  )
}

export async function getKioskBook(id: string, signal?: AbortSignal) {
  return readResponse(
    await publicFetch(`${KIOSK_URL}/books/${id}`, { signal }),
    kioskBookDetailSchema,
  )
}
