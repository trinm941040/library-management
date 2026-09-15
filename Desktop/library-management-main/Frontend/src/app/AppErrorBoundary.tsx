import { Component, type ErrorInfo, type ReactNode } from 'react'
import { ScreenState } from '@/common/components/molecules/ScreenState'

export class AppErrorBoundary extends Component<{ children: ReactNode }, { failed: boolean }> {
  state = { failed: false }
  static getDerivedStateFromError() {
    return { failed: true }
  }
  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('Lỗi hiển thị ứng dụng', error, info)
  }
  render() {
    return this.state.failed ? (
      <main className="p-8">
        <ScreenState
          kind="error"
          title="Ứng dụng gặp sự cố"
          description="Không thể hiển thị nội dung vào lúc này."
          actionLabel="Tải lại"
          onAction={() => window.location.reload()}
        />
      </main>
    ) : (
      this.props.children
    )
  }
}
