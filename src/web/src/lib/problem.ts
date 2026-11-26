// Reads an RFC 9457 ProblemDetails error from an RTK Query error.
export type Problem = {
  status?: number | string
  code?: string
  // First message per field, keyed by the camelCase field name.
  fieldErrors: Record<string, string>
}

type ProblemBody = { code?: unknown; errors?: Record<string, unknown> }

export function parseProblem(error: unknown): Problem {
  if (!error || typeof error !== 'object' || !('status' in error)) return { fieldErrors: {} }

  const { status, data } = error as { status: number | string; data?: unknown }
  const body = (data && typeof data === 'object' ? data : {}) as ProblemBody

  const fieldErrors: Record<string, string> = {}
  for (const [field, messages] of Object.entries(body.errors ?? {})) {
    if (Array.isArray(messages) && typeof messages[0] === 'string') fieldErrors[field] = messages[0]
  }

  return { status, code: typeof body.code === 'string' ? body.code : undefined, fieldErrors }
}
