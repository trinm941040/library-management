import { useEffect, useState } from 'react'
import { Barcode, RefreshCw, Search } from 'lucide-react'
import { Button } from '@/common/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/common/components/ui/card'
import { Input } from '@/common/components/ui/input'
import { PageShell, ScreenState, StatusBadge, useToast } from '@/common/components'
import { PermissionBoundary } from '@/shared/auth/PermissionBoundary'
import { getCopies, getCopyByBarcode, updateCopyStatus, type BookCopy, type CopyStatus } from './copy-api'

const statuses: CopyStatus[] = ['Available', 'Borrowed', 'Reserved', 'InTransit', 'Lost', 'Damaged', 'Withdrawn']

export function CopiesPage() {
  const [copies, setCopies] = useState<BookCopy[]>([])
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState<CopyStatus | ''>('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [actionError, setActionError] = useState('')
  const { showToast } = useToast()

  const load = () => { setLoading(true); setError(''); getCopies({ search: search.trim() || undefined, status: status || undefined }).then((page) => setCopies(page.items)).catch((reason: unknown) => setError(reason instanceof Error ? reason.message : 'Không thể tải bản sao.')).finally(() => setLoading(false)) }
  useEffect(load, [status])

  const lookup = async () => {
    if (!search.trim()) return load()
    setLoading(true); setError('')
    try { setCopies([await getCopyByBarcode(search.trim())]) } catch (reason) { setError(reason instanceof Error ? reason.message : 'Không tìm thấy barcode.') } finally { setLoading(false) }
  }

  const changeStatus = async (copy: BookCopy, nextStatus: CopyStatus) => {
    setActionError('')
    try { const updated = await updateCopyStatus(copy, nextStatus); setCopies((items) => items.map((item) => item.id === updated.id ? updated : item)); showToast('Đã cập nhật trạng thái bản sao.') } catch (reason) { setActionError(reason instanceof Error ? reason.message : 'Không thể cập nhật trạng thái.') }
  }

  return <PageShell eyebrow="Kho vật lý" title="Quản lý bản sao" description="Tra cứu barcode, vị trí kệ và trạng thái lưu thông của từng bản sao." actions={<Button variant="outline" onClick={load}><RefreshCw className={loading ? 'animate-spin' : ''} /> Làm mới</Button>}>
    <Card>
      <CardHeader><CardTitle>Tra cứu barcode</CardTitle></CardHeader>
      <CardContent className="flex flex-col gap-3 sm:flex-row"><div className="relative flex-1"><Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" /><Input className="pl-9" value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Barcode hoặc tên sách" aria-label="Tìm bản sao" /></div><select className="h-10 rounded-md border bg-background px-3 text-sm" value={status} onChange={(event) => setStatus(event.target.value as CopyStatus | '')}><option value="">Mọi trạng thái</option>{statuses.map((value) => <option key={value} value={value}>{value}</option>)}</select><Button onClick={lookup}><Barcode /> Tra cứu</Button></CardContent>
    </Card>
    {actionError ? <div className="mt-4 rounded-md border border-destructive/30 bg-destructive/5 p-3 text-sm">{actionError}</div> : null}
    {loading ? <p className="p-6 text-center">Đang tải bản sao...</p> : error ? <ScreenState kind="error" title="Không thể tải bản sao" description={error} actionLabel="Thử lại" onAction={load} /> : copies.length === 0 ? <ScreenState kind="empty" title="Chưa có bản sao phù hợp" /> : <div className="mt-4 grid gap-4">{copies.map((copy) => <Card key={copy.id}><CardContent className="grid gap-3 p-5 md:grid-cols-[1fr_auto_auto] md:items-center"><div><p className="font-mono font-semibold">{copy.barcode}</p><p className="text-sm">{copy.bookTitle}</p><p className="text-xs text-muted-foreground">{copy.branchCode ?? 'Chưa có chi nhánh'} · {copy.shelfCode ?? 'Chưa có kệ'}</p></div><StatusBadge label={copy.status} tone={copy.status === 'Available' ? 'success' : copy.status === 'Lost' || copy.status === 'Damaged' ? 'danger' : 'neutral'} /><PermissionBoundary requiredPermissions={['copies.update']}><select className="h-9 rounded-md border bg-background px-2 text-sm" value={copy.status} onChange={(event) => changeStatus(copy, event.target.value as CopyStatus)} aria-label={`Đổi trạng thái ${copy.barcode}`}>{statuses.map((value) => <option key={value} value={value}>{value}</option>)}</select></PermissionBoundary></CardContent></Card>)}</div>}
  </PageShell>
}
