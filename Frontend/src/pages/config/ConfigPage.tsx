import { useState } from 'react'
import { Save, RefreshCw, Layers, FolderTree } from 'lucide-react'
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
import { CirculationPolicyList } from './components/CirculationPolicyList'
import { CirculationPolicyPreview } from './components/CirculationPolicyPreview'

export function ConfigPage() {
  const [activeTab, setActiveTab] = useState<'policies' | 'general'>('policies')

  // General settings state
  const [isSaving, setIsSaving] = useState(false)
  const [isRestoring, setIsRestoring] = useState(false)

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
      localStorage.setItem('config_maxDays', maxDays)
      localStorage.setItem('config_maxBooks', maxBooks)
      localStorage.setItem('config_finePerDay', finePerDay)
      localStorage.setItem('config_blockOverdue', blockOverdue.toString())
      setIsSaving(false)
      alert('Đã lưu cấu hình hệ thống thành công!')
    }, 600)
  }

  const handleRestore = () => {
    if (confirm('Bạn có chắc chắn muốn khôi phục tất cả cài đặt về mặc định?')) {
      setIsRestoring(true)
      setTimeout(() => {
        setMaxDays('14')
        setMaxBooks('5')
        setFinePerDay('5000')
        setBlockOverdue(true)
        localStorage.removeItem('config_maxDays')
        localStorage.removeItem('config_maxBooks')
        localStorage.removeItem('config_finePerDay')
        localStorage.removeItem('config_blockOverdue')
        setIsRestoring(false)
        alert('Đã khôi phục cài đặt mặc định!')
      }, 600)
    }
  }

  return (
    <div className="mx-auto w-full max-w-7xl px-5 py-10 md:px-12">
      {/* Header */}
      <div className="mb-8 flex flex-col justify-between gap-4 sm:flex-row sm:items-end">
        <div>
          <p className="mb-2 text-xs font-bold tracking-widest text-primary uppercase">
            quản lý hệ thống
          </p>
          <h1 className="text-3xl font-bold tracking-tight">Cấu hình & Chính sách lưu thông</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Quản lý chính sách mượn trả, hạn mức, gia hạn, giữ chỗ và quy định tiền phạt.
          </p>
        </div>

        {activeTab === 'general' && (
          <div className="flex gap-2">
            <Button variant="outline" onClick={handleRestore} disabled={isRestoring || isSaving}>
              <RefreshCw className={`mr-2 h-4 w-4 ${isRestoring ? 'animate-spin' : ''}`} />
              {isRestoring ? 'Đang khôi phục...' : 'Khôi phục mặc định'}
            </Button>
            <Button onClick={handleSave} disabled={isSaving || isRestoring}>
              <Save className="mr-2 h-4 w-4" />
              {isSaving ? 'Đang lưu...' : 'Lưu cấu hình'}
            </Button>
          </div>
        )}
      </div>

      {/* Tabs */}
      <div className="mb-6 flex gap-2 border-b">
        <button
          onClick={() => setActiveTab('policies')}
          className={`flex items-center gap-2 border-b-2 px-4 py-2.5 text-sm font-medium transition-colors ${
            activeTab === 'policies'
              ? 'border-primary text-primary'
              : 'border-transparent text-muted-foreground hover:text-foreground'
          }`}
        >
          <Layers className="h-4 w-4" />
          Chính sách lưu thông
        </button>

        <button
          onClick={() => setActiveTab('general')}
          className={`flex items-center gap-2 border-b-2 px-4 py-2.5 text-sm font-medium transition-colors ${
            activeTab === 'general'
              ? 'border-primary text-primary'
              : 'border-transparent text-muted-foreground hover:text-foreground'
          }`}
        >
          <FolderTree className="h-4 w-4" />
          Cấu hình chung
        </button>
      </div>

      {/* Tab Content */}
      {activeTab === 'policies' ? (
        <div className="space-y-6">
          <CirculationPolicyPreview />
          <CirculationPolicyList />
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-5 md:grid-cols-2">
          <Card>
            <CardHeader>
              <CardTitle>Quy định mượn/trả sách cơ bản</CardTitle>
              <CardDescription>Thiết lập thời hạn và số lượng tối đa mặc định.</CardDescription>
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
              <CardTitle>Quy định phạt vi phạm cơ bản</CardTitle>
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
      )}
    </div>
  )
}
