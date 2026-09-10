import { Save } from 'lucide-react'
import { Button } from '@/common/components/ui/button'
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/common/components/ui/card'
import { Input } from '@/common/components/ui/input'
import { Label } from '@/common/components/ui/label'
import { Switch } from '@/common/components/ui/switch'
import { useState } from 'react'

export function OtherSettingsPage() {
  const [isSaving, setIsSaving] = useState(false)

  // Controlled states loaded from localStorage
  const [smtpHost, setSmtpHost] = useState(() => localStorage.getItem('other_smtpHost') || 'smtp.gmail.com')
  const [smtpPort, setSmtpPort] = useState(() => localStorage.getItem('other_smtpPort') || '587')
  const [smtpUser, setSmtpUser] = useState(() => localStorage.getItem('other_smtpUser') || 'no-reply@northstarlibrary.com')
  const [backupRetention, setBackupRetention] = useState(() => localStorage.getItem('other_backupRetention') || '30')
  const [autoBackup, setAutoBackup] = useState(() => {
    const saved = localStorage.getItem('other_autoBackup')
    return saved !== null ? saved === 'true' : true
  })

  const handleSave = () => {
    setIsSaving(true)
    setTimeout(() => {
      // Save to localStorage
      localStorage.setItem('other_smtpHost', smtpHost)
      localStorage.setItem('other_smtpPort', smtpPort)
      localStorage.setItem('other_smtpUser', smtpUser)
      localStorage.setItem('other_backupRetention', backupRetention)
      localStorage.setItem('other_autoBackup', autoBackup.toString())
      
      setIsSaving(false)
      alert('Đã lưu các thiết lập khác thành công!')
    }, 800)
  }
  return (
    <div className="mx-auto w-full max-w-5xl px-5 py-10 md:px-12">
      <div className="mb-7 flex items-start justify-between">
        <div>
          <p className="mb-2 text-xs font-bold uppercase tracking-widest text-primary">
            Quản lý hệ thống
          </p>
          <h1 className="text-3xl font-bold tracking-tight">Thiết lập khác</h1>
          <p className="mt-2 text-sm text-muted-foreground">
            Cấu hình email server, tự động sao lưu và các tính năng nâng cao.
          </p>
        </div>
        <div className="flex gap-3">
          <Button onClick={handleSave} disabled={isSaving}>
            <Save className={`mr-2 h-4 w-4 ${isSaving ? 'animate-pulse' : ''}`} /> 
            {isSaving ? 'Đang lưu...' : 'Lưu cài đặt'}
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 gap-5 md:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Cấu hình Email (SMTP)</CardTitle>
            <CardDescription>Dùng để gửi thông báo mượn/trả sách cho độc giả.</CardDescription>
          </CardHeader>
          <CardContent className="grid gap-6">
            <div className="grid gap-2">
              <Label htmlFor="smtp-host">Máy chủ SMTP</Label>
              <Input id="smtp-host" value={smtpHost} onChange={(e) => setSmtpHost(e.target.value)} />
            </div>
            <div className="grid gap-2">
              <Label htmlFor="smtp-port">Cổng SMTP</Label>
              <Input id="smtp-port" value={smtpPort} onChange={(e) => setSmtpPort(e.target.value)} />
            </div>
            <div className="grid gap-2">
              <Label htmlFor="smtp-user">Tài khoản Email</Label>
              <Input id="smtp-user" value={smtpUser} onChange={(e) => setSmtpUser(e.target.value)} />
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Sao lưu dữ liệu</CardTitle>
            <CardDescription>Thiết lập sao lưu cơ sở dữ liệu tự động.</CardDescription>
          </CardHeader>
          <CardContent className="grid gap-6">
            <div className="flex items-center justify-between gap-6 pt-2">
              <div className="grid gap-1">
                <Label htmlFor="auto-backup">Tự động sao lưu</Label>
                <p className="text-xs text-muted-foreground">
                  Sao lưu cơ sở dữ liệu mỗi ngày vào lúc 00:00.
                </p>
              </div>
              <Switch id="auto-backup" checked={autoBackup} onCheckedChange={setAutoBackup} />
            </div>
            <div className="grid gap-2">
              <Label htmlFor="backup-retention">Thời gian lưu trữ bản sao (ngày)</Label>
              <Input id="backup-retention" type="number" value={backupRetention} onChange={(e) => setBackupRetention(e.target.value)} />
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
