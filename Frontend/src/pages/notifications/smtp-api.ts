import { z } from 'zod'
import { authenticatedFetch, readResponse } from '@/auth/auth-api'

const smtpSchema = z.object({
  enabled: z.boolean(), host: z.string(), port: z.number().int(),
  securityMode: z.enum(['None', 'StartTls', 'SslTls']),
  username: z.string().nullable(), hasPassword: z.boolean(),
  fromAddress: z.string(), fromName: z.string(), replyToAddress: z.string().nullable(),
  timeoutSeconds: z.number().int(), maxRetryCount: z.number().int(), batchSize: z.number().int(),
})
const testSchema = z.object({ success: z.boolean(), message: z.string() })
export type SmtpSettings = z.infer<typeof smtpSchema>

export async function getSmtpSettings(signal?: AbortSignal) {
  return readResponse(await authenticatedFetch('/api/v1/smtp', { signal }), smtpSchema)
}

export async function testSmtp(recipient: string, sendMessage: boolean) {
  return readResponse(await authenticatedFetch('/api/v1/smtp/test', {
    method: 'POST', headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ recipient, sendMessage }),
  }), testSchema)
}
