import { Link } from 'react-router-dom'
import { money, shortDate } from '../lib/format'
import { TRANSACTION_TONES } from '../lib/status'
import type { Transaction } from '../lib/types'
import { EmptyState, StatusPill } from './ui'

/**
 * "Bitimlarim" ro'yxati. Katta ekranda — jadval, telefonda — kartochka:
 * tor ekranda ustunlar siqilib, sarlavhasiz qatorlar o'qib bo'lmas edi.
 */
export default function TransactionTable({ items }: { items: Transaction[] }) {
  if (items.length === 0) {
    return (
      <EmptyState
        glyph="🧾"
        title="Bitimlar yo'q"
        text="Birinchi akkauntni sotib olganingizdan keyin bitimlar shu yerda ko'rinadi."
        action={
          <Link to="/listings" className="btn-primary inline-block px-6 py-3">
            E'lonlarni ko'rish
          </Link>
        }
      />
    )
  }

  return (
    <>
      {/* ── Telefon: kartochkalar ─────────────────────────────────── */}
      <div className="flex flex-col gap-3 md:hidden">
        {items.map((tx) => (
          <Link key={tx.id} to={`/transactions/${tx.id}`} className="card block p-4 text-white hover:text-white">
            <div className="mb-3 flex items-start gap-3">
              <div
                className="grid h-11 w-11 flex-shrink-0 place-items-center rounded-[10px] text-xl"
                style={{ background: `linear-gradient(135deg, ${tx.listing.gameColor}, #12122A)` }}
              >
                {tx.listing.gameGlyph}
              </div>

              <div className="min-w-0 flex-1">
                <div className="line-clamp-2 text-sm font-semibold leading-snug">{tx.listing.title}</div>
                <div className="mt-1 flex items-center gap-2 text-xs text-dim">
                  <span className="font-mono">#{tx.number}</span>
                  <span>·</span>
                  <span>{shortDate(tx.createdAt)}</span>
                </div>
              </div>
            </div>

            <div className="flex items-center justify-between gap-3 border-t border-white/[.06] pt-3">
              <div>
                <div className="text-[11px] text-dim">Miqdor</div>
                <div className="font-display text-base font-bold">{money(tx.amount)} so'm</div>
              </div>
              <StatusPill tone={TRANSACTION_TONES[tx.status]} />
            </div>
          </Link>
        ))}
      </div>

      {/* ── Katta ekran: jadval ───────────────────────────────────── */}
      <div className="card hidden overflow-hidden md:block">
        <div className="grid grid-cols-[130px_minmax(0,1fr)_160px_150px] gap-4 bg-white/[.03] px-[22px] py-4 text-xs font-bold tracking-[.08em] text-dim">
          <div>SANA</div>
          <div>O'YIN / AKKAUNT</div>
          <div>MIQDOR</div>
          <div>HOLAT</div>
        </div>

        {items.map((tx) => (
          <Link
            key={tx.id}
            to={`/transactions/${tx.id}`}
            className="grid grid-cols-[130px_minmax(0,1fr)_160px_150px] items-center gap-4 border-t border-white/[.06] px-[22px] py-[18px] text-sm text-white transition-colors hover:bg-white/[.02] hover:text-white"
          >
            <div className="text-muted">{shortDate(tx.createdAt)}</div>

            <div className="truncate font-medium">
              <span className="mr-2">{tx.listing.gameGlyph}</span>
              {tx.listing.title}
              <span className="ml-2 font-mono text-xs text-dim">#{tx.number}</span>
            </div>

            <div className="font-display font-bold">{money(tx.amount)}</div>

            <div>
              <StatusPill tone={TRANSACTION_TONES[tx.status]} />
            </div>
          </Link>
        ))}
      </div>
    </>
  )
}
