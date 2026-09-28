import React, { useState, useEffect, useMemo, useCallback } from 'react'
import type {
  NotificationTemplate,
  NotificationRecipient,
  RecipientType,
  NotificationPreviewResult,
} from '../notifications-api'
import {
  previewNotification,
  sendBulkNotification,
  sendNotification,
  searchRecipients,
} from '../notifications-api'
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from '@/common/components/ui/card'
import { Button } from '@/common/components/ui/button'
import { Input } from '@/common/components/ui/input'
import { Label } from '@/common/components/ui/label'
import { Badge } from '@/common/components/ui/badge'
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '@/common/components/ui/dialog'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/common/components/ui/select'
import {
  Search,
  Send,
  Eye,
  CheckCircle2,
  User,
  UserCheck,
  AlertCircle,
  Mail,
  Bell,
} from 'lucide-react'

type Props = {
  templates: NotificationTemplate[]
  loading?: boolean
  onSent?: () => void
  onSentSuccess?: () => void
}

const VARIABLE_CONFIG: Record<
  string,
  { label: string; placeholder: string; hint?: string }
> = {
  title: { label: 'Tên tài liệu / sách', placeholder: 'Ví dụ: Lập trình C# nâng cao' },
  book_title: { label: 'Tên tài liệu / sách', placeholder: 'Ví dụ: Lập trình C# nâng cao' },
  name: { label: 'Tên người nhận', placeholder: 'Ví dụ: Nguyễn Văn A' },
  borrower_name: { label: 'Tên người mượn', placeholder: 'Ví dụ: Nguyễn Văn A' },
  recipient_name: { label: 'Tên người nhận', placeholder: 'Ví dụ: Nguyễn Văn A' },
  member_code: { label: 'Mã độc giả', placeholder: 'Ví dụ: DG-2026-001' },
  due_date: { label: 'Hạn trả sách', placeholder: 'Ví dụ: 25/09/2026' },
  expiry_date: { label: 'Hạn nhận tài liệu', placeholder: 'Ví dụ: 22/09/2026' },
  amount: { label: 'Số tiền phí / phạt (VNĐ)', placeholder: 'Ví dụ: 50.000' },
  reason: { label: 'Lý do / Mô tả', placeholder: 'Ví dụ: Trả sách trễ hạn' },
  branch_name: { label: 'Chi nhánh / Điểm phục vụ', placeholder: 'Ví dụ: Thư viện cơ sở 1' },
  hold_until: { label: 'Thời hạn giữ sách', placeholder: 'Ví dụ: 28/09/2026' },
}

function getChannelLabel(channel?: string) {
  switch (channel) {
    case 'Email':
      return 'Thư điện tử'
    case 'InApp':
    default:
      return 'Nội bộ'
  }
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

function getVariableMeta(varName: string) {
  const normalized = varName.toLowerCase().trim()
  if (VARIABLE_CONFIG[normalized]) {
    return VARIABLE_CONFIG[normalized]
  }
  const formatted = varName
    .replace(/[_-]/g, ' ')
    .replace(/\b\w/g, (c) => c.toUpperCase())
  return {
    label: formatted,
    placeholder: `Nhập giá trị cho ${varName}...`,
  }
}



export const SendNotificationTab: React.FC<Props> = ({
  templates,
  loading,
  onSent,
  onSentSuccess,
}) => {
  const activeTemplates = useMemo(() => templates.filter((t) => t.isActive), [templates])

  const [selectedTemplateCode, setSelectedTemplateCode] = useState<string>('')
  const [recipientType, setRecipientType] = useState<RecipientType>('Member')
  const [searchKeyword, setSearchKeyword] = useState('')
  const [recipients, setRecipients] = useState<NotificationRecipient[]>([])
  const [searching, setSearching] = useState(false)
  const [selectedRecipient, setSelectedRecipient] = useState<NotificationRecipient | null>(null)
  const [selectedRecipients, setSelectedRecipients] = useState<NotificationRecipient[]>([])
  const [deliveryScope, setDeliveryScope] = useState<'single' | 'multiple' | 'all'>('single')
  const [customDestination, setCustomDestination] = useState('')

  // Variable inputs
  const [variables, setVariables] = useState<Record<string, string>>({})
  const [preview, setPreview] = useState<NotificationPreviewResult | null>(null)
  const [previewLoading, setPreviewLoading] = useState(false)
  const [previewOpen, setPreviewOpen] = useState(false)

  // Send state
  const [sending, setSending] = useState(false)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  // Set default template
  useEffect(() => {
    if (activeTemplates.length > 0 && !selectedTemplateCode) {
      setSelectedTemplateCode(activeTemplates[0].code)
    }
  }, [activeTemplates, selectedTemplateCode])

  const currentTemplate = useMemo(() => {
    return activeTemplates.find((t) => t.code === selectedTemplateCode) || activeTemplates[0]
  }, [activeTemplates, selectedTemplateCode])

  useEffect(() => {
    if (currentTemplate?.channel !== 'InApp') {
      setDeliveryScope('single')
      return
    }
    setRecipientType('Staff')
    setSelectedRecipient(null)
    setSelectedRecipients([])
    setCustomDestination('')
  }, [currentTemplate?.channel])

  // Extract variables from template body & subject
  const detectedVariables = useMemo(() => {
    if (!currentTemplate) return []
    const combined = `${currentTemplate.subjectTemplate || ''} ${currentTemplate.bodyTemplate}`
    const matches = combined.matchAll(/\{\{([a-zA-Z0-9_-]+)\}\}/g)
    const vars = new Set<string>()
    for (const match of matches) {
      vars.add(match[1].trim())
    }
    return Array.from(vars)
  }, [currentTemplate])

  // Search recipients when type or keyword changes
  const handleSearchRecipients = useCallback(async () => {
    setSearching(true)
    try {
      const list = await searchRecipients(recipientType, searchKeyword)
      setRecipients(list)
    } catch {
      setRecipients([])
    } finally {
      setSearching(false)
    }
  }, [recipientType, searchKeyword])

  useEffect(() => {
    void handleSearchRecipients()
  }, [handleSearchRecipients])

  // Select recipient & autofill relevant variables
  const handleSelectRecipient = (r: NotificationRecipient) => {
    if (deliveryScope === 'multiple') {
      setSelectedRecipients((current) =>
        current.some((item) => item.id === r.id)
          ? current.filter((item) => item.id !== r.id)
          : [...current, r],
      )
      return
    }
    setSelectedRecipient(r)
    setCustomDestination(
      currentTemplate?.channel === 'Email' ? r.email || '' : r.id,
    )

    setVariables((prev) => {
      const next = { ...prev }
      if (detectedVariables.includes('borrower_name') || detectedVariables.includes('name')) {
        next.borrower_name = r.name
        next.name = r.name
      }
      if (detectedVariables.includes('member_code')) {
        next.member_code = r.code
      }
      return next
    })
  }

  const handleVariableChange = (key: string, value: string) => {
    setVariables((prev) => ({ ...prev, [key]: value }))
  }

  const handlePreview = async () => {
    if (!currentTemplate) return
    setPreviewLoading(true)
    setErrorMessage(null)
    try {
      const res = await previewNotification({
        templateCode: currentTemplate.code,
        variables,
      })
      setPreview(res)
      setPreviewOpen(true)
    } catch (err) {
      setErrorMessage(err instanceof Error ? err.message : 'Không thể tạo xem trước.')
      setPreview(null)
    } finally {
      setPreviewLoading(false)
    }
  }

  const handleSend = async () => {
    if (!currentTemplate) return
    if (deliveryScope === 'single' && !selectedRecipient) {
      setErrorMessage('Vui lòng chọn đối tượng nhận thông báo.')
      return
    }
    if (deliveryScope === 'multiple' && selectedRecipients.length === 0) {
      setErrorMessage('Vui lòng chọn ít nhất một nhân viên nhận thông báo.')
      return
    }

    setSending(true)
    setErrorMessage(null)
    setSuccessMessage(null)

    try {
      if (currentTemplate.channel === 'InApp' && deliveryScope !== 'single') {
        const result = await sendBulkNotification({
          templateCode: currentTemplate.code,
          recipientIds: deliveryScope === 'multiple' ? selectedRecipients.map((item) => item.id) : [],
          allStaff: deliveryScope === 'all',
          variables,
          eventCode: currentTemplate.code,
          idempotencyKey: `manual:${crypto.randomUUID()}`,
        })
        setSuccessMessage(`Đã gửi thông báo thành công đến ${result.recipientCount} nhân viên.`)
      } else {
        await sendNotification({
          templateCode: currentTemplate.code,
          recipientType,
          recipientId: selectedRecipient!.id,
          destination: customDestination.trim() || undefined,
          variables,
        })
        setSuccessMessage(`Đã gửi thông báo thành công đến '${selectedRecipient!.name}'.`)
      }
      if (onSent) {
        onSent()
      } else if (onSentSuccess) {
        onSentSuccess()
      }
    } catch (err) {
      setErrorMessage(err instanceof Error ? err.message : 'Không thể gửi thông báo.')
    } finally {
      setSending(false)
    }
  }

  const renderChannelIcon = (channel?: string) => {
    switch (channel) {
      case 'Email':
        return <Mail className="size-4 text-sky-500" />
      case 'InApp':
      default:
        return <Bell className="size-4 text-amber-500" />
    }
  }

  return (
    <div className="space-y-6">
      {successMessage && (
        <div className="flex items-center justify-between rounded-lg border border-emerald-500/20 bg-emerald-500/10 p-4 text-sm text-emerald-700 dark:text-emerald-300">
          <div className="flex items-center gap-2">
            <CheckCircle2 className="size-5 shrink-0" />
            <span className="font-medium">{successMessage}</span>
          </div>
          <Button variant="ghost" size="sm" onClick={() => setSuccessMessage(null)}>
            Đóng
          </Button>
        </div>
      )}

      {errorMessage && (
        <div className="flex items-center justify-between rounded-lg border border-destructive/20 bg-destructive/10 p-4 text-sm text-destructive">
          <div className="flex items-center gap-2">
            <AlertCircle className="size-5 shrink-0" />
            <span className="font-medium">{errorMessage}</span>
          </div>
          <Button variant="ghost" size="sm" onClick={() => setErrorMessage(null)}>
            Đóng
          </Button>
        </div>
      )}

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-12 lg:items-start">
        {/* Left Column: Form Configuration */}
        <div className="space-y-6 lg:col-span-7">
          {/* Step 1: Select Template */}
          <Card>
            <CardHeader className="pb-4">
              <div className="flex items-center justify-between">
                <div>
                  <CardTitle className="text-base font-semibold">1. Chọn mẫu thông báo</CardTitle>
                  <CardDescription>Chọn sự kiện nghiệp vụ và mẫu nội dung đã cấu hình</CardDescription>
                </div>
                {currentTemplate && (
                  <Badge variant="outline" className="gap-1.5 font-normal">
                    {renderChannelIcon(currentTemplate.channel)}
                    <span>Kênh: <strong>{getChannelLabel(currentTemplate.channel)}</strong></span>
                  </Badge>
                )}
              </div>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="templateSelect">Mẫu thông báo áp dụng</Label>
                <Select
                  value={selectedTemplateCode}
                  onValueChange={(val) => {
                    setSelectedTemplateCode(val)
                    setPreview(null)
                    setPreviewOpen(false)
                  }}
                >
                  <SelectTrigger id="templateSelect" className="w-full">
                    <SelectValue placeholder="Chọn mẫu thông báo..." />
                  </SelectTrigger>
                  <SelectContent>
                    {activeTemplates.map((t) => (
                      <SelectItem key={t.code} value={t.code}>
                        [{getChannelLabel(t.channel)}] {t.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              {currentTemplate && (
                <div className="rounded-md border bg-muted/40 p-3.5 text-sm space-y-2">
                  {currentTemplate.subjectTemplate && (
                    <div className="flex flex-col gap-0.5 sm:flex-row sm:gap-2">
                      <span className="font-medium text-muted-foreground shrink-0">Tiêu đề mẫu:</span>
                      <span className="font-semibold">{formatTemplateDisplay(currentTemplate.subjectTemplate)}</span>
                    </div>
                  )}
                  <div className="flex flex-col gap-0.5 sm:flex-row sm:gap-2">
                    <span className="font-medium text-muted-foreground shrink-0">Nội dung mẫu:</span>
                    <span className="italic text-muted-foreground leading-relaxed">
                      {formatTemplateDisplay(currentTemplate.bodyTemplate)}
                    </span>
                  </div>
                </div>
              )}
            </CardContent>
          </Card>

          {/* Step 2: Select Recipient */}
          <Card>
            <CardHeader className="pb-4">
              <div className="flex items-center justify-between">
                <div>
                  <CardTitle className="text-base font-semibold">2. Chọn đối tượng nhận</CardTitle>
                  <CardDescription>Tra cứu độc giả hoặc nhân viên để gửi thông báo</CardDescription>
                </div>
                <span className="text-xs text-muted-foreground">
                  {recipients.length} kết quả
                </span>
              </div>
            </CardHeader>
            <CardContent className="space-y-4">
              {currentTemplate?.channel === 'InApp' && (
                <div className="grid gap-2 sm:grid-cols-3" role="group" aria-label="Phạm vi người nhận">
                  {([
                    ['single', 'Một nhân viên'],
                    ['multiple', 'Nhiều nhân viên'],
                    ['all', 'Toàn bộ nhân viên'],
                  ] as const).map(([value, label]) => (
                    <Button key={value} type="button" size="sm"
                      variant={deliveryScope === value ? 'default' : 'outline'}
                      onClick={() => {
                        setDeliveryScope(value)
                        setSelectedRecipient(null)
                        setSelectedRecipients([])
                        setCustomDestination('')
                      }}>
                      {label}
                    </Button>
                  ))}
                </div>
              )}

              {deliveryScope !== 'all' ? <div className="space-y-4">
              <div className="flex items-center gap-2">
                <Button
                  type="button"
                  variant={recipientType === 'Member' ? 'default' : 'outline'}
                  size="sm"
                  onClick={() => {
                    setRecipientType('Member')
                    setSelectedRecipient(null)
                  }}
                  disabled={currentTemplate?.channel === 'InApp'}
                  className="gap-2"
                >
                  <User className="size-4" />
                  Độc giả
                </Button>
                <Button
                  type="button"
                  variant={recipientType === 'Staff' ? 'default' : 'outline'}
                  size="sm"
                  onClick={() => {
                    setRecipientType('Staff')
                    setSelectedRecipient(null)
                  }}
                  className="gap-2"
                >
                  <UserCheck className="size-4" />
                  Nhân viên
                </Button>
              </div>

              <div className="relative">
                <Search className="absolute left-3 top-2.5 size-4 text-muted-foreground" />
                <Input
                  value={searchKeyword}
                  onChange={(e) => setSearchKeyword(e.target.value)}
                  placeholder={
                    recipientType === 'Member'
                      ? 'Tìm theo tên, mã thẻ độc giả, email...'
                      : 'Tìm theo tên nhân viên, mã nhân viên, email...'
                  }
                  className="pl-9"
                />
              </div>

              <div className="max-h-52 overflow-y-auto space-y-1.5 rounded-md border bg-muted/20 p-2">
                {searching ? (
                  <div className="py-6 text-center text-sm text-muted-foreground">
                    Đang tìm kiếm đối tượng...
                  </div>
                ) : recipients.length === 0 ? (
                  <div className="py-6 text-center text-sm text-muted-foreground">
                    Không tìm thấy đối tượng phù hợp.
                  </div>
                ) : (
                  recipients.map((r) => {
                    const isSelected = deliveryScope === 'multiple'
                      ? selectedRecipients.some((item) => item.id === r.id)
                      : selectedRecipient?.id === r.id
                    return (
                      <button
                        type="button"
                        key={r.id}
                        onClick={() => handleSelectRecipient(r)}
                        disabled={loading}
                        className={`flex w-full cursor-pointer items-center justify-between rounded-md border p-2.5 text-left text-sm transition-colors ${
                          isSelected
                            ? 'border-primary bg-primary/10 shadow-xs'
                            : 'border-transparent hover:bg-accent/60'
                        }`}
                      >
                        <div className="flex items-center gap-2.5 min-w-0">
                          <div
                            className={`flex size-8 shrink-0 items-center justify-center rounded-full text-xs font-semibold ${
                              isSelected
                                ? 'bg-primary text-primary-foreground'
                                : 'bg-muted text-muted-foreground'
                            }`}
                          >
                            {r.name.charAt(0).toUpperCase()}
                          </div>
                          <div className="min-w-0">
                            <p className="font-medium truncate">{r.name}</p>
                            <p className="text-xs text-muted-foreground truncate">
                              Mã: <span className="font-mono font-medium">{r.code}</span>
                              {r.email ? ` • ${r.email}` : ''}
                              {r.phoneNumber ? ` • ${r.phoneNumber}` : ''}
                            </p>
                          </div>
                        </div>

                        {isSelected && (
                          <CheckCircle2 className="size-4 shrink-0 text-primary ml-2" />
                        )}
                      </button>
                    )
                  })
                )}
              </div>

              {selectedRecipient && (
                <div className="rounded-md border bg-muted/30 p-3.5 space-y-2">
                  <div className="flex items-center justify-between text-xs">
                    <Label htmlFor="customDestination" className="font-medium">
                      Địa chỉ nhận ({currentTemplate?.channel === 'Email' ? 'Thư điện tử' : 'Tài khoản nội bộ'}):
                    </Label>
                    <span className="text-xs text-muted-foreground">Tự động điền</span>
                  </div>
                  <Input
                    id="customDestination"
                    value={customDestination}
                    onChange={(e) => setCustomDestination(e.target.value)}
                    placeholder="Nhập địa chỉ đích..."
                    className="font-mono text-sm"
                  />
                </div>
              )}
              {deliveryScope === 'multiple' && selectedRecipients.length > 0 && (
                <p className="text-sm font-medium text-primary">Đã chọn {selectedRecipients.length} nhân viên.</p>
              )}
              </div> : (
                <div className="rounded-md border border-primary/30 bg-primary/5 p-4 text-sm">
                  Thông báo sẽ được tạo riêng cho mọi tài khoản nhân viên đang hoạt động trong phạm vi bạn được phép quản lý.
                </div>
              )}
            </CardContent>
          </Card>

          {/* Step 3: Fill Variables */}
          <Card>
            <CardHeader className="pb-4">
              <div className="flex items-center justify-between">
                <div>
                  <CardTitle className="text-base font-semibold">3. Điền giá trị thông tin</CardTitle>
                  <CardDescription>Cung cấp dữ liệu thực tế để cá nhân hóa nội dung</CardDescription>
                </div>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={handlePreview}
                  disabled={previewLoading || !currentTemplate}
                  className="gap-2"
                >
                  <Eye className="size-4" />
                  {previewLoading ? 'Đang tạo xem trước...' : 'Xem trước'}
                </Button>
              </div>
            </CardHeader>
            <CardContent>
              {detectedVariables.length === 0 ? (
                <p className="py-2 text-sm italic text-muted-foreground">
                  Mẫu thông báo này là nội dung cố định, không yêu cầu điền tham số động.
                </p>
              ) : (
                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                  {detectedVariables.map((v) => {
                    const meta = getVariableMeta(v)
                    return (
                      <div key={v} className="space-y-1.5">
                        <Label htmlFor={`notification-variable-${v}`} className="text-sm font-medium">{meta.label}</Label>
                        <Input
                          id={`notification-variable-${v}`}
                          name={v}
                          placeholder={meta.placeholder}
                          value={variables[v] || ''}
                          onChange={(e) => handleVariableChange(v, e.target.value)}
                        />
                      </div>
                    )
                  })}
                </div>
              )}
            </CardContent>
          </Card>
        </div>

        {/* Right Column: Live Preview & Send Action */}
        <div className="space-y-6 lg:col-span-5 lg:sticky lg:top-6">
          <Card>
            <CardHeader className="pb-4">
              <div className="flex items-center justify-between">
                <CardTitle className="text-base font-semibold flex items-center gap-2">
                  <Eye className="size-4 text-primary" />
                  Nội dung xem trước
                </CardTitle>
                {currentTemplate && (
                  <Badge variant="secondary">{getChannelLabel(currentTemplate.channel)}</Badge>
                )}
              </div>
              <CardDescription>Kết quả sau khi render với tham số thực tế</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              {preview ? (
                <div className="space-y-3 rounded-lg border bg-muted/30 p-4">
                  {preview.renderedSubject && (
                    <div className="border-b pb-2.5">
                      <span className="text-xs font-medium text-muted-foreground block mb-0.5">
                        Tiêu đề thông báo:
                      </span>
                      <p className="text-sm font-semibold">{preview.renderedSubject}</p>
                    </div>
                  )}
                  <p className="text-sm text-muted-foreground">Bản xem trước đã được tạo theo đúng kênh gửi.</p>
                  <Button type="button" variant="outline" className="w-full gap-2" onClick={() => setPreviewOpen(true)}>
                    <Eye className="size-4" /> Mở bản xem trước lớn
                  </Button>
                </div>
              ) : (
                <div className="rounded-lg border border-dashed p-8 text-center text-sm text-muted-foreground space-y-2">
                  <Eye className="mx-auto size-8 opacity-40" />
                  <p>Bấm nút <strong>&quot;Xem trước&quot;</strong> sau khi điền thông tin để xem kết quả render của thông báo.</p>
                </div>
              )}

              {(selectedRecipient || deliveryScope !== 'single') && (
                <div className="rounded-md border bg-muted/40 p-3 text-xs space-y-1">
                  <div>
                    <strong className="text-foreground">Người nhận:</strong>{' '}
                    <span className="text-muted-foreground">
                      {deliveryScope === 'all'
                        ? 'Toàn bộ nhân viên'
                        : deliveryScope === 'multiple'
                          ? `${selectedRecipients.length} nhân viên`
                          : `${selectedRecipient!.name} (${selectedRecipient!.code})`}
                    </span>
                  </div>
                  {deliveryScope === 'single' && <div>
                    <strong className="text-foreground">Gửi tới:</strong>{' '}
                    <span className="font-mono text-primary font-medium">{customDestination || 'Mặc định'}</span>
                  </div>}
                </div>
              )}
            </CardContent>
            <CardFooter className="flex flex-col gap-2 pt-2">
              <Button
                className="w-full gap-2"
                size="lg"
                onClick={handleSend}
                disabled={sending || (deliveryScope === 'single' && !selectedRecipient) || (deliveryScope === 'multiple' && selectedRecipients.length === 0)}
              >
                <Send className="size-4" />
                {sending ? 'Đang gửi thông báo...' : 'Xác nhận gửi thông báo'}
              </Button>
              <p className="text-center text-xs text-muted-foreground">
                Snapshot nội dung và kết quả gửi sẽ được lưu vết độc lập vào nhật ký kiểm toán.
              </p>
            </CardFooter>
          </Card>
        </div>
      </div>
      <Dialog open={previewOpen && Boolean(preview)} onOpenChange={setPreviewOpen}>
        <DialogContent className="flex h-[90dvh] max-h-[90dvh] flex-col overflow-hidden sm:max-w-6xl">
          <DialogHeader>
            <DialogTitle>Xem trước {currentTemplate?.channel === 'Email' ? 'email' : 'thông báo nội bộ'}</DialogTitle>
            <DialogDescription>Nội dung sau khi binding biến, hiển thị gần giống kết quả người nhận sẽ thấy.</DialogDescription>
          </DialogHeader>
          {preview ? <div className="flex min-h-0 flex-1 flex-col gap-3">
            <div className="grid gap-1 rounded-lg border bg-muted/30 p-3 text-sm">
              <span><strong>Người nhận:</strong> {deliveryScope === 'all' ? 'Toàn bộ nhân viên' : deliveryScope === 'multiple' ? `${selectedRecipients.length} nhân viên` : selectedRecipient?.name ?? 'Chưa chọn'}</span>
              <span><strong>Địa chỉ:</strong> {customDestination || 'Mặc định'}</span>
              <span><strong>Tiêu đề:</strong> {preview.renderedSubject || '(Không có tiêu đề)'}</span>
            </div>
            {currentTemplate?.channel === 'Email' ? <iframe title="Nội dung email xem trước" sandbox=""
              className="min-h-0 flex-1 rounded-lg border bg-white"
              srcDoc={`<!doctype html><html lang="vi"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width"><style>html,body{margin:0;background:#f3f4f6;color:#111827;font-family:Arial,sans-serif}main{box-sizing:border-box;max-width:760px;min-height:100%;margin:0 auto;background:#fff;padding:32px;line-height:1.6;overflow-wrap:anywhere}img{max-width:100%;height:auto}a{color:#1d4ed8}</style></head><body><main>${preview.renderedBody}</main></body></html>`} /> :
              <div className="min-h-0 flex-1 overflow-y-auto whitespace-pre-wrap rounded-lg border bg-card p-6 text-sm leading-7">{preview.renderedBody}</div>}
          </div> : null}
        </DialogContent>
      </Dialog>
    </div>
  )
}

export default SendNotificationTab
