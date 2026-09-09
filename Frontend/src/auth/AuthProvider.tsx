import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'
import * as authApi from './auth-api'
import type { User } from './auth-api'

type AuthStatus = 'loading' | 'authenticated' | 'unauthenticated'

type AuthContextValue = {
  user: User | null
  status: AuthStatus
  login: (email: string, password: string) => Promise<void>
  logout: () => Promise<void>
  updateUser: (user: User) => void
  clearSession: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [status, setStatus] = useState<AuthStatus>('loading')

  useEffect(() => {
    authApi
      .restoreSession()
      .then((currentUser) => {
        setUser(currentUser)
        setStatus('authenticated')
      })
      .catch(() => {
        setUser(null)
        setStatus('unauthenticated')
      })
  }, [])

  useEffect(() => {
    const expire = () => { setUser(null); setStatus('unauthenticated') }
    window.addEventListener('auth:expired', expire)
    return () => window.removeEventListener('auth:expired', expire)
  }, [])

  const login = async (email: string, password: string) => {
    const currentUser = await authApi.login(email, password)
    setUser(currentUser)
    setStatus('authenticated')
  }

  const clearSession = () => {
    authApi.clearLocalSession()
    setUser(null)
    setStatus('unauthenticated')
  }

  const logout = async () => {
    try { await authApi.logout() } finally { clearSession() }
  }

  return (
    <AuthContext.Provider value={{ user, status, login, logout, updateUser: setUser, clearSession }}>{children}</AuthContext.Provider>
  )
}

export function useAuth() {
  const context = useContext(AuthContext)

  if (!context) {
    throw new Error('useAuth phải được dùng bên trong AuthProvider.')
  }

  return context
}
