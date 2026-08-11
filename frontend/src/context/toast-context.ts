import { createContext, useContext } from 'react'

export type ToastKind = 'success' | 'error' | 'info'

export interface ToastApi {
  success: (text: string) => void
  error: (text: string) => void
  info: (text: string) => void
}

/** Kontekst va hook komponentdan alohida — Fast Refresh buzilmasligi uchun. */
export const ToastContext = createContext<ToastApi | null>(null)

export function useToast(): ToastApi {
  const ctx = useContext(ToastContext)
  if (!ctx) throw new Error('useToast faqat ToastProvider ichida ishlatiladi.')
  return ctx
}
