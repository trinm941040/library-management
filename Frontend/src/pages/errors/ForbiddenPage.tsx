import { useNavigate } from 'react-router-dom'
import { ScreenState } from '@/common/components/molecules/ScreenState'

export function ForbiddenPage() {
  const navigate = useNavigate()
  return (
    <main className="p-4 sm:p-8">
      <ScreenState
        kind="forbidden"
        title="Không có quyền truy cập"
        description="Tài khoản của bạn không được cấp quyền cho trang này."
        actionLabel="Về trang tổng quan"
        onAction={() => navigate('/dashboard')}
      />
    </main>
  )
}
