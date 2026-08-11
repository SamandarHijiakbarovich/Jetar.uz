import { Component, type ErrorInfo, type ReactNode } from 'react'

interface Props {
  children: ReactNode
}

interface State {
  hasError: boolean
  message?: string
}

/**
 * Ilova ichida kutilmagan xato yuz berganda butun sahifa "qora ekran" bo'lib
 * qolmasligi uchun. Uning o'rniga foydalanuvchiga tushunarli xabar va
 * "Yangilash" tugmasi ko'rsatiladi.
 *
 * Eng ko'p uchraydigan sabab — sayt yangi versiyaga o'tganda, brauzerda ochiq
 * turgan eski tab yo'q bo'lib ketgan fayllarni so'rashi. Bu holda main.tsx dagi
 * handler avtomatik yangilaydi; bu chegara esa qolgan barcha xatolarni ushlaydi.
 */
export default class ErrorBoundary extends Component<Props, State> {
  state: State = { hasError: false }

  static getDerivedStateFromError(error: unknown): State {
    return { hasError: true, message: error instanceof Error ? error.message : undefined }
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    // Ishlab chiqarishda bu yerdan Sentry kabi xizmatga yuborish mumkin.
    console.error('UI xatosi:', error, info.componentStack)
  }

  render() {
    if (!this.state.hasError) return this.props.children

    return (
      <div className="flex min-h-screen flex-col items-center justify-center gap-5 bg-ink px-6 text-center text-white">
        <div className="text-5xl">😕</div>
        <h1 className="m-0 font-display text-2xl font-bold">Nimadir xato ketdi</h1>
        <p className="m-0 max-w-md leading-relaxed text-muted">
          Sahifani yuklab bo'lmadi. Odatda buni oddiy yangilash hal qiladi.
        </p>

        <div className="flex flex-wrap justify-center gap-3">
          <button onClick={() => window.location.reload()} className="btn-primary px-7 py-3.5">
            Sahifani yangilash
          </button>
          <button onClick={() => (window.location.href = '/')} className="btn-ghost px-7 py-3.5">
            Bosh sahifa
          </button>
        </div>

        {import.meta.env.DEV && this.state.message && (
          <pre className="mt-4 max-w-lg overflow-x-auto rounded-lg bg-surface p-3 text-left text-xs text-danger-fg">
            {this.state.message}
          </pre>
        )}
      </div>
    )
  }
}
