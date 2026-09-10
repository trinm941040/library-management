import { Component, type ErrorInfo, type ReactNode } from 'react'
export class AppErrorBoundary extends Component<{ children: ReactNode }, { failed: boolean }> {
  state = { failed: false }
  static getDerivedStateFromError() { return { failed: true } }
  componentDidCatch(error: Error, info: ErrorInfo) { console.error('Lỗi hiển thị ứng dụng', error, info) }
  render() { return this.state.failed ? <main className="p-8 text-center"><h1 className="text-2xl font-semibold">Ứng dụng gặp sự cố</h1><button className="mt-4 underline" onClick={() => window.location.reload()}>Tải lại</button></main> : this.props.children }
}
