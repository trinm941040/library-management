import { useNavigate } from 'react-router-dom'
import { ScreenState } from '@/common/components/molecules/ScreenState'

export function NotFoundPage() {
  const navigate = useNavigate()
  return (
    <main className="p-4 sm:p-8">
      <ScreenState
        kind="empty"
        title="Không tìm thấy trang"
        description="Địa chỉ này không tồn tại hoặc đã được di chuyển."
        actionLabel="Về trang tổng quan"
        onAction={() => navigate('/dashboard')}
      />
    </main>
  )
}
