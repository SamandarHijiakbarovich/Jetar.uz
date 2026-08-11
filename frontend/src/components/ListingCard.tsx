import { Link } from 'react-router-dom'
import { money } from '../lib/format'
import { gameImage } from '../lib/games'
import { LISTING_TONES } from '../lib/status'
import type { ListingCard as ListingCardType } from '../lib/types'
import GameArt from './GameArt'
import { StatusPill } from './ui'

/**
 * E'lon kartochkasi. Uch o'lchamda ishlaydi:
 *   featured — bosh sahifadagi boost qilingan e'lonlar (kengroq, kattaroq narx)
 *   default  — asosiy grid
 *   compact  — yon paneldagi o'xshash e'lonlar
 */
export default function ListingCard({
  listing,
  variant = 'default',
  showStatus = false,
}: {
  listing: ListingCardType
  variant?: 'default' | 'featured' | 'compact'
  showStatus?: boolean
}) {
  const featured = variant === 'featured'
  const compact = variant === 'compact'

  return (
    <Link
      to={`/listings/${listing.id}`}
      className="group relative flex flex-col overflow-hidden rounded-[18px] border border-white/[.07] bg-surface text-white shadow-rim
                 transition-[transform,border-color,box-shadow] duration-300 ease-out
                 hover:text-white focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand/70
                 motion-safe:hover:-translate-y-1.5 hover:border-brand/45 hover:shadow-rim-lg"
    >
      {/* Hover paytida chekka bo'ylab brend nuri */}
      <span
        aria-hidden
        className="pointer-events-none absolute inset-0 -z-0 opacity-0 transition-opacity duration-300 group-hover:opacity-100"
        style={{ background: `radial-gradient(120% 60% at 50% 0%, ${listing.gameColor}22, transparent 70%)` }}
      />

      <GameArt
        glyph={listing.gameGlyph}
        color={listing.gameColor}
        seed={listing.id}
        type={listing.type}
        image={listing.coverImage}
        gameImage={gameImage(listing.gameType)}
        size={featured ? 'lg' : compact ? 'sm' : 'md'}
        className="transition-transform duration-500 ease-out motion-safe:group-hover:scale-[1.04]"
      >
        {/* Yuqori chap: o'yin va tur */}
        <span className="absolute left-3 top-3 z-10 flex flex-wrap items-center gap-1.5">
          <span className="rounded-full border border-white/10 bg-ink/70 px-2.5 py-1 text-[11px] font-semibold backdrop-blur-md">
            {listing.gameGlyph} {listing.gameName}
          </span>
          {!compact && listing.type !== 'Account' && (
            <span
              className="rounded-full border px-2.5 py-1 text-[11px] font-semibold backdrop-blur-md"
              style={{
                borderColor: `${listing.gameColor}55`,
                background: `${listing.gameColor}22`,
                color: '#fff',
              }}
            >
              {listing.typeGlyph} {listing.typeName}
            </span>
          )}
        </span>

        {/* Yuqori o'ng: holat yoki tekshiruv belgisi */}
        {showStatus ? (
          <StatusPill tone={LISTING_TONES[listing.status]} className="absolute right-3 top-3 z-10 !text-[11px]" />
        ) : (
          listing.isVerified && (
            <span className="absolute right-3 top-3 z-10 rounded-full border border-success/40 bg-success/20 px-2.5 py-1 text-[11px] font-bold text-success-fg backdrop-blur-md">
              ✓ Tekshirilgan
            </span>
          )
        )}

        {listing.isBoosted && (
          <span className="absolute bottom-3 left-3 z-10 rounded-full bg-gradient-to-r from-brand to-brand-400 px-2.5 py-1 text-[11px] font-extrabold text-ink shadow-brand-sm">
            ⚡ TOP
          </span>
        )}
      </GameArt>

      <div className={`relative z-10 flex flex-1 flex-col ${compact ? 'p-3.5' : 'p-4 sm:p-[18px]'}`}>
        <h3
          className={`m-0 font-display font-semibold leading-snug text-pretty transition-colors group-hover:text-brand-300
            ${featured ? 'text-[17px] sm:text-lg' : compact ? 'line-clamp-2 text-sm' : 'line-clamp-2 min-h-[42px] text-[15px] sm:text-base'}`}
        >
          {listing.title}
        </h3>

        {!compact && (
          <div className="mt-2.5 flex flex-wrap items-center gap-1.5 text-[11.5px] text-muted">
            {listing.rankLevel && (
              <span className="rounded-md bg-white/[.05] px-2 py-1 font-medium text-soft">{listing.rankLevel}</span>
            )}
            <span className="rounded-md bg-white/[.05] px-2 py-1">{listing.serverRegion}</span>
          </div>
        )}

        {/* Narx pastga yopishadi — kartochkalar bir xil balandlikda tugaydi */}
        <div className="mt-auto pt-3.5">
          <div className="flex items-end justify-between gap-2">
            <div>
              <div className="text-[10.5px] uppercase tracking-wider text-dim">Narx</div>
              <div
                className={`font-display font-extrabold leading-none text-brand ${
                  featured ? 'text-[26px]' : compact ? 'text-base' : 'text-[21px]'
                }`}
              >
                {money(listing.price)}
                <span className="ml-1 text-[11px] font-semibold text-muted">so'm</span>
              </div>
            </div>

            {!compact && (
              <div className="text-right">
                <div className="text-[11px] text-dim">@{listing.sellerUsername}</div>
                <div className="text-[12px] font-semibold text-warning">
                  ★ <span className="text-soft">{listing.sellerRating.toFixed(1)}</span>
                </div>
              </div>
            )}
          </div>

          {!compact && (
            <div
              className="mt-3.5 w-full rounded-xl border border-brand/35 bg-brand/[.08] py-2.5 text-center text-sm font-semibold text-brand
                         transition-colors duration-200 group-hover:border-transparent group-hover:bg-brand group-hover:text-white"
            >
              Sotib olish
            </div>
          )}
        </div>
      </div>
    </Link>
  )
}
