import { Link } from 'react-router'
import { useAppSelector } from '@/app/store'
import { UserMenu } from '@/components/UserMenu'
import { Button } from '@/components/ui/button'

export function AppHeader() {
  const user = useAppSelector((state) => state.auth.user)

  return (
    <header className="border-b">
      <div className="mx-auto flex h-14 max-w-5xl items-center justify-between gap-4 px-4">
        <Link to="/" className="text-lg font-semibold tracking-tight">
          Open Bargain
        </Link>
        <nav className="flex items-center gap-2">
          {user ? (
            <UserMenu user={user} />
          ) : (
            <>
              <Button variant="ghost" asChild>
                <Link to="/login">Log in</Link>
              </Button>
              <Button asChild>
                <Link to="/register">Sign up</Link>
              </Button>
            </>
          )}
        </nav>
      </div>
    </header>
  )
}
