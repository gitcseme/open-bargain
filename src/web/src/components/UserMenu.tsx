import { Link, useNavigate } from 'react-router'
import { useAppDispatch } from '@/app/store'
import { UserInitials } from '@/components/UserInitials'
import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { api, useLogoutMutation } from '@/features/auth/api'
import { loggedOut } from '@/features/auth/authSlice'
import type { UserResponse } from '@/features/auth/types'

export function UserMenu({ user }: { user: UserResponse }) {
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const [logout] = useLogoutMutation()

  // Clears the session even if the API call fails.
  async function logOut() {
    try {
      await logout().unwrap()
    } catch {
      // Nothing to do: the local session ends either way.
    }
    dispatch(loggedOut())
    dispatch(api.util.resetApiState())
    navigate('/')
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon-lg" className="rounded-full" aria-label="Account menu">
          <UserInitials displayName={user.displayName} />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="min-w-48">
        <DropdownMenuLabel className="truncate">{user.displayName}</DropdownMenuLabel>
        <DropdownMenuSeparator />
        <DropdownMenuItem asChild>
          <Link to="/profile">Profile</Link>
        </DropdownMenuItem>
        <DropdownMenuItem onSelect={() => void logOut()}>Log out</DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
