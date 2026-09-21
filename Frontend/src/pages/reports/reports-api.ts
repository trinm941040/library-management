import { authenticatedFetch } from '@/auth/auth-api'

const REPORTS_URL = '/api/v1/reports'
const SAVED_FILTERS_URL = '/api/v1/saved-filters'

export type ReportColumnDefinition = {
  key: string
  label: string
  type: 'string' | 'number' | 'currency' | 'datetime' | 'badge'
  align?: 'left' | 'center' | 'right'
}

export type ReportFilterOption = {
  value: string
  label: string
}

export type ReportFilterFieldDefinition = {
  key: string
  label: string
  type: 'select' | 'date' | 'branch' | 'daterange'
  options?: ReportFilterOption[]
}

export type ReportDefinition = {
  code: string
  name: string
  description: string
  type: string
  columns: ReportColumnDefinition[]
  filterFields: ReportFilterFieldDefinition[]
}

export type ReportPreviewRequest = {
  reportCode: string
  filters?: Record<string, string | null | undefined>
  pageNumber?: number
  pageSize?: number
  sortField?: string | null
  sortAscending?: boolean
  timezoneOffsetMinutes?: number
}

export type ReportPreviewResult = {
  reportCode: string
  reportName: string
  columns: ReportColumnDefinition[]
  rows: Record<string, unknown>[]
  totalRows: number
  pageNumber: number
  pageSize: number
  totalPages: number
  summaryStats?: Record<string, unknown> | null
  generatedAtUtc: string
}

export type ReportExportRequest = {
  reportCode: string
  filters?: Record<string, string | null | undefined>
  format?: string
  sortField?: string | null
  sortAscending?: boolean
  timezoneOffsetMinutes?: number
}

export type ReportExportResult = {
  reportId: string
  reportCode: string
  fileName: string
  contentType: string
  totalRecords: number
  expiresAtUtc: string
  downloadUrl: string
}

export type SavedFilter = {
  id: string
  name: string
  scope: string
  criteria: string
  sort?: string | null
  createdAtUtc: string
}

export type CreateSavedFilterPayload = {
  name: string
  scope: string
  criteria: string
  sort?: string | null
}

export type UpdateSavedFilterPayload = {
  name: string
  criteria: string
  sort?: string | null
}

type ProblemDetails = {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

export class ApiError extends Error {
  status: number
  constructor(message: string, status: number) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

async function readResponse<T>(response: Response): Promise<T> {
  if (response.ok) {
    if (response.status === 204) return undefined as T
    return response.json() as Promise<T>
  }

  const problem = (await response.json().catch(() => null)) as ProblemDetails | null
  const validationMessage = problem?.errors
    ? Object.values(problem.errors).flat().find(Boolean)
    : undefined

  const message = validationMessage ?? problem?.detail ?? problem?.title ?? 'Không thể xử lý yêu cầu báo cáo.'
  throw new ApiError(message, response.status)
}

export async function fetchReportDefinitions(signal?: AbortSignal): Promise<ReportDefinition[]> {
  const response = await authenticatedFetch(`${REPORTS_URL}/definitions`, { signal })
  return readResponse<ReportDefinition[]>(response)
}

export async function previewReport(
  request: ReportPreviewRequest,
  signal?: AbortSignal,
): Promise<ReportPreviewResult> {
  const payload = {
    ...request,
    timezoneOffsetMinutes: request.timezoneOffsetMinutes ?? new Date().getTimezoneOffset(),
  }
  const response = await authenticatedFetch(`${REPORTS_URL}/preview`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
    signal,
  })
  return readResponse<ReportPreviewResult>(response)
}

export async function exportReport(
  request: ReportExportRequest,
  signal?: AbortSignal,
): Promise<ReportExportResult> {
  const payload = {
    ...request,
    timezoneOffsetMinutes: request.timezoneOffsetMinutes ?? new Date().getTimezoneOffset(),
  }
  const response = await authenticatedFetch(`${REPORTS_URL}/export`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
    signal,
  })
  return readResponse<ReportExportResult>(response)
}

export async function downloadReportFile(reportId: string, fallbackFileName: string): Promise<void> {
  const response = await authenticatedFetch(`${REPORTS_URL}/${reportId}/download`)
  if (!response.ok) {
    const err = await response.json().catch(() => null)
    throw new ApiError(err?.detail ?? 'Không thể tải tệp báo cáo.', response.status)
  }

  const blob = await response.blob()
  const contentDisposition = response.headers.get('content-disposition')
  let fileName = fallbackFileName

  if (contentDisposition) {
    const match = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/.exec(contentDisposition)
    if (match?.[1]) {
      fileName = match[1].replace(/['"]/g, '')
    }
  }

  const url = window.URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = fileName
  document.body.appendChild(a)
  a.click()
  window.URL.revokeObjectURL(url)
  document.body.removeChild(a)
}

export async function fetchSavedFilters(scope: string, signal?: AbortSignal): Promise<SavedFilter[]> {
  const response = await authenticatedFetch(`${SAVED_FILTERS_URL}?scope=${encodeURIComponent(scope)}`, {
    signal,
  })
  return readResponse<SavedFilter[]>(response)
}

export async function createSavedFilter(
  payload: CreateSavedFilterPayload,
  signal?: AbortSignal,
): Promise<SavedFilter> {
  const response = await authenticatedFetch(SAVED_FILTERS_URL, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
    signal,
  })
  return readResponse<SavedFilter>(response)
}

export async function updateSavedFilter(
  id: string,
  payload: UpdateSavedFilterPayload,
  signal?: AbortSignal,
): Promise<void> {
  const response = await authenticatedFetch(`${SAVED_FILTERS_URL}/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
    signal,
  })
  return readResponse<void>(response)
}

export async function deleteSavedFilter(id: string, signal?: AbortSignal): Promise<void> {
  const response = await authenticatedFetch(`${SAVED_FILTERS_URL}/${id}`, {
    method: 'DELETE',
    signal,
  })
  return readResponse<void>(response)
}
