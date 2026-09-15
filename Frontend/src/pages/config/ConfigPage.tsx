import { useState } from 'react'
import { Button } from '@/common/components/ui/button'
import { useAuth } from '@/auth/AuthProvider'
import { can } from '@/shared/auth/permissions'
import { CirculationPolicyList } from './components/CirculationPolicyList'
import { CirculationPolicyPreview } from './components/CirculationPolicyPreview'

export function ConfigPage() {
  const { user } = useAuth()
  const permissions = user?.permissions ?? []
  const canRead = can(permissions, 'circulation-policies.read')
  const canManage = can(permissions, 'circulation-policies.manage')
  const [view, setView] = useState<'policies' | 'preview'>('policies')

  if (!canRead) {
    return (
      <div className="mx-auto w-full max-w-5xl px-5 py-10 md:px-12">
        <h1 className="text-2xl font-bold">Không có quyền truy cập</h1>
        <p className="mt-2 text-sm text-muted-foreground">Tài khoản cần quyền xem chính sách lưu thông.</p>
      </div>
    )
  }

  return (
    <div className="mx-auto w-full max-w-7xl px-5 py-8 md:px-10">
      <div className="mb-6">
        <p className="mb-2 text-xs font-bold uppercase tracking-widest text-primary">Quản lý hệ thống</p>
        <h1 className="text-3xl font-bold tracking-tight">Chính sách lưu thông</h1>
        <p className="mt-2 text-sm text-muted-foreground">
          Quản lý hạn mức mượn, gia hạn, giữ chỗ và tiền phạt theo phạm vi hiệu lực.
        </p>
      </div>

      <div className="mb-5 flex gap-2" role="tablist" aria-label="Chính sách lưu thông">
        <Button role="tab" aria-selected={view === 'policies'} variant={view === 'policies' ? 'default' : 'outline'} onClick={() => setView('policies')}>
          Danh sách và phiên bản
        </Button>
        <Button role="tab" aria-selected={view === 'preview'} variant={view === 'preview' ? 'default' : 'outline'} onClick={() => setView('preview')}>
          Xem trước kết quả
        </Button>
      </div>

      {view === 'policies' ? <CirculationPolicyList canManage={canManage} /> : <CirculationPolicyPreview />}
    </div>
  )
}
