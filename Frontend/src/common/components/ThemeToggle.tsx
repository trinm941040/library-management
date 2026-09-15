import { Moon, Sun } from 'lucide-react'
import { Button } from '@/common/components/ui/button'
import { useSettings } from '@/settings/SettingsProvider'

export function ThemeToggle() {
  const { setTheme } = useSettings()

  const toggleTheme = () => {
    const currentTheme = document.documentElement.dataset.theme
    setTheme(currentTheme === 'dark' ? 'light' : 'dark')
  }

  return (
    <Button
      type="button"
      variant="outline"
      size="icon"
      className="fixed top-4 right-4 z-50 bg-background shadow-sm"
      aria-label="Chuyển giao diện sáng hoặc tối"
      title="Chuyển giao diện sáng hoặc tối"
      onClick={toggleTheme}
    >
      <Moon className="dark:hidden" />
      <Sun className="hidden dark:block" />
    </Button>
  )
}
