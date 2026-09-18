import { authenticatedFetch } from '@/auth/auth-api'

export type SettingType = 'String' | 'Number' | 'Boolean' | 'Json' | 'Secret'

export type SystemSettingItem = {
  id: string
  key: string
  value: string
  valueType: SettingType
  scope: string
  description?: string
  isSecret: boolean
  updatedByUserId: string
  updatedByDisplayName?: string
  updatedAtUtc: string
  concurrencyToken: string
}

export type SettingDiffItem = {
  key: string
  oldValue?: string | null
  newValue?: string | null
  valueType: SettingType
  scope: string
  diffType: 'ADD' | 'CHANGE' | 'SAME'
  description?: string | null
  isSecret: boolean
  hasImpactWarning: boolean
  impactWarning?: string | null
}

export type PackageValidationResult = {
  isValid: boolean
  errorMessage?: string | null
  version: string
  checksum: string
  totalSettings: number
  diffs: SettingDiffItem[]
}

export type ConfirmImportResult = {
  success: boolean
  message: string
  addedCount: number
  updatedCount: number
  unchangedCount: number
  packageId: string
}

export type ConfigurationPackageSummary = {
  id: string
  version: string
  checksum: string
  createdByUserId: string
  createdByDisplayName?: string
  createdAtUtc: string
  settingsCount: number
}

type ProblemDetails = {
  title?: string
  detail?: string
  message?: string
  errors?: Record<string, string[]>
}

async function readResponse<T>(response: Response, defaultError = 'Lỗi yêu cầu máy chủ.'): Promise<T> {
  if (response.ok) {
    if (response.status === 204) return undefined as T
    return response.json() as Promise<T>
  }

  const problem = (await response.json().catch(() => null)) as ProblemDetails | null
  const validationMessage = problem?.errors
    ? Object.values(problem.errors).flat().find(Boolean)
    : undefined

  throw new Error(validationMessage ?? problem?.message ?? problem?.detail ?? problem?.title ?? defaultError)
}

export async function getSystemSettings(scope?: string): Promise<SystemSettingItem[]> {
  const url = scope ? `/api/v1/settings?scope=${encodeURIComponent(scope)}` : '/api/v1/settings'
  const res = await authenticatedFetch(url, { method: 'GET' })
  return readResponse<SystemSettingItem[]>(res, 'Không thể tải thiết lập hệ thống.')
}

export async function updateSystemSetting(
  key: string,
  value: string,
  concurrencyToken?: string,
): Promise<SystemSettingItem> {
  const res = await authenticatedFetch(`/api/v1/settings/${encodeURIComponent(key)}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ key, value, concurrencyToken }),
  })
  return readResponse<SystemSettingItem>(res, 'Không thể cập nhật thiết lập.')
}

export async function batchUpdateSystemSettings(
  settings: Array<{ key: string; value: string }>,
): Promise<SystemSettingItem[]> {
  const res = await authenticatedFetch('/api/v1/settings/batch', {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ settings }),
  })
  return readResponse<SystemSettingItem[]>(res, 'Không thể lưu danh sách thiết lập.')
}

export async function exportConfigurationPackage(): Promise<void> {
  const res = await authenticatedFetch('/api/v1/settings/export-package', {
    method: 'GET',
  })

  if (!res.ok) {
    throw new Error('Không thể tải gói cấu hình.')
  }

  const blob = await res.blob()
  const contentDisposition = res.headers.get('Content-Disposition')
  let filename = `Cau_Hinh_He_Thong_${new Date().toISOString().slice(0, 10)}.json`

  if (contentDisposition) {
    const match = contentDisposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/)
    if (match?.[1]) {
      filename = match[1].replace(/['"]/g, '')
    }
  }

  const url = window.URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = filename
  document.body.appendChild(a)
  a.click()
  document.body.removeChild(a)
  window.URL.revokeObjectURL(url)
}

export async function validateConfigurationPackage(packageJson: string): Promise<PackageValidationResult> {
  const res = await authenticatedFetch('/api/v1/settings/validate-package', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(JSON.parse(packageJson)),
  })
  return readResponse<PackageValidationResult>(res, 'Không thể xác thực gói cấu hình.')
}

export async function importConfigurationPackage(packageJson: string): Promise<ConfirmImportResult> {
  const res = await authenticatedFetch('/api/v1/settings/import-package', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ packageJson }),
  })
  return readResponse<ConfirmImportResult>(res, 'Không thể nạp gói cấu hình.')
}

export async function getConfigurationPackageHistory(): Promise<ConfigurationPackageSummary[]> {
  const res = await authenticatedFetch('/api/v1/settings/packages', {
    method: 'GET',
  })
  return readResponse<ConfigurationPackageSummary[]>(res, 'Không thể tải lịch sử gói cấu hình.')
}

export async function resetSystemSettingsToDefaults(): Promise<{ message: string }> {
  const res = await authenticatedFetch('/api/v1/settings/reset-defaults', {
    method: 'POST',
  })
  return readResponse<{ message: string }>(res, 'Không thể khôi phục cài đặt mặc định.')
}
