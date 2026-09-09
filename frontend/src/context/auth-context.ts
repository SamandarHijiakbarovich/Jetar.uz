import { createContext, useContext } from 'react'
import type { User } from '../lib/types'

export interface AuthState {
  user: User | null
  loading: boolean
  isModerator: boolean
  login: (login: string, password: string) => Promise<User>
  register: (data: {
    firstName: string
    lastName: string
    username: string
    phone: string
    email: string
    password: string
    telegramUsername?: string
  }) => Promise<User>
  logout: () => void
  refreshUser: () => Promise<void>
  setUser: (user: User) => void
}

/**
 * Kontekst va hook komponentdan alohida faylda turadi: Vite Fast Refresh
 * faqat komponent eksport qiladigan modullarni qismlab yangilay oladi.
 */
export const AuthContext = createContext<AuthState | null>(null)

export function useAuth(): AuthState {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth faqat AuthProvider ichida ishlatiladi.')
  return ctx
}
