import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { useState, type ReactNode } from 'react'
import { BrowserRouter } from 'react-router-dom'
import { AuthProvider } from '@/auth/AuthProvider'
import { SettingsProvider } from '@/settings/SettingsProvider'
import { AppErrorBoundary } from './AppErrorBoundary'
export function AppProviders({ children }: { children: ReactNode }) {
  const [client] = useState(() => new QueryClient({ defaultOptions: { queries: { staleTime: 30_000, retry: 1, refetchOnWindowFocus: false }, mutations: { retry: 0 } } }))
  return <AppErrorBoundary><QueryClientProvider client={client}><BrowserRouter><SettingsProvider><AuthProvider>{children}</AuthProvider></SettingsProvider></BrowserRouter></QueryClientProvider></AppErrorBoundary>
}
