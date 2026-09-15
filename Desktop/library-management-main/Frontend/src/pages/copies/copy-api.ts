import { authenticatedFetch } from '@/auth/auth-api'

const COPIES_URL = '/api/v1/copies'
export type CopyStatus = 'Available' | 'Borrowed' | 'Reserved' | 'InTransit' | 'Lost' | 'Damaged' | 'Withdrawn'
export type CopyCondition = 'New' | 'Good' | 'Worn' | 'Damaged' | 'Lost'
export type BookCopy = { id: string; bookId: string; bookTitle: string; barcode: string; condition: CopyCondition; status: CopyStatus; acquiredAtUtc: string; shelfId: string | null; shelfCode: string | null; branchId: string | null; branchCode: string | null; stockReceiptItemId: string | null; concurrencyToken: string }
export type CopyPage = { items: BookCopy[]; pageNumber: number; pageSize: number; totalCount: number; totalPages: number }
export type CopyFilters = { search?: string; status?: CopyStatus; pageNumber?: number; pageSize?: number }
type Problem = { detail?: string; title?: string }
async function read<T>(response: Response): Promise<T> { if (response.ok) return response.status === 204 ? (undefined as T) : response.json(); const problem = (await response.json().catch(() => null)) as Problem | null; throw new Error(problem?.detail ?? problem?.title ?? 'Không thể xử lý bản sao.') }
export async function getCopies(filters: CopyFilters, signal?: AbortSignal) { const query = new URLSearchParams(); if (filters.search) query.set('search', filters.search); if (filters.status) query.set('status', filters.status); query.set('pageNumber', String(filters.pageNumber ?? 1)); query.set('pageSize', String(filters.pageSize ?? 20)); return read<CopyPage>(await authenticatedFetch(`${COPIES_URL}?${query}`, { signal })) }
export async function getCopyByBarcode(barcode: string) { return read<BookCopy>(await authenticatedFetch(`${COPIES_URL}/barcode/${encodeURIComponent(barcode)}`)) }
export async function updateCopyStatus(copy: BookCopy, status: CopyStatus) { return read<BookCopy>(await authenticatedFetch(`${COPIES_URL}/${copy.id}/status`, { method: 'PATCH', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ status, concurrencyToken: copy.concurrencyToken }) })) }
export async function relocateCopy(copy: BookCopy, shelfId: string) { return read<BookCopy>(await authenticatedFetch(`${COPIES_URL}/${copy.id}/relocate`, { method: 'PATCH', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ shelfId, concurrencyToken: copy.concurrencyToken }) })) }
