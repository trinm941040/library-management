import { z } from 'zod'
import { authenticatedFetch, guidSchema, readResponse, userSchema } from '@/auth/auth-api'

export const profileFormSchema = z.object({
  fullName: z.string().trim().min(2, 'Họ tên phải có ít nhất 2 ký tự.').max(150),
  phoneNumber: z.string().trim().max(30).refine((value) => !value || /^\+?[0-9 .()-]{7,30}$/.test(value), 'Số điện thoại không hợp lệ.'),
  dateOfBirth: z.string().refine((value) => !value || !Number.isNaN(Date.parse(value)), 'Ngày sinh không hợp lệ.').refine((value) => !value || new Date(`${value}T00:00:00`) <= new Date(), 'Ngày sinh không được ở tương lai.'),
  address: z.string().trim().max(500, 'Địa chỉ không được quá 500 ký tự.'),
  rowVersion: guidSchema,
})
export const passwordFormSchema = z.object({
  currentPassword: z.string().min(1, 'Vui lòng nhập mật khẩu hiện tại.'),
  newPassword: z.string().min(8, 'Mật khẩu mới phải có ít nhất 8 ký tự.'),
  confirmPassword: z.string().min(1, 'Vui lòng xác nhận mật khẩu mới.'),
}).refine((value) => value.newPassword === value.confirmPassword, { path: ['confirmPassword'], message: 'Mật khẩu xác nhận không khớp.' })
export type ProfileFormValues = z.infer<typeof profileFormSchema>
export type PasswordFormValues = z.infer<typeof passwordFormSchema>

export async function updateProfile(values: ProfileFormValues) {
  const response = await authenticatedFetch('/api/v1/me/profile', {
    method: 'PATCH', headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ fullName: values.fullName, phoneNumber: values.phoneNumber || null, dateOfBirth: values.dateOfBirth || null, address: values.address || null, rowVersion: values.rowVersion }),
  })
  return readResponse(response, userSchema)
}
export async function changePassword(values: PasswordFormValues) {
  const response = await authenticatedFetch('/api/v1/me/change-password', {
    method: 'POST', headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ currentPassword: values.currentPassword, newPassword: values.newPassword }),
  })
  if (!response.ok) await readResponse(response)
}
