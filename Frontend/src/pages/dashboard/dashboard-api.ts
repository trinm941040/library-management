import { authenticatedFetch } from '@/auth/auth-api'

const DASHBOARD_URL = '/api/v1/dashboard'

export type KpiMetrics = {
  totalBooks: number
  totalCopies: number
  availableCopies: number
  activeBorrowings: number
  returnedInPeriod: number
  activeMembers: number
  activeReservations: number
  outstandingFineBalance: number
  collectedFineInPeriod: number
}

export type OverdueAlertItem = {
  borrowingId: string
  borrowerName: string
  borrowerEmail: string
  bookTitle: string
  copyBarcode?: string | null
  dueAtUtc: string
  overdueDays: number
}

export type ExpiringReservationAlertItem = {
  reservationId: string
  reserverName: string
  reserverEmail: string
  bookTitle: string
  expiresAtUtc: string
  remainingHours: number
}

export type DamagedOrLostCopyAlertItem = {
  bookCopyId: string
  barcode: string
  bookTitle: string
  condition: string
  status: string
}

export type InventoryDiscrepancyAlertItem = {
  auditItemId: string
  auditId: string
  bookTitle: string
  barcode: string
  result: string
  scannedAtUtc?: string | null
}

export type OperationalAlerts = {
  overdueLoansCount: number
  overdueLoans: OverdueAlertItem[]
  expiringReservationsCount: number
  expiringReservations: ExpiringReservationAlertItem[]
  damagedOrLostCopiesCount: number
  damagedOrLostCopies: DamagedOrLostCopyAlertItem[]
  inventoryDiscrepanciesCount: number
  inventoryDiscrepancies: InventoryDiscrepancyAlertItem[]
  totalAlertCount: number
}

export type DashboardActivityItem = {
  id: string
  action: string
  entityType: string
  entityId?: string | null
  actorName: string
  timestampUtc: string
  details?: string | null
}

export type DashboardBranchItem = {
  id: string
  code: string
  name: string
}

export type DashboardSummaryResponse = {
  kpis: KpiMetrics
  alerts: OperationalAlerts
  recentActivities: DashboardActivityItem[]
  availableBranches: DashboardBranchItem[]
  selectedBranchId?: string | null
  selectedBranchName?: string | null
  timeRange: string
  generatedAtUtc: string
  clientTimezoneOffsetMinutes: number
}

export type DashboardSummaryFilter = {
  range?: string
  branchId?: string
  timezoneOffsetMinutes?: number
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

  const message = validationMessage ?? problem?.detail ?? problem?.title ?? 'Không thể tải dữ liệu bảng điều khiển.'
  throw new ApiError(message, response.status)
}

export async function fetchDashboardSummary(
  filter?: DashboardSummaryFilter,
  signal?: AbortSignal,
): Promise<DashboardSummaryResponse> {
  const params = new URLSearchParams()
  if (filter?.range) params.set('range', filter.range)
  if (filter?.branchId) params.set('branchId', filter.branchId)
  const offset = filter?.timezoneOffsetMinutes ?? new Date().getTimezoneOffset()
  params.set('timezoneOffsetMinutes', offset.toString())

  const response = await authenticatedFetch(`${DASHBOARD_URL}/summary?${params.toString()}`, {
    signal,
  })

  return readResponse<DashboardSummaryResponse>(response)
}

export async function fetchDashboardBranches(signal?: AbortSignal): Promise<DashboardBranchItem[]> {
  const response = await authenticatedFetch(`${DASHBOARD_URL}/branches`, { signal })
  return readResponse<DashboardBranchItem[]>(response)
}
