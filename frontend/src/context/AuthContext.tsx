import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { api, tokenStore } from '../lib/api'
import type { User } from '../lib/types'
import { AuthContext, type AuthState } from './auth-context'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [loading, setLoading] = useState(true)

  // Sahifa yangilanganda saqlangan token bo'yicha foydalanuvchini tiklaymiz.
  useEffect(() => {
    if (!tokenStore.access) {
      setLoading(false)
      return
    }

    api.auth
      .me()
      .then(setUser)
      .catch(() => tokenStore.clear())
      .finally(() => setLoading(false))
  }, [])

  const login = useCallback(async (login: string, password: string) => {
    const res = await api.auth.login({ login, password })
    tokenStore.set(res.accessToken, res.refreshToken)
    setUser(res.user)
    return res.user
  }, [])

  // 1-bosqich: kod yuboriladi, akkaunt hali yaratilmaydi.
  const register = useCallback(
    (data: {
      firstName: string
      lastName: string
      username: string
      phone: string
      email: string
      password: string
      telegramUsername?: string
    }) => api.auth.register(data),
    [],
  )

  // 2-bosqich: kod tasdiqlanadi, akkaunt yaratiladi va kiriladi.
  const verifyEmail = useCallback(async (email: string, code: string) => {
    const res = await api.auth.verifyEmail({ email, code })
    tokenStore.set(res.accessToken, res.refreshToken)
    setUser(res.user)
    return res.user
  }, [])

  const resendCode = useCallback((email: string) => api.auth.resendCode(email), [])

  const logout = useCallback(() => {
    tokenStore.clear()
    setUser(null)
  }, [])

  const refreshUser = useCallback(async () => {
    if (!tokenStore.access) return
    try {
      setUser(await api.auth.me())
    } catch {
      logout()
    }
  }, [logout])

  const value = useMemo<AuthState>(
    () => ({
      user,
      loading,
      isModerator: user?.role === 'Moderator' || user?.role === 'Admin',
      login,
      register,
      verifyEmail,
      resendCode,
      logout,
      refreshUser,
      setUser,
    }),
    [user, loading, login, register, verifyEmail, resendCode, logout, refreshUser],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
