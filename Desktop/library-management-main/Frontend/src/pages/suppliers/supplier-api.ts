import { authenticatedFetch } from '@/auth/auth-api'

const SUPPLIERS_URL = '/api/v1/suppliers'
export type SupplierStatus = 'Active' | 'Inactive'
export type Supplier = { id: string; code: string; name: string; contactName: string | null; email: string | null; phoneNumber: string | null; address: string | null; status: SupplierStatus; concurrencyToken: string; hasStockReceipts: boolean }
export type SupplierPage = { items: Supplier[]; pageNumber: number; pageSize: number; totalCount: number; totalPages: number }
export type SupplierInput = { code: string; name: string; contactName?: string | null; email?: string | null; phoneNumber?: string | null; address?: string | null; concurrencyToken?: string }
type Problem = { detail?: string; title?: string }
async function read<T>(response: Response): Promise<T> { if (response.ok) return response.status === 204 ? (undefined as T) : response.json(); const problem = (await response.json().catch(() => null)) as Problem | null; throw new Error(problem?.detail ?? problem?.title ?? 'Không thể xử lý nhà cung cấp.') }
export async function getSuppliers(search?: string, status?: SupplierStatus, signal?: AbortSignal) { const query = new URLSearchParams(); if (search) query.set('search', search); if (status) query.set('status', status); return read<SupplierPage>(await authenticatedFetch(`${SUPPLIERS_URL}?${query}`, { signal })) }
export async function createSupplier(input: SupplierInput) { return read<Supplier>(await authenticatedFetch(SUPPLIERS_URL, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(input) })) }
export async function updateSupplier(id: string, input: SupplierInput) { return read<Supplier>(await authenticatedFetch(`${SUPPLIERS_URL}/${id}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(input) })) }
export async function deactivateSupplier(supplier: Supplier) { return read<void>(await authenticatedFetch(`${SUPPLIERS_URL}/${supplier.id}`, { method: 'DELETE' })) }
