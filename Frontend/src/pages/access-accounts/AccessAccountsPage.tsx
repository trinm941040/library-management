import { useEffect, useId, useMemo, useState, type FormEvent } from 'react'
import { KeyRound, Lock, MonitorSmartphone, Plus, RefreshCw, Search, ShieldCheck, Unlock } from 'lucide-react'
import { ConfirmDialog, PageShell, ScreenState, useToast } from '@/common/components'
import { Badge } from '@/common/components/ui/badge'
import { Button } from '@/common/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/common/components/ui/card'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/common/components/ui/dialog'
import { Input } from '@/common/components/ui/input'
import { Label } from '@/common/components/ui/label'
import { Pagination } from '@/common/components/ui/pagination'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/common/components/ui/select'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/common/components/ui/table'
import { useAuth } from '@/auth/AuthProvider'
import { can } from '@/shared/auth/permissions'
import { PermissionBoundary } from '@/shared/auth/PermissionBoundary'
import { useUrlListState } from '@/shared/data/use-url-list-state'
import { getAllRoles, type Role } from '@/pages/roles/role-permission-api'
import {
  createAccessAccount,
  getAccessAccount,
  getAccessAccounts,
  getAccessAccountSessions,
  getEligibleEmployees,
  replaceAccessAccountRoles,
  resetAccessAccountPassword,
  revokeAccessAccountSession,
  setAccessAccountStatus,
  type AccessAccount,
  type AccessAccountPage,
  type AccountSession,
  type EligibleEmployee,
} from './access-account-api'

type StatusFilter = 'all' | 'active' | 'locked'
const formatDate = (value: string | null) => value
  ? new Intl.DateTimeFormat('vi-VN', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(value))
  : '—'

export function AccessAccountsPage() {
  const urlState = useUrlListState()
  const { search, pageNumber, pageSize, update } = urlState
  const status = (['active', 'locked'].includes(urlState.status) ? urlState.status : 'all') as StatusFilter
  const { user } = useAuth()
  const permissions = user?.permissions ?? []
  const [page, setPage] = useState<AccessAccountPage | null>(null)
  const [searchInput, setSearchInput] = useState(search)
  const [reload, setReload] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [createOpen, setCreateOpen] = useState(false)
  const [detailId, setDetailId] = useState<string | null>(null)
  const [statusTarget, setStatusTarget] = useState<AccessAccount | null>(null)
  const [pending, setPending] = useState(false)
  const { showToast } = useToast()

  useEffect(() => setSearchInput(search), [search])
  useEffect(() => {
    const timer = window.setTimeout(() => {
      const value = searchInput.trim()
      if (value !== search) update({ search: value || undefined, pageNumber: 1 })
    }, 350)
    return () => window.clearTimeout(timer)
  }, [search, searchInput, update])

  useEffect(() => {
    const controller = new AbortController()
    setLoading(true); setError('')
    getAccessAccounts({
      search: search || undefined,
      isActive: status === 'all' ? undefined : status === 'active',
      pageNumber,
      pageSize,
    }, controller.signal)
      .then((value) => {
        setPage(value)
        if (value.totalPages > 0 && pageNumber > value.totalPages) update({ pageNumber: value.totalPages })
      })
      .catch((reason: unknown) => {
        if (!(reason instanceof DOMException && reason.name === 'AbortError'))
          setError(reason instanceof Error ? reason.message : 'Không thể tải danh sách tài khoản.')
      })
      .finally(() => { if (!controller.signal.aborted) setLoading(false) })
    return () => controller.abort()
  }, [pageNumber, pageSize, reload, search, status, update])

  const changeStatus = async () => {
    if (!statusTarget) return
    setPending(true)
    try {
      await setAccessAccountStatus(statusTarget.id, !statusTarget.isActive)
      showToast(statusTarget.isActive ? 'Đã khóa tài khoản và thu hồi toàn bộ phiên.' : 'Đã mở khóa tài khoản.')
      setStatusTarget(null); setReload((value) => value + 1)
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Không thể thay đổi trạng thái.')
    } finally { setPending(false) }
  }

  return (
    <PageShell eyebrow="Quản lý người dùng" title="Tài khoản truy cập"
      description="Cấp tài khoản cho nhân viên, kiểm soát vai trò, trạng thái và phiên đăng nhập."
      actions={<div className="flex w-full flex-col gap-2 sm:w-auto sm:flex-row"><Button variant="outline" onClick={() => setReload((value) => value + 1)} disabled={loading} loading={loading && Boolean(page)} loadingLabel="Đang tải lại tài khoản"><RefreshCw />Làm mới</Button><PermissionBoundary requiredPermissions={['users.create', 'roles.read']}><Button className="w-full sm:w-auto" onClick={() => setCreateOpen(true)}><Plus />Cấp tài khoản</Button></PermissionBoundary></div>}>
      <Card>
        <CardHeader className="gap-4">
          <div className="flex flex-col justify-between gap-3 lg:flex-row lg:items-center">
            <div><CardTitle>Danh sách tài khoản</CardTitle><p className="mt-1 text-sm text-muted-foreground">{page?.totalCount ?? 0} tài khoản gắn với nhân viên.</p></div>
            <div className="relative md:w-80"><Search className="absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" /><Input className="pl-9" value={searchInput} onChange={(event) => setSearchInput(event.target.value)} placeholder="Tìm tên, email, mã nhân viên..." /></div>
          </div>
          <Select value={status} onValueChange={(value) => update({ status: value, pageNumber: 1 })}><SelectTrigger className="w-full sm:w-52" aria-label="Lọc trạng thái"><SelectValue /></SelectTrigger><SelectContent><SelectItem value="all">Tất cả trạng thái</SelectItem><SelectItem value="active">Đang hoạt động</SelectItem><SelectItem value="locked">Đã khóa</SelectItem></SelectContent></Select>
        </CardHeader>
        <CardContent>
          {error && !page ? <ScreenState kind="error" title="Không thể tải tài khoản" description={error} actionLabel="Thử lại" onAction={() => setReload((value) => value + 1)} /> : null}
          {loading && !page ? <ScreenState kind="loading" title="Đang tải tài khoản" /> : (
            <div className="max-h-[55vh] overflow-auto rounded-md border"><Table><TableHeader className="sticky top-0 z-10 bg-background"><TableRow><TableHead>Nhân viên</TableHead><TableHead>Tài khoản</TableHead><TableHead>Vai trò</TableHead><TableHead>Trạng thái</TableHead><TableHead>Đăng nhập cuối</TableHead><TableHead className="text-right">Thao tác</TableHead></TableRow></TableHeader><TableBody>
              {!loading && page?.items.length === 0 ? <TableRow><TableCell colSpan={6} className="h-28 text-center text-muted-foreground">Không có tài khoản phù hợp.</TableCell></TableRow> : null}
              {page?.items.map((account) => <TableRow key={account.id}>
                <TableCell><strong>{account.employee.fullName}</strong><small className="block text-muted-foreground">{account.employee.employeeCode} · {account.employee.employmentStatus}</small></TableCell>
                <TableCell>{account.email}<small className="block text-muted-foreground">Tạo {formatDate(account.createdAtUtc)}</small></TableCell>
                <TableCell><div className="flex max-w-52 flex-wrap gap-1">{account.roles.map((role) => <Badge key={role} variant="outline">{role}</Badge>)}</div></TableCell>
                <TableCell><Badge variant={account.isActive ? 'secondary' : 'destructive'}>{account.isActive ? 'Hoạt động' : 'Đã khóa'}</Badge></TableCell>
                <TableCell>{formatDate(account.lastLoginAtUtc)}</TableCell>
                <TableCell><div className="flex justify-end gap-1"><Button variant="ghost" size="sm" onClick={() => setDetailId(account.id)}><MonitorSmartphone />Chi tiết</Button><PermissionBoundary requiredPermissions={['users.deactivate']}><Button variant="ghost" size="icon-sm" aria-label={account.isActive ? `Khóa ${account.displayName}` : `Mở khóa ${account.displayName}`} disabled={account.isProtected || account.id === user?.id} onClick={() => setStatusTarget(account)}>{account.isActive ? <Lock /> : <Unlock />}</Button></PermissionBoundary></div></TableCell>
              </TableRow>)}
            </TableBody></Table></div>
          )}
          {page && page.totalPages > 0 ? <Pagination className="mt-4" currentPage={page.pageNumber} totalPages={page.totalPages} totalCount={page.totalCount} itemCount={page.items.length} loading={loading} onPageChange={(value) => update({ pageNumber: value })} pageSize={pageSize} onPageSizeChange={(size) => update({ pageSize: size, pageNumber: 1 })} /> : null}
        </CardContent>
      </Card>
      <CreateAccountDialog open={createOpen} onOpenChange={setCreateOpen} onCreated={() => { setCreateOpen(false); update({ pageNumber: 1 }); setReload((value) => value + 1); showToast('Đã cấp tài khoản truy cập.') }} />
      <AccountDetailDialog accountId={detailId} permissions={permissions} onOpenChange={(open) => !open && setDetailId(null)} onChanged={() => setReload((value) => value + 1)} />
      <ConfirmDialog open={!!statusTarget} title={statusTarget?.isActive ? 'Khóa tài khoản?' : 'Mở khóa tài khoản?'} description={statusTarget?.isActive ? 'Tài khoản sẽ ngừng truy cập và toàn bộ phiên hiện tại bị thu hồi trong cùng workflow.' : 'Tài khoản có thể đăng nhập lại, nhưng các phiên cũ không được khôi phục.'} confirmLabel={statusTarget?.isActive ? 'Khóa tài khoản' : 'Mở khóa'} destructive={statusTarget?.isActive} isPending={pending} error={error} onOpenChange={(open) => { if (!open) setStatusTarget(null) }} onConfirm={() => void changeStatus()} />
    </PageShell>
  )
}

function CreateAccountDialog({ open, onOpenChange, onCreated }: { open: boolean; onOpenChange: (open: boolean) => void; onCreated: () => void }) {
  const [employees, setEmployees] = useState<EligibleEmployee[]>([])
  const [roles, setRoles] = useState<Role[]>([])
  const [employeeId, setEmployeeId] = useState('')
  const [email, setEmail] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [password, setPassword] = useState('')
  const [roleIds, setRoleIds] = useState<Set<string>>(new Set())
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  useEffect(() => {
    if (!open) return
    const controller = new AbortController()
    Promise.all([getEligibleEmployees(undefined, controller.signal), getAllRoles(controller.signal)])
      .then(([employeeItems, roleItems]) => { setEmployees(employeeItems); setRoles(roleItems.filter((role) => role.isActive)) })
      .catch((reason: unknown) => setError(reason instanceof Error ? reason.message : 'Không thể tải dữ liệu tạo tài khoản.'))
    return () => controller.abort()
  }, [open])
  useEffect(() => { if (!open) { setEmployeeId(''); setEmail(''); setDisplayName(''); setPassword(''); setRoleIds(new Set()); setError('') } }, [open])
  const submit = async (event: FormEvent) => {
    event.preventDefault(); setError('')
    if (!employeeId || roleIds.size === 0) { setError('Vui lòng chọn nhân viên và ít nhất một vai trò.'); return }
    setSaving(true)
    try { await createAccessAccount({ employeeId, email, displayName, password, roleIds: [...roleIds] }); onCreated() }
    catch (reason) { setError(reason instanceof Error ? reason.message : 'Không thể cấp tài khoản.') }
    finally { setSaving(false) }
  }
  return <Dialog open={open} onOpenChange={(value) => !saving && onOpenChange(value)}><DialogContent><form onSubmit={submit}><DialogHeader><DialogTitle>Cấp tài khoản truy cập</DialogTitle><DialogDescription>Mỗi nhân viên chỉ được liên kết một tài khoản và phải có ít nhất một vai trò.</DialogDescription></DialogHeader><div className="grid gap-4 py-5">
    <div className="grid gap-2"><Label htmlFor="access-employee">Nhân viên</Label><Select value={employeeId} onValueChange={(id) => { setEmployeeId(id); const employee = employees.find((item) => item.id === id); if (employee) { setEmail(employee.email); setDisplayName(employee.fullName) } }}><SelectTrigger id="access-employee"><SelectValue placeholder="Chọn nhân viên chưa có tài khoản" /></SelectTrigger><SelectContent>{employees.map((employee) => <SelectItem key={employee.id} value={employee.id}>{employee.employeeCode} · {employee.fullName}</SelectItem>)}</SelectContent></Select></div>
    <div className="grid gap-2"><Label htmlFor="access-email">Email đăng nhập</Label><Input id="access-email" type="email" value={email} onChange={(event) => setEmail(event.target.value)} required /></div>
    <div className="grid gap-2"><Label htmlFor="access-name">Tên hiển thị</Label><Input id="access-name" value={displayName} onChange={(event) => setDisplayName(event.target.value)} minLength={2} required /></div>
    <div className="grid gap-2"><Label htmlFor="access-password">Mật khẩu tạm thời</Label><Input id="access-password" type="password" autoComplete="new-password" value={password} onChange={(event) => setPassword(event.target.value)} minLength={8} required /></div>
    <RolePicker roles={roles} selected={roleIds} onChange={setRoleIds} />
    {error ? <p role="alert" className="text-sm text-destructive">{error}</p> : null}
  </div><DialogFooter><Button type="button" variant="outline" onClick={() => onOpenChange(false)}>Hủy</Button><Button disabled={saving}>{saving ? 'Đang tạo...' : 'Cấp tài khoản'}</Button></DialogFooter></form></DialogContent></Dialog>
}

function AccountDetailDialog({ accountId, permissions, onOpenChange, onChanged }: { accountId: string | null; permissions: string[]; onOpenChange: (open: boolean) => void; onChanged: () => void }) {
  const [account, setAccount] = useState<AccessAccount | null>(null)
  const [sessions, setSessions] = useState<AccountSession[]>([])
  const [roles, setRoles] = useState<Role[]>([])
  const [selectedRoles, setSelectedRoles] = useState<Set<string>>(new Set())
  const [password, setPassword] = useState('')
  const [resetConfirmation, setResetConfirmation] = useState(false)
  const [revokeTarget, setRevokeTarget] = useState<AccountSession | null>(null)
  const [pending, setPending] = useState(false)
  const [error, setError] = useState('')
  const { showToast } = useToast()
  const canUpdate = can(permissions, 'users.update')
  const canAssign = can(permissions, 'roles.assign') && can(permissions, 'roles.read')
  const reload = async () => {
    if (!accountId) return
    const [detail, sessionItems, roleItems] = await Promise.all([
      getAccessAccount(accountId), getAccessAccountSessions(accountId), canAssign ? getAllRoles() : Promise.resolve([]),
    ])
    setAccount(detail); setSessions(sessionItems); setRoles(roleItems.filter((role) => role.isActive))
    setSelectedRoles(new Set(roleItems.filter((role) => detail.roles.includes(role.name)).map((role) => role.id)))
  }
  useEffect(() => { if (accountId) void reload().catch((reason: unknown) => setError(reason instanceof Error ? reason.message : 'Không thể tải chi tiết.')); else { setAccount(null); setSessions([]); setPassword(''); setError('') } }, [accountId]) // eslint-disable-line react-hooks/exhaustive-deps
  const saveRoles = async () => { if (!account || selectedRoles.size === 0) return; setPending(true); setError(''); try { await replaceAccessAccountRoles(account.id, [...selectedRoles]); await reload(); onChanged(); showToast('Đã cập nhật vai trò.') } catch (reason) { setError(reason instanceof Error ? reason.message : 'Không thể cập nhật vai trò.') } finally { setPending(false) } }
  const resetPassword = async () => { if (!account || password.length < 8) { setError('Mật khẩu tạm thời phải có ít nhất 8 ký tự.'); return }; setPending(true); setError(''); try { await resetAccessAccountPassword(account.id, password); setPassword(''); setResetConfirmation(false); await reload(); showToast('Đã đặt lại mật khẩu và thu hồi toàn bộ phiên.') } catch (reason) { setError(reason instanceof Error ? reason.message : 'Không thể đặt lại mật khẩu.') } finally { setPending(false) } }
  const revoke = async () => { if (!account || !revokeTarget) return; setPending(true); try { await revokeAccessAccountSession(account.id, revokeTarget.id); setRevokeTarget(null); await reload(); showToast('Đã thu hồi phiên đăng nhập.') } catch (reason) { setError(reason instanceof Error ? reason.message : 'Không thể thu hồi phiên.') } finally { setPending(false) } }
  return <><Dialog open={!!accountId} onOpenChange={onOpenChange}><DialogContent className="max-h-[85vh] overflow-y-auto sm:max-w-3xl"><DialogHeader><DialogTitle>Chi tiết tài khoản truy cập</DialogTitle><DialogDescription>Thông tin Identity nhạy cảm và token không được hiển thị.</DialogDescription></DialogHeader>{!account ? <ScreenState kind="loading" title="Đang tải chi tiết" /> : <div className="grid gap-5">
    <div className="grid gap-2 rounded-md border p-4 sm:grid-cols-2"><p><span className="text-sm text-muted-foreground">Nhân viên</span><strong className="block">{account.employee.fullName}</strong></p><p><span className="text-sm text-muted-foreground">Mã nhân viên</span><strong className="block">{account.employee.employeeCode}</strong></p><p><span className="text-sm text-muted-foreground">Email đăng nhập</span><strong className="block">{account.email}</strong></p><p><span className="text-sm text-muted-foreground">Trạng thái</span><Badge className="block w-fit" variant={account.isActive ? 'secondary' : 'destructive'}>{account.isActive ? 'Hoạt động' : 'Đã khóa'}</Badge></p></div>
    <section className="grid gap-2"><h3 className="font-semibold"><ShieldCheck className="mr-2 inline size-4" />Vai trò hiện tại</h3><div className="flex flex-wrap gap-2">{account.roles.map((role) => <Badge key={role} variant="outline">{role}</Badge>)}</div></section>
    {canAssign ? <section className="grid gap-3"><RolePicker roles={roles} selected={selectedRoles} onChange={setSelectedRoles} /><Button className="w-fit" variant="outline" disabled={pending || selectedRoles.size === 0} onClick={() => void saveRoles()}>Lưu vai trò</Button></section> : null}
    {canUpdate ? <section className="grid gap-3"><h3 className="font-semibold"><KeyRound className="mr-2 inline size-4" />Đặt lại quyền truy cập</h3><div className="flex flex-col gap-2 sm:flex-row"><Input type="password" autoComplete="new-password" placeholder="Mật khẩu tạm thời mới" value={password} onChange={(event) => setPassword(event.target.value)} /><Button variant="outline" disabled={pending || password.length < 8} onClick={() => setResetConfirmation(true)}>Đặt lại mật khẩu</Button></div></section> : null}
    <section className="grid gap-3"><h3 className="font-semibold"><MonitorSmartphone className="mr-2 inline size-4" />Phiên đăng nhập</h3><div className="max-h-72 overflow-auto rounded-md border"><Table><TableHeader className="sticky top-0 bg-background"><TableRow><TableHead>Thời gian</TableHead><TableHead>Thiết bị / IP</TableHead><TableHead>Trạng thái</TableHead><TableHead /></TableRow></TableHeader><TableBody>{sessions.length === 0 ? <TableRow><TableCell colSpan={4} className="text-center text-muted-foreground">Chưa có phiên.</TableCell></TableRow> : sessions.map((session) => <TableRow key={session.id}><TableCell>{formatDate(session.createdAtUtc)}<small className="block text-muted-foreground">Hết hạn {formatDate(session.expiresAtUtc)}</small></TableCell><TableCell className="max-w-64 truncate">{session.userAgent || 'Không xác định'}<small className="block text-muted-foreground">{session.createdByIp || '—'}</small></TableCell><TableCell><Badge variant={session.isActive ? 'secondary' : 'outline'}>{session.isActive ? 'Đang hoạt động' : session.revocationReason || 'Đã kết thúc'}</Badge></TableCell><TableCell>{canUpdate && session.isActive ? <Button variant="ghost" size="sm" onClick={() => setRevokeTarget(session)}>Thu hồi</Button> : null}</TableCell></TableRow>)}</TableBody></Table></div></section>
    {error ? <p role="alert" className="text-sm text-destructive">{error}</p> : null}
  </div>}<DialogFooter><Button variant="outline" onClick={() => onOpenChange(false)}>Đóng</Button></DialogFooter></DialogContent></Dialog><ConfirmDialog open={resetConfirmation} title="Đặt lại mật khẩu?" description="Mật khẩu hiện tại và toàn bộ phiên đăng nhập sẽ mất hiệu lực. Thao tác không thể hoàn tác." confirmLabel="Đặt lại và thu hồi phiên" destructive isPending={pending} error={error} onOpenChange={setResetConfirmation} onConfirm={() => void resetPassword()} /><ConfirmDialog open={!!revokeTarget} title="Thu hồi phiên?" description="Toàn bộ refresh token trong cùng session family sẽ bị thu hồi và không thể khôi phục." confirmLabel="Thu hồi phiên" destructive isPending={pending} error={error} onOpenChange={(open) => { if (!open) setRevokeTarget(null) }} onConfirm={() => void revoke()} /></>
}

function RolePicker({ roles, selected, onChange }: { roles: Role[]; selected: Set<string>; onChange: (value: Set<string>) => void }) {
  const fieldId = useId()
  const sorted = useMemo(() => [...roles].sort((a, b) => a.name.localeCompare(b.name, 'vi')), [roles])
  return <fieldset className="grid gap-2"><legend className="mb-2 text-sm font-medium">Vai trò (bắt buộc)</legend><div className="grid max-h-40 gap-2 overflow-auto rounded-md border p-3 sm:grid-cols-2">{sorted.map((role) => { const id = `${fieldId}-${role.id}`; return <div key={role.id} className="flex items-start gap-2 text-sm"><input id={id} type="checkbox" className="mt-1" checked={selected.has(role.id)} onChange={(event) => { const next = new Set(selected); if (event.target.checked) next.add(role.id); else next.delete(role.id); onChange(next) }} /><label htmlFor={id}><strong className="block">{role.name}</strong><small className="text-muted-foreground">{role.description || 'Không có mô tả'}</small></label></div> })}</div></fieldset>
}
