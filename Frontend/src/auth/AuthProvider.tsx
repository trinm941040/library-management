import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useRef,
  useState,
  type ReactNode,
} from 'react'
import { useQueryClient } from '@tanstack/react-query'
import * as authApi from './auth-api'
import type { User } from './auth-api'

type AuthStatus = 'loading' | 'authenticated' | 'unauthenticated' | 'error'
type AuthContextValue = {
  user: User | null
  status: AuthStatus
  login: (email: string, password: string) => Promise<void>
  logout: () => Promise<void>
  updateUser: (user: User) => void
  clearSession: () => void
  retrySession: () => void
}
const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [status, setStatus] = useState<AuthStatus>('loading')
  const client = useQueryClient()
  const authorizationKey = useRef('')
  const channel = useRef<BroadcastChannel | null>(null)

  const updateUser = useCallback(
    (currentUser: User | null) => {
      const key = currentUser
        ? JSON.stringify([
            currentUser.id,
            [...currentUser.roles].sort(),
            [...currentUser.permissions].sort(),
          ])
        : ''
      if (key !== authorizationKey.current) {
        void client.cancelQueries()
        client.clear()
        authorizationKey.current = key
      }
      setUser(currentUser)
      setStatus(currentUser ? 'authenticated' : 'unauthenticated')
    },
    [client],
  )

  const handleSessionError = useCallback(
    (error: unknown) => {
      if (error instanceof DOMException && error.name === 'AbortError') return
      if (error instanceof authApi.ApiError && error.status === 401) {
        updateUser(null)
      } else {
        // Hide authorization consumers when current rights cannot be verified.
        setUser(null)
        setStatus('error')
      }
    },
    [updateUser],
  )

  const retrySession = useCallback(() => {
    setStatus('loading')
    void authApi.restoreSession().catch(handleSessionError)
  }, [handleSessionError])

  useEffect(() => {
    const unsubscribe = authApi.subscribeSession(updateUser)
    void authApi.restoreSession().catch(handleSessionError)
    return unsubscribe
  }, [updateUser, handleSessionError])

  useEffect(() => {
    if (status !== 'authenticated') return
    const synchronize = () => {
      if (document.visibilityState === 'visible')
        void authApi.getProfile().catch(handleSessionError)
    }
    const timer = window.setInterval(synchronize, 30_000)
    window.addEventListener('focus', synchronize)
    window.addEventListener('auth:authorization-stale', synchronize)
    document.addEventListener('visibilitychange', synchronize)
    return () => {
      window.clearInterval(timer)
      window.removeEventListener('focus', synchronize)
      window.removeEventListener('auth:authorization-stale', synchronize)
      document.removeEventListener('visibilitychange', synchronize)
    }
  }, [status, handleSessionError])

  useEffect(() => {
    if (typeof BroadcastChannel === 'undefined') return
    const currentChannel = new BroadcastChannel('uth-auth-events')
    channel.current = currentChannel
    currentChannel.onmessage = (event: MessageEvent<unknown>) => {
      if (event.data === 'logout') authApi.clearLocalSession()
      if (event.data === 'authorization-changed' && authorizationKey.current) {
        void authApi.getProfile().catch(handleSessionError)
      }
    }
    const broadcastChange = () => currentChannel.postMessage('authorization-changed')
    window.addEventListener('auth:authorization-changed', broadcastChange)
    return () => {
      window.removeEventListener('auth:authorization-changed', broadcastChange)
      channel.current = null
      currentChannel.close()
    }
  }, [handleSessionError])

  const clearSession = () => {
    authApi.clearLocalSession()
    channel.current?.postMessage('logout')
  }
  const logout = async () => {
    clearSession()
    await authApi.logout()
  }
  const login = async (email: string, password: string) => {
    await authApi.login(email, password)
  }

  return (
    <AuthContext.Provider
      value={{ user, status, login, logout, updateUser, clearSession, retrySession }}
    >
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth phải được dùng bên trong AuthProvider.')
  return context
}
