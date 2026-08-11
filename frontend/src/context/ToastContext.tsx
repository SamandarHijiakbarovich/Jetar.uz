import { useCallback, useMemo, useState, type ReactNode } from 'react'
import { ToastContext, type ToastApi, type ToastKind } from './toast-context'

interface Toast {
  id: number
  kind: ToastKind
  text: string
}

const TONES: Record<ToastKind, { border: string; bg: string; fg: string; glyph: string }> = {
  success: { border: 'rgba(16,185,129,.35)', bg: 'rgba(16,185,129,.12)', fg: '#6EE7B7', glyph: '✓' },
  error: { border: 'rgba(239,68,68,.35)', bg: 'rgba(239,68,68,.12)', fg: '#FCA5A5', glyph: '!' },
  info: { border: 'rgba(255,107,53,.35)', bg: 'rgba(255,107,53,.12)', fg: '#FFB08A', glyph: 'i' },
}

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([])

  const push = useCallback((kind: ToastKind, text: string) => {
    const id = Date.now() + Math.random()
    setToasts((current) => [...current, { id, kind, text }])
    setTimeout(() => setToasts((current) => current.filter((t) => t.id !== id)), 4500)
  }, [])

  const value = useMemo<ToastApi>(
    () => ({
      success: (text) => push('success', text),
      error: (text) => push('error', text),
      info: (text) => push('info', text),
    }),
    [push],
  )

  return (
    <ToastContext.Provider value={value}>
      {children}

      {/* Telefonda ekran bo'ylab pastda; pastki panel va uy tugmasi ustida turadi. */}
      <div className="pointer-events-none fixed inset-x-3 bottom-[calc(env(safe-area-inset-bottom)+5.5rem)] z-[100] flex flex-col gap-3 sm:inset-x-auto sm:bottom-6 sm:right-6 sm:w-[min(92vw,380px)]">
        {toasts.map((toast) => {
          const tone = TONES[toast.kind]
          return (
            <div
              key={toast.id}
              role="status"
              className="pointer-events-auto flex animate-jfade items-start gap-3 rounded-[14px] border px-4 py-3.5 text-sm leading-relaxed backdrop-blur-md"
              style={{ borderColor: tone.border, background: tone.bg, color: tone.fg }}
            >
              <span
                className="mt-0.5 grid h-5 w-5 flex-shrink-0 place-items-center rounded-full text-[11px] font-bold"
                style={{ background: tone.fg, color: '#0B0B14' }}
              >
                {tone.glyph}
              </span>
              <span className="flex-1">{toast.text}</span>
            </div>
          )
        })}
      </div>
    </ToastContext.Provider>
  )
}
