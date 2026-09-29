import { useEffect, useMemo, useState } from 'react'
import { Eye, EyeOff, RefreshCw, Save, Sparkles } from 'lucide-react'
import { PageShell, ScreenState, useToast } from '@/common/components'
import { Button } from '@/common/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/common/components/ui/card'
import { Input } from '@/common/components/ui/input'
import { Label } from '@/common/components/ui/label'
import { Switch } from '@/common/components/ui/switch'
import { useAuth } from '@/auth/AuthProvider'
import { can } from '@/shared/auth/permissions'
import { getSettings, updateSetting, type SystemSetting } from './settings-api'
import { useSettings } from '@/settings/SettingsProvider'

const scopeLabels = {
  System: 'Hệ thống',
  Notifications: 'Thông báo',
  Operations: 'Vận hành',
} as const

function draftValue(setting: SystemSetting) {
  if (setting.isSecret) return ''
  if (setting.valueType === 'Json') return JSON.stringify(setting.value, null, 2)
  return String(setting.value ?? '')
}

function SettingEditor({
  setting,
  canUpdate,
  onSaved,
}: {
  setting: SystemSetting
  canUpdate: boolean
  onSaved: (setting: SystemSetting) => void
}) {
  const [draft, setDraft] = useState(() => draftValue(setting))
  const [showSecret, setShowSecret] = useState(false)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const { showToast } = useToast()

  useEffect(() => setDraft(draftValue(setting)), [setting])
  const dirty = setting.isSecret ? draft.length > 0 : draft !== draftValue(setting)

  const save = async () => {
    setError('')
    let value: unknown = draft
    try {
      if (setting.valueType === 'Number') {
        value = Number(draft)
        if (!Number.isFinite(value)) throw new Error('Giá trị phải là số hợp lệ.')
      } else if (setting.valueType === 'Boolean') {
        value = draft === 'true'
      } else if (setting.valueType === 'Json') {
        value = JSON.parse(draft) as unknown
      }
      setSaving(true)
      const updated = await updateSetting(setting.key, value, setting.concurrencyToken)
      onSaved(updated)
      showToast(`Đã cập nhật ${setting.displayName}.`)
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Không thể cập nhật thiết lập.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <article className="grid gap-4 border-b py-5 last:border-b-0 lg:grid-cols-[minmax(14rem,1fr)_minmax(16rem,1.2fr)_auto] lg:items-start">
      <div className="grid gap-1">
        <Label htmlFor={`setting-${setting.key}`}>{setting.displayName}</Label>
        <p className="text-sm text-muted-foreground">{setting.description}</p>
        <code className="text-xs text-muted-foreground">{setting.key}</code>
        {setting.updatedAtUtc ? (
          <p className="text-xs text-muted-foreground">
            Cập nhật lúc {new Date(setting.updatedAtUtc).toLocaleString('vi-VN')}
          </p>
        ) : <p className="text-xs text-muted-foreground">Đang dùng giá trị mặc định.</p>}
      </div>
      <div className="grid gap-2">
        {setting.valueType === 'Boolean' ? (
          <div className="flex min-h-9 items-center gap-3">
            <Switch
              id={`setting-${setting.key}`}
              checked={draft === 'true'}
              disabled={!canUpdate || saving}
              onCheckedChange={(checked) => setDraft(String(checked))}
            />
            <span className="text-sm">{draft === 'true' ? 'Đang bật' : 'Đang tắt'}</span>
          </div>
        ) : setting.valueType === 'Json' ? (
          <textarea
            id={`setting-${setting.key}`}
            className="min-h-28 rounded-md border border-input bg-background px-3 py-2 font-mono text-sm outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:opacity-50"
            value={draft}
            disabled={!canUpdate || saving}
            onChange={(event) => setDraft(event.target.value)}
          />
        ) : (
          <div className="relative">
            <Input
              id={`setting-${setting.key}`}
              type={setting.isSecret && !showSecret ? 'password' : setting.valueType === 'Number' ? 'number' : 'text'}
              value={draft}
              disabled={!canUpdate || saving}
              placeholder={setting.isSecret && setting.hasValue ? 'Đã cấu hình — nhập để thay thế' : undefined}
              onChange={(event) => setDraft(event.target.value)}
              className={setting.isSecret ? 'pr-10' : undefined}
              autoComplete={setting.isSecret ? 'new-password' : undefined}
            />
            {setting.isSecret ? (
              <Button
                type="button"
                variant="ghost"
                size="icon-sm"
                className="absolute right-1 top-0.5"
                aria-label={showSecret ? 'Ẩn giá trị bí mật' : 'Hiện giá trị bí mật đang nhập'}
                onClick={() => setShowSecret((value) => !value)}
              >
                {showSecret ? <EyeOff /> : <Eye />}
              </Button>
            ) : null}
          </div>
        )}
        {setting.isSecret ? (
          <p className="text-xs text-muted-foreground">
            Giá trị hiện tại không được trả về trình duyệt hoặc đưa vào gói export.
          </p>
        ) : null}
        {error ? <p className="text-sm text-destructive" role="alert">{error}</p> : null}
      </div>
      <Button
        type="button"
        variant="outline"
        disabled={!canUpdate || !dirty}
        loading={saving}
        loadingLabel="Đang lưu thiết lập"
        onClick={() => void save()}
      >
        <Save /> Lưu
      </Button>
    </article>
  )
}

export function SettingsPage() {
  const { user } = useAuth()
  const { aiEnabled, setAiEnabled } = useSettings()
  const canUpdate = can(user?.permissions ?? [], 'settings.update')
  const [settings, setSettings] = useState<SystemSetting[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [reloadKey, setReloadKey] = useState(0)

  useEffect(() => {
    const controller = new AbortController()
    setLoading(true)
    setError('')
    getSettings(controller.signal)
      .then(setSettings)
      .catch((reason: unknown) => {
        if (reason instanceof DOMException && reason.name === 'AbortError') return
        setError(reason instanceof Error ? reason.message : 'Không thể tải thiết lập hệ thống.')
      })
      .finally(() => { if (!controller.signal.aborted) setLoading(false) })
    return () => controller.abort()
  }, [reloadKey])

  const grouped = useMemo(() => Object.entries(scopeLabels).map(([scope, label]) => ({
    scope,
    label,
    settings: settings.filter((setting) => setting.scope === scope),
  })), [settings])

  return (
    <PageShell
      eyebrow="Quản lý hệ thống"
      title="Thiết lập hệ thống"
      description="Quản lý tham số hệ thống theo schema và kiểu dữ liệu được backend kiểm soát."
      actions={<Button variant="outline" disabled={loading} loading={loading && settings.length > 0} loadingLabel="Đang tải lại thiết lập" onClick={() => setReloadKey((value) => value + 1)}><RefreshCw />Làm mới</Button>}
    >
      <div className="grid gap-5">
        <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Sparkles className="size-5 text-primary" /> AI &amp; Tìm kiếm
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="flex flex-col gap-4 rounded-lg border p-4 sm:flex-row sm:items-center sm:justify-between">
                <div className="grid gap-1">
                  <Label htmlFor="ai-features-enabled">Tính năng AI</Label>
                  <p className="text-sm text-muted-foreground">
                    Hiển thị tìm kiếm ngữ nghĩa AI trên bảng điều khiển, danh mục và kiosk.
                  </p>
                  <p className="text-xs text-muted-foreground">
                    Tùy chọn chỉ được lưu trên trình duyệt này và không thay đổi cấu hình backend.
                  </p>
                </div>
                <div className="flex min-h-11 shrink-0 items-center gap-3 rounded-md border px-4">
                  <Switch
                    id="ai-features-enabled"
                    checked={aiEnabled}
                    onCheckedChange={setAiEnabled}
                  />
                  <span className="text-sm font-medium">{aiEnabled ? 'Đang bật' : 'Đang tắt'}</span>
                </div>
              </div>
            </CardContent>
        </Card>
        {error && settings.length === 0 ? (
          <ScreenState kind="error" title="Không thể tải thiết lập" description={error} actionLabel="Thử lại" onAction={() => setReloadKey((value) => value + 1)} />
        ) : loading && settings.length === 0 ? (
          <ScreenState kind="loading" title="Đang tải thiết lập" />
        ) : (
          <>
          {grouped.filter((group) => group.settings.length > 0).map((group) => (
            <Card key={group.scope}>
              <CardHeader><CardTitle>{group.label}</CardTitle></CardHeader>
              <CardContent>
                {group.settings.map((setting) => (
                  <SettingEditor
                    key={setting.key}
                    setting={setting}
                    canUpdate={canUpdate}
                    onSaved={(updated) => setSettings((values) => values.map((value) => value.key === updated.key ? updated : value))}
                  />
                ))}
              </CardContent>
            </Card>
          ))}
          </>
        )}
      </div>
    </PageShell>
  )
}
