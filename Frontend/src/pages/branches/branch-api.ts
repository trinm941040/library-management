import { z } from 'zod'
import { authenticatedFetch, guidSchema, readResponse } from '@/auth/auth-api'

const URL = '/api/v1/locations'

const readinessSchema = z.object({
  canActivate: z.boolean(),
  activeAreaCount: z.number().int().nonnegative(),
  activeShelfCount: z.number().int().nonnegative(),
  missingRequirements: z.array(z.string()),
})

export type LocationType = 'Branch' | 'Area' | 'Shelf'
export type LocationNode = {
  id: string
  type: LocationType
  code: string
  name: string
  address: string | null
  isActive: boolean
  parentId: string | null
  concurrencyToken: string
  readiness: z.infer<typeof readinessSchema> | null
  children: LocationNode[]
}

const locationSchema: z.ZodType<LocationNode> = z.lazy(() =>
  z.object({
    id: guidSchema,
    type: z.enum(['Branch', 'Area', 'Shelf']),
    code: z.string(),
    name: z.string(),
    address: z.string().nullable(),
    isActive: z.boolean(),
    parentId: guidSchema.nullable(),
    concurrencyToken: guidSchema,
    readiness: readinessSchema.nullable(),
    children: z.array(locationSchema),
  }),
)

const impactSchema = z.object({
  employeeCount: z.number().int().nonnegative(),
  bookCopyCount: z.number().int().nonnegative(),
  activeInventoryAuditCount: z.number().int().nonnegative(),
  editableStockReceiptCount: z.number().int().nonnegative(),
  hasBlockingReferences: z.boolean(),
})

export type LocationImpact = z.infer<typeof impactSchema>
export type LocationInput = {
  code: string
  name: string
  address?: string | null
  parentId?: string | null
  concurrencyToken?: string
}

const segment: Record<LocationType, string> = {
  Branch: 'branches',
  Area: 'areas',
  Shelf: 'shelves',
}

function body(type: LocationType, input: LocationInput) {
  if (type === 'Branch')
    return {
      code: input.code,
      name: input.name,
      address: input.address || null,
      concurrencyToken: input.concurrencyToken,
    }
  if (type === 'Area')
    return {
      branchId: input.parentId,
      code: input.code,
      name: input.name,
      concurrencyToken: input.concurrencyToken,
    }
  return {
    areaId: input.parentId,
    code: input.code,
    label: input.name,
    concurrencyToken: input.concurrencyToken,
  }
}

export async function getLocations(signal?: AbortSignal) {
  return readResponse(
    await authenticatedFetch(`${URL}?includeInactive=true`, { signal }),
    z.array(locationSchema),
  )
}

export async function getLocationImpact(type: LocationType, id: string, signal?: AbortSignal) {
  return readResponse(
    await authenticatedFetch(`${URL}/${type}/${id}/impact`, { signal }),
    impactSchema,
  )
}

export async function createLocation(type: LocationType, input: LocationInput) {
  return readResponse(
    await authenticatedFetch(`${URL}/${segment[type]}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body(type, input)),
    }),
    locationSchema,
  )
}

export async function updateLocation(type: LocationType, id: string, input: LocationInput) {
  return readResponse(
    await authenticatedFetch(`${URL}/${segment[type]}/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body(type, input)),
    }),
    locationSchema,
  )
}

export async function activateLocation(location: LocationNode) {
  return readResponse(
    await authenticatedFetch(`${URL}/${segment[location.type]}/${location.id}/activate`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ concurrencyToken: location.concurrencyToken }),
    }),
    locationSchema,
  )
}

export async function deactivateLocation(location: LocationNode) {
  return readResponse(
    await authenticatedFetch(`${URL}/${segment[location.type]}/${location.id}`, {
      method: 'DELETE',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ concurrencyToken: location.concurrencyToken }),
    }),
    locationSchema,
  )
}
