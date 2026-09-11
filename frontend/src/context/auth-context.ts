import { createContext, useContext } from 'react'
import type { RegistrationStart, User } from '../lib/types'

export interface AuthState {
  user: User | null
  loading: boolean
  isModerator: boolean
  login: (login: string, password: string) => Promise<User>
  /** 1-bosqich: emailga kod yuboradi (akkaunt hali yaratilmaydi). */
  register: (data: {
    firstName: string
    lastName: string
    username: string
    phone: string
    email: string
    password: string
    telegramUsername?: string
  }) => Promise<RegistrationStart>
  /** 2-bosqich: kodни tasdiqlaydi, akkaunt yaratiladi va kirasiz. */
  verifyEmail: (email: string, code: string) => Promise<User>
  resendCode: (email: string) => Promise<RegistrationStart>
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
