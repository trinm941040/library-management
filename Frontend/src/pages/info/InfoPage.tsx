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
import { useState } from 'react'

export function InfoPage() {
  const [isSaving, setIsSaving] = useState(false)

  // Controlled states loaded from localStorage
  const [libName, setLibName] = useState(() => localStorage.getItem('info_libName') || 'Northstar Library')
  const [libEmail, setLibEmail] = useState(() => localStorage.getItem('info_libEmail') || 'contact@northstarlibrary.com')
  const [libAddress, setLibAddress] = useState(() => localStorage.getItem('info_libAddress') || '123 Đường Điện Biên Phủ, Quận Bình Thạnh, TP.HCM')
  const [libPhone, setLibPhone] = useState(() => localStorage.getItem('info_libPhone') || '028 3812 3456')
  const [libHours, setLibHours] = useState(() => localStorage.getItem('info_libHours') || '07:30 - 20:30 (Thứ 2 - Thứ 7)')

  const handleSave = () => {
    setIsSaving(true)
    setTimeout(() => {
      // Save to localStorage
      localStorage.setItem('info_libName', libName)
      localStorage.setItem('info_libEmail', libEmail)
      localStorage.setItem('info_libAddress', libAddress)
      localStorage.setItem('info_libPhone', libPhone)
      localStorage.setItem('info_libHours', libHours)
      
      setIsSaving(false)
      alert('Đã cập nhật thông tin thư viện thành công!')
    }, 800)
  }
  return (
    <div className="mx-auto w-full max-w-5xl px-5 py-10 md:px-12">
      <div className="mb-7 flex items-start justify-between">
        <div>
          <p className="mb-2 text-xs font-bold uppercase tracking-widest text-primary">
            Quản lý hệ thống
          </p>
          <h1 className="text-3xl font-bold tracking-tight">Thông tin thư viện</h1>
          <p className="mt-2 text-sm text-muted-foreground">
            Quản lý các thông tin chung và liên hệ của thư viện.
          </p>
        </div>
        <div className="flex gap-3">
          <Button onClick={handleSave} disabled={isSaving}>
            <Save className={`mr-2 h-4 w-4 ${isSaving ? 'animate-pulse' : ''}`} /> 
            {isSaving ? 'Đang lưu...' : 'Lưu thông tin'}
          </Button>
        </div>
      </div>

      <div className="grid gap-5">
        <Card>
          <CardHeader>
            <CardTitle>Thông tin cơ bản</CardTitle>
            <CardDescription>Các thông tin sẽ hiển thị trên trang chủ và phiếu in.</CardDescription>
          </CardHeader>
          <CardContent className="grid gap-6">
            <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
              <div className="grid gap-2">
                <Label htmlFor="library-name">Tên thư viện</Label>
                <Input id="library-name" value={libName} onChange={(e) => setLibName(e.target.value)} />
              </div>
              <div className="grid gap-2">
                <Label htmlFor="library-email">Email liên hệ</Label>
                <Input id="library-email" type="email" value={libEmail} onChange={(e) => setLibEmail(e.target.value)} />
              </div>
              <div className="grid gap-2 md:col-span-2">
                <Label htmlFor="library-address">Địa chỉ</Label>
                <Input id="library-address" value={libAddress} onChange={(e) => setLibAddress(e.target.value)} />
              </div>
              <div className="grid gap-2">
                <Label htmlFor="library-phone">Số điện thoại</Label>
                <Input id="library-phone" value={libPhone} onChange={(e) => setLibPhone(e.target.value)} />
              </div>
              <div className="grid gap-2">
                <Label htmlFor="library-hours">Giờ hoạt động</Label>
                <Input id="library-hours" value={libHours} onChange={(e) => setLibHours(e.target.value)} />
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
