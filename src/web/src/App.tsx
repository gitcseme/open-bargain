import { HealthStatus } from '@/HealthStatus'

export default function App() {
  return (
    <main className="flex min-h-svh flex-col items-center gap-6 px-4 py-12">
      <h1 className="text-3xl font-semibold tracking-tight">Open Bargain</h1>
      <HealthStatus />
    </main>
  )
}
