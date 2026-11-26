import { Form, Formik } from 'formik'
import { Link, useSearchParams } from 'react-router'
import { useAppDispatch } from '@/app/store'
import { PageCard } from '@/components/PageCard'
import { TextField } from '@/components/TextField'
import { Button } from '@/components/ui/button'
import { parseProblem } from '@/lib/problem'
import { useLoginMutation } from './api'
import { sessionStarted } from './authSlice'
import { loginSchema } from './validation'

export function LoginPage() {
  const dispatch = useAppDispatch()
  const [login] = useLoginMutation()
  const [params] = useSearchParams()
  const returnTo = params.get('returnTo')

  return (
    <PageCard title="Log in">
      {params.get('registered') === '1' && (
        <p role="status" className="mb-4 rounded-md bg-muted px-3 py-2 text-sm">
          Account created, please log in.
        </p>
      )}
      <Formik
        initialValues={{ email: '', password: '' }}
        validationSchema={loginSchema}
        onSubmit={async (values, { setStatus, setErrors }) => {
          setStatus(undefined)
          try {
            // GuestOnly then sends the user on to returnTo.
            dispatch(sessionStarted(await login(values).unwrap()))
          } catch (error) {
            const problem = parseProblem(error)
            if (problem.status === 400) setErrors(problem.fieldErrors)
            else setStatus(loginErrorMessage(problem.status))
          }
        }}
      >
        {({ isSubmitting, status }) => (
          <Form noValidate className="space-y-4">
            <TextField name="email" label="Email" type="email" autoComplete="email" />
            <TextField name="password" label="Password" type="password" autoComplete="current-password" />
            {status && (
              <p role="alert" className="text-sm text-destructive">
                {status}
              </p>
            )}
            <Button type="submit" className="w-full" disabled={isSubmitting}>
              Log in
            </Button>
          </Form>
        )}
      </Formik>
      <p className="mt-4 text-sm text-muted-foreground">
        No account yet?{' '}
        <Link to={returnTo ? `/register?returnTo=${encodeURIComponent(returnTo)}` : '/register'} className="underline">
          Sign up
        </Link>
      </p>
    </PageCard>
  )
}

function loginErrorMessage(status: number | string | undefined) {
  if (status === 401) return 'Email or password is incorrect.'
  if (status === 429) return 'Too many attempts. Try again in a minute.'
  return 'Something went wrong. Try again.'
}
