import { describe, expect, it } from 'vitest'
import { initials } from './initials'
import { parseProblem } from './problem'
import { safeReturnTo } from './returnTo'

describe('safeReturnTo', () => {
  it.each([
    ['/profile', '/profile'],
    ['/rooms/1?tab=offers', '/rooms/1?tab=offers'],
    ['//evil.com', '/'],
    ['/\\evil.com', '/'],
    ['https://evil.com', '/'],
    ['profile', '/'],
    [null, '/'],
    ['', '/'],
  ])('%s → %s', (value, expected) => expect(safeReturnTo(value)).toBe(expected))
})

describe('initials', () => {
  it.each([
    ['Ada Lovelace', 'AL'],
    ['ada', 'A'],
    ['  grace  brewster hopper ', 'GB'],
    ['Åse Ødegård', 'ÅØ'],
  ])('%s → %s', (name, expected) => expect(initials(name)).toBe(expected))
})

describe('parseProblem', () => {
  it('reads the status, code and first message per field', () => {
    const problem = parseProblem({
      status: 400,
      data: { code: 'x', errors: { email: ['Bad email.', 'Second.'], password: ['Too short.'] } },
    })

    expect(problem).toEqual({ status: 400, code: 'x', fieldErrors: { email: 'Bad email.', password: 'Too short.' } })
  })

  it('copes with errors that are not ProblemDetails', () => {
    expect(parseProblem({ status: 'FETCH_ERROR', error: 'offline' })).toEqual({ status: 'FETCH_ERROR', code: undefined, fieldErrors: {} })
    expect(parseProblem(new Error('boom'))).toEqual({ fieldErrors: {} })
  })
})
