import { FormEvent, useCallback, useEffect, useMemo, useState } from 'react'
import { acknowledgeIncident, listIncidents, recordEvent } from './api'
import type { FireEventInput, Incident, TenantCredentials } from './types'
import './styles.css'

const emptyEvent: FireEventInput = {
  deviceId: '',
  eventType: 'Alarm',
  message: '',
}

export default function App() {
  const [credentials, setCredentials] = useState<TenantCredentials>({ tenantId: '', apiKey: '' })
  const [connectedCredentials, setConnectedCredentials] = useState<TenantCredentials | null>(null)
  const [incidents, setIncidents] = useState<Incident[]>([])
  const [eventInput, setEventInput] = useState<FireEventInput>(emptyEvent)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  const refresh = useCallback(async (activeCredentials: TenantCredentials) => {
    setBusy(true)
    setError('')
    try {
      setIncidents(await listIncidents(activeCredentials))
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : 'Unable to load incidents.')
    } finally {
      setBusy(false)
    }
  }, [])

  useEffect(() => {
    if (connectedCredentials) void refresh(connectedCredentials)
  }, [connectedCredentials, refresh])

  const activeCount = useMemo(
    () => incidents.filter((incident) => incident.status !== 'Resolved').length,
    [incidents],
  )

  const connect = (event: FormEvent) => {
    event.preventDefault()
    if (!credentials.tenantId.trim() || !credentials.apiKey.trim()) {
      setError('Tenant ID and API key are required.')
      return
    }
    setConnectedCredentials({ ...credentials })
  }

  const submitEvent = async (event: FormEvent) => {
    event.preventDefault()
    if (!connectedCredentials) return
    setBusy(true)
    setError('')
    try {
      await recordEvent(connectedCredentials, eventInput)
      setEventInput(emptyEvent)
      await refresh(connectedCredentials)
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : 'Unable to record event.')
      setBusy(false)
    }
  }

  const acknowledge = async (incidentId: string) => {
    if (!connectedCredentials) return
    setBusy(true)
    setError('')
    try {
      await acknowledgeIncident(connectedCredentials, incidentId)
      await refresh(connectedCredentials)
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : 'Unable to acknowledge incident.')
      setBusy(false)
    }
  }

  return (
    <main>
      <header className="hero">
        <p className="eyebrow">Operational monitoring</p>
        <h1>Fire System Event Monitor</h1>
        <p>Review device events and manage tenant-isolated incident state.</p>
      </header>

      <section className="panel" aria-labelledby="connection-heading">
        <div>
          <p className="eyebrow">Tenant access</p>
          <h2 id="connection-heading">Connect to a workspace</h2>
        </div>
        <form className="credentials" onSubmit={connect}>
          <label>Tenant ID<input aria-label="Tenant ID" value={credentials.tenantId} onChange={(e) => setCredentials({ ...credentials, tenantId: e.target.value })} /></label>
          <label>API key<input aria-label="API key" type="password" value={credentials.apiKey} onChange={(e) => setCredentials({ ...credentials, apiKey: e.target.value })} /></label>
          <button type="submit">Connect</button>
        </form>
      </section>

      {error && <div className="error" role="alert">{error}</div>}

      {connectedCredentials && (
        <>
          <section className="metrics" aria-label="Incident summary">
            <article><span>Workspace</span><strong>{connectedCredentials.tenantId}</strong></article>
            <article><span>Active incidents</span><strong>{activeCount}</strong></article>
            <article><span>Total incidents</span><strong>{incidents.length}</strong></article>
          </section>

          <section className="workspace">
            <div className="panel">
              <p className="eyebrow">Ingestion</p>
              <h2>Record device event</h2>
              <form className="event-form" onSubmit={submitEvent}>
                <label>Device ID<input required minLength={2} value={eventInput.deviceId} onChange={(e) => setEventInput({ ...eventInput, deviceId: e.target.value })} /></label>
                <label>Event type<select value={eventInput.eventType} onChange={(e) => setEventInput({ ...eventInput, eventType: e.target.value as FireEventInput['eventType'] })}><option>Alarm</option><option>Fault</option><option>Warning</option><option>Restored</option></select></label>
                <label>Message<textarea required minLength={2} value={eventInput.message} onChange={(e) => setEventInput({ ...eventInput, message: e.target.value })} /></label>
                <button disabled={busy} type="submit">Record event</button>
              </form>
            </div>

            <div className="panel incidents">
              <div className="section-heading">
                <div><p className="eyebrow">Incident queue</p><h2>Latest state</h2></div>
                <button className="secondary" disabled={busy} onClick={() => void refresh(connectedCredentials)}>Refresh</button>
              </div>
              {busy && incidents.length === 0 ? <p>Loading incidents...</p> : incidents.length === 0 ? <p className="empty">No incidents recorded for this tenant.</p> : (
                <ul>
                  {incidents.map((incident) => (
                    <li key={incident.id}>
                      <div className="incident-title"><strong>{incident.deviceId}</strong><span className={`severity ${incident.severity.toLowerCase()}`}>{incident.severity}</span></div>
                      <p>{incident.summary}</p>
                      <div className="incident-meta"><span>{incident.status}</span><span>{incident.eventCount} event{incident.eventCount === 1 ? '' : 's'}</span></div>
                      {incident.status === 'Open' && <button className="secondary" disabled={busy} onClick={() => void acknowledge(incident.id)}>Acknowledge</button>}
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </section>
        </>
      )}
    </main>
  )
}

