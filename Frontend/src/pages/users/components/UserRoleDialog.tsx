import { useEffect, useState } from 'react'
import { ShieldCheck } from 'lucide-react'
import { Button } from '@/common/components/ui/button'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/common/components/ui/dialog'
import { getRoles, replaceUserRoles, type Role } from '@/pages/roles/role-permission-api'
import type { SystemUser } from '../user-api'

export function UserRoleDialog({ user, onOpenChange, onSaved }: { user: SystemUser | null; onOpenChange: (open: boolean) => void; onSaved: () => void }) {
  const [roles, setRoles] = useState<Role[]>([])
  const [selected, setSelected] = useState<Set<string>>(new Set())
  const [loading, setLoading] = useState(false)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    if (!user) return
    const controller = new AbortController()
    setLoading(true)
    setError('')
    getRoles(undefined, controller.signal).then((items) => {
      setRoles(items)
      setSelected(new Set(items.filter((role) => user.roles.includes(role.name)).map((role) => role.id)))
    }).catch((cause: unknown) => {
      if (!(cause instanceof DOMException && cause.name === 'AbortError')) setError(cause instanceof Error ? cause.message : 'Không thể tải danh sách vai trò.')
    }).finally(() => { if (!controller.signal.aborted) setLoading(false) })
    return () => controller.abort()
  }, [user])

  const save = async () => {
    if (!user || selected.size === 0) return setError('Người dùng phải có ít nhất một vai trò.')
    setSaving(true)
    setError('')
    try { await replaceUserRoles(user.id, [...selected]); onOpenChange(false); onSaved() }
    catch (cause) { setError(cause instanceof Error ? cause.message : 'Không thể cập nhật vai trò.') }
    finally { setSaving(false) }
  }

  return (
    <Dialog open={!!user} onOpenChange={(open) => !saving && onOpenChange(open)}>
      <DialogContent>
        <DialogHeader><DialogTitle>Vai trò của {user?.displayName}</DialogTitle><DialogDescription>Chọn một hoặc nhiều vai trò. Bỏ chọn tương đương xóa vai trò khỏi người dùng.</DialogDescription></DialogHeader>
        <div className="grid max-h-80 gap-2 overflow-y-auto py-4">
          {loading ? <p className="text-sm text-muted-foreground">Đang tải vai trò...</p> : roles.map((role) => (
            <div key={role.id} className="flex items-start gap-3 rounded-md border p-3">
              <input aria-label={`Gán vai trò ${role.name}`} className="mt-1" type="checkbox" checked={selected.has(role.id)} disabled={saving} onChange={(event) => setSelected((current) => { const next = new Set(current); if (event.target.checked) next.add(role.id); else next.delete(role.id); return next })} />
              <span><strong className="flex items-center gap-2"><ShieldCheck className="size-4" />{role.name}</strong><small className="text-muted-foreground">{role.description || 'Không có mô tả'}</small></span>
            </div>
          ))}
        </div>
        {error ? <p className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive" role="alert">{error}</p> : null}
        <DialogFooter><Button variant="outline" disabled={saving} onClick={() => onOpenChange(false)}>Hủy</Button><Button disabled={loading || saving || selected.size === 0} onClick={save}>{saving ? 'Đang lưu...' : 'Lưu vai trò'}</Button></DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
