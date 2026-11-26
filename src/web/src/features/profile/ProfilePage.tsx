import { Form, Formik } from 'formik'
import { useAppDispatch, useAppSelector } from '@/app/store'
import { PageCard } from '@/components/PageCard'
import { TextField } from '@/components/TextField'
import { UserInitials } from '@/components/UserInitials'
import { Button } from '@/components/ui/button'
import { useUpdateMeMutation } from '@/features/auth/api'
import { userUpdated } from '@/features/auth/authSlice'
import { profileSchema } from '@/features/auth/validation'
import { parseProblem } from '@/lib/problem'

type SaveStatus = { kind: 'saved' } | { kind: 'error'; message: string } | undefined

// Only the display name can change. Email and password stay as they are; avatar upload comes in M2.
export function ProfilePage() {
  const dispatch = useAppDispatch()
  const user = useAppSelector((state) => state.auth.user)!
  const [updateMe] = useUpdateMeMutation()

  return (
    <PageCard title="Profile">
      <div className="mb-6 flex items-center gap-3">
        <UserInitials displayName={user.displayName} size="lg" />
        <div className="min-w-0">
          <p className="truncate font-medium">{user.displayName}</p>
          <p className="truncate text-sm text-muted-foreground">{user.email}</p>
        </div>
      </div>
      <Formik
        initialValues={{ displayName: user.displayName }}
        validationSchema={profileSchema}
        onSubmit={async (values, { setStatus, setErrors, setFieldError }) => {
          setStatus(undefined)
          try {
            dispatch(userUpdated(await updateMe(values).unwrap()))
            setStatus({ kind: 'saved' } satisfies SaveStatus)
          } catch (error) {
            const problem = parseProblem(error)
            if (problem.code === 'display_name_taken') setFieldError('displayName', 'This display name is taken.')
            else if (problem.status === 400) setErrors(problem.fieldErrors)
            else setStatus({ kind: 'error', message: 'Something went wrong. Try again.' } satisfies SaveStatus)
          }
        }}
      >
        {({ isSubmitting, status }: { isSubmitting: boolean; status?: SaveStatus }) => (
          <Form noValidate className="space-y-4">
            <TextField name="displayName" label="Display name" autoComplete="nickname" />
            {status?.kind === 'saved' && (
              <p role="status" className="text-sm text-green-700">
                Saved
              </p>
            )}
            {status?.kind === 'error' && (
              <p role="alert" className="text-sm text-destructive">
                {status.message}
              </p>
            )}
            <Button type="submit" disabled={isSubmitting}>
              Save
            </Button>
          </Form>
        )}
      </Formik>
    </PageCard>
  )
}
