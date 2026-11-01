import { render, screen, within } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { HealthStatus } from '@/HealthStatus'

function statusOf(label: string) {
  const row = screen.getByText(label).parentElement!
  return within(row).getByRole('definition')
}

describe('HealthStatus', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('shows API and database ok when the API is healthy', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(
      Response.json({ status: 'ok', database: 'ok' })))

    render(<HealthStatus />)

    expect(await screen.findAllByText('ok')).toHaveLength(2)
  })

  it('shows the database as unavailable when the API reports it down', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(
      Response.json({ status: 'degraded', database: 'unavailable' }, { status: 503 })))

    render(<HealthStatus />)

    expect(await screen.findByText('unavailable')).toBeInTheDocument()
    expect(statusOf('API')).toHaveTextContent('ok')
    expect(statusOf('Database')).toHaveTextContent('unavailable')
  })

  it('shows both as unavailable when the API cannot be reached', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')))

    render(<HealthStatus />)

    expect(await screen.findAllByText('unavailable')).toHaveLength(2)
  })
})
