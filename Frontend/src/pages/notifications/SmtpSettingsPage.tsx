import { useEffect, useMemo, useState } from 'react'
import { PageShell, ScreenState, useToast } from '@/common/components'
import { Button } from '@/common/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/common/components/ui/card'
import { Input } from '@/common/components/ui/input'
import { Label } from '@/common/components/ui/label'
import { Switch } from '@/common/components/ui/switch'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/common/components/ui/select'
import { useAuth } from '@/auth/AuthProvider'
import { can } from '@/shared/auth/permissions'
import { getSettings, updateSetting, type SystemSetting } from '@/pages/settings/settings-api'
import { getSmtpSettings, testSmtp, type SmtpSettings } from './smtp-api'

type Form = SmtpSettings & { password: string; testRecipient: string }
const empty: Form = { enabled: false, host: '', port: 587, securityMode: 'StartTls', username: '',
  hasPassword: false, password: '', fromAddress: '', fromName: '', replyToAddress: '',
  timeoutSeconds: 30, maxRetryCount: 5, batchSize: 25, testRecipient: '' }

export function SmtpSettingsPage() {
  const { user } = useAuth()
  const editable = can(user?.permissions ?? [], 'settings.update')
  const { showToast } = useToast()
  const [form, setForm] = useState<Form>(empty)
  const [settings, setSettings] = useState<SystemSetting[]>([])
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [testing, setTesting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const map = useMemo(() => new Map(settings.map((item) => [item.key, item])), [settings])

  const load = async () => {
    setLoading(true); setError(null)
    try {
      const [view, all] = await Promise.all([getSmtpSettings(), getSettings()])
      setSettings(all)
      setForm({ ...view, username: view.username ?? '', replyToAddress: view.replyToAddress ?? '',
        password: '', testRecipient: view.fromAddress })
    } catch (caught) { setError(caught instanceof Error ? caught.message : 'Không thể tải cấu hình SMTP.') }
    finally { setLoading(false) }
  }
  useEffect(() => { void load() }, [])

  const save = async () => {
    setSaving(true); setError(null)
    const values: Array<[string, unknown]> = [
      ['notifications.email.enabled', form.enabled], ['notifications.smtp.host', form.host],
      ['notifications.smtp.port', form.port], ['notifications.smtp.security-mode', form.securityMode],
      ['notifications.smtp.username', form.username], ['notifications.smtp.from-address', form.fromAddress],
      ['notifications.smtp.from-name', form.fromName], ['notifications.smtp.reply-to-address', form.replyToAddress],
      ['notifications.smtp.timeout-seconds', form.timeoutSeconds],
      ['notifications.smtp.max-retry-count', form.maxRetryCount], ['notifications.email.batch-size', form.batchSize],
    ]
    if (form.password) values.push(['notifications.smtp.password', form.password])
    try {
      for (const [key, value] of values) await updateSetting(key, value, map.get(key)?.concurrencyToken ?? null)
      showToast('Đã lưu cấu hình SMTP.', 'success'); await load()
    } catch (caught) { setError(caught instanceof Error ? caught.message : 'Không thể lưu cấu hình SMTP.') }
    finally { setSaving(false) }
  }
  const test = async (sendMessage: boolean) => {
    setTesting(true); setError(null)
    try { const result = await testSmtp(form.testRecipient, sendMessage); showToast(result.message, 'success') }
    catch (caught) { setError(caught instanceof Error ? caught.message : 'Kiểm tra SMTP thất bại.') }
    finally { setTesting(false) }
  }
  const field = <K extends keyof Form>(key: K, value: Form[K]) => setForm((old) => ({ ...old, [key]: value }))
  if (loading) return <ScreenState kind="loading" title="Đang tải cấu hình SMTP" />

  return <PageShell eyebrow="Thông báo email" title="Cấu hình SMTP" description="Credential được mã hóa; mật khẩu hiện có không được trả về giao diện.">
    {error && <ScreenState kind="error" title="Không thể xử lý cấu hình" description={error} actionLabel="Thử lại" onAction={load} />}
    <Card><CardHeader><CardTitle>Máy chủ gửi thư</CardTitle><CardDescription>Thay đổi có hiệu lực từ lần gửi tiếp theo.</CardDescription></CardHeader>
      <CardContent className="grid gap-5 md:grid-cols-2">
        <div className="flex items-center justify-between md:col-span-2"><Label htmlFor="smtp-enabled">Bật gửi email</Label><Switch id="smtp-enabled" checked={form.enabled} disabled={!editable} onCheckedChange={(v) => field('enabled', v)} /></div>
        <div><Label htmlFor="smtp-host">Máy chủ</Label><Input id="smtp-host" value={form.host} disabled={!editable} onChange={(e) => field('host', e.target.value)} /></div>
        <div><Label htmlFor="smtp-port">Cổng</Label><Input id="smtp-port" type="number" value={form.port} disabled={!editable} onChange={(e) => field('port', Number(e.target.value))} /></div>
        <div><Label>Chế độ bảo mật</Label><Select value={form.securityMode} disabled={!editable} onValueChange={(v) => field('securityMode', v as Form['securityMode'])}><SelectTrigger><SelectValue /></SelectTrigger><SelectContent><SelectItem value="None">Không mã hóa</SelectItem><SelectItem value="StartTls">STARTTLS</SelectItem><SelectItem value="SslTls">SSL/TLS</SelectItem></SelectContent></Select></div>
        <div><Label htmlFor="smtp-user">Tài khoản</Label><Input id="smtp-user" value={form.username ?? ''} disabled={!editable} onChange={(e) => field('username', e.target.value)} /></div>
        <div><Label htmlFor="smtp-password">Mật khẩu {form.hasPassword ? '(đã cấu hình)' : ''}</Label><Input id="smtp-password" type="password" autoComplete="new-password" value={form.password} disabled={!editable} placeholder="Để trống để giữ mật khẩu hiện tại" onChange={(e) => field('password', e.target.value)} /></div>
        <div><Label htmlFor="smtp-from">Email người gửi</Label><Input id="smtp-from" type="email" value={form.fromAddress} disabled={!editable} onChange={(e) => field('fromAddress', e.target.value)} /></div>
        <div><Label htmlFor="smtp-name">Tên người gửi</Label><Input id="smtp-name" value={form.fromName} disabled={!editable} onChange={(e) => field('fromName', e.target.value)} /></div>
        <div><Label htmlFor="smtp-reply">Email phản hồi</Label><Input id="smtp-reply" type="email" value={form.replyToAddress ?? ''} disabled={!editable} onChange={(e) => field('replyToAddress', e.target.value)} /></div>
        <div><Label htmlFor="smtp-timeout">Timeout (giây)</Label><Input id="smtp-timeout" type="number" value={form.timeoutSeconds} disabled={!editable} onChange={(e) => field('timeoutSeconds', Number(e.target.value))} /></div>
        <div><Label htmlFor="smtp-retry">Số lần gửi lại</Label><Input id="smtp-retry" type="number" value={form.maxRetryCount} disabled={!editable} onChange={(e) => field('maxRetryCount', Number(e.target.value))} /></div>
        <div><Label htmlFor="smtp-batch">Kích thước lô</Label><Input id="smtp-batch" type="number" value={form.batchSize} disabled={!editable} onChange={(e) => field('batchSize', Number(e.target.value))} /></div>
        <div className="md:col-span-2 flex flex-wrap gap-3"><Button loading={saving} disabled={!editable} onClick={save}>Lưu cấu hình</Button></div>
      </CardContent></Card>
    <Card className="mt-5"><CardHeader><CardTitle>Kiểm tra SMTP</CardTitle></CardHeader><CardContent className="flex flex-col gap-3 sm:flex-row"><Input type="email" aria-label="Email nhận thư kiểm thử" value={form.testRecipient} onChange={(e) => field('testRecipient', e.target.value)} /><Button variant="outline" loading={testing} disabled={!editable || !form.testRecipient} onClick={() => test(false)}>Kiểm tra kết nối</Button><Button loading={testing} disabled={!editable || !form.testRecipient} onClick={() => test(true)}>Gửi email thử</Button></CardContent></Card>
  </PageShell>
}
