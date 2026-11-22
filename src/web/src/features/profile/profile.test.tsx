import { fireEvent, screen } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import { renderApp } from '@/test/renderApp'
import { authResponse, problem, server, testUser } from '@/test/server'

async function openProfileSignedIn() {
  server.use(http.post('*/api/auth/refresh', () => HttpResponse.json(authResponse())))
  renderApp('/profile')
  await screen.findByRole('heading', { name: 'Profile' })
}

function saveDisplayName(value: string) {
  fireEvent.change(screen.getByLabelText('Display name'), { target: { value } })
  fireEvent.click(screen.getByRole('button', { name: 'Save' }))
}

describe('profile', () => {
  it('shows the email and current display name', async () => {
    await openProfileSignedIn()

    expect(screen.getByText(testUser.email)).toBeInTheDocument()
    expect(screen.getByLabelText('Display name')).toHaveValue(testUser.displayName)
  })

  it('saves the display name and updates the header', async () => {
    let body: unknown
    server.use(
      http.put('*/api/me', async ({ request }) => {
        body = await request.json()
        return HttpResponse.json({ ...testUser, displayName: 'Grace Hopper' })
      }),
    )
    await openProfileSignedIn()
    expect(screen.getByRole('button', { name: 'Account menu' })).toHaveTextContent('AL')

    saveDisplayName('Grace Hopper')

    expect(await screen.findByText('Saved')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Account menu' })).toHaveTextContent('GH')
    expect(body).toEqual({ displayName: 'Grace Hopper' })
  })

  it('shows a field error when the name is taken', async () => {
    server.use(http.put('*/api/me', () => problem(409, 'display_name_taken')))
    await openProfileSignedIn()

    saveDisplayName('Someone Else')

    expect(await screen.findByText('This display name is taken.')).toBeInTheDocument()
    expect(screen.queryByText('Saved')).not.toBeInTheDocument()
  })

  it('validates the name before sending it', async () => {
    await openProfileSignedIn()

    saveDisplayName(' x ')

    expect(await screen.findByText('Display name must be 2–40 characters.')).toBeInTheDocument()
  })
})
