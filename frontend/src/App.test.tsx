import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import App from './App'

describe('App', () => {
  afterEach(() => vi.restoreAllMocks())

  it('connects to a tenant and renders its incidents', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(JSON.stringify([{
      id: '34f37ea2-4e62-4f58-9b7a-6a1ae3abbd71',
      deviceId: 'panel-a-17',
      status: 'Open',
      severity: 'Critical',
      summary: 'Smoke detector activated',
      openedAt: '2026-07-14T08:00:00Z',
      updatedAt: '2026-07-14T08:00:00Z',
      eventCount: 1,
    }]), { status: 200, headers: { 'Content-Type': 'application/json' } }))

    render(<App />)
    fireEvent.change(screen.getByLabelText('Tenant ID'), { target: { value: 'alpha' } })
    fireEvent.change(screen.getByLabelText('API key'), { target: { value: 'alpha-key' } })
    fireEvent.click(screen.getByRole('button', { name: 'Connect' }))

    await waitFor(() => expect(screen.getByText('panel-a-17')).toBeInTheDocument())
    expect(screen.getByText('Smoke detector activated')).toBeInTheDocument()
    expect(screen.getByText('Critical')).toBeInTheDocument()
  })

  it('requires both tenant credentials before connecting', () => {
    render(<App />)
    fireEvent.click(screen.getByRole('button', { name: 'Connect' }))
    expect(screen.getByRole('alert')).toHaveTextContent('Tenant ID and API key are required.')
  })
})

