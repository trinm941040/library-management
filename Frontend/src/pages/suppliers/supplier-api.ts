import { z } from 'zod'
import { authenticatedFetch, guidSchema, readResponse } from '@/auth/auth-api'

const URL = '/api/v1/suppliers'
export const supplierStatusSchema = z.enum(['Active', 'Inactive'])
const supplierSchema = z.object({
  id: guidSchema, code: z.string(), name: z.string(),
  contactName: z.string().nullable(), email: z.string().nullable(),
  phoneNumber: z.string().nullable(), address: z.string().nullable(),
  status: supplierStatusSchema, concurrencyToken: guidSchema,
  hasStockReceipts: z.boolean(),
})
const pageSchema = z.object({
  items: z.array(supplierSchema), pageNumber: z.number().int().positive(),
  pageSize: z.number().int().positive(), totalCount: z.number().int().nonnegative(),
  totalPages: z.number().int().nonnegative(),
})

export type Supplier = z.infer<typeof supplierSchema>
export type SupplierStatus = z.infer<typeof supplierStatusSchema>
export type SupplierInput = {
  code: string; name: string; contactName?: string | null; email?: string | null;
  phoneNumber?: string | null; address?: string | null; concurrencyToken?: string
}
export type SupplierFilters = { search?: string; status?: SupplierStatus; pageNumber?: number; pageSize?: number }

export async function getSuppliers(filters: SupplierFilters, signal?: AbortSignal) {
  const query = new URLSearchParams()
  Object.entries(filters).forEach(([key, value]) => {
    if (value !== undefined && value !== '') query.set(key, String(value))
  })
  return readResponse(await authenticatedFetch(`${URL}?${query}`, { signal }), pageSchema)
}
export async function getSupplier(id: string, signal?: AbortSignal) {
  return readResponse(await authenticatedFetch(`${URL}/${id}`, { signal }), supplierSchema)
}
export async function getActiveSuppliers(signal?: AbortSignal) {
  return readResponse(await authenticatedFetch(`${URL}/active`, { signal }), z.array(supplierSchema))
}
export async function createSupplier(input: SupplierInput) {
  return readResponse(await authenticatedFetch(URL, {
    method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(input),
  }), supplierSchema)
}
export async function updateSupplier(id: string, input: SupplierInput) {
  return readResponse(await authenticatedFetch(`${URL}/${id}`, {
    method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(input),
  }), supplierSchema)
}
export async function changeSupplierStatus(supplier: Supplier, status: SupplierStatus) {
  const target = status === 'Active' ? `${URL}/${supplier.id}/activate` : `${URL}/${supplier.id}`
  return readResponse(await authenticatedFetch(target, {
    method: status === 'Active' ? 'POST' : 'DELETE',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ concurrencyToken: supplier.concurrencyToken }),
  }), supplierSchema)
}
