import { useCallback, useEffect, useMemo, useState } from 'react'
import { Building2, MapPinned, Pencil, Plus, Power, PowerOff, RefreshCw, Rows3 } from 'lucide-react'
import { ConfirmDialog, PageShell, ScreenState, StatusBadge, useToast } from '@/common/components'
import { Button } from '@/common/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/common/components/ui/card'
import { PermissionBoundary } from '@/shared/auth/PermissionBoundary'
import {
  activateLocation,
  createLocation,
  deactivateLocation,
  getLocationImpact,
  getLocations,
  updateLocation,
  type LocationImpact,
  type LocationInput,
  type LocationNode,
  type LocationType,
} from './branch-api'
import { LocationFormDialog } from './LocationFormDialog'

type FormState = {
  type: LocationType
  editing: LocationNode | null
  parentId: string | null
}

type DeactivateState = {
  node: LocationNode
  impact: LocationImpact | null
  loading: boolean
  error: string
}

const emptyImpact: LocationImpact = {
  employeeCount: 0,
  bookCopyCount: 0,
  activeInventoryAuditCount: 0,
  editableStockReceiptCount: 0,
  hasBlockingReferences: false,
}

export function BranchesPage() {
  const [locations, setLocations] = useState<LocationNode[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [actionError, setActionError] = useState('')
  const [form, setForm] = useState<FormState | null>(null)
  const [deactivating, setDeactivating] = useState<DeactivateState | null>(null)
  const [actionPending, setActionPending] = useState(false)
  const { showToast } = useToast()

  const load = useCallback((signal?: AbortSignal) => {
    setLoading(true)
    setError('')
    return getLocations(signal)
      .then(setLocations)
      .catch((reason: unknown) => {
        if (reason instanceof DOMException && reason.name === 'AbortError') return
        setError(reason instanceof Error ? reason.message : 'Không thể tải cấu trúc vị trí.')
      })
      .finally(() => {
        if (!signal?.aborted) setLoading(false)
      })
  }, [])

  useEffect(() => {
    const controller = new AbortController()
    void load(controller.signal)
    return () => controller.abort()
  }, [load])

  const areas = useMemo(() => locations.flatMap((branch) => branch.children), [locations])
  const parentOptions = useMemo(() => {
    if (form?.type === 'Area')
      return locations.map((branch) => ({ id: branch.id, label: `${branch.code} · ${branch.name}` }))
    if (form?.type === 'Shelf')
      return areas
        .filter((area) => area.isActive)
        .map((area) => ({ id: area.id, label: `${area.code} · ${area.name}` }))
    return []
  }, [areas, form?.type, locations])

  const save = async (input: LocationInput) => {
    if (!form) return 'Không xác định được biểu mẫu vị trí.'
    try {
      if (form.editing) await updateLocation(form.type, form.editing.id, input)
      else await createLocation(form.type, input)
      await load()
      showToast(form.editing ? 'Đã cập nhật vị trí.' : 'Đã tạo vị trí mới.')
      return null
    } catch (reason) {
      return reason instanceof Error ? reason.message : 'Không thể lưu vị trí.'
    }
  }

  const activate = async (node: LocationNode) => {
    setActionPending(true)
    setActionError('')
    try {
      await activateLocation(node)
      await load()
      showToast(`Đã kích hoạt ${node.code}.`)
    } catch (reason) {
      const message = reason instanceof Error ? reason.message : 'Không thể kích hoạt vị trí.'
      setActionError(message)
      showToast(message, { tone: 'error' })
    } finally {
      setActionPending(false)
    }
  }

  const inspectDeactivate = async (node: LocationNode) => {
    setDeactivating({ node, impact: null, loading: true, error: '' })
    try {
      const impact = await getLocationImpact(node.type, node.id)
      setDeactivating({ node, impact, loading: false, error: '' })
    } catch (reason) {
      setDeactivating({
        node,
        impact: null,
        loading: false,
        error: reason instanceof Error ? reason.message : 'Không thể kiểm tra liên kết vị trí.',
      })
    }
  }

  const confirmDeactivate = async () => {
    if (!deactivating?.impact || deactivating.impact.hasBlockingReferences) return
    setActionPending(true)
    setDeactivating((current) => current ? { ...current, error: '' } : current)
    try {
      await deactivateLocation(deactivating.node)
      setDeactivating(null)
      await load()
      showToast(`Đã ngừng hoạt động ${deactivating.node.code}.`)
    } catch (reason) {
      setDeactivating((current) => current ? {
        ...current,
        error: reason instanceof Error ? reason.message : 'Không thể ngừng hoạt động vị trí.',
      } : current)
    } finally {
      setActionPending(false)
    }
  }

  const impact = deactivating?.impact ?? emptyImpact
  const impactDescription = deactivating?.loading
    ? 'Đang kiểm tra các đối tượng đang sử dụng vị trí này...'
    : deactivating?.impact
      ? `Nhân viên: ${impact.employeeCount}; bản sao sách: ${impact.bookCopyCount}; kiểm kê đang hoạt động: ${impact.activeInventoryAuditCount}; phiếu nhập chưa khóa: ${impact.editableStockReceiptCount}. ${impact.hasBlockingReferences ? 'Hãy xử lý các liên kết này trước khi ngừng hoạt động.' : 'Không có liên kết chặn thao tác.'}`
      : 'Không thể xác định ảnh hưởng của thao tác.'

  return (
    <>
      <PageShell
        eyebrow="Cấu trúc thư viện"
        title="Chi nhánh, khu vực và kệ"
        description="Quản lý cấu trúc Branch → Area → Shelf và điều kiện sẵn sàng trước khi kích hoạt."
        actions={
          <>
            <Button variant="outline" loading={loading && locations.length > 0} onClick={() => void load()}>
              <RefreshCw /> Làm mới
            </Button>
            <PermissionBoundary requiredPermissions={['locations.create']}>
              <Button onClick={() => setForm({ type: 'Branch', editing: null, parentId: null })}>
                <Plus /> Tạo chi nhánh
              </Button>
            </PermissionBoundary>
          </>
        }
      >
        {actionError ? <p className="mb-4 rounded-md border border-destructive/30 bg-destructive/5 p-3 text-sm text-destructive" role="alert">{actionError}</p> : null}
        {loading && locations.length === 0 ? (
          <ScreenState kind="loading" title="Đang tải cấu trúc vị trí" />
        ) : error ? (
          <ScreenState kind="error" title="Không thể tải cấu trúc vị trí" description={error} actionLabel="Thử lại" onAction={() => void load()} />
        ) : locations.length === 0 ? (
          <ScreenState kind="empty" title="Chưa có chi nhánh" description="Tạo chi nhánh, sau đó thêm khu vực và kệ để kích hoạt." />
        ) : (
          <div className="grid gap-5">
            {locations.map((branch) => (
              <LocationCard
                key={branch.id}
                node={branch}
                pending={actionPending}
                onCreate={(type, parentId) => setForm({ type, editing: null, parentId })}
                onEdit={(node) => setForm({ type: node.type, editing: node, parentId: node.parentId })}
                onActivate={(node) => void activate(node)}
                onDeactivate={(node) => void inspectDeactivate(node)}
              />
            ))}
          </div>
        )}
      </PageShell>

      {form ? (
        <LocationFormDialog
          open
          type={form.type}
          editing={form.editing}
          defaultParentId={form.parentId}
          parentOptions={parentOptions}
          onOpenChange={(open) => !open && setForm(null)}
          onSave={save}
        />
      ) : null}

      <ConfirmDialog
        open={deactivating !== null}
        title={`Ngừng hoạt động ${deactivating?.node.code ?? ''}?`}
        description={impactDescription}
        confirmLabel="Ngừng hoạt động"
        destructive
        confirmDisabled={deactivating?.loading || !deactivating?.impact || impact.hasBlockingReferences}
        isPending={actionPending}
        error={deactivating?.error}
        onConfirm={() => void confirmDeactivate()}
        onOpenChange={(open) => !open && setDeactivating(null)}
      />
    </>
  )
}

function LocationCard({
  node,
  pending,
  onCreate,
  onEdit,
  onActivate,
  onDeactivate,
}: {
  node: LocationNode
  pending: boolean
  onCreate: (type: LocationType, parentId: string) => void
  onEdit: (node: LocationNode) => void
  onActivate: (node: LocationNode) => void
  onDeactivate: (node: LocationNode) => void
}) {
  const Icon = node.type === 'Branch' ? Building2 : node.type === 'Area' ? MapPinned : Rows3
  const readiness = node.type === 'Branch' ? node.readiness : null
  const canActivate = node.type !== 'Branch' || readiness?.canActivate === true
  return (
    <Card className={node.type === 'Branch' ? '' : 'shadow-none'}>
      <CardHeader className="flex flex-row flex-wrap items-start justify-between gap-3">
        <div>
          <CardTitle className="flex items-center gap-2 text-base"><Icon className="size-4" /> {node.code} · {node.name}</CardTitle>
          {node.address ? <p className="mt-1 text-sm text-muted-foreground">{node.address}</p> : null}
          {readiness && !readiness.canActivate ? (
            <div className="mt-2 rounded-md border border-amber-500/30 bg-amber-50 p-2 text-xs text-amber-950 dark:bg-amber-950/30 dark:text-amber-100">
              <strong>Chưa đủ điều kiện kích hoạt.</strong>
              <ul className="mt-1 list-disc pl-4">{readiness.missingRequirements.map((value) => <li key={value}>{value}</li>)}</ul>
            </div>
          ) : readiness ? (
            <p className="mt-2 text-xs text-muted-foreground">{readiness.activeAreaCount} khu vực active · {readiness.activeShelfCount} kệ active</p>
          ) : null}
        </div>
        <div className="flex flex-wrap items-center gap-1">
          <StatusBadge label={node.isActive ? 'Đang hoạt động' : 'Ngừng hoạt động'} tone={node.isActive ? 'success' : 'danger'} />
          <PermissionBoundary requiredPermissions={['locations.update']}>
            <Button variant="ghost" size="icon" aria-label={`Sửa ${node.code}`} onClick={() => onEdit(node)}><Pencil /></Button>
            {!node.isActive ? (
              <Button variant="ghost" size="icon" disabled={pending || !canActivate} title={!canActivate ? 'Hãy thêm Area và Shelf active trước' : undefined} aria-label={`Kích hoạt ${node.code}`} onClick={() => onActivate(node)}><Power /></Button>
            ) : null}
          </PermissionBoundary>
          <PermissionBoundary requiredPermissions={['locations.deactivate']}>
            {node.isActive ? <Button variant="ghost" size="icon" disabled={pending} aria-label={`Ngừng hoạt động ${node.code}`} onClick={() => onDeactivate(node)}><PowerOff /></Button> : null}
          </PermissionBoundary>
          <PermissionBoundary requiredPermissions={['locations.create']}>
            {node.type === 'Branch' ? <Button variant="outline" size="sm" onClick={() => onCreate('Area', node.id)}><Plus /> Khu vực</Button> : null}
            {node.type === 'Area' && node.isActive ? <Button variant="outline" size="sm" onClick={() => onCreate('Shelf', node.id)}><Plus /> Kệ</Button> : null}
          </PermissionBoundary>
        </div>
      </CardHeader>
      {node.children.length > 0 ? (
        <CardContent className="grid gap-3 border-t pt-4">
          {node.children.map((child) => (
            <LocationCard key={child.id} node={child} pending={pending} onCreate={onCreate} onEdit={onEdit} onActivate={onActivate} onDeactivate={onDeactivate} />
          ))}
        </CardContent>
      ) : null}
    </Card>
  )
}
