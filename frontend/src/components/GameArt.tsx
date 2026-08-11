import { useState } from 'react'
import { mediaUrl } from '../lib/media'
import type { ListingType } from '../lib/types'

/**
 * E'lonlarda haqiqiy rasm bo'lmaganda ko'rsatiladigan generativ muqova.
 * Oldin bu oddiy gradient + emoji edi va sayt shu sababdan "bo'sh" ko'rinardi.
 *
 * Har bir muqova to'rt qatlamdan iborat:
 *   1. Asosiy gradient (o'yin rangi)
 *   2. Aylanuvchi yorug'lik dog'lari
 *   3. Nozik to'r (grid)
 *   4. Katta, kesilgan emoji — kompozitsiya markazi
 *
 * Naqsh e'lon id'sidan hosil qilinadi, ya'ni har bir e'lon o'z ko'rinishiga ega
 * bo'ladi, lekin sahifa har yangilanganda o'zgarmaydi.
 */

/** id satridan barqaror son — bir xil e'lon doim bir xil naqsh oladi. */
function hashOf(seed: string): number {
  let h = 0
  for (let i = 0; i < seed.length; i++) h = (h * 31 + seed.charCodeAt(i)) | 0
  return Math.abs(h)
}

/** Turga qarab qo'shimcha belgi — valyuta va xizmatlar akkauntdan farq qilsin. */
const TYPE_MOTIF: Record<ListingType, string> = {
  Account: '',
  Currency: '◈',
  Item: '✦',
  Service: '➤',
}

export default function GameArt({
  glyph,
  color,
  seed,
  type = 'Account',
  image,
  gameImage,
  size = 'md',
  eager = false,
  className = '',
  children,
}: {
  glyph: string
  color: string
  seed: string
  type?: ListingType
  /** E'lonning o'z rasmi (sotuvchi yuklagan skrinshot). */
  image?: string | null
  /** O'yin muqovasi (hero) — e'lonning o'z rasmi bo'lmaganda ishlatiladi. */
  gameImage?: string | null
  size?: 'sm' | 'md' | 'lg'
  /** Muqova ekranning yuqorisida bo'lsa (e'lon sahifasi) darhol yuklanadi. */
  eager?: boolean
  className?: string
  children?: React.ReactNode
}) {
  // Rasm yuklanmasa (masalan o'yin muqovasi hali qo'yilmagan) generativ san'atga qaytamiz.
  const [failed, setFailed] = useState(false)
  const resolved = failed ? undefined : mediaUrl(image) ?? gameImage ?? undefined
  const h = hashOf(seed)

  // Kompozitsiyani id bo'yicha siljitamiz — kartochkalar bir-biriga o'xshamaydi.
  const tilt = (h % 24) - 12
  const offsetX = 55 + (h % 25)
  const offsetY = 20 + ((h >> 3) % 30)
  const glowX = 15 + ((h >> 5) % 30)
  const motif = TYPE_MOTIF[type]

  const glyphSize = size === 'sm' ? 'text-[44px]' : size === 'lg' ? 'text-[150px]' : 'text-[92px]'

  return (
    <div
      className={`relative isolate grid aspect-video place-items-center overflow-hidden ${className}`}
      style={{ background: `linear-gradient(145deg, ${color}2E 0%, #0D0D1C 62%, #08080F 100%)` }}
    >
      {resolved ? (
        <img
          src={resolved}
          alt=""
          className="absolute inset-0 h-full w-full object-cover"
          loading={eager ? 'eager' : 'lazy'}
          onError={() => setFailed(true)}
        />
      ) : (
        <>
          {/* Yorug'lik dog'lari — sekin harakatlanadi */}
          <span
            aria-hidden
            className="pointer-events-none absolute -z-10 animate-jorbit"
            style={{
              inset: '-30%',
              background: `radial-gradient(closest-side at ${glowX}% 25%, ${color}66, transparent 70%),
                           radial-gradient(closest-side at 85% 80%, ${color}33, transparent 70%)`,
            }}
          />

          {/* Nozik to'r */}
          <span
            aria-hidden
            className="pointer-events-none absolute inset-0 -z-10 bg-grid-fade bg-grid opacity-40"
            style={{ maskImage: 'radial-gradient(120% 90% at 50% 40%, #000 35%, transparent 100%)' }}
          />

          {/* Asosiy belgi — chetdan kesilgan, katta */}
          <span
            aria-hidden
            className={`pointer-events-none absolute select-none leading-none ${glyphSize}`}
            style={{
              left: `${offsetX}%`,
              top: `${offsetY}%`,
              transform: `translate(-50%,-50%) rotate(${tilt}deg)`,
              filter: `drop-shadow(0 12px 28px ${color}55)`,
              opacity: 0.92,
            }}
          >
            {glyph}
          </span>

          {/* Tur belgisi — chap pastda, xira */}
          {motif && (
            <span
              aria-hidden
              className="pointer-events-none absolute bottom-2 left-3 select-none text-[42px] leading-none opacity-[.13]"
              style={{ color }}
            >
              {motif}
            </span>
          )}

          {/* Pastki qorayish — matn o'qilishi uchun */}
          <span
            aria-hidden
            className="pointer-events-none absolute inset-x-0 bottom-0 h-1/2"
            style={{ background: 'linear-gradient(to top, rgba(8,8,15,.85), transparent)' }}
          />
        </>
      )}

      {children}
    </div>
  )
}
