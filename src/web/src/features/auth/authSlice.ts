import { createSlice, type PayloadAction } from '@reduxjs/toolkit'
import type { AuthResponse, UserResponse } from './types'

export type AuthStatus = 'restoring' | 'authenticated' | 'guest'

// The access token lives only here, in memory. It is never written to browser storage.
export type AuthState = {
  accessToken: string | null
  user: UserResponse | null
  status: AuthStatus
}

const initialState: AuthState = { accessToken: null, user: null, status: 'restoring' }

const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {
    // After login, refresh, or the auto-login after sign-up.
    sessionStarted(state, action: PayloadAction<AuthResponse>) {
      state.accessToken = action.payload.accessToken
      state.user = action.payload.user
      state.status = 'authenticated'
    },
    userUpdated(state, action: PayloadAction<UserResponse>) {
      state.user = action.payload
    },
    loggedOut(): AuthState {
      return { accessToken: null, user: null, status: 'guest' }
    },
  },
})

export const { sessionStarted, userUpdated, loggedOut } = authSlice.actions
export const authReducer = authSlice.reducer
