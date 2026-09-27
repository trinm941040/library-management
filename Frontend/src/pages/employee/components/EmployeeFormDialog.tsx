import { useEffect, useState, type FormEvent, type ReactNode } from 'react'
import { Button } from '@/common/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/common/components/ui/dialog'
import { Input } from '@/common/components/ui/input'
import { Label } from '@/common/components/ui/label'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/common/components/ui/select'
import {
  employmentStatusLabels,
  employmentStatuses,
  getEmployeeBranches,
  type Employee,
  type EmployeeBranch,
  type EmploymentStatus,
  type SaveEmployeeInput,
} from '../employee-api'

type EmployeeFormData = {
  employeeCode: string
  fullName: string
  email: string
  phoneNumber: string
  dateOfBirth: string
  address: string
  position: string
  department: string
  hireDate: string
  status: EmploymentStatus
  branchId: string
  deactivateLinkedAccount: boolean
}

type EmployeeFormDialogProps = {
  open: boolean
  employee: Employee | null
  onOpenChange: (open: boolean) => void
  onSave: (data: SaveEmployeeInput) => Promise<string | null>
}

function today() {
  const date = new Date()
  const offset = date.getTimezoneOffset() * 60_000
  return new Date(date.getTime() - offset).toISOString().slice(0, 10)
}

const emptyForm = (): EmployeeFormData => ({
  employeeCode: '',
  fullName: '',
  email: '',
  phoneNumber: '',
  dateOfBirth: '',
  address: '',
  position: '',
  department: '',
  hireDate: today(),
  status: 'Active',
  branchId: '',
  deactivateLinkedAccount: false,
})

const formFromEmployee = (employee: Employee | null): EmployeeFormData => employee
  ? {
      employeeCode: employee.employeeCode,
      fullName: employee.fullName,
      email: employee.email,
      phoneNumber: employee.phoneNumber ?? '',
      dateOfBirth: employee.dateOfBirth ?? '',
      address: employee.address ?? '',
      position: employee.position,
      department: employee.department,
      hireDate: employee.hireDate,
      status: employee.status,
      branchId: employee.branchId,
      deactivateLinkedAccount: false,
    }
  : emptyForm()

export function EmployeeFormDialog({
  open,
  employee,
  onOpenChange,
  onSave,
}: EmployeeFormDialogProps) {
  const [form, setForm] = useState<EmployeeFormData>(() => formFromEmployee(employee))
  const [error, setError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [branches, setBranches] = useState<EmployeeBranch[]>([])

  useEffect(() => {
    if (!open) return
    const controller = new AbortController()
    getEmployeeBranches(controller.signal)
      .then((items) => {
        setBranches(items)
        setForm((current) => ({
          ...current,
          branchId: current.branchId || items[0]?.id || '',
        }))
      })
      .catch((requestError: unknown) => {
        if (!(requestError instanceof DOMException && requestError.name === 'AbortError')) {
          setError(requestError instanceof Error ? requestError.message : 'Không thể tải danh sách chi nhánh.')
        }
      })
    return () => controller.abort()
  }, [open])

  useEffect(() => {
    setForm(formFromEmployee(employee))
    setError('')
  }, [employee, open])

  const updateField = <Key extends keyof EmployeeFormData>(
    field: Key,
    value: EmployeeFormData[Key],
  ) => setForm((current) => ({ ...current, [field]: value }))

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError('')

    if (form.dateOfBirth && form.dateOfBirth >= form.hireDate) {
      setError('Ngày sinh phải trước ngày vào làm.')
      return
    }
    if (!form.branchId) {
      setError('Vui lòng chọn chi nhánh đang hoạt động.')
      return
    }

    setIsSubmitting(true)
    try {
      const saveError = await onSave({
        employeeCode: form.employeeCode.trim().toUpperCase(),
        fullName: form.fullName.trim(),
        email: form.email.trim().toLowerCase(),
        phoneNumber: form.phoneNumber.trim() || null,
        dateOfBirth: form.dateOfBirth || null,
        address: form.address.trim() || null,
        position: form.position.trim(),
        department: form.department.trim(),
        hireDate: form.hireDate,
        status: form.status,
        branchId: form.branchId,
        deactivateLinkedAccount: form.deactivateLinkedAccount,
      })
      if (saveError) {
        setError(saveError)
        return
      }
      onOpenChange(false)
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={(nextOpen) => !isSubmitting && onOpenChange(nextOpen)}>
      <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-2xl">
        <form onSubmit={handleSubmit}>
          <DialogHeader>
            <DialogTitle>
              {employee ? 'Cập nhật hồ sơ nhân viên' : 'Tạo hồ sơ nhân viên'}
            </DialogTitle>
            <DialogDescription>
              Hồ sơ nhân sự được quản lý độc lập với tài khoản truy cập hệ thống.
            </DialogDescription>
          </DialogHeader>

          <div className="grid gap-5 py-6 sm:grid-cols-2">
            <FormField label="Mã nhân viên" htmlFor="employee-code">
              <Input
                id="employee-code"
                value={form.employeeCode}
                onChange={(event) => updateField('employeeCode', event.target.value)}
                placeholder="NV001"
                maxLength={30}
                disabled={isSubmitting}
                required
              />
            </FormField>
            <FormField label="Họ và tên" htmlFor="employee-name">
              <Input
                id="employee-name"
                value={form.fullName}
                onChange={(event) => updateField('fullName', event.target.value)}
                placeholder="Nguyễn Văn A"
                minLength={2}
                maxLength={150}
                autoComplete="name"
                disabled={isSubmitting}
                required
              />
            </FormField>
            <FormField label="Email" htmlFor="employee-email">
              <Input
                id="employee-email"
                type="email"
                value={form.email}
                onChange={(event) => updateField('email', event.target.value)}
                placeholder="nhanvien@library.vn"
                maxLength={256}
                autoComplete="email"
                disabled={isSubmitting}
                required
              />
            </FormField>
            <FormField label="Số điện thoại" htmlFor="employee-phone">
              <Input
                id="employee-phone"
                type="tel"
                value={form.phoneNumber}
                onChange={(event) => updateField('phoneNumber', event.target.value)}
                placeholder="0901 234 567"
                maxLength={30}
                autoComplete="tel"
                disabled={isSubmitting}
              />
            </FormField>
            <FormField label="Chi nhánh" htmlFor="employee-branch">
              <Select
                value={form.branchId}
                onValueChange={(value) => updateField('branchId', value)}
                disabled={isSubmitting || branches.length === 0}
              >
                <SelectTrigger id="employee-branch" className="w-full">
                  <SelectValue placeholder="Chọn chi nhánh mặc định" />
                </SelectTrigger>
                <SelectContent>
                  {branches.map((branch) => (
                    <SelectItem key={branch.id} value={branch.id}>
                      {branch.code} - {branch.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </FormField>
            <FormField label="Đơn vị công tác" htmlFor="employee-department">
              <Input
                id="employee-department"
                value={form.department}
                onChange={(event) => updateField('department', event.target.value)}
                placeholder="Thư viện trung tâm"
                maxLength={100}
                disabled={isSubmitting}
                required
              />
            </FormField>
            <FormField label="Chức vụ" htmlFor="employee-position">
              <Input
                id="employee-position"
                value={form.position}
                onChange={(event) => updateField('position', event.target.value)}
                placeholder="Nhân viên lưu thông"
                maxLength={100}
                disabled={isSubmitting}
                required
              />
            </FormField>
            <FormField label="Ngày sinh" htmlFor="employee-birth-date">
              <Input
                id="employee-birth-date"
                type="date"
                value={form.dateOfBirth}
                max={form.hireDate || undefined}
                onChange={(event) => updateField('dateOfBirth', event.target.value)}
                disabled={isSubmitting}
              />
            </FormField>
            <FormField label="Ngày bắt đầu công tác" htmlFor="employee-hire-date">
              <Input
                id="employee-hire-date"
                type="date"
                value={form.hireDate}
                min={form.dateOfBirth || undefined}
                onChange={(event) => updateField('hireDate', event.target.value)}
                disabled={isSubmitting}
                required
              />
            </FormField>
            <FormField label="Trạng thái việc làm" htmlFor="employee-status">
              <Select
                value={form.status}
                onValueChange={(value) => updateField('status', value as EmploymentStatus)}
                disabled={isSubmitting}
              >
                <SelectTrigger id="employee-status" className="w-full">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {employmentStatuses.map((status) => (
                    <SelectItem key={status} value={status}>
                      {employmentStatusLabels[status]}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </FormField>
            <FormField label="Địa chỉ" htmlFor="employee-address" className="sm:col-span-2">
              <Input
                id="employee-address"
                value={form.address}
                onChange={(event) => updateField('address', event.target.value)}
                placeholder="Địa chỉ liên hệ"
                maxLength={500}
                autoComplete="street-address"
                disabled={isSubmitting}
              />
            </FormField>
            {employee && employee.status !== 'Terminated' && form.status === 'Terminated' ? (
              <div
                className="grid gap-3 rounded-lg border border-amber-500/40 bg-amber-500/10 p-4 sm:col-span-2"
                role="alert"
              >
                <p className="text-sm font-medium">Xác nhận nhân viên nghỉ việc</p>
                <p className="text-sm text-muted-foreground">
                  Hồ sơ và lịch sử nghiệp vụ vẫn được giữ nguyên. Access Account liên kết
                  không bị thay đổi trừ khi bạn chọn tùy chọn dưới đây.
                </p>
                {employee.userId ? (
                  <label className="flex cursor-pointer items-start gap-3 text-sm">
                    <input
                      className="mt-1 size-4"
                      type="checkbox"
                      checked={form.deactivateLinkedAccount}
                      disabled={isSubmitting}
                      onChange={(event) =>
                        updateField('deactivateLinkedAccount', event.target.checked)
                      }
                    />
                    Vô hiệu hóa Access Account liên kết và thu hồi các phiên đăng nhập.
                  </label>
                ) : (
                  <p className="text-sm text-muted-foreground">
                    Hồ sơ này chưa có Access Account liên kết.
                  </p>
                )}
              </div>
            ) : null}
          </div>

          {error ? (
            <p
              className="mb-5 rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive"
              role="alert"
            >
              {error}
            </p>
          ) : null}

          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              disabled={isSubmitting}
              onClick={() => onOpenChange(false)}
            >
              Hủy
            </Button>
            <Button type="submit" loading={isSubmitting} loadingLabel="Đang lưu hồ sơ nhân viên">
              {employee ? 'Lưu thay đổi' : 'Tạo hồ sơ'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}

function FormField({
  label,
  htmlFor,
  className,
  children,
}: {
  label: string
  htmlFor: string
  className?: string
  children: ReactNode
}) {
  return (
    <div className={`grid gap-2 ${className ?? ''}`}>
      <Label htmlFor={htmlFor}>{label}</Label>
      {children}
    </div>
  )
}
