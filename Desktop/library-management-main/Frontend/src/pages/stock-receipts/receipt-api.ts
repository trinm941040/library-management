import { authenticatedFetch } from '@/auth/auth-api'

const URL = '/api/v1/stock-receipts'
export type ReceiptStatus = 'Draft' | 'Received' | 'Confirmed' | 'Cancelled'
export type ReceiptItem = { id: string; bookId: string; bookTitle: string; isbn: string; expectedQuantity: number; receivedQuantity: number; damagedQuantity: number; unitCost: number | null; totalValue: number; concurrencyToken: string }
export type StockReceipt = { id: string; receiptNumber: string; supplierId: string; supplierName: string; branchId: string; branchCode: string; receivedByUserId: string; status: ReceiptStatus; receivedAtUtc: string; notes: string | null; concurrencyToken: string; totalQuantity: number; totalValue: number; items: ReceiptItem[] }
export type ReceiptInput = { supplierId: string; branchId: string; receivedAtUtc: string; notes?: string | null; items: Array<{ bookId: string; expectedQuantity: number; receivedQuantity: number; damagedQuantity: number; unitCost: number | null }>; concurrencyToken?: string }
type Problem = { detail?: string; title?: string }
async function read<T>(response: Response): Promise<T> { if (response.ok) return response.json(); const problem = (await response.json().catch(() => null)) as Problem | null; throw new Error(problem?.detail ?? problem?.title ?? 'Không thể xử lý phiếu nhập.') }
export async function getReceipts(search?: string, signal?: AbortSignal) { const query = new URLSearchParams(); if (search) query.set('search', search); return read<{ items: StockReceipt[]; totalCount: number }>(await authenticatedFetch(`${URL}?${query}`, { signal })) }
export async function createReceipt(input: ReceiptInput) { return read<StockReceipt>(await authenticatedFetch(URL, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(input) })) }
export async function updateReceipt(id: string, input: ReceiptInput) { return read<StockReceipt>(await authenticatedFetch(`${URL}/${id}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(input) })) }
