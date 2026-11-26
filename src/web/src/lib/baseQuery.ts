import type { Dispatch } from '@reduxjs/toolkit'
import { type BaseQueryFn, type FetchArgs, fetchBaseQuery, type FetchBaseQueryError } from '@reduxjs/toolkit/query/react'
import { type AuthState, loggedOut, sessionStarted } from '@/features/auth/authSlice'
import type { AuthResponse } from '@/features/auth/types'

// Absolute, so it also works under Node in tests. Same origin in the browser.
export const apiBaseUrl = () => `${window.location.origin}/api`

const accessToken = (state: unknown) => (state as { auth: AuthState }).auth.accessToken

const rawBaseQuery = fetchBaseQuery({
  baseUrl: apiBaseUrl(),
  credentials: 'include',
  prepareHeaders: (headers, { getState }) => {
    const token = accessToken(getState())
    if (token) headers.set('Authorization', `Bearer ${token}`)
    return headers
  },
})

let inFlight: Promise<boolean> | null = null

// Gets a new access token with the refresh cookie. Concurrent callers in this tab share one
// request, and Web Locks make tabs take turns, because a refresh token works only once.
export function refreshOnce(dispatch: Dispatch): Promise<boolean> {
  inFlight ??= withRefreshLock(() => refresh(dispatch)).finally(() => {
    inFlight = null
  })
  return inFlight
}

async function refresh(dispatch: Dispatch): Promise<boolean> {
  try {
    const response = await fetch(`${apiBaseUrl()}/auth/refresh`, { method: 'POST', credentials: 'include' })
    if (response.ok) {
      dispatch(sessionStarted((await response.json()) as AuthResponse))
      return true
    }
  } catch {
    // Network error: treat as logged out.
  }
  dispatch(loggedOut())
  return false
}

function withRefreshLock<T>(fn: () => Promise<T>): Promise<T> {
  if (typeof navigator !== 'undefined' && navigator.locks) {
    return navigator.locks.request('ob-refresh', fn)
  }
  return fn()
}

// These answer 401 for reasons a refresh can't fix.
const noReauth = ['/auth/login', '/auth/register', '/auth/refresh', '/auth/logout']

// On a 401, refresh once and retry the request once. If the refresh fails, the user is logged out.
export const baseQueryWithReauth: BaseQueryFn<string | FetchArgs, unknown, FetchBaseQueryError> = async (
  args,
  api,
  extraOptions,
) => {
  const tokenUsed = accessToken(api.getState())
  const result = await rawBaseQuery(args, api, extraOptions)

  const url = typeof args === 'string' ? args : args.url
  if (result.error?.status !== 401 || noReauth.includes(url)) return result

  // Another request may already have refreshed while this one was in flight.
  const current = accessToken(api.getState())
  const refreshed = (current !== null && current !== tokenUsed) || (await refreshOnce(api.dispatch))
  return refreshed ? rawBaseQuery(args, api, extraOptions) : result
}
