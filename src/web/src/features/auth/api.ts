import { createApi } from '@reduxjs/toolkit/query/react'
import { baseQueryWithReauth } from '@/lib/baseQuery'
import type { AuthResponse, LoginRequest, RegisterRequest, UpdateMeRequest, UserResponse } from './types'

// The one RTK Query API for the app. Callers update the auth slice themselves
// (see LoginPage), so the store is current before the next render.
// Refresh is not an endpoint: it goes through refreshOnce in lib/baseQuery.
export const api = createApi({
  reducerPath: 'api',
  baseQuery: baseQueryWithReauth,
  endpoints: (build) => ({
    register: build.mutation<UserResponse, RegisterRequest>({
      query: (body) => ({ url: '/auth/register', method: 'POST', body }),
    }),
    login: build.mutation<AuthResponse, LoginRequest>({
      query: (body) => ({ url: '/auth/login', method: 'POST', body }),
    }),
    logout: build.mutation<void, void>({
      query: () => ({ url: '/auth/logout', method: 'POST' }),
    }),
    getMe: build.query<UserResponse, void>({
      query: () => '/me',
    }),
    updateMe: build.mutation<UserResponse, UpdateMeRequest>({
      query: (body) => ({ url: '/me', method: 'PUT', body }),
    }),
  }),
})

export const { useRegisterMutation, useLoginMutation, useLogoutMutation, useGetMeQuery, useUpdateMeMutation } = api
