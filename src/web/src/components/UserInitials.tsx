import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { initials } from '@/lib/initials'

// Avatar upload arrives in M2; until then the avatar shows initials.
export function UserInitials({ displayName, size }: { displayName: string; size?: 'default' | 'sm' | 'lg' }) {
  return (
    <Avatar size={size}>
      <AvatarFallback>{initials(displayName)}</AvatarFallback>
    </Avatar>
  )
}
