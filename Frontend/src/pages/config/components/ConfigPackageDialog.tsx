import { useState, useRef } from 'react'
import {
  Upload,
  AlertTriangle,
  CheckCircle2,
  XCircle,
  FileCode,
  ArrowRight,
  Loader2,
} from 'lucide-react'
import { Button } from '@/common/components/ui/button'
import { Badge } from '@/common/components/ui/badge'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/common/components/ui/dialog'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/common/components/ui/table'
import {
  validateConfigurationPackage,
  importConfigurationPackage,
  type PackageValidationResult,
} from '../system-settings-api'

type ConfigPackageDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSuccess: () => void
}

export function ConfigPackageDialog({ open, onOpenChange, onSuccess }: ConfigPackageDialogProps) {
  const [jsonText, setJsonText] = useState('')
  const [fileName, setFileName] = useState('')
  const [isValidating, setIsValidating] = useState(false)
  const [isImporting, setIsImporting] = useState(false)
  const [validation, setValidation] = useState<PackageValidationResult | null>(null)
  const [filterType, setFilterType] = useState<'ALL' | 'CHANGE' | 'ADD'>('ALL')
  const fileInputRef = useRef<HTMLInputElement>(null)

  const handleReset = () => {
    setJsonText('')
    setFileName('')
    setValidation(null)
    setFilterType('ALL')
  }

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return

    setFileName(file.name)
    const text = await file.text()
    setJsonText(text)

    setIsValidating(true)
    setValidation(null)
    try {
      const res = await validateConfigurationPackage(text)
      setValidation(res)
    } catch (err) {
      setValidation({
        isValid: false,
        errorMessage: err instanceof Error ? err.message : 'Tệp cấu hình không hợp lệ.',
        version: '',
        checksum: '',
        totalSettings: 0,
        diffs: [],
      })
    } finally {
      setIsValidating(false)
    }
  }

  const handleConfirmImport = async () => {
    if (!jsonText || !validation?.isValid) return

    setIsImporting(true)
    try {
      const res = await importConfigurationPackage(jsonText)
      if (res.success) {
        onSuccess()
        onOpenChange(false)
        handleReset()
      } else {
        alert(res.message)
      }
    } catch (err) {
      alert(err instanceof Error ? err.message : 'Lỗi khi nạp gói cấu hình.')
    } finally {
      setIsImporting(false)
    }
  }

  const filteredDiffs = (validation?.diffs || []).filter((d) => {
    if (filterType === 'CHANGE') return d.diffType === 'CHANGE'
    if (filterType === 'ADD') return d.diffType === 'ADD'
    return true
  })

  const changesCount = validation?.diffs.filter((d) => d.diffType === 'CHANGE').length || 0
  const addsCount = validation?.diffs.filter((d) => d.diffType === 'ADD').length || 0
  const sameCount = validation?.diffs.filter((d) => d.diffType === 'SAME').length || 0

  return (
    <Dialog open={open} onOpenChange={(v) => { onOpenChange(v); if (!v) handleReset() }}>
      <DialogContent className="max-w-4xl max-h-[90vh] flex flex-col p-6">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2 text-xl font-bold">
            <Upload className="h-5 w-5 text-primary" />
            Nạp gói cấu hình hệ thống (Import Package)
          </DialogTitle>
          <DialogDescription>
            Quy trình an toàn: Tải lên tệp tin $\rightarrow$ Kiểm tra Checksum &amp; Xem trước thay đổi $\rightarrow$ Xác nhận áp dụng trong Transaction.
          </DialogDescription>
        </DialogHeader>

        <div className="flex-1 overflow-y-auto space-y-5 my-2 pr-1">
          {/* File Upload Zone */}
          <div
            onClick={() => fileInputRef.current?.click()}
            className="border-2 border-dashed rounded-lg p-6 text-center cursor-pointer hover:border-primary/60 transition-colors bg-muted/20"
          >
            <input
              ref={fileInputRef}
              type="file"
              accept=".json,application/json"
              className="hidden"
              onChange={handleFileChange}
            />
            <div className="flex flex-col items-center justify-center gap-2">
              <FileCode className="h-10 w-10 text-muted-foreground" />
              {fileName ? (
                <div>
                  <p className="font-semibold text-primary">{fileName}</p>
                  <p className="text-xs text-muted-foreground">Bấm để chọn tệp tin khác</p>
                </div>
              ) : (
                <div>
                  <p className="font-medium">Kéo thả tệp tin hoặc bấm để chọn tệp .json</p>
                  <p className="text-xs text-muted-foreground mt-1">Định dạng gói cấu hình chuẩn JSON (tối đa 2MB)</p>
                </div>
              )}
            </div>
          </div>

          {/* Loading State */}
          {isValidating && (
            <div className="flex items-center justify-center gap-2 py-8 text-muted-foreground">
              <Loader2 className="h-5 w-5 animate-spin text-primary" />
              <span>Đang giải mã và kiểm tra mã toàn vẹn Checksum SHA-256...</span>
            </div>
          )}

          {/* Validation Result */}
          {validation && (
            <div className="space-y-4">
              {!validation.isValid ? (
                <div className="flex items-start gap-3 rounded-lg border border-destructive/40 bg-destructive/10 p-4 text-destructive">
                  <XCircle className="h-5 w-5 shrink-0 mt-0.5" />
                  <div>
                    <h4 className="font-semibold">Tệp cấu hình không hợp lệ hoặc đã bị chỉnh sửa</h4>
                    <p className="text-sm mt-1">{validation.errorMessage}</p>
                  </div>
                </div>
              ) : (
                <>
                  <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 rounded-lg border border-emerald-500/30 bg-emerald-500/10 p-4 text-emerald-500">
                    <div className="flex items-center gap-3">
                      <CheckCircle2 className="h-6 w-6 shrink-0 text-emerald-500" />
                      <div>
                        <h4 className="font-semibold text-foreground">Gói cấu hình hợp lệ (v{validation.version})</h4>
                        <p className="text-xs text-muted-foreground font-mono truncate max-w-lg mt-0.5">
                          Checksum: {validation.checksum}
                        </p>
                      </div>
                    </div>
                    <div className="flex items-center gap-2">
                      <Badge variant="outline" className="bg-background text-foreground">
                        {validation.totalSettings} thiết lập
                      </Badge>
                    </div>
                  </div>

                  {/* Summary Bar & Filter Tabs */}
                  <div className="flex flex-wrap items-center justify-between gap-3 bg-muted/30 p-3 rounded-lg border">
                    <div className="flex items-center gap-2 text-xs">
                      <span className="font-medium text-muted-foreground">Thay đổi dự kiến:</span>
                      <Badge variant="secondary" className="bg-amber-500/15 text-amber-600 border-amber-500/30">
                        ~ {changesCount} Cập nhật
                      </Badge>
                      <Badge variant="secondary" className="bg-emerald-500/15 text-emerald-600 border-emerald-500/30">
                        + {addsCount} Thêm mới
                      </Badge>
                      <Badge variant="outline">
                        = {sameCount} Giữ nguyên
                      </Badge>
                    </div>

                    <div className="flex gap-1">
                      <Button
                        size="xs"
                        variant={filterType === 'ALL' ? 'default' : 'ghost'}
                        onClick={() => setFilterType('ALL')}
                      >
                        Tất cả ({validation.diffs.length})
                      </Button>
                      <Button
                        size="xs"
                        variant={filterType === 'CHANGE' ? 'default' : 'ghost'}
                        onClick={() => setFilterType('CHANGE')}
                      >
                        Chỉ xem thay đổi ({changesCount})
                      </Button>
                      <Button
                        size="xs"
                        variant={filterType === 'ADD' ? 'default' : 'ghost'}
                        onClick={() => setFilterType('ADD')}
                      >
                        Chỉ xem thêm mới ({addsCount})
                      </Button>
                    </div>
                  </div>

                  {/* Diff Table */}
                  <div className="rounded-md border overflow-hidden">
                    <Table>
                      <TableHeader>
                        <TableRow>
                          <TableHead className="w-[220px]">Khóa tham số (Key)</TableHead>
                          <TableHead className="w-[100px]">Trạng thái</TableHead>
                          <TableHead>Giá trị hiện tại (DB)</TableHead>
                          <TableHead className="w-[20px] text-center"></TableHead>
                          <TableHead>Giá trị mới (Gói nạp)</TableHead>
                        </TableRow>
                      </TableHeader>
                      <TableBody>
                        {filteredDiffs.length === 0 ? (
                          <TableRow>
                            <TableCell colSpan={5} className="h-24 text-center text-muted-foreground text-sm">
                              Không có thiết lập nào thuộc bộ lọc này.
                            </TableCell>
                          </TableRow>
                        ) : (
                          filteredDiffs.map((diff) => (
                            <TableRow key={diff.key} className={diff.hasImpactWarning ? 'bg-amber-500/5' : undefined}>
                              <TableCell className="text-xs">
                                <div className="font-medium text-foreground">{diff.description || diff.key}</div>
                                {diff.hasImpactWarning && (
                                  <div className="flex items-center gap-1 text-[11px] text-amber-500 mt-1 font-sans">
                                    <AlertTriangle className="h-3 w-3 shrink-0" />
                                    <span>{diff.impactWarning}</span>
                                  </div>
                                )}
                              </TableCell>
                              <TableCell>
                                {diff.diffType === 'CHANGE' && (
                                  <Badge className="bg-amber-500/15 text-amber-600 border-amber-500/30 text-[11px]">
                                    ~ Thay đổi
                                  </Badge>
                                )}
                                {diff.diffType === 'ADD' && (
                                  <Badge className="bg-emerald-500/15 text-emerald-600 border-emerald-500/30 text-[11px]">
                                    + Mới
                                  </Badge>
                                )}
                                {diff.diffType === 'SAME' && (
                                  <Badge variant="outline" className="text-muted-foreground text-[11px]">
                                    = Giữ nguyên
                                  </Badge>
                                )}
                              </TableCell>
                              <TableCell className="font-mono text-xs text-muted-foreground">
                                {diff.oldValue ?? <span className="italic text-muted-foreground/60">(chưa có)</span>}
                              </TableCell>
                              <TableCell className="text-center text-muted-foreground">
                                <ArrowRight className="h-3.5 w-3.5 inline" />
                              </TableCell>
                              <TableCell className="font-mono text-xs font-semibold text-foreground">
                                {diff.newValue}
                              </TableCell>
                            </TableRow>
                          ))
                        )}
                      </TableBody>
                    </Table>
                  </div>
                </>
              )}
            </div>
          )}
        </div>

        <DialogFooter className="gap-2 sm:gap-0 pt-3 border-t">
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Đóng
          </Button>
          {validation?.isValid && (
            <Button
              onClick={handleConfirmImport}
              disabled={isImporting || changesCount + addsCount === 0}
            >
              {isImporting ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Đang ghi nhận vào Transaction...
                </>
              ) : (
                'Xác nhận áp dụng vào hệ thống'
              )}
            </Button>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
