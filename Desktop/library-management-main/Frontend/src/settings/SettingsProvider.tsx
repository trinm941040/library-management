import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'

export type ThemePreference = 'light' | 'dark' | 'system'

type SettingsContextValue = {
  theme: ThemePreference
  sidebarPinned: boolean
  setTheme: (theme: ThemePreference) => void
  setSidebarPinned: (pinned: boolean) => void
}

const THEME_KEY = 'library-theme'
const SIDEBAR_KEY = 'library-sidebar-pinned'
const SettingsContext = createContext<SettingsContextValue | null>(null)

function getSavedTheme(): ThemePreference {
  const savedTheme = localStorage.getItem(THEME_KEY)
  return savedTheme === 'light' || savedTheme === 'dark' || savedTheme === 'system'
    ? savedTheme
    : 'system'
}

export function SettingsProvider({ children }: { children: ReactNode }) {
  const [theme, setTheme] = useState<ThemePreference>(getSavedTheme)
  const [sidebarPinned, setSidebarPinned] = useState(
    () => localStorage.getItem(SIDEBAR_KEY) === 'true',
  )

  useEffect(() => {
    const systemTheme = window.matchMedia('(prefers-color-scheme: dark)')

    const applyTheme = () => {
      const resolvedTheme = theme === 'system' ? (systemTheme.matches ? 'dark' : 'light') : theme
      document.documentElement.dataset.theme = resolvedTheme
      document.documentElement.style.colorScheme = resolvedTheme
    }

    localStorage.setItem(THEME_KEY, theme)
    applyTheme()
    systemTheme.addEventListener('change', applyTheme)

    return () => systemTheme.removeEventListener('change', applyTheme)
  }, [theme])

  useEffect(() => {
    localStorage.setItem(SIDEBAR_KEY, String(sidebarPinned))
  }, [sidebarPinned])

  return (
    <SettingsContext.Provider value={{ theme, sidebarPinned, setTheme, setSidebarPinned }}>
      {children}
    </SettingsContext.Provider>
  )
}

export function useSettings() {
  const context = useContext(SettingsContext)

  if (!context) {
    throw new Error('useSettings phải được dùng bên trong SettingsProvider.')
  }

  return context
}
