import { useEffect, useState } from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

type Status = 'checking' | 'ok' | 'unavailable'

type Health = { api: Status; database: Status }

// The API answers 503 with a body when only the database is down.
async function fetchHealth(signal: AbortSignal): Promise<Health> {
  try {
    const response = await fetch('/api/health', { signal })
    const body = (await response.json()) as { database?: string }
    return { api: 'ok', database: body.database === 'ok' ? 'ok' : 'unavailable' }
  } catch (error) {
    if (signal.aborted) throw error
    return { api: 'unavailable', database: 'unavailable' }
  }
}

export function HealthStatus() {
  const [health, setHealth] = useState<Health>({ api: 'checking', database: 'checking' })

  useEffect(() => {
    const controller = new AbortController()
    fetchHealth(controller.signal).then(setHealth, () => {})
    return () => controller.abort()
  }, [])

  return (
    <Card className="w-full max-w-sm">
      <CardHeader>
        <CardTitle>System status</CardTitle>
      </CardHeader>
      <CardContent>
        <dl className="space-y-2 text-sm">
          <StatusRow label="API" status={health.api} />
          <StatusRow label="Database" status={health.database} />
        </dl>
      </CardContent>
    </Card>
  )
}

const statusColor: Record<Status, string> = {
  checking: 'text-muted-foreground',
  ok: 'text-green-700',
  unavailable: 'text-destructive',
}

function StatusRow({ label, status }: { label: string; status: Status }) {
  return (
    <div className="flex justify-between">
      <dt>{label}</dt>
      <dd className={statusColor[status]}>{status}</dd>
    </div>
  )
}
