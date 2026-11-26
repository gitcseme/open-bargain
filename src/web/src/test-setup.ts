import '@testing-library/jest-dom/vitest'
import { cleanup } from '@testing-library/react'
import { afterAll, afterEach, beforeAll } from 'vitest'
import { server } from '@/test/server'

beforeAll(() => {
  server.listen({ onUnhandledFrame: 'error' })
  // jsdom has no Web Locks: run the callback straight away.
  Object.defineProperty(navigator, 'locks', {
    configurable: true,
    value: { request: (_name: string, callback: () => unknown) => callback() },
  })
})

afterEach(() => {
  cleanup()
  server.resetHandlers()
})

afterAll(() => server.close())
