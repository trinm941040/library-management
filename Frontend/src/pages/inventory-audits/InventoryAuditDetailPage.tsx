import { useCallback, useEffect, useRef, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { Barcode, Check, Download, RefreshCw } from 'lucide-react'
import { PageShell, ScreenState, useToast } from '@/common/components'
import { Button } from '@/common/components/ui/button'
import { Input } from '@/common/components/ui/input'
import { PermissionBoundary } from '@/shared/auth/PermissionBoundary'
import type { LocationNode } from '@/pages/branches/branch-api'
import { applyAuditCorrection, completeAudit, exportAudit, getAudit, reconcileAudit, scanAudit,
  getAuditLocations, type CopyCondition, type CopyStatus, type InventoryAudit, type InventoryAuditItem } from './inventory-audit-api'

const resultName: Record<InventoryAuditItem['result'], string> = {
  Pending: 'Chờ quét', Found: 'Đúng', Misplaced: 'Sai vị trí', Missing: 'Thiếu',
  Damaged: 'Hỏng', Unexpected: 'Ngoài phạm vi', StatusMismatch: 'Sai trạng thái',
  ConditionMismatch: 'Sai tình trạng',
}
const copyStatusNames: Record<CopyStatus, string> = {
  Available: 'Có sẵn', Borrowed: 'Đang mượn', Reserved: 'Đã đặt trước',
  InTransit: 'Đang chuyển', Lost: 'Thất lạc', Damaged: 'Hỏng', Withdrawn: 'Đã thanh lý',
}
const conditionNames: Record<CopyCondition, string> = {
  New: 'Mới', Good: 'Tốt', Worn: 'Cũ', Damaged: 'Hỏng', Lost: 'Thất lạc',
}
const shelvesFrom = (node: LocationNode): LocationNode[] =>
  [...(node.type === 'Shelf' ? [node] : []), ...node.children.flatMap(shelvesFrom)]

export function InventoryAuditDetailPage() {
  const { id } = useParams()
  const { showToast } = useToast()
  const scanner = useRef<HTMLInputElement>(null)
  const [audit, setAudit] = useState<InventoryAudit | null>(null)
  const [review, setReview] = useState<InventoryAudit | null>(null)
  const [shelves, setShelves] = useState<LocationNode[]>([])
  const [barcode, setBarcode] = useState('')
  const [actualShelfId, setActualShelfId] = useState('')
  const [actualStatus, setActualStatus] = useState<CopyStatus>('Available')
  const [actualCondition, setActualCondition] = useState<CopyCondition>('Good')
  const [acknowledge, setAcknowledge] = useState(false)
  const [correction, setCorrection] = useState<InventoryAuditItem | null>(null)
  const [correctionShelf, setCorrectionShelf] = useState('')
  const [correctionStatus, setCorrectionStatus] = useState<CopyStatus | ''>('')
  const [correctionCondition, setCorrectionCondition] = useState<CopyCondition | ''>('')
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const load = useCallback((signal?: AbortSignal) => { if (!id) return Promise.resolve()
    setLoading(true); setError('')
    return getAudit(id, signal).then(value => { setAudit(value); setActualShelfId(value.shelfId ?? '') })
      .catch((reason: unknown) => { if (reason instanceof DOMException && reason.name === 'AbortError') return
        setError(reason instanceof Error ? reason.message : 'Không thể tải đợt kiểm kê.') })
      .finally(() => { if (!signal?.aborted) setLoading(false) })
  }, [id])
  useEffect(() => { const controller = new AbortController(); void load(controller.signal)
    return () => controller.abort() }, [load])
  useEffect(() => { const controller = new AbortController()
    void getAuditLocations(controller.signal).then(nodes => setShelves(nodes.flatMap(shelvesFrom)))
      .catch(() => setError('Không thể tải vị trí thực tế.'))
    return () => controller.abort() }, [])
  useEffect(() => { const row = audit?.items.find(item => item.barcode.toUpperCase() === barcode.trim().toUpperCase())
    if (row) { setActualStatus(row.expectedStatus); setActualCondition(row.expectedCondition) }
  }, [barcode, audit])
  const scan = async () => { if (!audit || !barcode.trim() || !actualShelfId || busy) return
    setBusy(true); setError('')
    try { const saved = await scanAudit(audit.id, { barcode: barcode.trim(), actualShelfId,
      actualStatus, actualCondition, concurrencyToken: audit.concurrencyToken })
      setAudit(saved); setReview(null); setBarcode(''); scanner.current?.focus()
      showToast('Đã ghi nhận mã vạch.')
    } catch (reason) { setError(reason instanceof Error ? reason.message : 'Không thể ghi nhận mã vạch. Tải lại nếu có xung đột.') }
    finally { setBusy(false) }
  }
  const reconcile = async () => { if (!audit) return
    setBusy(true); setError('')
    try { setReview(await reconcileAudit(audit.id)) }
    catch (reason) { setError(reason instanceof Error ? reason.message : 'Không thể đối soát.') }
    finally { setBusy(false) }
  }
  const complete = async () => { if (!audit || !review || busy ||
      (review.discrepancyCount > 0 && !acknowledge) ||
      !window.confirm('Hoàn tất sẽ khóa dữ liệu quét của đợt kiểm kê. Tiếp tục?')) return
    setBusy(true); setError('')
    try { const saved = await completeAudit(audit.id, { concurrencyToken: audit.concurrencyToken,
      acknowledgeDiscrepancies: acknowledge }); setAudit(saved); setReview(null)
      showToast('Đã hoàn tất đợt kiểm kê.') }
    catch (reason) { setError(reason instanceof Error ? reason.message : 'Không thể hoàn tất. Tải lại nếu có xung đột.') }
    finally { setBusy(false) }
  }
  const download = async () => { if (!audit) return
    try { const blob = await exportAudit(audit.id); const url = URL.createObjectURL(blob)
      const link = document.createElement('a'); link.href = url; link.download = `inventory-audit-${audit.id}.csv`
      link.click(); URL.revokeObjectURL(url) }
    catch (reason) { setError(reason instanceof Error ? reason.message : 'Không thể xuất kiểm kê.') }
  }
  const applyCorrection = async () => {
    if (!audit || !correction || busy ||
      (!correctionShelf && !correctionStatus && !correctionCondition) ||
      !window.confirm('Áp dụng điều chỉnh đã chọn lên bản sao này? Thao tác được ghi AuditLog.')) return
    setBusy(true); setError('')
    try { await applyAuditCorrection(audit.id, { bookCopyId: correction.bookCopyId,
      concurrencyToken: correction.copyConcurrencyToken, shelfId: correctionShelf || null,
      status: correctionStatus || null, condition: correctionCondition || null })
      setCorrection(null); setCorrectionShelf(''); setCorrectionStatus(''); setCorrectionCondition('')
      await load(); showToast('Đã áp dụng điều chỉnh bản sao.') }
    catch (reason) { setError(reason instanceof Error ? reason.message : 'Không thể áp dụng; tải lại nếu bản sao đã thay đổi.') }
    finally { setBusy(false) }
  }
  if (loading) return <ScreenState kind="loading" title="Đang tải kiểm kê" />
  if (!audit) return <ScreenState kind="error" title="Không tìm thấy đợt kiểm kê" description={error} onAction={() => void load()} actionLabel="Thử lại" />
  const progress = audit.expectedCount === 0 ? 100 : Math.min(100, Math.round(audit.scannedCount / audit.expectedCount * 100))
  const discrepancies = (review ?? audit).items.filter(item => item.result !== 'Found' && item.result !== 'Pending')
  return <PageShell eyebrow="Kho vật lý" title={`Đợt kiểm kê ${audit.id.slice(0, 8)}`}
    description={`${audit.status === 'InProgress' ? 'Đang kiểm kê' : 'Đã hoàn tất'} · Bắt đầu ${new Date(audit.startedAtUtc).toLocaleString('vi-VN')}`}
    actions={<><Button variant="outline" asChild><Link to="/inventory-audits">Danh sách</Link></Button>
      <Button variant="outline" onClick={() => void load()}><RefreshCw /> Làm mới</Button>
      <PermissionBoundary requiredPermissions={['inventory-audits.export']}><Button variant="outline" onClick={() => void download()}><Download /> Xuất CSV</Button></PermissionBoundary></>}>
    <div className="grid gap-5">
      <section className="grid gap-2 rounded-xl border bg-card p-4" aria-label="Tiến độ kiểm kê">
        <div className="flex flex-wrap justify-between gap-3"><span>Dự kiến: <strong>{audit.expectedCount}</strong> · Đã quét: <strong>{audit.scannedCount}</strong> · Chờ: <strong>{audit.pendingCount}</strong></span><span>{progress}%</span></div>
        <div className="h-2 overflow-hidden rounded-full bg-muted" role="progressbar" aria-valuenow={progress} aria-valuemin={0} aria-valuemax={100} aria-label="Tiến độ quét"><div className="h-full bg-primary" style={{ width: `${progress}%` }} /></div>
      </section>
      {audit.status === 'InProgress' ? <PermissionBoundary requiredPermissions={['inventory-audits.scan']}>
        <form className="grid gap-3 rounded-xl border bg-card p-4" onSubmit={event => { event.preventDefault(); void scan() }}>
          <h2 className="text-lg font-semibold">Quét mã vạch</h2>
          <Input ref={scanner} aria-label="Mã vạch" placeholder="Quét hoặc nhập mã vạch rồi Enter" value={barcode} onChange={event => setBarcode(event.target.value)} />
          <div className="grid gap-2 md:grid-cols-3"><select aria-label="Vị trí thực tế" className="h-10 rounded-md border bg-background px-3" value={actualShelfId} onChange={event => setActualShelfId(event.target.value)}><option value="">Chọn kệ thực tế</option>{shelves.map(shelf => <option key={shelf.id} value={shelf.id}>{shelf.code} · {shelf.name}</option>)}</select>
            <select aria-label="Trạng thái thực tế" className="h-10 rounded-md border bg-background px-3" value={actualStatus} onChange={event => setActualStatus(event.target.value as CopyStatus)}>{Object.entries(copyStatusNames).map(([value, name]) => <option key={value} value={value}>{name}</option>)}</select>
            <select aria-label="Tình trạng thực tế" className="h-10 rounded-md border bg-background px-3" value={actualCondition} onChange={event => setActualCondition(event.target.value as CopyCondition)}>{Object.entries(conditionNames).map(([value, name]) => <option key={value} value={value}>{name}</option>)}</select></div>
          <Button className="w-fit" type="submit" disabled={!barcode.trim() || !actualShelfId || busy}><Barcode /> {busy ? 'Đang ghi...' : 'Ghi nhận quét'}</Button>
        </form>
      </PermissionBoundary> : null}
      <section className="grid gap-3 rounded-xl border bg-card p-4" aria-label="Đối soát">
        <div className="flex flex-wrap items-center justify-between gap-2"><h2 className="text-lg font-semibold">Đối soát</h2>{audit.status === 'InProgress' ? <Button variant="outline" onClick={() => void reconcile()} disabled={busy}>Xem trước chênh lệch</Button> : null}</div>
        <p className="text-sm">{review ? 'Xem trước: bản sao chưa quét được tính là thiếu.' : 'Số chênh lệch đã ghi nhận:'} <strong>{review?.discrepancyCount ?? audit.discrepancyCount}</strong></p>
        {discrepancies.length ? <div className="overflow-x-auto"><table className="w-full min-w-[720px] text-sm"><thead><tr className="border-b text-left"><th className="p-2">Mã vạch</th><th className="p-2">Sách</th><th className="p-2">Phân loại</th><th className="p-2">Kệ dự kiến</th><th className="p-2">Kệ thực tế</th><th className="p-2">Trạng thái dự kiến/thực tế</th>{audit.status === 'Completed' ? <th className="p-2">Xử lý</th> : null}</tr></thead><tbody>{discrepancies.map(item => <tr className="border-b" key={item.id}><td className="p-2 font-mono">{item.barcode}</td><td className="p-2">{item.bookTitle}</td><td className="p-2">{resultName[item.result]}</td><td className="p-2">{item.expectedShelfId ? shelves.find(shelf => shelf.id === item.expectedShelfId)?.code ?? 'Không rõ' : 'Ngoài phạm vi'}</td><td className="p-2">{item.actualShelfId ? shelves.find(shelf => shelf.id === item.actualShelfId)?.code ?? 'Không rõ' : 'Chưa quét'}</td><td className="p-2">{copyStatusNames[item.expectedStatus]} / {item.actualStatus ? copyStatusNames[item.actualStatus] : 'Chưa quét'}</td>{audit.status === 'Completed' ? <td className="p-2"><PermissionBoundary requiredPermissions={['inventory-audits.apply']}><Button size="sm" variant="outline" onClick={() => { setCorrection(item); setCorrectionShelf(''); setCorrectionStatus(''); setCorrectionCondition('') }}>Điều chỉnh</Button></PermissionBoundary></td> : null}</tr>)}</tbody></table></div> : <p className="text-sm text-muted-foreground">Chưa có chênh lệch.</p>}
        {audit.status === 'Completed' && correction ? <PermissionBoundary requiredPermissions={['inventory-audits.apply']}>
          <div className="grid gap-3 rounded-md border p-3" aria-label="Điều chỉnh bản sao">
            <h3 className="font-medium">Điều chỉnh {correction.barcode}</h3>
            <p className="text-sm text-muted-foreground">Chỉ chọn trường cần sửa. Dữ liệu quét gốc được giữ nguyên.</p>
            <div className="grid gap-2 md:grid-cols-3"><select aria-label="Kệ đích" className="h-10 rounded-md border bg-background px-3" value={correctionShelf} onChange={event => setCorrectionShelf(event.target.value)}><option value="">Không đổi kệ</option>{shelves.map(shelf => <option key={shelf.id} value={shelf.id}>{shelf.code} · {shelf.name}</option>)}</select>
              <select aria-label="Trạng thái mới" className="h-10 rounded-md border bg-background px-3" value={correctionStatus} onChange={event => setCorrectionStatus(event.target.value as CopyStatus | '')}><option value="">Không đổi trạng thái</option>{Object.entries(copyStatusNames).map(([value, name]) => <option key={value} value={value}>{name}</option>)}</select>
              <select aria-label="Tình trạng mới" className="h-10 rounded-md border bg-background px-3" value={correctionCondition} onChange={event => setCorrectionCondition(event.target.value as CopyCondition | '')}><option value="">Không đổi tình trạng</option>{Object.entries(conditionNames).map(([value, name]) => <option key={value} value={value}>{name}</option>)}</select></div>
            <div className="flex gap-2"><Button disabled={busy || (!correctionShelf && !correctionStatus && !correctionCondition)} onClick={() => void applyCorrection()}>Áp dụng có ghi log</Button><Button variant="outline" onClick={() => setCorrection(null)}>Hủy</Button></div>
          </div>
        </PermissionBoundary> : null}
        {audit.status === 'InProgress' && review ? <PermissionBoundary requiredPermissions={['inventory-audits.complete']}>
          <div className="grid gap-3 border-t pt-3">{review.discrepancyCount > 0 ? <label className="flex items-start gap-2 text-sm"><input type="checkbox" className="mt-1" checked={acknowledge} onChange={event => setAcknowledge(event.target.checked)} />Tôi đã xem chênh lệch và sẽ xử lý riêng; hệ thống không tự đổi vị trí hoặc trạng thái bản sao.</label> : null}
            <Button className="w-fit" disabled={busy || (review.discrepancyCount > 0 && !acknowledge)} onClick={() => void complete()}><Check /> Hoàn tất và khóa đợt</Button></div>
        </PermissionBoundary> : null}
      </section>
      <section className="rounded-xl border bg-card p-4"><h2 className="mb-3 text-lg font-semibold">Bản sao trong đợt</h2><div className="grid max-h-96 gap-2 overflow-y-auto">{audit.items.map(item => <div className="flex flex-wrap justify-between gap-2 rounded-md border p-2 text-sm" key={item.id}><span><strong className="font-mono">{item.barcode}</strong> · {item.bookTitle}</span><span>{resultName[item.result]}</span></div>)}</div></section>
      {error ? <p role="alert" className="text-sm text-destructive">{error}</p> : null}
    </div>
  </PageShell>
}
