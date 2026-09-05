import { useEffect, useState, type FormEvent } from 'react'
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
import type { Employee, EmploymentStatus, SaveEmployeeInput } from '../employee-api'

export type EmployeeFormData = {
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
}

type EmployeeFormDialogProps = {
  open: boolean
  employee: Employee | null
  onOpenChange: (open: boolean) => void
  onSave: (data: SaveEmployeeInput) => Promise<string | null>
}

const statusOptions: { value: EmploymentStatus; label: string }[] = [
  { value: 'Active', label: 'Đang làm việc' },
  { value: 'OnLeave', label: 'Đang nghỉ phép' },
  { value: 'Inactive', label: 'Tạm ngưng' },
  { value: 'Terminated', label: 'Đã nghỉ việc' },
]

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
})

export function EmployeeFormDialog({
  open,
  employee,
  onOpenChange,
  onSave,
}: EmployeeFormDialogProps) {
  const [form, setForm] = useState<EmployeeFormData>(emptyForm)
  const [error, setError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  useEffect(() => {
    setForm(
      employee
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
          }
        : emptyForm(),
    )
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
            <DialogTitle>{employee ? 'Chỉnh sửa nhân viên' : 'Thêm nhân viên mới'}</DialogTitle>
            <DialogDescription>
              Cập nhật hồ sơ công việc và thông tin liên hệ của nhân viên thư viện.
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

            <FormField label="Phòng ban" htmlFor="employee-department">
              <Input
                id="employee-department"
                value={form.department}
                onChange={(event) => updateField('department', event.target.value)}
                placeholder="Lưu thông"
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
                placeholder="Thủ thư"
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

            <FormField label="Ngày vào làm" htmlFor="employee-hire-date">
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

            <FormField label="Trạng thái" htmlFor="employee-status">
              <Select
                value={form.status}
                onValueChange={(value) => updateField('status', value as EmploymentStatus)}
                disabled={isSubmitting}
              >
                <SelectTrigger id="employee-status" className="w-full">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {statusOptions.map((option) => (
                    <SelectItem key={option.value} value={option.value}>
                      {option.label}
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
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? 'Đang lưu...' : employee ? 'Lưu thay đổi' : 'Thêm nhân viên'}
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
  children: React.ReactNode
}) {
  return (
    <div className={`grid gap-2 ${className ?? ''}`}>
      <Label htmlFor={htmlFor}>{label}</Label>
      {children}
    </div>
  )
}
