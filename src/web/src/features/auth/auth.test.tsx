import { fireEvent, screen, waitFor } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import { api } from '@/features/auth/api'
import { renderApp } from '@/test/renderApp'
import { authResponse, problem, server, testUser } from '@/test/server'

function fillIn(label: string, value: string) {
  fireEvent.change(screen.getByLabelText(label), { target: { value } })
}

async function waitForRestore() {
  await waitFor(() => expect(screen.queryByRole('status', { name: 'Loading' })).not.toBeInTheDocument())
}

// Refresh succeeds with the given tokens in turn (the first call is the session restore).
function refreshReturns(...tokens: string[]) {
  const calls = { count: 0 }
  server.use(
    http.post('*/api/auth/refresh', () => {
      const token = tokens[Math.min(calls.count, tokens.length - 1)]
      calls.count++
      return HttpResponse.json(authResponse(token))
    }),
  )
  return calls
}

describe('login', () => {
  it('logs in, goes to returnTo and shows the initials in the header', async () => {
    server.use(http.post('*/api/auth/login', () => HttpResponse.json(authResponse())))
    renderApp('/login?returnTo=%2Fprofile')
    await waitForRestore()

    fillIn('Email', testUser.email)
    fillIn('Password', 'correct horse')
    fireEvent.click(screen.getByRole('button', { name: 'Log in' }))

    expect(await screen.findByRole('heading', { name: 'Profile' })).toBeInTheDocument()
    expect(window.location.pathname).toBe('/profile')
    expect(screen.getByRole('button', { name: 'Account menu' })).toHaveTextContent('AL')
  })

  it('shows an error when the credentials are wrong', async () => {
    server.use(http.post('*/api/auth/login', () => problem(401, 'invalid_credentials')))
    renderApp('/login')
    await waitForRestore()

    fillIn('Email', testUser.email)
    fillIn('Password', 'wrong password')
    fireEvent.click(screen.getByRole('button', { name: 'Log in' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Email or password is incorrect.')
    expect(window.location.pathname).toBe('/login')
  })

  it('shows a message when rate limited', async () => {
    server.use(http.post('*/api/auth/login', () => problem(429, 'rate_limited')))
    renderApp('/login')
    await waitForRestore()

    fillIn('Email', testUser.email)
    fillIn('Password', 'whatever1')
    fireEvent.click(screen.getByRole('button', { name: 'Log in' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Too many attempts. Try again in a minute.')
  })

  it('ignores an unsafe returnTo and goes home', async () => {
    server.use(http.post('*/api/auth/login', () => HttpResponse.json(authResponse())))
    renderApp('/login?returnTo=%2F%2Fevil.com')
    await waitForRestore()

    fillIn('Email', testUser.email)
    fillIn('Password', 'correct horse')
    fireEvent.click(screen.getByRole('button', { name: 'Log in' }))

    expect(await screen.findByRole('heading', { name: 'Open Bargain' })).toBeInTheDocument()
    expect(window.location.pathname).toBe('/')
  })
})

describe('register', () => {
  async function submitRegistration() {
    renderApp('/register')
    await waitForRestore()
    fillIn('Email', testUser.email)
    fillIn('Display name', testUser.displayName)
    fillIn('Password', 'correct horse')
    fireEvent.click(screen.getByRole('button', { name: 'Sign up' }))
  }

  it('shows the email error from a 409 email_taken', async () => {
    server.use(http.post('*/api/auth/register', () => problem(409, 'email_taken')))

    await submitRegistration()

    expect(await screen.findByText('An account with this email already exists.')).toBeInTheDocument()
  })

  it('shows the display name error from a 409 display_name_taken', async () => {
    server.use(http.post('*/api/auth/register', () => problem(409, 'display_name_taken')))

    await submitRegistration()

    expect(await screen.findByText('This display name is taken.')).toBeInTheDocument()
  })

  it('logs in automatically after signing up and goes home', async () => {
    let loginBody: unknown
    server.use(
      http.post('*/api/auth/register', () => HttpResponse.json(testUser, { status: 201 })),
      http.post('*/api/auth/login', async ({ request }) => {
        loginBody = await request.json()
        return HttpResponse.json(authResponse())
      }),
    )

    await submitRegistration()

    expect(await screen.findByRole('button', { name: 'Account menu' })).toHaveTextContent('AL')
    expect(window.location.pathname).toBe('/')
    expect(loginBody).toEqual({ email: testUser.email, password: 'correct horse' })
  })

  it('sends the user to log in when the automatic login fails', async () => {
    server.use(
      http.post('*/api/auth/register', () => HttpResponse.json(testUser, { status: 201 })),
      http.post('*/api/auth/login', () => problem(429, 'rate_limited')),
    )

    await submitRegistration()

    expect(await screen.findByText('Account created, please log in.')).toBeInTheDocument()
    expect(window.location.pathname).toBe('/login')
  })
})

describe('session restore', () => {
  it('signs the user in when the refresh succeeds', async () => {
    refreshReturns('token-1')

    renderApp('/')

    expect(await screen.findByRole('button', { name: 'Account menu' })).toHaveTextContent('AL')
  })

  it('shows the guest header when the refresh fails', async () => {
    renderApp('/')

    expect(await screen.findByRole('link', { name: 'Log in' })).toBeInTheDocument()
    expect(screen.queryByRole('status', { name: 'Loading' })).not.toBeInTheDocument()
  })
})

describe('refresh and retry', () => {
  // token-1 is expired on the server; token-2 is fresh.
  function meAcceptsOnly(token: string) {
    const isValid = (request: Request) => request.headers.get('Authorization') === `Bearer ${token}`
    server.use(
      http.get('*/api/me', ({ request }) => (isValid(request) ? HttpResponse.json(testUser) : problem(401))),
      http.put('*/api/me', ({ request }) => (isValid(request) ? HttpResponse.json(testUser) : problem(401))),
    )
  }

  it('refreshes once on a 401 and retries the request', async () => {
    const refreshes = refreshReturns('token-1', 'token-2')
    meAcceptsOnly('token-2')
    const { store } = renderApp('/')
    await screen.findByRole('button', { name: 'Account menu' })

    const result = await store.dispatch(api.endpoints.getMe.initiate())

    expect(result.data).toEqual(testUser)
    expect(refreshes.count).toBe(2) // the restore, then exactly one more
    expect(store.getState().auth.accessToken).toBe('token-2')
  })

  it('shares one refresh between parallel 401s', async () => {
    const refreshes = refreshReturns('token-1', 'token-2')
    meAcceptsOnly('token-2')
    const { store } = renderApp('/')
    await screen.findByRole('button', { name: 'Account menu' })

    const [me, update] = await Promise.all([
      store.dispatch(api.endpoints.getMe.initiate()),
      store.dispatch(api.endpoints.updateMe.initiate({ displayName: 'Ada' })),
    ])

    expect(me.data).toEqual(testUser)
    expect(update.data).toEqual(testUser)
    expect(refreshes.count).toBe(2)
  })

  it('logs the user out and sends them to log in when the refresh fails', async () => {
    const refreshes = refreshReturns('token-1')
    meAcceptsOnly('token-2')
    const { store } = renderApp('/profile')
    await screen.findByRole('heading', { name: 'Profile' })

    server.use(http.post('*/api/auth/refresh', () => problem(401, 'invalid_refresh_token')))
    await store.dispatch(api.endpoints.getMe.initiate())

    expect(await screen.findByRole('heading', { name: 'Log in' })).toBeInTheDocument()
    expect(window.location.pathname).toBe('/login')
    expect(store.getState().auth.status).toBe('guest')
    expect(refreshes.count).toBe(1)
  })
})

describe('route guard', () => {
  it('sends a guest from /profile to log in with returnTo', async () => {
    renderApp('/profile')

    expect(await screen.findByRole('heading', { name: 'Log in' })).toBeInTheDocument()
    expect(window.location.pathname).toBe('/login')
    expect(window.location.search).toBe('?returnTo=%2Fprofile')
  })

  it('sends a signed-in user away from /login', async () => {
    refreshReturns('token-1')

    renderApp('/login')

    expect(await screen.findByRole('heading', { name: 'Open Bargain' })).toBeInTheDocument()
    expect(window.location.pathname).toBe('/')
  })
})
