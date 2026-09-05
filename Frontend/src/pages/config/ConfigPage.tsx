import { Save, RefreshCw } from 'lucide-react'
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

export function ConfigPage() {
  const [isSaving, setIsSaving] = useState(false)
  const [isRestoring, setIsRestoring] = useState(false)

  // Load from localStorage or use defaults
  const [maxDays, setMaxDays] = useState(() => localStorage.getItem('config_maxDays') || '14')
  const [maxBooks, setMaxBooks] = useState(() => localStorage.getItem('config_maxBooks') || '5')
  const [finePerDay, setFinePerDay] = useState(() => localStorage.getItem('config_finePerDay') || '5000')
  const [blockOverdue, setBlockOverdue] = useState(() => {
    const saved = localStorage.getItem('config_blockOverdue')
    return saved !== null ? saved === 'true' : true
  })

  const handleSave = () => {
    setIsSaving(true)
    setTimeout(() => {
      // Save to localStorage
      localStorage.setItem('config_maxDays', maxDays)
      localStorage.setItem('config_maxBooks', maxBooks)
      localStorage.setItem('config_finePerDay', finePerDay)
      localStorage.setItem('config_blockOverdue', blockOverdue.toString())
      
      setIsSaving(false)
      alert('Đã lưu cấu hình hệ thống thành công!')
    }, 800)
  }

  const handleRestore = () => {
    if (confirm('Bạn có chắc chắn muốn khôi phục tất cả cài đặt về mặc định?')) {
      setIsRestoring(true)
      setTimeout(() => {
        // Reset all states to default values
        setMaxDays('14')
        setMaxBooks('5')
        setFinePerDay('5000')
        setBlockOverdue(true)
        
        // Remove from localStorage
        localStorage.removeItem('config_maxDays')
        localStorage.removeItem('config_maxBooks')
        localStorage.removeItem('config_finePerDay')
        localStorage.removeItem('config_blockOverdue')
        
        setIsRestoring(false)
        alert('Đã khôi phục cài đặt mặc định!')
      }, 800)
    }
  }

  return (
    <div className="mx-auto w-full max-w-5xl px-5 py-10 md:px-12">
      <div className="mb-7 flex items-start justify-between">
        <div>
          <p className="mb-2 text-xs font-bold uppercase tracking-widest text-primary">
            Quản lý hệ thống
          </p>
          <h1 className="text-3xl font-bold tracking-tight">Cấu hình</h1>
          <p className="mt-2 text-sm text-muted-foreground">
            Thiết lập các thông số cơ bản cho quá trình vận hành thư viện.
          </p>
        </div>
        <div className="flex gap-3">
          <Button variant="outline" onClick={handleRestore} disabled={isRestoring || isSaving}>
            <RefreshCw className={`mr-2 h-4 w-4 ${isRestoring ? 'animate-spin' : ''}`} /> 
            {isRestoring ? 'Đang khôi phục...' : 'Khôi phục mặc định'}
          </Button>
          <Button onClick={handleSave} disabled={isSaving || isRestoring}>
            <Save className="mr-2 h-4 w-4" /> 
            {isSaving ? 'Đang lưu...' : 'Lưu cấu hình'}
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 gap-5 md:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Quy định mượn/trả sách</CardTitle>
            <CardDescription>Thiết lập thời hạn và số lượng tối đa.</CardDescription>
          </CardHeader>
          <CardContent className="grid gap-6">
            <div className="grid gap-2">
              <Label htmlFor="max-days">Số ngày mượn tối đa</Label>
              <Input 
                id="max-days" 
                type="number" 
                value={maxDays} 
                onChange={(e) => setMaxDays(e.target.value)} 
              />
              <p className="text-xs text-muted-foreground">Số ngày tối đa độc giả được giữ sách.</p>
            </div>
            <div className="grid gap-2">
              <Label htmlFor="max-books">Số sách mượn tối đa</Label>
              <Input 
                id="max-books" 
                type="number" 
                value={maxBooks} 
                onChange={(e) => setMaxBooks(e.target.value)} 
              />
              <p className="text-xs text-muted-foreground">Số cuốn sách tối đa một độc giả được mượn cùng lúc.</p>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Quy định phạt vi phạm</CardTitle>
            <CardDescription>Thiết lập phí phạt trễ hạn hoặc làm mất sách.</CardDescription>
          </CardHeader>
          <CardContent className="grid gap-6">
            <div className="grid gap-2">
              <Label htmlFor="fine-per-day">Phí phạt quá hạn (VNĐ/ngày)</Label>
              <Input 
                id="fine-per-day" 
                type="number" 
                value={finePerDay} 
                onChange={(e) => setFinePerDay(e.target.value)} 
              />
              <p className="text-xs text-muted-foreground">Số tiền phạt cho mỗi ngày trả sách trễ.</p>
            </div>
            <div className="flex items-center justify-between gap-6 pt-2">
              <div className="grid gap-1">
                <Label htmlFor="block-overdue">Khóa tài khoản tự động</Label>
                <p className="text-xs text-muted-foreground">
                  Tự động khóa mượn sách nếu độc giả có sách quá hạn.
                </p>
              </div>
              <Switch 
                id="block-overdue" 
                checked={blockOverdue} 
                onCheckedChange={setBlockOverdue} 
              />
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
