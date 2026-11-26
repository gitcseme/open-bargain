import { Navigate, Outlet, useLocation } from 'react-router'
import { useAppSelector } from '@/app/store'

// Guests are sent to /login, which brings them back here afterwards.
export function RequireAuth() {
  const status = useAppSelector((state) => state.auth.status)
  const location = useLocation()

  if (status !== 'authenticated') {
    const returnTo = encodeURIComponent(location.pathname + location.search)
    return <Navigate to={`/login?returnTo=${returnTo}`} replace />
  }
  return <Outlet />
}
