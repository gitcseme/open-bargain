import { http, HttpResponse } from 'msw'
import { setupServer } from 'msw/node'
import type { AuthResponse, UserResponse } from '@/features/auth/types'

export const testUser: UserResponse = {
  id: '0193a000-0000-7000-8000-000000000001',
  email: 'ada@example.com',
  displayName: 'Ada Lovelace',
  avatarUrl: null,
  role: 'User',
}

export function authResponse(accessToken = 'token-1', user: UserResponse = testUser): AuthResponse {
  return { accessToken, expiresAt: '2026-11-22T12:15:00Z', user }
}

export function problem(status: number, code?: string, errors?: Record<string, string[]>) {
  return HttpResponse.json({ status, title: 'Problem', code, errors }, { status })
}

// Defaults: nobody is signed in, and the API is healthy.
export const server = setupServer(
  http.post('*/api/auth/refresh', () => problem(401, 'invalid_refresh_token')),
  http.get('*/api/health', () => HttpResponse.json({ status: 'ok', database: 'ok' })),
)
