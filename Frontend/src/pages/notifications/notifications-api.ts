import { authenticatedFetch } from '@/auth/auth-api'

const TEMPLATES_URL = '/api/v1/notification-templates'
const NOTIFICATIONS_URL = '/api/v1/notifications'

export type NotificationChannel = 'Email' | 'Sms' | 'InApp'
export type RecipientType = 'Staff' | 'Member'
export type NotificationStatus = 'Pending' | 'Sent' | 'Failed' | 'Cancelled'

export type NotificationTemplate = {
  id: string
  code: string
  name: string
  channel: NotificationChannel
  subjectTemplate?: string | null
  bodyTemplate: string
  allowedVariables?: string | null
  isActive: boolean
  updatedAtUtc: string
  concurrencyToken: string
}

export type CreateNotificationTemplatePayload = {
  code: string
  name: string
  channel: NotificationChannel
  subjectTemplate?: string | null
  bodyTemplate: string
  allowedVariables?: string | null
  isActive?: boolean
}

export type UpdateNotificationTemplatePayload = {
  name: string
  channel: NotificationChannel
  subjectTemplate?: string | null
  bodyTemplate: string
  allowedVariables?: string | null
  isActive: boolean
  concurrencyToken: string
}

export type NotificationPreviewRequest = {
  templateCode: string
  variables?: Record<string, string>
}

export type NotificationPreviewResult = {
  templateCode: string
  channel: string
  renderedSubject?: string | null
  renderedBody: string
}

export type SendNotificationPayload = {
  templateCode: string
  recipientType: RecipientType
  recipientId: string
  destination?: string | null
  variables?: Record<string, string>
}

export type NotificationItem = {
  id: string
  templateId: string
  templateCode: string
  templateName: string
  channel: NotificationChannel
  recipientType: RecipientType
  recipientId: string
  recipientName: string
  destination: string
  subject?: string | null
  body: string
  status: NotificationStatus
  scheduledAtUtc?: string | null
  sentAtUtc?: string | null
  failureReason?: string | null
  readAtUtc?: string | null
  isRead: boolean
}

export type NotificationPageResult = {
  items: NotificationItem[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export type NotificationRecipient = {
  id: string
  recipientType: RecipientType
  code: string
  name: string
  email?: string | null
  phoneNumber?: string | null
}

export type NotificationUnreadCountResult = {
  unreadCount: number
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

  const message = validationMessage ?? problem?.detail ?? problem?.title ?? 'Không thể xử lý yêu cầu thông báo.'
  throw new ApiError(message, response.status)
}

// Templates API
export async function fetchTemplates(signal?: AbortSignal): Promise<NotificationTemplate[]> {
  const res = await authenticatedFetch(TEMPLATES_URL, { signal })
  return readResponse<NotificationTemplate[]>(res)
}

export async function createTemplate(
  payload: CreateNotificationTemplatePayload,
  signal?: AbortSignal,
): Promise<NotificationTemplate> {
  const res = await authenticatedFetch(TEMPLATES_URL, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
    signal,
  })
  return readResponse<NotificationTemplate>(res)
}

export async function updateTemplate(
  id: string,
  payload: UpdateNotificationTemplatePayload,
  signal?: AbortSignal,
): Promise<NotificationTemplate> {
  const res = await authenticatedFetch(`${TEMPLATES_URL}/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
    signal,
  })
  return readResponse<NotificationTemplate>(res)
}

export async function deleteTemplate(id: string, signal?: AbortSignal): Promise<void> {
  const res = await authenticatedFetch(`${TEMPLATES_URL}/${id}`, {
    method: 'DELETE',
    signal,
  })
  return readResponse<void>(res)
}

// Operational Notifications API
export async function previewNotification(
  payload: NotificationPreviewRequest,
  signal?: AbortSignal,
): Promise<NotificationPreviewResult> {
  const res = await authenticatedFetch(`${NOTIFICATIONS_URL}/preview`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
    signal,
  })
  return readResponse<NotificationPreviewResult>(res)
}

export async function sendNotification(
  payload: SendNotificationPayload,
  signal?: AbortSignal,
): Promise<NotificationItem> {
  const res = await authenticatedFetch(`${NOTIFICATIONS_URL}/send`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
    signal,
  })
  return readResponse<NotificationItem>(res)
}

export async function retryNotification(id: string, signal?: AbortSignal): Promise<NotificationItem> {
  const res = await authenticatedFetch(`${NOTIFICATIONS_URL}/${id}/retry`, {
    method: 'POST',
    signal,
  })
  return readResponse<NotificationItem>(res)
}

export async function fetchNotificationHistory(
  params: {
    channel?: string
    status?: string
    fromDate?: string
    toDate?: string
    pageNumber?: number
    pageSize?: number
  } = {},
  signal?: AbortSignal,
): Promise<NotificationPageResult> {
  const query = new URLSearchParams()
  if (params.channel && params.channel !== 'all') query.set('channel', params.channel)
  if (params.status && params.status !== 'all') query.set('status', params.status)
  if (params.fromDate) query.set('fromDate', params.fromDate)
  if (params.toDate) query.set('toDate', params.toDate)
  if (params.pageNumber) query.set('pageNumber', params.pageNumber.toString())
  if (params.pageSize) query.set('pageSize', params.pageSize.toString())

  const url = `${NOTIFICATIONS_URL}/history?${query.toString()}`
  const res = await authenticatedFetch(url, { signal })
  return readResponse<NotificationPageResult>(res)
}

// Recipient / Internal Notifications API
export async function fetchMyNotifications(
  params: { unreadOnly?: boolean; pageNumber?: number; pageSize?: number } = {},
  signal?: AbortSignal,
): Promise<NotificationPageResult> {
  const query = new URLSearchParams()
  if (params.unreadOnly !== undefined) query.set('unreadOnly', String(params.unreadOnly))
  if (params.pageNumber) query.set('pageNumber', params.pageNumber.toString())
  if (params.pageSize) query.set('pageSize', params.pageSize.toString())

  const url = `${NOTIFICATIONS_URL}/my?${query.toString()}`
  const res = await authenticatedFetch(url, { signal })
  return readResponse<NotificationPageResult>(res)
}

export async function fetchUnreadCount(signal?: AbortSignal): Promise<number> {
  const res = await authenticatedFetch(`${NOTIFICATIONS_URL}/unread-count`, { signal })
  const data = await readResponse<NotificationUnreadCountResult>(res)
  return data.unreadCount
}

export async function markNotificationRead(id: string, signal?: AbortSignal): Promise<void> {
  const res = await authenticatedFetch(`${NOTIFICATIONS_URL}/${id}/read`, {
    method: 'PUT',
    signal,
  })
  return readResponse<void>(res)
}

export async function markAllNotificationsRead(signal?: AbortSignal): Promise<void> {
  const res = await authenticatedFetch(`${NOTIFICATIONS_URL}/read-all`, {
    method: 'PUT',
    signal,
  })
  return readResponse<void>(res)
}

export async function searchRecipients(
  type: RecipientType,
  keyword?: string,
  signal?: AbortSignal,
): Promise<NotificationRecipient[]> {
  const query = new URLSearchParams({ type })
  if (keyword) query.set('keyword', keyword)

  const url = `${NOTIFICATIONS_URL}/recipients?${query.toString()}`
  const res = await authenticatedFetch(url, { signal })
  return readResponse<NotificationRecipient[]>(res)
}
