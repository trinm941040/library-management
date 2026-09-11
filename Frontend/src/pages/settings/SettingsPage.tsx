import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/common/components/ui/card'
import { Label } from '@/common/components/ui/label'
import { Switch } from '@/common/components/ui/switch'
import { useSettings } from '@/settings/SettingsProvider'

export function SettingsPage() {
  const { sidebarPinned, setSidebarPinned } = useSettings()

  return (
    <div className="mx-auto w-full max-w-4xl px-5 py-10 md:px-12">
      <div className="mb-7">
        <p className="mb-2 text-xs font-bold tracking-widest text-primary">HỆ THỐNG</p>
        <h1 className="text-3xl font-bold tracking-tight">Cài đặt</h1>
        <p className="mt-2 text-sm text-muted-foreground">Tùy chỉnh cách sidebar hoạt động.</p>
      </div>

      <div className="grid gap-5">
        <Card>
          <CardHeader>
            <CardTitle>Sidebar</CardTitle>
            <CardDescription>Cấu hình cách menu bên trái hiển thị.</CardDescription>
          </CardHeader>
          <CardContent className="flex items-center justify-between gap-6">
            <div className="grid gap-1">
              <Label htmlFor="sidebar-pinned">Cố định sidebar</Label>
              <p className="text-sm text-muted-foreground">
                Giữ sidebar luôn hiển thị và không cho phép ẩn.
              </p>
            </div>
            <Switch
              id="sidebar-pinned"
              checked={sidebarPinned}
              onCheckedChange={setSidebarPinned}
            />
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
