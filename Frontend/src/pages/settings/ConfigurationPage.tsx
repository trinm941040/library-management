import { useState } from 'react'
import { AlertTriangle, Download, FileCheck2, Upload } from 'lucide-react'
import { ConfirmDialog, PageShell, ScreenState, useToast } from '@/common/components'
import { Badge } from '@/common/components/ui/badge'
import { Button } from '@/common/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/common/components/ui/card'
import { Input } from '@/common/components/ui/input'
import { useAuth } from '@/auth/AuthProvider'
import { can } from '@/shared/auth/permissions'
import { PermissionBoundary } from '@/shared/auth/PermissionBoundary'
import {
  confirmConfiguration,
  exportConfiguration,
  validateConfiguration,
  type ConfigurationImportResult,
  type ConfigurationPreview,
} from './settings-api'

const kindLabels = { Add: 'Thêm', Change: 'Thay đổi', Remove: 'Xóa ghi đè' } as const
const kindVariants = { Add: 'default', Change: 'secondary', Remove: 'destructive' } as const
const displayValue = (value: unknown) => value === null || value === undefined
  ? '—'
  : typeof value === 'object' ? JSON.stringify(value, null, 2) : String(value)

export function ConfigurationPage() {
  const { user } = useAuth()
  const canImport = can(user?.permissions ?? [], 'settings.update')
  const [file, setFile] = useState<File | null>(null)
  const [preview, setPreview] = useState<ConfigurationPreview | null>(null)
  const [result, setResult] = useState<ConfigurationImportResult | null>(null)
  const [validating, setValidating] = useState(false)
  const [exporting, setExporting] = useState(false)
  const [confirming, setConfirming] = useState(false)
  const [confirmOpen, setConfirmOpen] = useState(false)
  const [error, setError] = useState('')
  const { showToast } = useToast()

  const exportPackage = async () => {
    setExporting(true)
    setError('')
    try {
      const exported = await exportConfiguration()
      const url = URL.createObjectURL(exported.blob)
      const link = document.createElement('a')
      link.href = url
      link.download = exported.fileName
      link.click()
      URL.revokeObjectURL(url)
      showToast('Đã xuất gói cấu hình.')
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Không thể xuất gói cấu hình.')
    } finally {
      setExporting(false)
    }
  }

  const validate = async () => {
    if (!file) return
    setValidating(true)
    setError('')
    setResult(null)
    try {
      setPreview(await validateConfiguration(file))
    } catch (reason) {
      setPreview(null)
      setError(reason instanceof Error ? reason.message : 'Gói cấu hình không hợp lệ.')
    } finally {
      setValidating(false)
    }
  }

  const confirm = async () => {
    if (!preview) return
    setConfirming(true)
    setError('')
    try {
      const imported = await confirmConfiguration(preview.confirmationToken)
      setResult(imported)
      setPreview(null)
      setFile(null)
      setConfirmOpen(false)
      showToast('Đã áp dụng gói cấu hình trong một transaction.')
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Không thể áp dụng gói cấu hình.')
    } finally {
      setConfirming(false)
    }
  }

  return (
    <PageShell
      eyebrow="Quản lý hệ thống"
      title="Gói cấu hình"
      description="Xuất bản sao cấu hình hoặc kiểm tra thay đổi trước khi nhập và xác nhận áp dụng."
      actions={
        <PermissionBoundary requiredPermissions={['settings.read']}>
          <Button variant="outline" disabled={exporting} onClick={() => void exportPackage()}>
            <Download /> {exporting ? 'Đang xuất...' : 'Xuất cấu hình'}
          </Button>
        </PermissionBoundary>
      }
    >
      <div className="grid gap-5">
        <Card>
          <CardHeader><CardTitle>Nhập gói cấu hình</CardTitle></CardHeader>
          <CardContent className="grid gap-4">
            <div className="rounded-md border border-amber-500/40 bg-amber-500/10 p-4 text-sm">
              <p className="flex items-center gap-2 font-medium"><AlertTriangle className="size-4" />Lưu ý tác động</p>
              <p className="mt-1 text-muted-foreground">Chỉ tệp JSON tối đa 1 MiB, đúng schema version và checksum mới được xem trước. Dữ liệu không thay đổi cho tới bước xác nhận.</p>
            </div>
            <Input
              type="file"
              accept="application/json,.json"
              aria-label="Chọn gói cấu hình JSON"
              disabled={!canImport || validating || confirming}
              onChange={(event) => {
                setFile(event.target.files?.[0] ?? null)
                setPreview(null)
                setResult(null)
                setError('')
              }}
            />
            <div className="flex flex-wrap items-center gap-3">
              <Button disabled={!canImport || !file || validating} onClick={() => void validate()}>
                <Upload /> {validating ? 'Đang kiểm tra...' : 'Kiểm tra và xem trước'}
              </Button>
              {file ? <span className="text-sm text-muted-foreground">{file.name} · {(file.size / 1024).toFixed(1)} KiB</span> : null}
            </div>
            {error ? <p className="text-sm text-destructive" role="alert">{error}</p> : null}
          </CardContent>
        </Card>

        {result ? (
          <ScreenState
            kind="success"
            title="Đã nhập cấu hình thành công"
            description={`Thêm ${result.added}, thay đổi ${result.changed}, xóa ghi đè ${result.removed}.`}
          />
        ) : null}

        {preview ? (
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2"><FileCheck2 className="size-5" />Bản xem trước thay đổi</CardTitle>
              <div className="grid gap-1 text-sm text-muted-foreground sm:grid-cols-2">
                <p>Schema: {preview.schemaVersion}</p>
                <p>Hết hạn: {new Date(preview.expiresAtUtc).toLocaleString('vi-VN')}</p>
                <p className="break-all font-mono text-xs sm:col-span-2">Checksum: {preview.checksum}</p>
              </div>
            </CardHeader>
            <CardContent className="grid gap-4">
              {preview.differences.length === 0 ? (
                <ScreenState compact kind="empty" title="Không có khác biệt" description="Cấu hình hiện tại đã trùng với gói được tải lên." />
              ) : (
                <div className="max-h-[50vh] overflow-auto rounded-md border">
                  <table className="w-full min-w-[760px] text-left text-sm">
                    <thead className="sticky top-0 bg-background shadow-sm">
                      <tr><th className="p-3">Thiết lập</th><th className="p-3">Loại</th><th className="p-3">Trước</th><th className="p-3">Sau</th></tr>
                    </thead>
                    <tbody>
                      {preview.differences.map((difference) => (
                        <tr key={difference.key} className="border-t align-top">
                          <td className="p-3"><p className="font-medium">{difference.displayName}</p><code className="text-xs text-muted-foreground">{difference.key}</code></td>
                          <td className="p-3"><Badge variant={kindVariants[difference.kind]}>{kindLabels[difference.kind]}</Badge></td>
                          <td className="max-w-64 whitespace-pre-wrap break-words p-3 font-mono text-xs">{displayValue(difference.before)}</td>
                          <td className="max-w-64 whitespace-pre-wrap break-words p-3 font-mono text-xs">{displayValue(difference.after)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
              <div className="flex justify-end">
                <Button disabled={!canImport || preview.differences.length === 0} onClick={() => setConfirmOpen(true)}>Xác nhận áp dụng</Button>
              </div>
            </CardContent>
          </Card>
        ) : null}
      </div>

      <ConfirmDialog
        open={confirmOpen}
        title="Áp dụng gói cấu hình?"
        description="Toàn bộ thay đổi trong bản xem trước sẽ được áp dụng trong một transaction. Nếu cấu hình đã đổi, yêu cầu sẽ bị từ chối."
        confirmLabel="Áp dụng cấu hình"
        isPending={confirming}
        error={confirmOpen ? error : ''}
        onOpenChange={setConfirmOpen}
        onConfirm={() => void confirm()}
      />
    </PageShell>
  )
}
