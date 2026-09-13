import { useState, useEffect } from 'react'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/common/components/ui/dialog'
import { Button } from '@/common/components/ui/button'
import { Input } from '@/common/components/ui/input'
import { Label } from '@/common/components/ui/label'
import { Switch } from '@/common/components/ui/switch'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/common/components/ui/select'
import type {
  CirculationPolicy,
  CreateCirculationPolicyInput,
  UpdateCirculationPolicyInput,
  CreatePolicyVersionInput,
} from '../circulation-policy-api'
import {
  createCirculationPolicy,
  updateCirculationPolicy,
  createPolicyVersion,
} from '../circulation-policy-api'

type PolicyDialogProps = {
  open: boolean
  mode: 'create' | 'edit' | 'new-version'
  policy?: CirculationPolicy | null
  onOpenChange: (open: boolean) => void
  onSuccess: () => void
}

const idPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

export function CirculationPolicyDialog({
  open,
  mode,
  policy,
  onOpenChange,
  onSuccess,
}: PolicyDialogProps) {
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [memberGroup, setMemberGroup] = useState('')
  const [documentType, setDocumentType] = useState('')
  const [branchId, setBranchId] = useState('')
  const [effectiveFrom, setEffectiveFrom] = useState('')
  const [effectiveTo, setEffectiveTo] = useState('')

  // BorrowingLimit
  const [maxLoanBooks, setMaxLoanBooks] = useState(5)
  const [loanPeriodDays, setLoanPeriodDays] = useState(14)
  const [maxRenewals, setMaxRenewals] = useState(2)
  const [renewalPeriodDays, setRenewalPeriodDays] = useState(7)
  const [holdDays, setHoldDays] = useState(3)
  const [blockIfOverdue, setBlockIfOverdue] = useState(true)

  // FinePolicy
  const [finePerDay, setFinePerDay] = useState(5000)
  const [fixedFineAmount, setFixedFineAmount] = useState(0)
  const [maxFineAmount, setMaxFineAmount] = useState(100000)
  const [lostBookPenaltyRatio, setLostBookPenaltyRatio] = useState(150)

  // Options
  const [isActive, setIsActive] = useState(true)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  useEffect(() => {
    if (!open) return
    setErrorMessage(null)

    if (policy) {
      setName(mode === 'new-version' ? `${policy.name} (v${policy.version + 1})` : policy.name)
      setDescription(policy.description ?? '')
      setMemberGroup(policy.memberGroup ?? '')
      setDocumentType(policy.documentType ?? '')
      setBranchId(policy.branchId ?? '')

      const effFrom = policy.effectiveFrom
      const effTo = policy.effectiveTo
      setEffectiveFrom(effFrom ? effFrom.substring(0, 10) : new Date().toISOString().substring(0, 10))
      setEffectiveTo(effTo ? effTo.substring(0, 10) : '')

      setMaxLoanBooks(policy.maxLoanBooks ?? 5)
      setLoanPeriodDays(policy.loanPeriodDays ?? 14)
      setMaxRenewals(policy.maxRenewals ?? 2)
      setRenewalPeriodDays(policy.renewalPeriodDays ?? 7)
      setHoldDays(policy.holdDays ?? 3)
      setBlockIfOverdue(policy.blockIfOverdue ?? true)

      setFinePerDay(policy.finePerDay ?? 5000)
      setFixedFineAmount(policy.fixedFineAmount ?? 0)
      setMaxFineAmount(policy.maxFineAmount ?? 100000)
      setLostBookPenaltyRatio(policy.lostBookPenaltyRatio ?? 150)
      setIsActive(policy.isActive ?? true)
    } else {
      // Default new policy values
      setName('')
      setDescription('')
      setMemberGroup('')
      setDocumentType('')
      setBranchId('')
      setEffectiveFrom(new Date().toISOString().substring(0, 10))
      setEffectiveTo('')
      setMaxLoanBooks(5)
      setLoanPeriodDays(14)
      setMaxRenewals(2)
      setRenewalPeriodDays(7)
      setHoldDays(3)
      setBlockIfOverdue(true)
      setFinePerDay(5000)
      setFixedFineAmount(0)
      setMaxFineAmount(100000)
      setLostBookPenaltyRatio(150)
      setIsActive(true)
    }
  }, [open, policy, mode])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setErrorMessage(null)

    if (!name.trim()) {
      setErrorMessage('Vui lòng nhập tên chính sách.')
      return
    }

    if (!effectiveFrom) {
      setErrorMessage('Vui lòng chọn ngày bắt đầu hiệu lực.')
      return
    }

    if (effectiveTo && new Date(effectiveTo) <= new Date(effectiveFrom)) {
      setErrorMessage('Ngày kết thúc phải sau ngày bắt đầu hiệu lực.')
      return
    }

    if (branchId && !idPattern.test(branchId.trim())) {
      setErrorMessage('Mã chi nhánh phải đúng định dạng UUID.')
      return
    }

    if (maxLoanBooks < 1 || loanPeriodDays < 1 || maxRenewals < 0 || holdDays < 1) {
      setErrorMessage('Các thông số giới hạn mượn phải hợp lệ.')
      return
    }

    setIsSubmitting(true)
    try {
      if (mode === 'create') {
        const payload: CreateCirculationPolicyInput = {
          name: name.trim(),
          description: description.trim() || undefined,
          memberGroup: memberGroup.trim() || undefined,
          documentType: documentType.trim() || undefined,
          branchId: branchId.trim() || undefined,
          effectiveFrom: new Date(effectiveFrom).toISOString(),
          effectiveTo: effectiveTo ? new Date(effectiveTo).toISOString() : undefined,
          maxLoanBooks,
          loanPeriodDays,
          maxRenewals,
          renewalPeriodDays,
          holdDays,
          blockIfOverdue,
          finePerDay,
          fixedFineAmount,
          maxFineAmount,
          lostBookPenaltyRatio,
          isActive,
        }
        await createCirculationPolicy(payload)
      } else if (mode === 'edit' && policy) {
        const payload: UpdateCirculationPolicyInput = {
          name: name.trim(),
          description: description.trim() || undefined,
          memberGroup: memberGroup.trim() || undefined,
          documentType: documentType.trim() || undefined,
          branchId: branchId.trim() || undefined,
          effectiveFrom: new Date(effectiveFrom).toISOString(),
          effectiveTo: effectiveTo ? new Date(effectiveTo).toISOString() : undefined,
          maxLoanBooks,
          loanPeriodDays,
          maxRenewals,
          renewalPeriodDays,
          holdDays,
          blockIfOverdue,
          finePerDay,
          fixedFineAmount,
          maxFineAmount,
          lostBookPenaltyRatio,
          concurrencyToken: policy.concurrencyToken,
        }
        await updateCirculationPolicy(policy.id, payload)
      } else if (mode === 'new-version' && policy) {
        const payload: CreatePolicyVersionInput = {
          name: name.trim(),
          description: description.trim() || undefined,
          effectiveFrom: new Date(effectiveFrom).toISOString(),
          effectiveTo: effectiveTo ? new Date(effectiveTo).toISOString() : undefined,
          maxLoanBooks,
          loanPeriodDays,
          maxRenewals,
          renewalPeriodDays,
          holdDays,
          blockIfOverdue,
          finePerDay,
          fixedFineAmount,
          maxFineAmount,
          lostBookPenaltyRatio,
          concurrencyToken: policy.concurrencyToken,
        }
        await createPolicyVersion(policy.id, payload)
      }

      onSuccess()
      onOpenChange(false)
    } catch (err: unknown) {
      setErrorMessage(err instanceof Error ? err.message : 'Có lỗi xảy ra khi lưu chính sách.')
    } finally {
      setIsSubmitting(false)
    }
  }

  const dialogTitle =
    mode === 'create'
      ? 'Thêm chính sách lưu thông mới'
      : mode === 'edit'
        ? 'Chỉnh sửa chính sách lưu thông'
        : 'Tạo phiên bản chính sách mới'

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-2xl">
        <DialogHeader>
          <DialogTitle>{dialogTitle}</DialogTitle>
          <DialogDescription>
            Cấu hình quy định mượn trả, gia hạn, giữ chỗ và mức tiền phạt theo đối tượng.
          </DialogDescription>
        </DialogHeader>

        {errorMessage && (
          <div className="rounded-md border border-destructive/50 bg-destructive/10 p-3 text-sm text-destructive">
            {errorMessage}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-5">
          {/* Thông tin chung */}
          <div className="space-y-3">
            <h3 className="text-sm font-semibold text-foreground">1. Thông tin chung & Phạm vi</h3>
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              <div className="space-y-1.5 sm:col-span-2">
                <Label htmlFor="policy-name">Tên chính sách *</Label>
                <Input
                  id="policy-name"
                  placeholder="Ví dụ: Chính sách dành cho Giảng viên"
                  className="h-9 w-full"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  required
                />
              </div>

              <div className="space-y-1.5 sm:col-span-2">
                <Label htmlFor="policy-desc">Mô tả / Ghi chú</Label>
                <Input
                  id="policy-desc"
                  placeholder="Ghi chú mục đích áp dụng hoặc điều kiện đặc biệt"
                  className="h-9 w-full"
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                />
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="member-group">Nhóm độc giả áp dụng</Label>
                <Select
                  value={memberGroup || 'all'}
                  onValueChange={(val) => setMemberGroup(val === 'all' ? '' : val)}
                >
                  <SelectTrigger id="member-group" className="h-9 w-full">
                    <SelectValue placeholder="Chọn nhóm độc giả" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">Tất cả nhóm độc giả</SelectItem>
                    <SelectItem value="Student">Sinh viên</SelectItem>
                    <SelectItem value="Faculty">Giảng viên / Cán bộ</SelectItem>
                    <SelectItem value="Researcher">Nghiên cứu sinh</SelectItem>
                    <SelectItem value="General">Độc giả tự do</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="doc-type">Loại tài liệu / Thể loại</Label>
                <Select
                  value={documentType || 'all'}
                  onValueChange={(val) => setDocumentType(val === 'all' ? '' : val)}
                >
                  <SelectTrigger id="doc-type" className="h-9 w-full">
                    <SelectValue placeholder="Chọn loại sách" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">Tất cả loại sách</SelectItem>
                    <SelectItem value="Giáo trình">Giáo trình</SelectItem>
                    <SelectItem value="Tham khảo">Tài liệu tham khảo</SelectItem>
                    <SelectItem value="Luận văn">Luận văn / Khóa luận</SelectItem>
                    <SelectItem value="Công nghệ thông tin">Công nghệ thông tin</SelectItem>
                    <SelectItem value="Khoa học">Khoa học</SelectItem>
                    <SelectItem value="Kinh tế">Kinh tế</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="eff-from">Ngày bắt đầu hiệu lực *</Label>
                <Input
                  id="eff-from"
                  type="date"
                  className="h-9 w-full"
                  value={effectiveFrom}
                  onChange={(e) => setEffectiveFrom(e.target.value)}
                  required
                />
              </div>

              <div className="space-y-1.5 sm:col-span-2">
                <Label htmlFor="branch-id">Mã chi nhánh (UUID, không bắt buộc)</Label>
                <Input
                  id="branch-id"
                  value={branchId}
                  onChange={(e) => setBranchId(e.target.value)}
                  placeholder="Để trống để áp dụng toàn hệ thống"
                />
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="eff-to">Ngày kết thúc (để trống = vô hạn)</Label>
                <Input
                  id="eff-to"
                  type="date"
                  className="h-9 w-full"
                  value={effectiveTo}
                  onChange={(e) => setEffectiveTo(e.target.value)}
                />
              </div>
            </div>
          </div>

          {/* Giới hạn mượn sách */}
          <div className="space-y-3">
            <h3 className="text-sm font-semibold text-foreground">2. Quy định mượn & giữ chỗ</h3>
            <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
              <div className="space-y-1.5">
                <Label htmlFor="max-books">Số sách tối đa (cuốn)</Label>
                <Input
                  id="max-books"
                  type="number"
                  min="1"
                  max="100"
                  className="h-9 w-full"
                  value={maxLoanBooks}
                  onChange={(e) => setMaxLoanBooks(Number(e.target.value))}
                />
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="loan-days">Thời hạn mượn (ngày)</Label>
                <Input
                  id="loan-days"
                  type="number"
                  min="1"
                  max="365"
                  className="h-9 w-full"
                  value={loanPeriodDays}
                  onChange={(e) => setLoanPeriodDays(Number(e.target.value))}
                />
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="max-renewals">Số lần gia hạn tối đa</Label>
                <Input
                  id="max-renewals"
                  type="number"
                  min="0"
                  max="10"
                  className="h-9 w-full"
                  value={maxRenewals}
                  onChange={(e) => setMaxRenewals(Number(e.target.value))}
                />
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="renewal-days">Số ngày mỗi lần gia hạn</Label>
                <Input
                  id="renewal-days"
                  type="number"
                  min="1"
                  max="60"
                  className="h-9 w-full"
                  value={renewalPeriodDays}
                  onChange={(e) => setRenewalPeriodDays(Number(e.target.value))}
                />
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="hold-days">Thời gian giữ đặt trước (ngày)</Label>
                <Input
                  id="hold-days"
                  type="number"
                  min="1"
                  max="30"
                  className="h-9 w-full"
                  value={holdDays}
                  onChange={(e) => setHoldDays(Number(e.target.value))}
                />
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="block-overdue">Ràng buộc quá hạn</Label>
                <div className="flex h-9 items-center space-x-2 rounded-md border border-input/60 bg-muted/20 px-3">
                  <Switch
                    id="block-overdue"
                    checked={blockIfOverdue}
                    onCheckedChange={setBlockIfOverdue}
                  />
                  <Label htmlFor="block-overdue" className="cursor-pointer text-xs font-normal">
                    Chặn mượn khi có sách quá hạn
                  </Label>
                </div>
              </div>
            </div>
          </div>

          {/* Tiền phạt */}
          <div className="space-y-3">
            <h3 className="text-sm font-semibold text-foreground">3. Chính sách tiền phạt</h3>
            <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
              <div className="flex flex-col justify-between space-y-1.5">
                <Label htmlFor="fine-per-day" className="min-h-9 flex items-end text-sm font-medium">
                  Phạt quá hạn (VNĐ/ngày)
                </Label>
                <Input
                  id="fine-per-day"
                  type="number"
                  min="0"
                  step="1000"
                  className="h-9 w-full"
                  value={finePerDay}
                  onChange={(e) => setFinePerDay(Number(e.target.value))}
                />
              </div>

              <div className="flex flex-col justify-between space-y-1.5">
                <Label htmlFor="fixed-fine" className="min-h-9 flex items-end text-sm font-medium">
                  Phạt cố định (VNĐ)
                </Label>
                <Input
                  id="fixed-fine"
                  type="number"
                  min="0"
                  step="5000"
                  className="h-9 w-full"
                  value={fixedFineAmount}
                  onChange={(e) => setFixedFineAmount(Number(e.target.value))}
                />
              </div>

              <div className="flex flex-col justify-between space-y-1.5">
                <Label htmlFor="max-fine" className="min-h-9 flex items-end text-sm font-medium">
                  Trần phạt tối đa (VNĐ)
                </Label>
                <Input
                  id="max-fine"
                  type="number"
                  min="0"
                  step="10000"
                  className="h-9 w-full"
                  value={maxFineAmount}
                  onChange={(e) => setMaxFineAmount(Number(e.target.value))}
                />
              </div>

              <div className="flex flex-col justify-between space-y-1.5">
                <Label htmlFor="lost-penalty" className="min-h-9 flex items-end text-sm font-medium">
                  Đền bù mất sách (%)
                </Label>
                <Input
                  id="lost-penalty"
                  type="number"
                  min="100"
                  max="500"
                  className="h-9 w-full"
                  value={lostBookPenaltyRatio}
                  onChange={(e) => setLostBookPenaltyRatio(Number(e.target.value))}
                />
              </div>
            </div>
          </div>

          {mode === 'create' && (
            <div className="flex items-center space-x-2 pt-2">
              <Switch id="is-active" checked={isActive} onCheckedChange={setIsActive} />
              <Label htmlFor="is-active">Kích hoạt chính sách ngay sau khi tạo</Label>
            </div>
          )}

          <DialogFooter className="pt-3">
            <Button
              type="button"
              variant="outline"
              onClick={() => onOpenChange(false)}
              disabled={isSubmitting}
            >
              Hủy
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? 'Đang lưu...' : mode === 'new-version' ? 'Tạo phiên bản' : 'Lưu chính sách'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
