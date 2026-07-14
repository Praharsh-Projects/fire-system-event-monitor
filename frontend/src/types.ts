export type Incident = {
  id: string
  deviceId: string
  status: 'Open' | 'Acknowledged' | 'Resolved'
  severity: 'Critical' | 'High' | 'Medium' | 'Low'
  summary: string
  openedAt: string
  updatedAt: string
  eventCount: number
}

export type TenantCredentials = {
  tenantId: string
  apiKey: string
}

export type FireEventInput = {
  deviceId: string
  eventType: 'Alarm' | 'Fault' | 'Warning' | 'Restored'
  message: string
}

