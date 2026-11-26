import { Navigate, Outlet, useSearchParams } from 'react-router'
import { useAppSelector } from '@/app/store'
import { safeReturnTo } from '@/lib/returnTo'

// Login and sign-up pages. Once signed in (including right after a successful login here),
// the user goes to returnTo, or home.
export function GuestOnly() {
  const status = useAppSelector((state) => state.auth.status)
  const [params] = useSearchParams()

  if (status === 'authenticated') {
    return <Navigate to={safeReturnTo(params.get('returnTo'))} replace />
  }
  return <Outlet />
}
