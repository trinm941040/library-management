import { z } from 'zod'

export const pageSizeOptions = [10, 20, 50, 100] as const
export const sortDirectionSchema = z.enum(['asc', 'desc'])

export const tableUrlStateSchema = z.object({
  search: z.string().trim().max(200).default(''),
  pageNumber: z.coerce.number().int().positive().max(1_000_000).catch(1),
  pageSize: z.coerce.number().pipe(z.union([z.literal(10), z.literal(20), z.literal(50), z.literal(100)])).catch(20),
  sortBy: z.string().trim().max(50),
  sortDirection: sortDirectionSchema.catch('asc'),
})

export type TableUrlState = z.infer<typeof tableUrlStateSchema>

export function parseTableUrlState(
  params: URLSearchParams,
  allowedSortFields: readonly string[],
  defaultSort: string,
): TableUrlState {
  const parsed = tableUrlStateSchema.parse({
    search: params.get('search') ?? '',
    pageNumber: params.get('pageNumber') ?? params.get('page') ?? 1,
    pageSize: params.get('pageSize') ?? 20,
    sortBy: params.get('sortBy') ?? defaultSort,
    sortDirection: params.get('sortDirection') ?? 'asc',
  })
  return { ...parsed, sortBy: allowedSortFields.includes(parsed.sortBy) ? parsed.sortBy : defaultSort }
}

export function updateSearchParams(
  current: URLSearchParams,
  changes: Record<string, string | number | undefined>,
) {
  const next = new URLSearchParams(current)
  Object.entries(changes).forEach(([key, value]) => {
    if (value === undefined || value === '' || value === 'all') next.delete(key)
    else next.set(key, String(value))
  })
  return next
}

export const importFieldErrorSchema = z.object({
  rowNumber: z.number().int().positive(),
  field: z.string(),
  message: z.string(),
})

export const bulkResultSchema = z.object({
  items: z.array(z.object({ id: z.string().uuid(), succeeded: z.boolean(), error: z.string().nullable() })),
  succeededCount: z.number().int().nonnegative(),
  failedCount: z.number().int().nonnegative(),
  correlationId: z.string(),
})

export type BulkResult = z.infer<typeof bulkResultSchema>

export async function downloadResponse(response: Response, fallbackName: string) {
  if (!response.ok) throw new Error('Không thể xuất dữ liệu.')
  const disposition = response.headers.get('Content-Disposition') ?? ''
  const encoded = disposition.match(/filename\*=UTF-8''([^;]+)/i)?.[1]
  const plain = disposition.match(/filename="?([^";]+)"?/i)?.[1]
  const fileName = encoded ? decodeURIComponent(encoded) : plain ?? fallbackName
  const url = URL.createObjectURL(await response.blob())
  const link = document.createElement('a')
  link.href = url
  link.download = fileName
  link.click()
  URL.revokeObjectURL(url)
}
