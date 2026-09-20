import { useMemo, useState } from 'react'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/common/components/ui/dialog'
import { Badge } from '@/common/components/ui/badge'
import { Button } from '@/common/components/ui/button'
import { Check, Copy, Filter, Globe, Hash, Layers, ShieldCheck, User } from 'lucide-react'
import type { AuditLog } from '../audit-log-api'

type AuditDiffDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  log: AuditLog | null
  onFilterCorrelationId?: (correlationId: string) => void
  onFilterEntity?: (entityType: string, entityId: string) => void
}

type FieldDiff = {
  key: string
  before: unknown
  after: unknown
  status: 'added' | 'removed' | 'modified' | 'unchanged'
}

function parseJsonSafe(jsonStr?: string | null): Record<string, unknown> | null {
  if (!jsonStr) return null
  try {
    const parsed = JSON.parse(jsonStr)
    return typeof parsed === 'object' && parsed !== null ? parsed : null
  } catch {
    return null
  }
}

function formatValue(value: unknown): string {
  if (value === null || value === undefined) return '<trống>'
  if (typeof value === 'boolean') return value ? 'true' : 'false'
  if (typeof value === 'object') return JSON.stringify(value, null, 2)
  return String(value)
}

export function AuditDiffDialog({
  open,
  onOpenChange,
  log,
  onFilterCorrelationId,
  onFilterEntity,
}: AuditDiffDialogProps) {
  const [onlyShowChanges, setOnlyShowChanges] = useState(true)
  const [viewMode, setViewMode] = useState<'diff' | 'raw'>('diff')
  const [copiedCorrelation, setCopiedCorrelation] = useState(false)

  const beforeObj = useMemo(() => parseJsonSafe(log?.beforeJson), [log?.beforeJson])
  const afterObj = useMemo(() => parseJsonSafe(log?.afterJson), [log?.afterJson])

  const diffs: FieldDiff[] = useMemo(() => {
    if (!beforeObj && !afterObj) return []

    const keys = Array.from(
      new Set([...Object.keys(beforeObj || {}), ...Object.keys(afterObj || {})]),
    ).sort()

    return keys.map((key) => {
      const hasBefore = beforeObj && Object.prototype.hasOwnProperty.call(beforeObj, key)
      const hasAfter = afterObj && Object.prototype.hasOwnProperty.call(afterObj, key)

      const beforeVal = hasBefore ? beforeObj[key] : undefined
      const afterVal = hasAfter ? afterObj[key] : undefined

      let status: FieldDiff['status'] = 'unchanged'
      if (!hasBefore && hasAfter) {
        status = 'added'
      } else if (hasBefore && !hasAfter) {
        status = 'removed'
      } else if (JSON.stringify(beforeVal) !== JSON.stringify(afterVal)) {
        status = 'modified'
      }

      return {
        key,
        before: beforeVal,
        after: afterVal,
        status,
      }
    })
  }, [beforeObj, afterObj])

  const visibleDiffs = useMemo(() => {
    if (!onlyShowChanges) return diffs
    return diffs.filter((d) => d.status !== 'unchanged')
  }, [diffs, onlyShowChanges])

  const handleCopyCorrelation = () => {
    if (!log?.correlationId) return
    navigator.clipboard.writeText(log.correlationId)
    setCopiedCorrelation(true)
    setTimeout(() => setCopiedCorrelation(false), 2000)
  }

  const getActionBadge = (action: string) => {
    if (action.includes('added') || action.includes('create')) {
      return <Badge className="bg-emerald-600 text-white hover:bg-emerald-700">Tạo mới</Badge>
    }
    if (action.includes('deleted') || action.includes('remove')) {
      return <Badge variant="destructive">Xóa bỏ</Badge>
    }
    if (action.includes('modified') || action.includes('update')) {
      return <Badge className="bg-amber-600 text-white hover:bg-amber-700">Cập nhật</Badge>
    }
    return <Badge variant="secondary">{action}</Badge>
  }

  if (!log) return null

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[90vh] max-w-4xl overflow-y-auto sm:max-w-4xl">
        <DialogHeader>
          <div className="flex flex-wrap items-center justify-between gap-2 pr-6">
            <div className="flex items-center gap-2">
              <DialogTitle className="text-xl">Chi tiết bản ghi kiểm toán</DialogTitle>
              {getActionBadge(log.action)}
            </div>
            <span className="text-xs text-muted-foreground font-mono">
              {new Date(log.createdAtUtc).toLocaleString('vi-VN', {
                timeZone: 'Asia/Ho_Chi_Minh',
                year: 'numeric',
                month: '2-digit',
                day: '2-digit',
                hour: '2-digit',
                minute: '2-digit',
                second: '2-digit',
              })}
            </span>
          </div>
          <DialogDescription>
            Bản ghi bất biến ghi nhận mọi thay đổi dữ liệu kèm thông tin định danh và dấu vết mạng.
          </DialogDescription>
        </DialogHeader>

        {/* Metadata summary grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-3 rounded-lg border bg-muted/40 p-4 text-sm">
          <div className="space-y-1.5">
            <div className="flex items-center gap-2 text-muted-foreground">
              <User className="h-4 w-4 text-primary" />
              <span className="font-medium text-foreground">Người thực hiện:</span>
            </div>
            <p className="pl-6 font-medium">
              {log.actorName || 'Hệ thống'}
            </p>
          </div>

          <div className="space-y-1.5">
            <div className="flex items-center gap-2 text-muted-foreground">
              <Layers className="h-4 w-4 text-primary" />
              <span className="font-medium text-foreground">Đối tượng tác động:</span>
            </div>
            <div className="flex flex-wrap items-center gap-2 pl-6">
              <p className="font-mono text-xs">
                <span className="font-semibold text-foreground">{log.entityType}</span> / ID:{' '}
                {log.entityId}
              </p>
              {onFilterEntity && (
                <Button
                  variant="outline"
                  size="sm"
                  className="h-6 text-xs px-2"
                  onClick={() => {
                    onFilterEntity(log.entityType, log.entityId)
                    onOpenChange(false)
                  }}
                  title="Lọc tất cả nhật ký của đối tượng này"
                >
                  <Filter className="mr-1 h-3 w-3" />
                  Xem lịch sử
                </Button>
              )}
            </div>
          </div>

          <div className="space-y-1.5">
            <div className="flex items-center gap-2 text-muted-foreground">
              <Globe className="h-4 w-4 text-primary" />
              <span className="font-medium text-foreground">Địa chỉ IP nguồn:</span>
            </div>
            <p className="pl-6 font-mono text-xs">
              {log.ipAddress ? (
                <span className="inline-flex items-center gap-1 rounded bg-secondary px-2 py-0.5 font-medium">
                  {log.ipAddress}
                </span>
              ) : (
                <span className="italic text-muted-foreground">Không ghi nhận</span>
              )}
            </p>
          </div>

          <div className="space-y-1.5">
            <div className="flex items-center gap-2 text-muted-foreground">
              <Hash className="h-4 w-4 text-primary" />
              <span className="font-medium text-foreground">Correlation ID:</span>
            </div>
            <div className="flex items-center gap-2 pl-6">
              <span className="font-mono text-xs truncate max-w-[200px]" title={log.correlationId ?? ''}>
                {log.correlationId || <span className="italic text-muted-foreground">Không có</span>}
              </span>
              {log.correlationId && (
                <>
                  <Button
                    variant="ghost"
                    size="icon"
                    className="h-6 w-6 text-muted-foreground hover:text-foreground"
                    onClick={handleCopyCorrelation}
                    title="Sao chép Correlation ID"
                  >
                    {copiedCorrelation ? (
                      <Check className="h-3 w-3 text-emerald-500" />
                    ) : (
                      <Copy className="h-3 w-3" />
                    )}
                  </Button>
                  {onFilterCorrelationId && (
                    <Button
                      variant="outline"
                      size="sm"
                      className="h-6 text-xs px-2"
                      onClick={() => {
                        onFilterCorrelationId(log.correlationId!)
                        onOpenChange(false)
                      }}
                      title="Lọc tất cả nhật ký cùng phiên Correlation"
                    >
                      <Filter className="mr-1 h-3 w-3" />
                      Lọc phiên
                    </Button>
                  )}
                </>
              )}
            </div>
          </div>
        </div>

        {/* Diff Mode / Raw Mode Switcher */}
        <div className="flex flex-wrap items-center justify-between gap-2 border-b pb-2 pt-2">
          <div className="flex gap-1.5">
            <Button
              variant={viewMode === 'diff' ? 'default' : 'outline'}
              size="sm"
              onClick={() => setViewMode('diff')}
            >
              So sánh Before / After
            </Button>
            <Button
              variant={viewMode === 'raw' ? 'default' : 'outline'}
              size="sm"
              onClick={() => setViewMode('raw')}
            >
              Dữ liệu JSON gốc
            </Button>
          </div>

          {viewMode === 'diff' && diffs.length > 0 && (
            <label className="flex items-center gap-2 text-xs font-medium cursor-pointer text-muted-foreground select-none">
              <input
                type="checkbox"
                checked={onlyShowChanges}
                onChange={(e) => setOnlyShowChanges(e.target.checked)}
                className="h-3.5 w-3.5 rounded border-muted-foreground"
              />
              Chỉ xem các trường thay đổi ({diffs.filter((d) => d.status !== 'unchanged').length}/
              {diffs.length})
            </label>
          )}
        </div>

        {/* Main Diff Content */}
        {viewMode === 'diff' ? (
          <div className="space-y-3">
            {visibleDiffs.length === 0 ? (
              <div className="rounded-lg border border-dashed p-8 text-center text-sm text-muted-foreground">
                {diffs.length === 0
                  ? 'Không có dữ liệu chi tiết JSON cho bản ghi này.'
                  : 'Tất cả các trường đều giữ nguyên giá trị không thay đổi.'}
              </div>
            ) : (
              <div className="overflow-x-auto rounded-md border">
                <table className="w-full text-left text-xs border-collapse">
                  <thead className="bg-muted/70 text-muted-foreground font-semibold">
                    <tr className="border-b">
                      <th className="p-2.5 w-1/4">Trường (Field)</th>
                      <th className="p-2.5 w-5/12 bg-red-500/5 text-red-600 dark:text-red-400">
                        Giá trị trước (Before)
                      </th>
                      <th className="p-2.5 w-5/12 bg-emerald-500/5 text-emerald-600 dark:text-emerald-400">
                        Giá trị sau (After)
                      </th>
                    </tr>
                  </thead>
                  <tbody className="divide-y font-mono">
                    {visibleDiffs.map((d) => {
                      const isModified = d.status === 'modified'
                      const isAdded = d.status === 'added'
                      const isRemoved = d.status === 'removed'

                      return (
                        <tr
                          key={d.key}
                          className={
                            isModified
                              ? 'bg-amber-500/5'
                              : isAdded
                              ? 'bg-emerald-500/5'
                              : isRemoved
                              ? 'bg-red-500/5'
                              : ''
                          }
                        >
                          <td className="p-2.5 font-sans font-medium text-foreground align-top">
                            <div className="flex items-center gap-1.5">
                              <span>{d.key}</span>
                              {isModified && (
                                <span className="rounded bg-amber-100 text-amber-800 dark:bg-amber-900/50 dark:text-amber-300 px-1 py-0.2 text-[10px]">
                                  Sửa
                                </span>
                              )}
                              {isAdded && (
                                <span className="rounded bg-emerald-100 text-emerald-800 dark:bg-emerald-900/50 dark:text-emerald-300 px-1 py-0.2 text-[10px]">
                                  Mới
                                </span>
                              )}
                              {isRemoved && (
                                <span className="rounded bg-red-100 text-red-800 dark:bg-red-900/50 dark:text-red-300 px-1 py-0.2 text-[10px]">
                                  Đã xóa
                                </span>
                              )}
                            </div>
                          </td>
                          <td className="p-2.5 text-muted-foreground whitespace-pre-wrap break-all align-top bg-red-500/5">
                            {isAdded ? (
                              <span className="italic opacity-40">&lt;không có&gt;</span>
                            ) : (
                              formatValue(d.before)
                            )}
                          </td>
                          <td className="p-2.5 text-foreground whitespace-pre-wrap break-all align-top bg-emerald-500/5 font-semibold">
                            {isRemoved ? (
                              <span className="italic opacity-40 text-muted-foreground">&lt;đã xóa&gt;</span>
                            ) : (
                              formatValue(d.after)
                            )}
                          </td>
                        </tr>
                      )
                    })}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <div>
              <p className="text-xs font-semibold mb-1 text-red-500">Before JSON</p>
              <pre className="max-h-72 overflow-auto rounded bg-muted p-3 text-[11px] font-mono whitespace-pre-wrap">
                {log.beforeJson || '// Không có dữ liệu trước thay đổi'}
              </pre>
            </div>
            <div>
              <p className="text-xs font-semibold mb-1 text-emerald-500">After JSON</p>
              <pre className="max-h-72 overflow-auto rounded bg-muted p-3 text-[11px] font-mono whitespace-pre-wrap">
                {log.afterJson || '// Không có dữ liệu sau thay đổi'}
              </pre>
            </div>
          </div>
        )}

        {/* Security badge notice */}
        <div className="flex items-center gap-2 text-xs text-muted-foreground border-t pt-3">
          <ShieldCheck className="h-4 w-4 text-emerald-600" />
          <span>
            Các trường nhạy cảm (mật khẩu, hash, token, secret) đã được tự động làm sạch (Redaction)
            theo quy định bảo mật.
          </span>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Đóng
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
