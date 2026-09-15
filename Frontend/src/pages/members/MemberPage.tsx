import { useEffect, useState, type FormEvent } from 'react'
import {
  BadgeCheck,
  Ban,
  CalendarClock,
  CreditCard,
  Eye,
  Pencil,
  Plus,
  RefreshCw,
  Search,
  ShieldAlert,
  UserRound,
} from 'lucide-react'
import { Badge } from '@/common/components/ui/badge'
import { Button } from '@/common/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/common/components/ui/card'
import { Input } from '@/common/components/ui/input'
import { Label } from '@/common/components/ui/label'
import { Pagination } from '@/common/components/ui/pagination'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/common/components/ui/select'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/common/components/ui/table'
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/common/components/ui/dialog'
import {
  addAdjustment,
  addPayment,
  addRestriction,
  changeCardStatus,
  createMember,
  getMember,
  getMembers,
  issueCard,
  memberStatuses,
  removeRestriction,
  renewCard,
  statusLabels,
  updateMember,
  type CardStatus,
  type Member,
  type MemberStatus,
  type RestrictionType,
  type SaveMemberInput,
} from './member-api'
import { PermissionBoundary } from '@/shared/auth/PermissionBoundary'
const today = () => new Date().toISOString().slice(0, 10)
const money = (v: number) =>
  new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(v)
const date = (v: string | null) => (v ? new Intl.DateTimeFormat('vi-VN').format(new Date(v)) : '—')
const empty: SaveMemberInput = {
  memberCode: '',
  fullName: '',
  email: '',
  phoneNumber: null,
  dateOfBirth: null,
  address: null,
  memberGroup: 'Thông thường',
  status: 'Active',
  borrowingLimit: 5,
  loanPeriodDays: 14,
}
export function MemberPage() {
  const [items, setItems] = useState<Member[]>([]),
    [total, setTotal] = useState(0),
    [pages, setPages] = useState(0),
    [page, setPage] = useState(1),
    [pageSize, setPageSize] = useState(20),
    [search, setSearch] = useState(''),
    [status, setStatus] = useState<'all' | MemberStatus>('all'),
    [group, setGroup] = useState(''),
    [loading, setLoading] = useState(true),
    [error, setError] = useState(''),
    [reload, setReload] = useState(0),
    [editing, setEditing] = useState<Member | null>(null),
    [formOpen, setFormOpen] = useState(false),
    [selected, setSelected] = useState<Member | null>(null)
  useEffect(() => {
    const c = new AbortController()
    setLoading(true)
    getMembers(
      {
        search: search || undefined,
        status: status === 'all' ? undefined : status,
        memberGroup: group || undefined,
        pageNumber: page,
        pageSize,
      },
      c.signal,
    )
      .then((r) => {
        setItems(r.items)
        setTotal(r.totalCount)
        setPages(r.totalPages)
      })
      .catch((e) => {
        if (e.name !== 'AbortError') setError(e.message)
      })
      .finally(() => setLoading(false))
    return () => c.abort()
  }, [group, page, pageSize, reload, search, status])
  const openDetails = async (m: Member) => {
    try {
      setSelected(await getMember(m.id))
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Không tải được chi tiết')
    }
  }
  const refresh = (m?: Member) => {
    if (m) setSelected(m)
    setReload((x) => x + 1)
  }
  return (
    <div className="mx-auto w-full max-w-7xl px-5 py-10 md:px-12">
      <div className="mb-8 flex flex-col justify-between gap-4 sm:flex-row sm:items-end">
        <div>
          <p className="mb-2 text-xs font-bold tracking-widest text-primary uppercase">
            thành viên và lưu thông
          </p>
          <h1 className="text-3xl font-bold tracking-tight">Quản lý độc giả</h1>
          <p className="mt-2 text-sm text-muted-foreground">
            Hồ sơ, thẻ thư viện, hạn chế giao dịch, lịch sử và công nợ.
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={() => setReload((x) => x + 1)}>
            <RefreshCw className={loading ? 'animate-spin' : ''} />
            Làm mới
          </Button>
          <PermissionBoundary requiredPermissions={['members.create']}>
            <Button
              onClick={() => {
                setEditing(null)
                setFormOpen(true)
              }}
            >
              <Plus />
              Tạo độc giả
            </Button>
          </PermissionBoundary>
        </div>
      </div>
      <div className="mb-6 grid gap-4 sm:grid-cols-3">
        <Summary label="Tổng độc giả" value={total} icon={UserRound} />
        <Summary
          label="Hoạt động trên trang"
          value={items.filter((x) => x.status === 'Active').length}
          icon={BadgeCheck}
        />
        <Summary
          label="Cần xử lý trên trang"
          value={
            items.filter((x) => x.status !== 'Active' || x.restrictions.some((r) => r.isActive))
              .length
          }
          icon={ShieldAlert}
        />
      </div>
      {error && (
        <div className="mb-4 rounded-md border border-destructive/30 bg-destructive/5 p-3 text-sm text-destructive">
          {error}
        </div>
      )}
      <Card>
        <CardHeader>
          <CardTitle>Danh sách độc giả</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="mb-5 grid gap-3 md:grid-cols-[1fr_13rem_13rem_auto]">
            <div className="relative">
              <Search className="absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                className="pl-9"
                value={search}
                onChange={(e) => {
                  setSearch(e.target.value)
                  setPage(1)
                }}
                placeholder="Mã, tên, email, số điện thoại..."
              />
            </div>
            <Input
              value={group}
              onChange={(e) => {
                setGroup(e.target.value)
                setPage(1)
              }}
              placeholder="Nhóm độc giả"
            />
            <Select
              value={status}
              onValueChange={(v) => {
                setStatus(v as typeof status)
                setPage(1)
              }}
            >
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Tất cả trạng thái</SelectItem>
                {memberStatuses.map((s) => (
                  <SelectItem key={s} value={s}>
                    {statusLabels[s]}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <Button
              variant="outline"
              onClick={() => {
                setSearch('')
                setGroup('')
                setStatus('all')
              }}
            >
              Xóa lọc
            </Button>
          </div>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Mã / độc giả</TableHead>
                <TableHead>Liên hệ</TableHead>
                <TableHead>Nhóm & giới hạn</TableHead>
                <TableHead>Thẻ</TableHead>
                <TableHead>Trạng thái</TableHead>
                <TableHead className="text-right">Thao tác</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {loading ? (
                <TableRow>
                  <TableCell colSpan={6} className="py-10 text-center">
                    Đang tải...
                  </TableCell>
                </TableRow>
              ) : items.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={6} className="py-10 text-center text-muted-foreground">
                    Không có độc giả phù hợp.
                  </TableCell>
                </TableRow>
              ) : (
                items.map((m) => (
                  <TableRow key={m.id}>
                    <TableCell>
                      <b>{m.memberCode}</b>
                      <div className="text-sm text-muted-foreground">{m.fullName}</div>
                    </TableCell>
                    <TableCell>
                      {m.email}
                      <div className="text-sm text-muted-foreground">{m.phoneNumber || '—'}</div>
                    </TableCell>
                    <TableCell>
                      {m.memberGroup}
                      <div className="text-xs text-muted-foreground">
                        {m.borrowingLimit} cuốn / {m.loanPeriodDays} ngày
                      </div>
                    </TableCell>
                    <TableCell>
                      {m.card ? (
                        <>
                          <b>{m.card.cardNumber}</b>
                          <div className="text-xs text-muted-foreground">
                            Hạn {date(m.card.expiresOn)}
                          </div>
                        </>
                      ) : (
                        'Chưa cấp'
                      )}
                    </TableCell>
                    <TableCell>
                      <Status value={m.status} />
                    </TableCell>
                    <TableCell className="text-right">
                      <Button
                        variant="ghost"
                        size="icon"
                        aria-label="Chi tiết"
                        onClick={() => openDetails(m)}
                      >
                        <Eye />
                      </Button>
                      <PermissionBoundary requiredPermissions={['members.update']}>
                        <Button
                          variant="ghost"
                          size="icon"
                          aria-label="Sửa"
                          onClick={() => {
                            setEditing(m)
                            setFormOpen(true)
                          }}
                        >
                          <Pencil />
                        </Button>
                      </PermissionBoundary>
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
          {total > 0 && (
            <div className="mt-5">
              <Pagination
                currentPage={page}
                totalPages={pages}
                onPageChange={setPage}
                pageSize={pageSize}
                onPageSizeChange={(size) => {
                  setPage(1)
                  setPageSize(size)
                }}
              />
            </div>
          )}
        </CardContent>
      </Card>
      <PermissionBoundary requiredPermissions={[editing ? 'members.update' : 'members.create']}>
        <MemberForm
          open={formOpen}
          member={editing}
          onClose={() => setFormOpen(false)}
          onSaved={() => {
            setFormOpen(false)
            refresh()
          }}
        />
      </PermissionBoundary>
      {selected && (
        <MemberDetails member={selected} onClose={() => setSelected(null)} onChange={refresh} />
      )}
    </div>
  )
}
function Summary({
  label,
  value,
  icon: Icon,
}: {
  label: string
  value: number
  icon: typeof UserRound
}) {
  return (
    <Card>
      <CardContent className="flex items-center justify-between p-5">
        <div>
          <p className="text-sm text-muted-foreground">{label}</p>
          <p className="mt-1 text-2xl font-bold">{value}</p>
        </div>
        <Icon className="size-8 text-primary" />
      </CardContent>
    </Card>
  )
}
function Status({ value }: { value: MemberStatus }) {
  return <Badge variant={value === 'Active' ? 'default' : 'secondary'}>{statusLabels[value]}</Badge>
}
function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="grid gap-2">
      <Label>{label}</Label>
      {children}
    </div>
  )
}
function MemberForm({
  open,
  member,
  onClose,
  onSaved,
}: {
  open: boolean
  member: Member | null
  onClose: () => void
  onSaved: () => void
}) {
  const [data, setData] = useState<SaveMemberInput>(empty),
    [busy, setBusy] = useState(false),
    [error, setError] = useState('')
  useEffect(() => {
    setData(
      member
        ? {
            memberCode: member.memberCode,
            fullName: member.fullName,
            email: member.email,
            phoneNumber: member.phoneNumber,
            dateOfBirth: member.dateOfBirth,
            address: member.address,
            memberGroup: member.memberGroup,
            status: member.status,
            borrowingLimit: member.borrowingLimit,
            loanPeriodDays: member.loanPeriodDays,
            concurrencyToken: member.concurrencyToken,
          }
        : empty,
    )
    setError('')
  }, [member, open])
  const set = (k: keyof SaveMemberInput, v: unknown) => setData((x) => ({ ...x, [k]: v }))
  const submit = async (e: FormEvent) => {
    e.preventDefault()
    setBusy(true)
    try {
      if (member) await updateMember(member.id, data)
      else await createMember(data)
      onSaved()
    } catch (x) {
      setError(x instanceof Error ? x.message : 'Không thể lưu')
    } finally {
      setBusy(false)
    }
  }
  return (
    <Dialog open={open} onOpenChange={(v) => !v && onClose()}>
      <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-2xl">
        <DialogHeader>
          <DialogTitle>{member ? 'Cập nhật độc giả' : 'Tạo hồ sơ độc giả'}</DialogTitle>
        </DialogHeader>
        <form onSubmit={submit} className="grid gap-4 sm:grid-cols-2">
          <Field label="Mã độc giả">
            <Input
              required
              value={data.memberCode}
              onChange={(e) => set('memberCode', e.target.value)}
            />
          </Field>
          <Field label="Họ tên">
            <Input
              required
              value={data.fullName}
              onChange={(e) => set('fullName', e.target.value)}
            />
          </Field>
          <Field label="Email">
            <Input
              required
              type="email"
              value={data.email}
              onChange={(e) => set('email', e.target.value)}
            />
          </Field>
          <Field label="Số điện thoại">
            <Input
              value={data.phoneNumber ?? ''}
              onChange={(e) => set('phoneNumber', e.target.value || null)}
            />
          </Field>
          <Field label="Ngày sinh">
            <Input
              type="date"
              value={data.dateOfBirth ?? ''}
              onChange={(e) => set('dateOfBirth', e.target.value || null)}
            />
          </Field>
          <Field label="Nhóm độc giả">
            <Input
              required
              value={data.memberGroup}
              onChange={(e) => set('memberGroup', e.target.value)}
            />
          </Field>
          <Field label="Giới hạn số sách">
            <Input
              type="number"
              min={1}
              max={100}
              value={data.borrowingLimit}
              onChange={(e) => set('borrowingLimit', Number(e.target.value))}
            />
          </Field>
          <Field label="Số ngày mượn">
            <Input
              type="number"
              min={1}
              max={365}
              value={data.loanPeriodDays}
              onChange={(e) => set('loanPeriodDays', Number(e.target.value))}
            />
          </Field>
          <Field label="Trạng thái">
            <Select value={data.status} onValueChange={(v) => set('status', v)}>
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {memberStatuses.map((s) => (
                  <SelectItem key={s} value={s}>
                    {statusLabels[s]}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </Field>
          <Field label="Địa chỉ">
            <Input
              value={data.address ?? ''}
              onChange={(e) => set('address', e.target.value || null)}
            />
          </Field>
          {error && <p className="text-sm text-destructive sm:col-span-2">{error}</p>}
          <DialogFooter className="sm:col-span-2">
            <Button type="button" variant="outline" onClick={onClose}>
              Hủy
            </Button>
            <Button disabled={busy}>{busy ? 'Đang lưu...' : 'Lưu hồ sơ'}</Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
function MemberDetails({
  member,
  onClose,
  onChange,
}: {
  member: Member
  onClose: () => void
  onChange: (m: Member) => void
}) {
  const [action, setAction] = useState<'card' | 'restriction' | 'payment' | 'adjustment' | null>(
    null,
  )
  return (
    <Dialog open onOpenChange={(v) => !v && onClose()}>
      <DialogContent className="max-h-[92vh] overflow-y-auto sm:max-w-4xl">
        <DialogHeader>
          <DialogTitle>
            {member.memberCode} · {member.fullName}
          </DialogTitle>
        </DialogHeader>
        <div className="grid gap-5">
          <section className="grid gap-3 rounded-lg border p-4 sm:grid-cols-3">
            <Info label="Trạng thái" value={statusLabels[member.status]} />
            <Info label="Nhóm" value={member.memberGroup} />
            <Info
              label="Giới hạn"
              value={`${member.borrowingLimit} cuốn / ${member.loanPeriodDays} ngày`}
            />
            <Info label="Email" value={member.email} />
            <Info label="Điện thoại" value={member.phoneNumber || '—'} />
            <Info label="Ngày sinh" value={date(member.dateOfBirth)} />
          </section>
          <Section
            title="Thẻ thư viện"
            icon={CreditCard}
            action={
              <PermissionBoundary requiredPermissions={['members.manage-cards']}>
                <Button size="sm" onClick={() => setAction('card')}>
                  {member.card ? 'Quản lý thẻ' : 'Cấp thẻ'}
                </Button>
              </PermissionBoundary>
            }
          >
            {member.card ? (
              <div className="flex flex-wrap gap-x-8 gap-y-2 text-sm">
                <b>{member.card.cardNumber}</b>
                <span>Cấp: {date(member.card.issuedOn)}</span>
                <span>Hết hạn: {date(member.card.expiresOn)}</span>
                <Badge>{member.card.status}</Badge>
              </div>
            ) : (
              <p className="text-sm text-muted-foreground">Độc giả chưa được cấp thẻ.</p>
            )}
          </Section>
          <Section
            title="Hạn chế tài khoản"
            icon={Ban}
            action={
              <PermissionBoundary requiredPermissions={['members.manage-restrictions']}>
                <Button size="sm" onClick={() => setAction('restriction')}>
                  <Plus />
                  Thêm hạn chế
                </Button>
              </PermissionBoundary>
            }
          >
            {member.restrictions.length === 0 ? (
              <Empty />
            ) : (
              <div className="grid gap-2">
                {member.restrictions.map((r) => (
                  <div
                    key={r.id}
                    className="flex items-start justify-between rounded-md bg-muted/50 p-3 text-sm"
                  >
                    <div>
                      <b>{r.type}</b> · {r.reason}
                      <div className="text-xs text-muted-foreground">
                        {date(r.startsAtUtc)} — {date(r.endsAtUtc)}
                      </div>
                    </div>
                    {r.isActive && (
                      <PermissionBoundary requiredPermissions={['members.manage-restrictions']}>
                        <Button
                          size="sm"
                          variant="outline"
                          onClick={async () => {
                            const reason = window.prompt('Lý do gỡ hạn chế')
                            if (reason) onChange(await removeRestriction(member.id, r.id, reason))
                          }}
                        >
                          Gỡ bỏ
                        </Button>
                      </PermissionBoundary>
                    )}
                  </div>
                ))}
              </div>
            )}
          </Section>
          <Section title="Lịch sử mượn và đặt trước" icon={CalendarClock}>
            <div className="grid gap-4 md:grid-cols-2">
              <History
                title={`Mượn sách (${member.borrowings.length})`}
                rows={member.borrowings.map(
                  (x) =>
                    `${date(x.borrowedAtUtc)} · Hạn ${date(x.dueAtUtc)} · ${x.returnedAtUtc ? 'Đã trả' : 'Đang mượn'}`,
                )}
              />
              <History
                title={`Đặt trước (${member.reservations.length})`}
                rows={member.reservations.map(
                  (x) =>
                    `${date(x.reservedAtUtc)} · ${x.fulfilledAtUtc ? 'Đã nhận' : x.cancelledAtUtc ? 'Đã hủy' : 'Đang giữ chỗ'}`,
                )}
              />
            </div>
          </Section>
          <Section title="Tiền phạt và thanh toán" icon={ShieldAlert}>
            {member.fines.length === 0 ? (
              <Empty />
            ) : (
              <div className="grid gap-3">
                {member.fines.map((f) => (
                  <div key={f.id} className="rounded-md border p-3 text-sm">
                    <div className="flex flex-wrap items-center justify-between gap-2">
                      <div>
                        <b>
                          {f.type.toUpperCase()} {f.bookTitle && `· ${f.bookTitle}`}
                        </b>
                        <div className="text-muted-foreground">{f.note || 'Không có ghi chú'}</div>
                      </div>
                      <b className={f.balance ? 'text-destructive' : 'text-primary'}>
                        {money(f.balance)}
                      </b>
                    </div>
                    <div className="mt-2 flex flex-wrap gap-2 text-xs text-muted-foreground">
                      <span>Gốc {money(f.originalAmount)}</span>
                      <span>Điều chỉnh {money(f.adjustmentTotal)}</span>
                      <span>Đã thu {money(f.paymentTotal)}</span>
                    </div>
                    {f.balance > 0 && (
                      <PermissionBoundary requiredPermissions={['members.manage-finances']}>
                        <div className="mt-3 flex gap-2">
                          <Button
                            size="sm"
                            onClick={() => {
                              sessionStorage.setItem('member-fine', f.id)
                              setAction('payment')
                            }}
                          >
                            Thanh toán
                          </Button>
                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() => {
                              sessionStorage.setItem('member-fine', f.id)
                              setAction('adjustment')
                            }}
                          >
                            Điều chỉnh
                          </Button>
                        </div>
                      </PermissionBoundary>
                    )}
                  </div>
                ))}
              </div>
            )}
          </Section>
        </div>
        {action ? (
          <PermissionBoundary
            requiredPermissions={[
              action === 'card'
                ? 'members.manage-cards'
                : action === 'restriction'
                  ? 'members.manage-restrictions'
                  : 'members.manage-finances',
            ]}
          >
            <ActionDialog
              action={action}
              member={member}
              close={() => setAction(null)}
              changed={(m) => {
                setAction(null)
                onChange(m)
              }}
            />
          </PermissionBoundary>
        ) : null}
      </DialogContent>
    </Dialog>
  )
}
function Info({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className="font-medium">{value}</p>
    </div>
  )
}
function Empty() {
  return <p className="text-sm text-muted-foreground">Chưa có dữ liệu.</p>
}
function History({ title, rows }: { title: string; rows: string[] }) {
  return (
    <div>
      <b className="text-sm">{title}</b>
      <div className="mt-2 grid gap-2">
        {rows.length ? (
          rows.slice(0, 8).map((r, i) => (
            <p key={i} className="rounded bg-muted/50 p-2 text-xs">
              {r}
            </p>
          ))
        ) : (
          <Empty />
        )}
      </div>
    </div>
  )
}
function Section({
  title,
  icon: Icon,
  action,
  children,
}: {
  title: string
  icon: typeof UserRound
  action?: React.ReactNode
  children: React.ReactNode
}) {
  return (
    <section className="rounded-lg border p-4">
      <div className="mb-3 flex items-center justify-between">
        <h3 className="flex items-center gap-2 font-semibold">
          <Icon className="size-4 text-primary" />
          {title}
        </h3>
        {action}
      </div>
      {children}
    </section>
  )
}
function ActionDialog({
  action,
  member,
  close,
  changed,
}: {
  action: 'card' | 'restriction' | 'payment' | 'adjustment' | null
  member: Member
  close: () => void
  changed: (m: Member) => void
}) {
  const [a, setA] = useState(''),
    [b, setB] = useState(''),
    [c, setC] = useState(''),
    [error, setError] = useState('')
  useEffect(() => {
    setA('')
    setB('')
    setC('')
    setError('')
  }, [action])
  if (!action) return null
  const submit = async (e: FormEvent) => {
    e.preventDefault()
    try {
      let result: Member
      if (action === 'card') {
        result = member.card
          ? await renewCard(member.id, a)
          : await issueCard(member.id, { cardNumber: a, issuedOn: b, expiresOn: c })
      } else if (action === 'restriction') {
        result = await addRestriction(member.id, {
          type: a as RestrictionType,
          reason: b,
          startsAtUtc: new Date().toISOString(),
          endsAtUtc: c ? new Date(c).toISOString() : null,
        })
      } else if (action === 'payment') {
        result = await addPayment(member.id, sessionStorage.getItem('member-fine')!, {
          amount: Number(a),
          method: (b || 'Cash') as 'Cash',
          reference: c || null,
        })
      } else {
        result = await addAdjustment(member.id, sessionStorage.getItem('member-fine')!, {
          amountDelta: Number(a),
          reason: b,
        })
      }
      changed(result)
    } catch (x) {
      setError(x instanceof Error ? x.message : 'Không thể xử lý')
    }
  }
  return (
    <Dialog open onOpenChange={(v) => !v && close()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>
            {action === 'card'
              ? member.card
                ? 'Gia hạn / khóa thẻ'
                : 'Cấp thẻ'
              : action === 'restriction'
                ? 'Thêm hạn chế'
                : action === 'payment'
                  ? 'Ghi nhận thanh toán'
                  : 'Điều chỉnh tiền phạt'}
          </DialogTitle>
        </DialogHeader>
        <form className="grid gap-4" onSubmit={submit}>
          {action === 'card' ? (
            member.card ? (
              <>
                <Field label="Ngày hết hạn mới">
                  <Input
                    required
                    type="date"
                    min={member.card.expiresOn}
                    value={a}
                    onChange={(e) => setA(e.target.value)}
                  />
                </Field>
                <Button
                  type="button"
                  variant="outline"
                  onClick={async () =>
                    changed(
                      await changeCardStatus(
                        member.id,
                        member.card!.status === 'Suspended'
                          ? 'Active'
                          : ('Suspended' as CardStatus),
                      ),
                    )
                  }
                >
                  {member.card.status === 'Suspended' ? 'Kích hoạt lại' : 'Tạm khóa thẻ'}
                </Button>
              </>
            ) : (
              <>
                <Field label="Số thẻ">
                  <Input required value={a} onChange={(e) => setA(e.target.value)} />
                </Field>
                <Field label="Ngày cấp">
                  <Input
                    required
                    type="date"
                    value={b || today()}
                    onChange={(e) => setB(e.target.value)}
                  />
                </Field>
                <Field label="Ngày hết hạn">
                  <Input required type="date" value={c} onChange={(e) => setC(e.target.value)} />
                </Field>
              </>
            )
          ) : action === 'restriction' ? (
            <>
              <Field label="Loại hạn chế">
                <Select value={a} onValueChange={setA}>
                  <SelectTrigger>
                    <SelectValue placeholder="Chọn loại" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Borrowing">Mượn sách</SelectItem>
                    <SelectItem value="Reservation">Đặt trước</SelectItem>
                    <SelectItem value="AllTransactions">Mọi giao dịch</SelectItem>
                  </SelectContent>
                </Select>
              </Field>
              <Field label="Lý do">
                <Input required value={b} onChange={(e) => setB(e.target.value)} />
              </Field>
              <Field label="Kết thúc (tùy chọn)">
                <Input type="datetime-local" value={c} onChange={(e) => setC(e.target.value)} />
              </Field>
            </>
          ) : action === 'payment' ? (
            <>
              <Field label="Số tiền">
                <Input
                  required
                  type="number"
                  min="1"
                  value={a}
                  onChange={(e) => setA(e.target.value)}
                />
              </Field>
              <Field label="Phương thức">
                <Select value={b || 'Cash'} onValueChange={setB}>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Cash">Tiền mặt</SelectItem>
                    <SelectItem value="BankTransfer">Chuyển khoản</SelectItem>
                    <SelectItem value="Card">Thẻ</SelectItem>
                    <SelectItem value="Other">Khác</SelectItem>
                  </SelectContent>
                </Select>
              </Field>
              <Field label="Mã tham chiếu">
                <Input value={c} onChange={(e) => setC(e.target.value)} />
              </Field>
            </>
          ) : (
            <>
              <Field label="Số tiền điều chỉnh">
                <Input
                  required
                  type="number"
                  value={a}
                  onChange={(e) => setA(e.target.value)}
                  placeholder="Âm để giảm, dương để tăng"
                />
              </Field>
              <Field label="Lý do">
                <Input required value={b} onChange={(e) => setB(e.target.value)} />
              </Field>
            </>
          )}
          {error && <p className="text-sm text-destructive">{error}</p>}
          <DialogFooter>
            <Button type="button" variant="outline" onClick={close}>
              Hủy
            </Button>
            <Button>Xác nhận</Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
