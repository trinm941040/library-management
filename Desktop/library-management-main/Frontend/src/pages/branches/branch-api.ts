import { authenticatedFetch } from '@/auth/auth-api'

const LOCATIONS_URL = '/api/v1/locations'
export type LocationNode = {
  id: string
  code: string
  name: string
  address: string | null
  isActive: boolean
  branchId: string | null
  areaId: string | null
  status: 'Active' | 'Inactive' | null
  children?: LocationNode[] | null
}

type Problem = { detail?: string; title?: string }
async function read<T>(response: Response): Promise<T> {
  if (response.ok) return response.status === 204 ? (undefined as T) : response.json()
  const problem = (await response.json().catch(() => null)) as Problem | null
  throw new Error(problem?.detail ?? problem?.title ?? 'Không thể xử lý vị trí.')
}

export async function getLocations(signal?: AbortSignal) {
  return read<LocationNode[]>(await authenticatedFetch(LOCATIONS_URL, { signal }))
}
export async function deactivateLocation(type: 'branches' | 'areas' | 'shelves', id: string) {
  return read<void>(await authenticatedFetch(`${LOCATIONS_URL}/${type}/${id}`, { method: 'DELETE' }))
}
