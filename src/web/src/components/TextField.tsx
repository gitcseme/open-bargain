import { useField } from 'formik'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'

type TextFieldProps = {
  name: string
  label: string
  type?: 'text' | 'email' | 'password'
  autoComplete?: string
}

// A labelled Formik input with its error message underneath.
export function TextField({ name, label, type = 'text', autoComplete }: TextFieldProps) {
  const [field, meta] = useField<string>(name)
  const id = `field-${name}`
  const error = meta.touched ? meta.error : undefined

  return (
    <div className="space-y-1.5">
      <Label htmlFor={id}>{label}</Label>
      <Input
        {...field}
        id={id}
        type={type}
        autoComplete={autoComplete}
        aria-invalid={error ? true : undefined}
        aria-describedby={error ? `${id}-error` : undefined}
      />
      {error && (
        <p id={`${id}-error`} className="text-sm text-destructive">
          {error}
        </p>
      )}
    </div>
  )
}
