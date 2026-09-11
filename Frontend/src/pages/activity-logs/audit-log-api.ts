import { authenticatedFetch } from '@/auth/auth-api'

const AUDIT_LOGS_URL = '/api/v1/audit-logs'

export type AuditLogItem = {
  id: string
  actorUserId?: string | null
  actorDisplayName?: string | null
  actorEmail?: string | null
  action: string
  entityType: string
  entityId: string
  beforeJson?: string | null
  afterJson?: string | null
  createdAtUtc: string
  correlationId?: string | null
  ipAddress?: string | null
}

export type AuditLogQueryParams = {
  search?: string
  actorUserId?: string
  action?: string
  entityType?: string
  entityId?: string
  ipAddress?: string
  correlationId?: string
  fromDateUtc?: string
  toDateUtc?: string
  pageNumber?: number
  pageSize?: number
}

export type AuditLogPageResponse = {
  items: AuditLogItem[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
}

type ProblemDetails = {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
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

  throw new Error(validationMessage ?? problem?.detail ?? problem?.title ?? 'Không thể tải dữ liệu nhật ký kiểm toán.')
}

export async function getAuditLogs(
  params: AuditLogQueryParams = {},
  signal?: AbortSignal,
): Promise<AuditLogPageResponse> {
  const query = new URLSearchParams()
  if (params.search) query.set('search', params.search)
  if (params.actorUserId) query.set('actorUserId', params.actorUserId)
  if (params.action) query.set('action', params.action)
  if (params.entityType) query.set('entityType', params.entityType)
  if (params.entityId) query.set('entityId', params.entityId)
  if (params.ipAddress) query.set('ipAddress', params.ipAddress)
  if (params.correlationId) query.set('correlationId', params.correlationId)
  if (params.fromDateUtc) query.set('fromDateUtc', params.fromDateUtc)
  if (params.toDateUtc) query.set('toDateUtc', params.toDateUtc)
  if (params.pageNumber) query.set('pageNumber', String(params.pageNumber))
  if (params.pageSize) query.set('pageSize', String(params.pageSize))

  const queryString = query.toString()
  const url = queryString ? `${AUDIT_LOGS_URL}?${queryString}` : AUDIT_LOGS_URL
  const response = await authenticatedFetch(url, { signal })
  return readResponse<AuditLogPageResponse>(response)
}

export async function getAuditLogById(id: string, signal?: AbortSignal): Promise<AuditLogItem> {
  const response = await authenticatedFetch(`${AUDIT_LOGS_URL}/${id}`, { signal })
  return readResponse<AuditLogItem>(response)
}

export async function getEntityAuditHistory(
  entityType: string,
  entityId: string,
  signal?: AbortSignal,
): Promise<AuditLogItem[]> {
  const response = await authenticatedFetch(`${AUDIT_LOGS_URL}/entity/${encodeURIComponent(entityType)}/${entityId}`, { signal })
  return readResponse<AuditLogItem[]>(response)
}

export async function downloadAuditCsv(params: AuditLogQueryParams = {}): Promise<Blob> {
  const query = new URLSearchParams()
  if (params.search) query.set('search', params.search)
  if (params.actorUserId) query.set('actorUserId', params.actorUserId)
  if (params.action) query.set('action', params.action)
  if (params.entityType) query.set('entityType', params.entityType)
  if (params.entityId) query.set('entityId', params.entityId)
  if (params.ipAddress) query.set('ipAddress', params.ipAddress)
  if (params.correlationId) query.set('correlationId', params.correlationId)
  if (params.fromDateUtc) query.set('fromDateUtc', params.fromDateUtc)
  if (params.toDateUtc) query.set('toDateUtc', params.toDateUtc)

  const queryString = query.toString()
  const url = `${AUDIT_LOGS_URL}/export-csv${queryString ? `?${queryString}` : ''}`
  const response = await authenticatedFetch(url)
  if (!response.ok) {
    throw new Error('Không thể xuất file nhật ký kiểm toán CSV.')
  }
  return response.blob()
}
