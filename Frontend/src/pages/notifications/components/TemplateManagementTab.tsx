import React, { useState } from 'react'
import type {
  NotificationTemplate,
  CreateNotificationTemplatePayload,
  UpdateNotificationTemplatePayload,
  NotificationChannel,
} from '../notifications-api'
import {
  createTemplate,
  updateTemplate,
  deleteTemplate,
} from '../notifications-api'
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from '@/common/components/ui/card'
import { Button } from '@/common/components/ui/button'
import { Input } from '@/common/components/ui/input'
import { Label } from '@/common/components/ui/label'
import { Badge } from '@/common/components/ui/badge'
import { Switch } from '@/common/components/ui/switch'
import { Textarea } from '@/common/components/ui/textarea'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/common/components/ui/select'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '@/common/components/ui/dialog'
import { Plus, Edit2, Trash2, Mail, Bell, CheckCircle2, XCircle, Info } from 'lucide-react'

type Props = {
  templates: NotificationTemplate[]
  loading: boolean
  onReload: () => Promise<void>
  isManager: boolean
}

const VARIABLE_LABELS: Record<string, string> = {
  title: 'Tên tài liệu',
  book_title: 'Tên tài liệu',
  name: 'Tên người nhận',
  borrower_name: 'Tên người mượn',
  recipient_name: 'Tên người nhận',
  member_code: 'Mã độc giả',
  due_date: 'Hạn trả sách',
  expiry_date: 'Hạn nhận tài liệu',
  amount: 'Số tiền phạt',
  reason: 'Lý do',
  branch_name: 'Chi nhánh',
  hold_until: 'Thời hạn giữ',
}

function formatTemplateDisplay(text?: string | null): string {
  if (!text) return ''
  return text.replace(/\{\{([a-zA-Z0-9_-]+)\}\}/g, (_, varName) => {
    const key = varName.toLowerCase().trim()
    const label = VARIABLE_LABELS[key] || varName
    return `[${label}]`
  })
}

function getChannelLabel(ch: NotificationChannel) {
  switch (ch) {
    case 'Email':
      return 'Thư điện tử'
    case 'InApp':
    default:
      return 'Nội bộ'
  }
}

export const TemplateManagementTab: React.FC<Props> = ({
  templates,
  loading,
  onReload,
  isManager,
}) => {
  const [editingTemplate, setEditingTemplate] = useState<NotificationTemplate | null>(null)
  const [isCreating, setIsCreating] = useState(false)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // Form states
  const [code, setCode] = useState('')
  const [name, setName] = useState('')
  const [channel, setChannel] = useState<NotificationChannel>('InApp')
  const [subjectTemplate, setSubjectTemplate] = useState('')
  const [bodyTemplate, setBodyTemplate] = useState('')
  const [allowedVariables, setAllowedVariables] = useState('')
  const [isActive, setIsActive] = useState(true)

  const openCreateModal = () => {
    setCode('')
    setName('')
    setChannel('InApp')
    setSubjectTemplate('')
    setBodyTemplate('')
    setAllowedVariables('borrower_name, book_title, due_date')
    setIsActive(true)
    setError(null)
    setIsCreating(true)
  }

  const openEditModal = (t: NotificationTemplate) => {
    setEditingTemplate(t)
    setCode(t.code)
    setName(t.name)
    setChannel(t.channel)
    setSubjectTemplate(t.subjectTemplate || '')
    setBodyTemplate(t.bodyTemplate)
    setAllowedVariables(t.allowedVariables || '')
    setIsActive(t.isActive)
    setError(null)
  }

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!name.trim() || !bodyTemplate.trim()) {
      setError('Tên mẫu và nội dung thông báo là bắt buộc.')
      return
    }

    setSaving(true)
    setError(null)

    try {
      if (isCreating) {
        if (!code.trim()) {
          setError('Mã mẫu thông báo là bắt buộc.')
          setSaving(false)
          return
        }
        const payload: CreateNotificationTemplatePayload = {
          code: code.trim().toUpperCase(),
          name: name.trim(),
          channel,
          subjectTemplate: subjectTemplate.trim(),
          bodyTemplate: bodyTemplate.trim(),
          allowedVariables: allowedVariables.trim() || undefined,
          isActive,
        }
        await createTemplate(payload)
      } else if (editingTemplate) {
        const payload: UpdateNotificationTemplatePayload = {
          name: name.trim(),
          channel,
          subjectTemplate: subjectTemplate.trim(),
          bodyTemplate: bodyTemplate.trim(),
          allowedVariables: allowedVariables.trim() || undefined,
          isActive,
          concurrencyToken: editingTemplate.concurrencyToken,
        }
        await updateTemplate(editingTemplate.id, payload)
      }

      await onReload()
      setIsCreating(false)
      setEditingTemplate(null)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Không thể lưu mẫu thông báo.')
    } finally {
      setSaving(false)
    }
  }

  const handleDelete = async (t: NotificationTemplate) => {
    if (!window.confirm(`Bạn có chắc chắn muốn xóa mẫu thông báo '${t.name}' (${t.code})?`)) {
      return
    }

    try {
      await deleteTemplate(t.id)
      await onReload()
    } catch (err) {
      alert(err instanceof Error ? err.message : 'Không thể xóa mẫu thông báo.')
    }
  }

  const getChannelIcon = (ch: NotificationChannel) => {
    switch (ch) {
      case 'Email':
        return <Mail className="size-4 text-sky-500" />
      case 'InApp':
      default:
        return <Bell className="size-4 text-amber-500" />
    }
  }

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
          <div>
            <CardTitle className="text-base font-semibold">Danh mục mẫu thông báo vận hành</CardTitle>
            <CardDescription>
              Định nghĩa cú pháp nội dung, kênh truyền gửi và danh sách biến hợp lệ cho từng sự kiện.
            </CardDescription>
          </div>
          {isManager && (
            <Button onClick={openCreateModal} className="w-full shrink-0 gap-2 sm:w-auto">
              <Plus className="size-4" /> Thêm mẫu mới
            </Button>
          )}
        </CardHeader>
      </Card>

      {loading ? (
        <div className="text-center py-12 text-sm text-muted-foreground">Đang tải danh sách mẫu...</div>
      ) : templates.length === 0 ? (
        <div className="text-center py-12 text-sm text-muted-foreground rounded-xl border border-dashed p-8">
          Chưa có mẫu thông báo nào. Bấm &quot;Thêm mẫu mới&quot; để tạo.
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {templates.map((t) => (
            <Card key={t.id} className="flex flex-col justify-between">
              <CardHeader className="pb-3 space-y-2.5">
                <div className="flex items-center justify-between gap-2">
                  <div className="flex items-center gap-2">
                    <div className="size-8 rounded-lg bg-muted flex items-center justify-center shrink-0">
                      {getChannelIcon(t.channel)}
                    </div>
                    <div>
                      <Badge variant="outline" className="font-mono text-xs">
                        {t.code}
                      </Badge>
                      <span className="text-xs text-muted-foreground ml-2">
                        {getChannelLabel(t.channel)}
                      </span>
                    </div>
                  </div>
                  {t.isActive ? (
                    <Badge variant="default" className="gap-1 bg-emerald-600 hover:bg-emerald-700 text-xs">
                      <CheckCircle2 className="size-3" /> Hoạt động
                    </Badge>
                  ) : (
                    <Badge variant="secondary" className="gap-1 text-xs">
                      <XCircle className="size-3" /> Tạm dừng
                    </Badge>
                  )}
                </div>

                <div>
                  <CardTitle className="text-sm font-semibold">{t.name}</CardTitle>
                  {t.subjectTemplate && (
                    <p className="text-xs font-medium text-foreground/80 mt-1 truncate" title={t.subjectTemplate}>
                      Tiêu đề: <span className="font-normal italic">{formatTemplateDisplay(t.subjectTemplate)}</span>
                    </p>
                  )}
                </div>
              </CardHeader>

              <CardContent className="pb-3 space-y-2.5">
                <div className="text-xs text-muted-foreground bg-muted/40 p-2.5 rounded-md border whitespace-pre-wrap leading-relaxed">
                  {formatTemplateDisplay(t.bodyTemplate)}
                </div>

                {t.allowedVariables && (
                  <div className="text-xs text-muted-foreground">
                    <span className="font-medium text-foreground">Biến:</span>{' '}
                    <code className="text-primary font-mono text-[11px]">{t.allowedVariables}</code>
                  </div>
                )}
              </CardContent>

              {isManager && (
                <CardFooter className="pt-3 border-t flex justify-end gap-1.5">
                  <Button variant="ghost" size="sm" className="gap-1 text-xs" onClick={() => openEditModal(t)}>
                    <Edit2 className="size-3.5" /> Sửa
                  </Button>
                  <Button
                    variant="ghost"
                    size="sm"
                    className="gap-1 text-xs text-destructive hover:bg-destructive/10"
                    onClick={() => handleDelete(t)}
                  >
                    <Trash2 className="size-3.5" /> Xóa
                  </Button>
                </CardFooter>
              )}
            </Card>
          ))}
        </div>
      )}

      {/* Dialog Create / Edit */}
      <Dialog
        open={isCreating || Boolean(editingTemplate)}
        onOpenChange={(open) => {
          if (!open) {
            setIsCreating(false)
            setEditingTemplate(null)
          }
        }}
      >
        <DialogContent className="sm:max-w-xl">
          <DialogHeader>
            <DialogTitle>
              {isCreating ? 'Thêm mẫu thông báo mới' : `Chỉnh sửa mẫu: ${editingTemplate?.code}`}
            </DialogTitle>
            <DialogDescription>
              Cấu hình thông số gửi, tiêu đề và cú pháp nội dung kèm biến thay thế
            </DialogDescription>
          </DialogHeader>

          {error && (
            <div className="p-3 text-xs bg-destructive/10 text-destructive rounded-lg border border-destructive/20">
              {error}
            </div>
          )}

          <form onSubmit={handleSave} className="space-y-4">
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <Label htmlFor="templateCode">Mã mẫu thông báo *</Label>
                {isCreating ? (
                  <Input
                    id="templateCode"
                    placeholder="VD: RESERVATION_READY"
                    value={code}
                    onChange={(e) => setCode(e.target.value)}
                    className="uppercase font-mono text-xs"
                    required
                  />
                ) : (
                  <Input
                    id="templateCode"
                    value={code}
                    disabled
                    className="font-mono text-xs bg-muted"
                  />
                )}
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="templateChannel">Kênh gửi</Label>
                <Select value={channel} onValueChange={(val) => setChannel(val as NotificationChannel)}>
                  <SelectTrigger id="templateChannel" className="w-full">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="InApp">Nội bộ</SelectItem>
                    <SelectItem value="Email">Email</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="templateName">Tên mẫu thông báo *</Label>
              <Input
                id="templateName"
                placeholder="VD: Thông báo sách đặt trước đã sẵn sàng"
                value={name}
                onChange={(e) => setName(e.target.value)}
                required
              />
            </div>

            <div className="space-y-1.5">
                <Label htmlFor="templateSubject">Tiêu đề mẫu</Label>
                <Input
                  id="templateSubject"
                  placeholder="VD: [Thư viện] Sách {{book_title}} đã sẵn sàng nhận"
                  value={subjectTemplate}
                  onChange={(e) => setSubjectTemplate(e.target.value)}
                />
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="templateBody">Nội dung mẫu *</Label>
              <Textarea
                id="templateBody"
                rows={4}
                placeholder="VD: Chào {{borrower_name}}, sách {{book_title}} bạn đặt trước đã sẵn sàng nhận tại {{branch_name}}."
                value={bodyTemplate}
                onChange={(e) => setBodyTemplate(e.target.value)}
                required
              />
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="templateVars">
                Danh sách biến cho phép (phân cách bằng dấu phẩy)
              </Label>
              <Input
                id="templateVars"
                placeholder="borrower_name, book_title, branch_name, hold_until"
                value={allowedVariables}
                onChange={(e) => setAllowedVariables(e.target.value)}
                className="font-mono text-xs"
              />
              <div className="flex items-center gap-1.5 text-xs text-muted-foreground mt-1">
                <Info className="size-3.5 text-primary shrink-0" />
                <span>Sử dụng cú pháp <code className="text-primary font-mono font-medium">{'{{ten_bien}}'}</code> trong tiêu đề và nội dung.</span>
              </div>
            </div>

            <div className="flex items-center gap-3 pt-2">
              <Switch
                id="template-active"
                checked={isActive}
                onCheckedChange={setIsActive}
              />
              <Label htmlFor="template-active" className="cursor-pointer">
                Kích hoạt mẫu này để cho phép gửi
              </Label>
            </div>

            <DialogFooter className="pt-3">
              <Button
                variant="outline"
                type="button"
                onClick={() => {
                  setIsCreating(false)
                  setEditingTemplate(null)
                }}
              >
                Hủy
              </Button>
              <Button type="submit" disabled={saving}>
                {saving ? 'Đang lưu...' : isCreating ? 'Tạo mẫu' : 'Cập nhật'}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  )
}

export default TemplateManagementTab
