import { Form, Formik } from 'formik'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { useAppDispatch } from '@/app/store'
import { PageCard } from '@/components/PageCard'
import { TextField } from '@/components/TextField'
import { Button } from '@/components/ui/button'
import { parseProblem } from '@/lib/problem'
import { useLoginMutation, useRegisterMutation } from './api'
import { sessionStarted } from './authSlice'
import { registerSchema } from './validation'

export function RegisterPage() {
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const [register] = useRegisterMutation()
  const [login] = useLoginMutation()
  const [params] = useSearchParams()
  const returnTo = params.get('returnTo')
  const returnToQuery = returnTo ? `returnTo=${encodeURIComponent(returnTo)}` : ''

  return (
    <PageCard title="Sign up">
      <Formik
        initialValues={{ email: '', displayName: '', password: '' }}
        validationSchema={registerSchema}
        onSubmit={async (values, { setStatus, setErrors, setFieldError }) => {
          setStatus(undefined)
          try {
            await register(values).unwrap()
          } catch (error) {
            const problem = parseProblem(error)
            if (problem.code === 'email_taken') setFieldError('email', 'An account with this email already exists.')
            else if (problem.code === 'display_name_taken') setFieldError('displayName', 'This display name is taken.')
            else if (problem.status === 400) setErrors(problem.fieldErrors)
            else if (problem.status === 429) setStatus('Too many attempts. Try again in a minute.')
            else setStatus('Something went wrong. Try again.')
            return
          }

          // Registration creates the account only; log in with the same credentials.
          // GuestOnly then sends the user on to returnTo.
          try {
            dispatch(sessionStarted(await login({ email: values.email, password: values.password }).unwrap()))
          } catch {
            navigate(`/login?registered=1${returnToQuery ? `&${returnToQuery}` : ''}`)
          }
        }}
      >
        {({ isSubmitting, status }) => (
          <Form noValidate className="space-y-4">
            <TextField name="email" label="Email" type="email" autoComplete="email" />
            <TextField name="displayName" label="Display name" autoComplete="nickname" />
            <TextField name="password" label="Password" type="password" autoComplete="new-password" />
            {status && (
              <p role="alert" className="text-sm text-destructive">
                {status}
              </p>
            )}
            <Button type="submit" className="w-full" disabled={isSubmitting}>
              Sign up
            </Button>
          </Form>
        )}
      </Formik>
      <p className="mt-4 text-sm text-muted-foreground">
        Already have an account?{' '}
        <Link to={returnToQuery ? `/login?${returnToQuery}` : '/login'} className="underline">
          Log in
        </Link>
      </p>
    </PageCard>
  )
}
