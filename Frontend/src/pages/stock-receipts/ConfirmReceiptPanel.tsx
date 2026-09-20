import { useEffect, useMemo, useState } from 'react'
import { Barcode, CheckCircle2 } from 'lucide-react'
import { Button } from '@/common/components/ui/button'
import { Input } from '@/common/components/ui/input'
import { useToast } from '@/common/components'
import { useAuth } from '@/auth/AuthProvider'
import { canAll } from '@/shared/auth/permissions'
import { getLocations, type LocationNode } from '@/pages/branches/branch-api'
import { confirmReceipt, getReceiptConfirmation, type StockReceipt, type ReceiptConfirmation } from './receipt-api'

type DraftCopy = { barcode: string; shelfId: string; condition: 'New' | 'Good' | 'Worn' | 'Damaged' }
const conditionNames = { New: 'Mới', Good: 'Tốt', Worn: 'Cũ', Damaged: 'Hỏng' }
const statusNames: Record<string, string> = { Available: 'Có sẵn', Borrowed: 'Đang mượn',
  Reserved: 'Đã đặt trước', InTransit: 'Đang chuyển', Lost: 'Thất lạc',
  Damaged: 'Hỏng', Withdrawn: 'Đã thanh lý' }
const seed = (receipt: StockReceipt) => Object.fromEntries(receipt.items.map(item => [item.id,
  Array.from({ length: receipt.items.reduce((sum, row) => sum + row.receivedQuantity, 0) > 1000 ? 0 : item.receivedQuantity }, (_, index): DraftCopy => ({ barcode: '', shelfId: '',
    condition: index < item.damagedQuantity ? 'Damaged' : 'Good' }))])) as Record<string, DraftCopy[]>
const flattenShelves = (node: LocationNode): LocationNode[] =>
  [...(node.type === 'Shelf' && node.isActive ? [node] : []),
    ...node.children.flatMap(child => node.isActive ? flattenShelves(child) : [])]

export function ConfirmReceiptPanel({ receipt, onConfirmed }: {
  receipt: StockReceipt; onConfirmed: (value: StockReceipt) => void
}) {
  const { user } = useAuth()
  const { showToast } = useToast()
  const [copies, setCopies] = useState<Record<string, DraftCopy[]>>(() => seed(receipt))
  const [shelves, setShelves] = useState<LocationNode[]>([])
  const [bulkShelf, setBulkShelf] = useState('')
  const [barcodePrefix, setBarcodePrefix] = useState('')
  const [confirmation, setConfirmation] = useState<ReceiptConfirmation | null>(null)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const mayConfirm = canAll(user?.permissions ?? [], ['stock-receipts.confirm'])
  useEffect(() => { const controller = new AbortController()
    void getReceiptConfirmation(receipt.id, controller.signal).then(setConfirmation).catch(() => {})
    void getLocations(controller.signal).then(nodes => {
      const branch = nodes.find(node => node.id === receipt.branchId)
      setShelves(branch ? flattenShelves(branch) : [])
    }).catch(() => setError('Không thể tải kệ đang hoạt động.'))
    return () => controller.abort()
  }, [receipt.id, receipt.branchId])
  const discrepancyPreview = useMemo(() => receipt.items.flatMap(item => {
    const rows: Array<{ type: string; expected: number; actual: number; description: string }> = []
    if (item.expectedQuantity !== item.receivedQuantity) rows.push({
      type: item.receivedQuantity < item.expectedQuantity ? 'Thiếu' : 'Thừa',
      expected: item.expectedQuantity, actual: item.receivedQuantity,
      description: `${item.bookTitle}: dự kiến ${item.expectedQuantity}, thực nhận ${item.receivedQuantity}.` })
    if (item.damagedQuantity > 0) rows.push({ type: 'Hỏng', expected: item.receivedQuantity,
      actual: item.receivedQuantity - item.damagedQuantity,
      description: `${item.bookTitle}: ${item.damagedQuantity} bản sao hỏng.` })
    return rows
  }), [receipt.items])
  const allCopies = Object.values(copies).flat()
  const normalized = allCopies.map(copy => copy.barcode.trim().toUpperCase())
  const valid = allCopies.length === receipt.items.reduce((sum, item) => sum + item.receivedQuantity, 0) &&
    allCopies.every(copy => copy.barcode.trim() && copy.shelfId && shelves.some(shelf => shelf.id === copy.shelfId)) &&
    new Set(normalized).size === normalized.length && receipt.items.every(item =>
      (copies[item.id] ?? []).filter(copy => copy.condition === 'Damaged').length === item.damagedQuantity)
  const changeCopy = (itemId: string, index: number, patch: Partial<DraftCopy>) =>
    setCopies(current => ({ ...current, [itemId]: current[itemId].map((copy, i) => i === index ? { ...copy, ...patch } : copy) }))
  const assignBulkShelf = () => { if (!bulkShelf) return
    setCopies(current => Object.fromEntries(Object.entries(current).map(([key, rows]) =>
      [key, rows.map(row => ({ ...row, shelfId: bulkShelf }))]))) }
  const assignBarcodes = () => { const prefix = barcodePrefix.trim().toUpperCase()
    if (!prefix) return
    let counter = 0
    setCopies(current => Object.fromEntries(Object.entries(current).map(([key, rows]) =>
      [key, rows.map(row => ({ ...row, barcode: `${prefix}-${String(++counter).padStart(4, '0')}` }))]))) }
  const submit = async () => {
    if (!valid || !window.confirm(`Xác nhận phiếu và tạo ${allCopies.length} bản sao? Thao tác này không thể sửa lại.`)) return
    setBusy(true); setError('')
    try { const result = await confirmReceipt(receipt.id, { concurrencyToken: receipt.concurrencyToken,
      items: receipt.items.map(item => ({ stockReceiptItemId: item.id, copies: copies[item.id] })) })
      setConfirmation(result); onConfirmed(result.receipt)
      showToast(`Đã xác nhận phiếu và tạo ${result.copies.length} bản sao.`)
    } catch (reason) { setError(reason instanceof Error ? reason.message : 'Không thể xác nhận. Vui lòng tải lại phiếu nếu có xung đột.') }
    finally { setBusy(false) }
  }
  if (receipt.status === 'Confirmed') return <section className="grid gap-3 rounded-xl border bg-card p-4" aria-label="Kết quả nhập kho">
    <h2 className="flex items-center gap-2 text-lg font-semibold"><CheckCircle2 /> Đã xác nhận nhập kho</h2>
    <p>{confirmation?.copies.length ?? 0} bản sao · {confirmation?.discrepancies.length ?? 0} báo cáo sai lệch.</p>
    {confirmation?.receipt.items.map(item => {
      const itemCopies = confirmation.copies.filter(copy => copy.stockReceiptItemId === item.id)
      return <div className="grid gap-1 rounded-md border p-3" key={item.id}>
        <h3 className="font-medium">{item.bookTitle} · {itemCopies.length} bản sao</h3>
        {itemCopies.map(copy => <p className="text-sm" key={copy.id}>{copy.barcode} · {conditionNames[copy.condition as keyof typeof conditionNames] ?? copy.condition} · {statusNames[copy.status] ?? copy.status}</p>)}
      </div>
    })}
    {confirmation?.discrepancies.map(report => <p className="text-sm" key={report.id}>{report.type}: {report.expectedQuantity} → {report.actualQuantity} · {report.description} · {new Date(report.createdAtUtc).toLocaleString('vi-VN')}</p>)}
  </section>
  if (receipt.status === 'Cancelled' || !mayConfirm) return null
  return <section className="grid gap-4 rounded-xl border bg-card p-4" aria-label="Xác nhận nhập kho">
    <h2 className="text-lg font-semibold">Gán mã vạch và vị trí</h2>
    <div className="flex flex-wrap gap-2"><select aria-label="Kệ áp dụng hàng loạt" className="h-10 rounded-md border bg-background px-3" value={bulkShelf} onChange={e => setBulkShelf(e.target.value)}><option value="">Chọn kệ cho tất cả</option>{shelves.map(shelf => <option key={shelf.id} value={shelf.id}>{shelf.code} · {shelf.name}</option>)}</select><Button variant="outline" onClick={assignBulkShelf} disabled={!bulkShelf}>Gán kệ</Button>
      <Input className="max-w-xs" aria-label="Tiền tố mã vạch" placeholder="Tiền tố mã vạch" value={barcodePrefix} onChange={e => setBarcodePrefix(e.target.value)} /><Button variant="outline" onClick={assignBarcodes} disabled={!barcodePrefix.trim()}><Barcode /> Tạo mã hàng loạt</Button></div>
    {receipt.items.map(item => <div className="grid gap-2 rounded-md border p-3" key={item.id}>
      <h3 className="font-medium">{item.bookTitle} · {item.isbn} · {item.receivedQuantity} bản sao</h3>
      {(copies[item.id] ?? []).map((copy, index) => <div className="grid gap-2 sm:grid-cols-3" key={index}>
        <Input aria-label={`Mã vạch ${item.bookTitle} bản ${index + 1}`} placeholder={`Mã vạch ${index + 1}`} value={copy.barcode} onChange={e => changeCopy(item.id, index, { barcode: e.target.value })} />
        <select aria-label={`Kệ ${item.bookTitle} bản ${index + 1}`} className="h-10 rounded-md border bg-background px-3" value={copy.shelfId} onChange={e => changeCopy(item.id, index, { shelfId: e.target.value })}><option value="">Chọn kệ</option>{shelves.map(shelf => <option key={shelf.id} value={shelf.id}>{shelf.code} · {shelf.name}</option>)}</select>
        <select aria-label={`Tình trạng ${item.bookTitle} bản ${index + 1}`} className="h-10 rounded-md border bg-background px-3" value={copy.condition} onChange={e => changeCopy(item.id, index, { condition: e.target.value as DraftCopy['condition'] })}>{Object.entries(conditionNames).map(([value, name]) => <option key={value} value={value}>{name}</option>)}</select>
      </div>)}</div>)}
    <div className="grid gap-2 rounded-md border p-3"><h3 className="font-semibold">Xem trước sai lệch</h3>{discrepancyPreview.length ? discrepancyPreview.map((row, index) => <p className="text-sm" key={index}>{row.type}: {row.expected} → {row.actual} · {row.description}</p>) : <p className="text-sm">Không có sai lệch.</p>}</div>
    {error ? <p role="alert" className="text-sm text-destructive">{error}</p> : null}
    {!valid ? <p className="text-sm text-muted-foreground">Điền mã vạch duy nhất, kệ hợp lệ và đúng số bản sao hỏng trước khi xác nhận. Một phiếu tối đa 1000 bản sao.</p> : null}
    <Button className="w-fit" disabled={!valid || busy} onClick={() => void submit()}>{busy ? 'Đang xác nhận...' : 'Xác nhận nhập kho'}</Button>
  </section>
}
