import { z } from 'zod'
import { authenticatedFetch, guidSchema, readResponse } from '@/auth/auth-api'

export const settingTypeSchema = z.enum(['String', 'Number', 'Boolean', 'Json'])
export const settingScopeSchema = z.enum(['System', 'Notifications', 'Operations'])
const settingSchema = z.object({
  key: z.string(),
  displayName: z.string(),
  description: z.string(),
  valueType: settingTypeSchema,
  scope: settingScopeSchema,
  isSecret: z.boolean(),
  hasValue: z.boolean(),
  value: z.unknown().nullable(),
  defaultValue: z.unknown().nullable(),
  updatedByUserId: guidSchema.nullable(),
  updatedAtUtc: z.string().nullable(),
  concurrencyToken: guidSchema.nullable(),
})

const differenceSchema = z.object({
  key: z.string(),
  displayName: z.string(),
  scope: settingScopeSchema,
  valueType: settingTypeSchema,
  kind: z.enum(['Add', 'Change', 'Remove']),
  before: z.unknown().nullable(),
  after: z.unknown().nullable(),
})
const previewSchema = z.object({
  schemaVersion: z.string(),
  checksum: z.string().length(64),
  exportedAtUtc: z.string(),
  expiresAtUtc: z.string(),
  differences: z.array(differenceSchema),
  confirmationToken: z.string().min(1),
})
const importResultSchema = z.object({
  packageId: guidSchema,
  checksum: z.string().length(64),
  added: z.number().int().nonnegative(),
  changed: z.number().int().nonnegative(),
  removed: z.number().int().nonnegative(),
  appliedAtUtc: z.string(),
})

export type SystemSetting = z.infer<typeof settingSchema>
export type ConfigurationPreview = z.infer<typeof previewSchema>
export type ConfigurationDifference = z.infer<typeof differenceSchema>
export type ConfigurationImportResult = z.infer<typeof importResultSchema>

export async function getSettings(signal?: AbortSignal) {
  return readResponse(await authenticatedFetch('/api/v1/settings', { signal }), z.array(settingSchema))
}

export async function updateSetting(
  key: string,
  value: unknown,
  concurrencyToken: string | null,
) {
  return readResponse(
    await authenticatedFetch(`/api/v1/settings/${encodeURIComponent(key)}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ value, concurrencyToken }),
    }),
    settingSchema,
  )
}

export async function exportConfiguration() {
  const response = await authenticatedFetch('/api/v1/configuration/export', { method: 'POST' })
  if (!response.ok) await readResponse(response)
  const disposition = response.headers.get('content-disposition') ?? ''
  const fileName = /filename\*?=(?:UTF-8''|")?([^";]+)/i.exec(disposition)?.[1]
  return {
    blob: await response.blob(),
    fileName: fileName ? decodeURIComponent(fileName.replaceAll('"', '')) : 'uth-library-configuration.json',
  }
}

export async function validateConfiguration(file: File) {
  const body = new FormData()
  body.append('file', file)
  return readResponse(
    await authenticatedFetch('/api/v1/configuration/import/validate', { method: 'POST', body }),
    previewSchema,
  )
}

export async function confirmConfiguration(confirmationToken: string) {
  return readResponse(
    await authenticatedFetch('/api/v1/configuration/import/confirm', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ confirmationToken }),
    }),
    importResultSchema,
  )
}
