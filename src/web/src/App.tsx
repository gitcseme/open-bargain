import { useEffect } from 'react'
import { BrowserRouter, Link, Route, Routes } from 'react-router'
import { useAppDispatch, useAppSelector } from '@/app/store'
import { AppHeader } from '@/components/AppHeader'
import { FullPageSpinner } from '@/components/FullPageSpinner'
import { GuestOnly } from '@/features/auth/GuestOnly'
import { LoginPage } from '@/features/auth/LoginPage'
import { RegisterPage } from '@/features/auth/RegisterPage'
import { RequireAuth } from '@/features/auth/RequireAuth'
import { ProfilePage } from '@/features/profile/ProfilePage'
import { HealthStatus } from '@/HealthStatus'
import { refreshOnce } from '@/lib/baseQuery'

export default function App() {
  const dispatch = useAppDispatch()
  const status = useAppSelector((state) => state.auth.status)

  // Restore the session from the refresh cookie. Shared with any concurrent refresh,
  // so StrictMode's double effect still sends one request.
  useEffect(() => {
    void refreshOnce(dispatch)
  }, [dispatch])

  if (status === 'restoring') return <FullPageSpinner />

  return (
    <BrowserRouter>
      <AppHeader />
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route element={<GuestOnly />}>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
        </Route>
        <Route element={<RequireAuth />}>
          <Route path="/profile" element={<ProfilePage />} />
        </Route>
        <Route path="*" element={<NotFoundPage />} />
      </Routes>
    </BrowserRouter>
  )
}

function HomePage() {
  return (
    <main className="flex flex-col items-center gap-6 px-4 py-12">
      <h1 className="text-3xl font-semibold tracking-tight">Open Bargain</h1>
      <HealthStatus />
    </main>
  )
}

function NotFoundPage() {
  return (
    <main className="flex flex-col items-center gap-4 px-4 py-12">
      <h1 className="text-2xl font-semibold">Page not found</h1>
      <Link to="/" className="underline">
        Go to the home page
      </Link>
    </main>
  )
}
