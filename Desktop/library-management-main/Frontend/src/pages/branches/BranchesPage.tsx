import { useEffect, useState } from 'react'
import { Map, RefreshCw, XCircle } from 'lucide-react'
import { Button } from '@/common/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/common/components/ui/card'
import { PageShell, ScreenState, StatusBadge, useToast } from '@/common/components'
import { PermissionBoundary } from '@/shared/auth/PermissionBoundary'
import { deactivateLocation, getLocations, type LocationNode } from './branch-api'

export function BranchesPage() {
  const [locations, setLocations] = useState<LocationNode[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [actionError, setActionError] = useState('')
  const { showToast } = useToast()

  const load = () => {
    setLoading(true)
    setError('')
    getLocations()
      .then(setLocations)
      .catch((reason: unknown) => setError(reason instanceof Error ? reason.message : 'Không thể tải cây vị trí.'))
      .finally(() => setLoading(false))
  }
  useEffect(load, [])

  const deactivate = async (type: 'branches' | 'areas' | 'shelves', node: LocationNode) => {
    setActionError('')
    try {
      await deactivateLocation(type, node.id)
      showToast(`Đã ngừng hoạt động ${node.code}.`)
      load()
    } catch (reason) {
      setActionError(reason instanceof Error ? reason.message : 'Không thể ngừng hoạt động vị trí.')
    }
  }

  return (
    <PageShell
      eyebrow="Cấu trúc thư viện"
      title="Chi nhánh, khu vực và kệ"
      description="Quản lý cây vị trí và theo dõi các vị trí đang được sử dụng."
      actions={<Button variant="outline" disabled={loading} onClick={load}><RefreshCw className={loading ? 'animate-spin' : ''} /> Làm mới</Button>}
    >
      {actionError ? <div className="mb-4 rounded-md border border-destructive/30 bg-destructive/5 p-3 text-sm">{actionError}</div> : null}
      {loading ? <p className="p-6 text-center">Đang tải cây vị trí...</p> : error ? <ScreenState kind="error" title="Không thể tải vị trí" description={error} actionLabel="Thử lại" onAction={load} /> : locations.length === 0 ? <ScreenState kind="empty" title="Chưa có chi nhánh" description="Tạo chi nhánh đầu tiên để bắt đầu cấu hình vị trí." /> : <div className="grid gap-4">{locations.map((branch) => <LocationCard key={branch.id} node={branch} type="branches" onDeactivate={deactivate} />)}</div>}
    </PageShell>
  )
}

function LocationCard({ node, type, onDeactivate }: { node: LocationNode; type: 'branches' | 'areas' | 'shelves'; onDeactivate: (type: 'branches' | 'areas' | 'shelves', node: LocationNode) => void }) {
  const childType = type === 'branches' ? 'areas' : 'shelves'
  return <Card>
    <CardHeader className="flex flex-row items-center justify-between gap-3"><CardTitle className="flex items-center gap-2"><Map className="size-4" />{node.code} · {node.name}</CardTitle><StatusBadge label={node.isActive ? 'Đang hoạt động' : 'Ngừng hoạt động'} tone={node.isActive ? 'success' : 'danger'} /></CardHeader>
    <CardContent className="grid gap-3 pl-8">{node.address ? <p className="text-sm text-muted-foreground">{node.address}</p> : null}<PermissionBoundary requiredPermissions={['locations.deactivate']}><Button variant="ghost" size="sm" className="w-fit" disabled={!node.isActive} onClick={() => onDeactivate(type, node)}><XCircle /> Ngừng hoạt động</Button></PermissionBoundary>{node.children?.map((child) => <LocationCard key={child.id} node={child} type={childType} onDeactivate={onDeactivate} />)}</CardContent>
  </Card>
}
