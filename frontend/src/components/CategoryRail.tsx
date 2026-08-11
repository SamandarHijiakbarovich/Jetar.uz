import { Link } from 'react-router-dom'
import type { GameSummary, ListingTypeSummary } from '../lib/types'

/** Kategoriya kartochkasi — e'lon turi bo'yicha (Akkaunt, Valyuta, Skin, Xizmat). */
export function TypeCard({ type }: { type: ListingTypeSummary }) {
  return (
    <Link
      to={`/listings?type=${type.slug}`}
      className="group relative flex flex-col justify-between overflow-hidden rounded-[18px] border border-white/[.07]
                 bg-gradient-to-br from-surface to-surface-2 p-5 text-white shadow-rim transition-[transform,border-color,box-shadow]
                 duration-300 hover:border-brand/45 hover:text-white hover:shadow-rim-lg motion-safe:hover:-translate-y-1
                 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand/70 sm:p-6"
    >
      <span
        aria-hidden
        className="pointer-events-none absolute -right-6 -top-6 select-none text-[86px] leading-none opacity-[.10]
                   transition-transform duration-500 motion-safe:group-hover:scale-110 motion-safe:group-hover:rotate-6"
      >
        {type.glyph}
      </span>

      <div className="relative">
        <div className="mb-3 grid h-11 w-11 place-items-center rounded-2xl border border-white/[.08] bg-white/[.04] text-xl">
          {type.glyph}
        </div>
        <h3 className="m-0 font-display text-[17px] font-bold leading-tight sm:text-lg">{type.name}</h3>
        <p className="mt-1 text-[13px] leading-snug text-muted">{type.description}</p>
      </div>

      <div className="relative mt-5 flex items-center justify-between">
        <span className="text-[13px] font-semibold text-soft">
          {type.listingCount} <span className="font-normal text-dim">e'lon</span>
        </span>
        <span className="text-brand transition-transform duration-300 motion-safe:group-hover:translate-x-1">→</span>
      </div>
    </Link>
  )
}

/** O'yin plitasi — gorizontal lentada ishlatiladi. */
export function GameTile({ game }: { game: GameSummary }) {
  return (
    <Link
      to={`/listings?game=${game.slug}`}
      className="group relative flex w-[136px] flex-shrink-0 flex-col items-center overflow-hidden rounded-2xl border
                 border-white/[.07] bg-surface px-3 py-5 text-center text-white shadow-rim transition-[transform,border-color]
                 duration-300 hover:border-brand/45 hover:text-white motion-safe:hover:-translate-y-1
                 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand/70 sm:w-auto"
    >
      <span
        aria-hidden
        className="pointer-events-none absolute inset-x-0 top-0 h-16 opacity-60 transition-opacity group-hover:opacity-100"
        style={{ background: `radial-gradient(60% 100% at 50% 0%, ${game.color}40, transparent 75%)` }}
      />

      <span className="relative mb-2.5 text-[30px] leading-none transition-transform duration-300 motion-safe:group-hover:scale-110">
        {game.glyph}
      </span>
      <span className="relative font-display text-[13px] font-semibold leading-tight">{game.name}</span>
      <span className="relative mt-1 text-[11.5px] text-dim">{game.listingCount} e'lon</span>
    </Link>
  )
}
