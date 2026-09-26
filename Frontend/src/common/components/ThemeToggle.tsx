import { Moon, Sun } from 'lucide-react'
import { Button } from '@/common/components/ui/button'
import { useSettings } from '@/settings/SettingsProvider'
import { cn } from '@/utils/cn'

export function ThemeToggle({ className }: { className?: string }) {
  const { setTheme } = useSettings()

  const toggleTheme = () => {
    const currentTheme = document.documentElement.dataset.theme
    setTheme(currentTheme === 'dark' ? 'light' : 'dark')
  }

  return (
    <Button
      type="button"
      variant="ghost"
      size="icon"
      className={cn('header-action-button', className)}
      aria-label="Chuyển giao diện sáng hoặc tối"
      title="Chuyển giao diện sáng hoặc tối"
      onClick={toggleTheme}
    >
      <Moon className="dark:hidden" />
      <Sun className="hidden dark:block" />
    </Button>
  )
}
