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

  const register = useCallback(
    async (data: {
      firstName: string
      lastName: string
      username: string
      phone: string
      password: string
      telegramUsername?: string
    }) => {
      const res = await api.auth.register(data)
      tokenStore.set(res.accessToken, res.refreshToken)
      setUser(res.user)
      return res.user
    },
    [],
  )

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
      logout,
      refreshUser,
      setUser,
    }),
    [user, loading, login, register, logout, refreshUser],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
