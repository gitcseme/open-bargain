import * as yup from 'yup'

// Mirrors the server rules (Accounts validators).
export const emailRule = yup
  .string()
  .trim()
  .required('Email is required.')
  .email('Enter a valid email address.')
  .max(256, 'Email must be at most 256 characters.')

export const passwordRule = yup
  .string()
  .required('Password is required.')
  .min(8, 'Password must be 8–128 characters.')
  .max(128, 'Password must be 8–128 characters.')

export const displayNameRule = yup
  .string()
  .trim()
  .required('Display name is required.')
  .min(2, 'Display name must be 2–40 characters.')
  .max(40, 'Display name must be 2–40 characters.')

export const loginSchema = yup.object({
  email: emailRule,
  password: yup.string().required('Password is required.'),
})

export const registerSchema = yup.object({
  email: emailRule,
  displayName: displayNameRule,
  password: passwordRule,
})

export const profileSchema = yup.object({ displayName: displayNameRule })
