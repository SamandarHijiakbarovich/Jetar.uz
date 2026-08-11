import type { ReactNode } from 'react'
import { stars as starsOf } from '../lib/format'
import { mediaUrl } from '../lib/media'
import type { Tone } from '../lib/status'

/** Reyting yulduzchalari — dizayndagi #F59E0B rangda. */
export function Stars({ rating, count, className = '' }: { rating: number; count?: number; className?: string }) {
  return (
    <span className={`text-[13px] text-warning ${className}`}>
      {starsOf(rating)}{' '}
      <span className="text-muted">
        {rating.toFixed(1)}
        {count !== undefined && count > 0 ? ` · ${count} baho` : ''}
      </span>
    </span>
  )
}

/** Ism bosh harfi bilan avatar — dizayndagi to'q sariq–binafsha gradient. */
export function Avatar({ name, size = 40 }: { name: string; size?: number }) {
  const letter = (name.replace(/^@/, '')[0] ?? '?').toUpperCase()

  return (
    <div
      className="grid flex-shrink-0 place-items-center rounded-full bg-gradient-to-br from-brand to-[#4A4A6A] font-display font-bold"
      style={{ width: size, height: size, fontSize: Math.round(size * 0.38) }}
      aria-hidden
    >
      {letter}
    </div>
  )
}

export function StatusPill({ tone, className = '' }: { tone: Tone; className?: string }) {
  return (
    <span
      className={`inline-block whitespace-nowrap rounded-full px-3 py-1 text-xs font-semibold ${className}`}
      style={{ background: tone.bg, color: tone.fg }}
    >
      {tone.label}
    </span>
  )
}

/** O'yin muqovasi — dizayndagi 16:9 gradient blok. */
export function GameCover({
  glyph,
  color,
  image,
  className = '',
  glyphSize = 46,
  children,
}: {
  glyph: string
  color: string
  image?: string | null
  className?: string
  glyphSize?: number
  children?: ReactNode
}) {
  return (
    <div
      className={`relative grid aspect-video place-items-center overflow-hidden ${className}`}
      style={{ background: `linear-gradient(135deg, ${color} 0%, #12122A 100%)` }}
    >
      {mediaUrl(image) ? (
        <img src={mediaUrl(image)} alt="" className="absolute inset-0 h-full w-full object-cover" loading="lazy" />
      ) : (
        <span style={{ fontSize: glyphSize }} aria-hidden>
          {glyph}
        </span>
      )}
      {children}
    </div>
  )
}

export function Spinner({ size = 22 }: { size?: number }) {
  return (
    <span
      className="inline-block animate-spin rounded-full border-2 border-white/15 border-t-brand"
      style={{ width: size, height: size }}
      role="status"
      aria-label="Yuklanmoqda"
    />
  )
}

export function PageLoader({ label = 'Yuklanmoqda…' }: { label?: string }) {
  return (
    <div className="flex min-h-[50vh] flex-col items-center justify-center gap-4 text-muted">
      <Spinner size={30} />
      <span className="text-sm">{label}</span>
    </div>
  )
}

export function SkeletonCard() {
  return (
    <div className="card overflow-hidden">
      <div className="aspect-video animate-pulse bg-white/5" />
      <div className="space-y-3 p-[18px]">
        <div className="h-4 w-4/5 animate-pulse rounded bg-white/5" />
        <div className="h-4 w-2/5 animate-pulse rounded bg-white/5" />
        <div className="h-9 w-full animate-pulse rounded-[11px] bg-white/5" />
      </div>
    </div>
  )
}

export function EmptyState({
  glyph = '🔍',
  title,
  text,
  action,
}: {
  glyph?: string
  title: string
  text?: string
  action?: ReactNode
}) {
  return (
    <div className="card flex flex-col items-center gap-3 px-6 py-16 text-center">
      <div className="text-4xl" aria-hidden>
        {glyph}
      </div>
      <h3 className="font-display text-xl font-bold">{title}</h3>
      {text && <p className="max-w-md text-[15px] leading-relaxed text-muted">{text}</p>}
      {action && <div className="mt-2">{action}</div>}
    </div>
  )
}

/** Escrow xavfsizlik eslatmasi — dizaynda bir necha joyda takrorlanadi. */
export function EscrowNote({ children }: { children: ReactNode }) {
  return (
    <div className="rounded-xl border border-success/[.22] bg-success/[.08] p-3.5 text-[13px] leading-[1.55] text-[#8FE3C4]">
      🔒 {children}
    </div>
  )
}

export function SectionTitle({ children, action }: { children: ReactNode; action?: ReactNode }) {
  return (
    <div className="mb-8 flex items-end justify-between gap-4">
      <h2 className="m-0 font-display text-[clamp(26px,4vw,36px)] font-bold tracking-[-.02em]">{children}</h2>
      {action}
    </div>
  )
}
