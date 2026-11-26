export type UserResponse = {
  id: string
  email: string
  displayName: string
  avatarUrl: string | null
  role: 'User' | 'Admin'
}

export type AuthResponse = {
  accessToken: string
  expiresAt: string
  user: UserResponse
}

export type LoginRequest = { email: string; password: string }

export type RegisterRequest = { email: string; password: string; displayName: string }

export type UpdateMeRequest = { displayName: string }
