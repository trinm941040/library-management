import { z } from 'zod'
import { authenticatedFetch, guidSchema, readResponse } from '@/auth/auth-api'

const auditLogSchema = z.object({
  id: guidSchema,
  actorUserId: guidSchema.nullable(),
  actorName: z.string().nullable(),
  action: z.string(),
  entityType: z.string(),
  entityId: guidSchema,
  beforeJson: z.string().nullable(),
  afterJson: z.string().nullable(),
  createdAtUtc: z.string(),
  correlationId: z.string().nullable(),
  ipAddress: z.string().nullable(),
})

const auditLogPageSchema = z.object({
  items: z.array(auditLogSchema),
  pageNumber: z.number().int().positive(),
  pageSize: z.number().int().positive(),
  totalCount: z.number().int().nonnegative(),
  totalPages: z.number().int().nonnegative(),
})

export type AuditLog = z.infer<typeof auditLogSchema>
export type AuditLogPage = z.infer<typeof auditLogPageSchema>

export type AuditLogFilters = {
  actorUserId?: string
  action?: string
  entityType?: string
  entityId?: string
  correlationId?: string
  ipAddress?: string
  fromUtc?: string
  toUtc?: string
  pageNumber: number
  pageSize: number
}

function queryString(filters: AuditLogFilters) {
  const query = new URLSearchParams()
  Object.entries(filters).forEach(([key, value]) => {
    if (value !== undefined && value !== '') query.set(key, String(value))
  })
  return query.toString()
}

export async function getAuditLogs(filters: AuditLogFilters, signal?: AbortSignal) {
  return readResponse(
    await authenticatedFetch(`/api/v1/audit-log?${queryString(filters)}`, { signal }),
    auditLogPageSchema,
  )
}

export async function getAuditLog(id: string, signal?: AbortSignal) {
  return readResponse(
    await authenticatedFetch(`/api/v1/audit-log/${id}`, { signal }),
    auditLogSchema,
  )
}
