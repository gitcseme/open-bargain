import { useAppSelector } from '@/app/store'
import { PageCard } from '@/components/PageCard'

// The display-name form arrives in M1-6.
export function ProfilePage() {
  const user = useAppSelector((state) => state.auth.user)!

  return (
    <PageCard title="Profile">
      <p className="text-sm">{user.displayName}</p>
      <p className="text-sm text-muted-foreground">{user.email}</p>
    </PageCard>
  )
}
