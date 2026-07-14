import type { FireEventInput, Incident, TenantCredentials } from './types'

const requestHeaders = (credentials: TenantCredentials): HeadersInit => ({
  'Content-Type': 'application/json',
  'X-Tenant-Id': credentials.tenantId,
  'X-Api-Key': credentials.apiKey,
})

const ensureSuccess = async (response: Response): Promise<Response> => {
  if (response.ok) return response
  const problem = await response.json().catch(() => null) as { error?: string; title?: string } | null
  throw new Error(problem?.error ?? problem?.title ?? `Request failed with status ${response.status}`)
}

export const listIncidents = async (credentials: TenantCredentials): Promise<Incident[]> => {
  const response = await ensureSuccess(await fetch('/api/incidents', {
    headers: requestHeaders(credentials),
  }))
  return response.json() as Promise<Incident[]>
}

export const recordEvent = async (
  credentials: TenantCredentials,
  input: FireEventInput,
): Promise<void> => {
  await ensureSuccess(await fetch('/api/events', {
    method: 'POST',
    headers: requestHeaders(credentials),
    body: JSON.stringify(input),
  }))
}

export const acknowledgeIncident = async (
  credentials: TenantCredentials,
  incidentId: string,
): Promise<void> => {
  await ensureSuccess(await fetch(`/api/incidents/${incidentId}/acknowledge`, {
    method: 'POST',
    headers: requestHeaders(credentials),
  }))
}

