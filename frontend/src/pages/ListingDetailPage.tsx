import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import GameArt from '../components/GameArt'
import ListingCard from '../components/ListingCard'
import { gameImage } from '../lib/games'
import { Avatar, EscrowNote, PageLoader, Spinner, Stars } from '../components/ui'
import { useAuth } from '../context/auth-context'
import { useToast } from '../context/toast-context'
import { ApiError, api } from '../lib/api'
import { money, relativeTime } from '../lib/format'
import { mediaUrl } from '../lib/media'
import type { ListingDetail } from '../lib/types'

export default function ListingDetailPage() {
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const { user } = useAuth()
  const toast = useToast()

  const [listing, setListing] = useState<ListingDetail | null>(null)
  const [notFound, setNotFound] = useState(false)
  const [buying, setBuying] = useState(false)
  const [activeImage, setActiveImage] = useState(0)

  useEffect(() => {
    let alive = true
    setListing(null)
    setNotFound(false)
    setActiveImage(0)

    api.listings
      .get(id)
      .then((data) => alive && setListing(data))
      .catch(() => alive && setNotFound(true))

    return () => {
      alive = false
    }
  }, [id])

  async function handleBuy() {
    if (!user) {
      navigate('/login', { state: { from: `/listings/${id}` } })
      return
    }

    setBuying(true)
    try {
      const tx = await api.transactions.initiate(id)
      navigate(`/checkout/${tx.id}`)
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : 'Bitimni ochib bo‘lmadi.')
    } finally {
      setBuying(false)
    }
  }

  if (notFound) {
    return (
      <div className="page py-24 text-center">
        <h1 className="font-display text-3xl font-bold">E'lon topilmadi</h1>
        <p className="mt-3 text-muted">Bu e'lon o'chirilgan yoki manzil noto'g'ri.</p>
        <Link to="/listings" className="btn-primary mt-6 inline-block px-7 py-3.5">
          Barcha e'lonlar
        </Link>
      </div>
    )
  }

  if (!listing) return <PageLoader />

  const isOwn = user?.id === listing.seller.id
  const isAvailable = listing.status === 'Active'
  const cover = listing.images[activeImage] ?? null

  return (
    // Telefonda pastdagi yopishgan panel kontentni yopib qolmasligi uchun qo'shimcha bo'shliq.
    <div className="page pb-32 pt-6 sm:pt-9 lg:pb-[88px]">
      <Link to="/listings" className="mb-5 inline-block text-sm font-semibold text-muted hover:text-brand sm:mb-6">
        ← Barcha e'lonlar
      </Link>

      <div className="grid items-start gap-6 sm:gap-8 lg:grid-cols-[minmax(0,1.35fr)_minmax(0,.75fr)]">
        <div>
          <GameArt
            glyph={listing.gameGlyph}
            color={listing.gameColor}
            seed={listing.id}
            type={listing.type}
            image={cover}
            gameImage={gameImage(listing.gameType)}
            size="lg"
            className="mb-4 rounded-2xl border border-white/[.08] shadow-rim sm:mb-6 sm:rounded-[20px]"
          >
            <span className="absolute left-3 top-3 z-10 flex flex-wrap gap-2 sm:left-4 sm:top-4">
              <span className="rounded-full border border-white/10 bg-ink/70 px-3 py-1.5 text-xs font-semibold backdrop-blur-md sm:text-[13px]">
                {listing.gameGlyph} {listing.gameName}
              </span>
              <span
                className="rounded-full border px-3 py-1.5 text-xs font-semibold backdrop-blur-md sm:text-[13px]"
                style={{
                  borderColor: `${listing.gameColor}55`,
                  background: `${listing.gameColor}22`,
                }}
              >
                {listing.typeGlyph} {listing.typeName}
              </span>
            </span>
          </GameArt>

          {listing.images.length > 1 && (
            <div className="no-scrollbar -mx-4 mb-5 flex gap-3 overflow-x-auto px-4 sm:mx-0 sm:mb-6 sm:flex-wrap sm:px-0">
              {listing.images.map((img, i) => (
                <button
                  key={img}
                  onClick={() => setActiveImage(i)}
                  className={`h-16 w-24 flex-shrink-0 overflow-hidden rounded-[10px] border-2 transition-colors ${
                    i === activeImage ? 'border-brand' : 'border-white/10 hover:border-white/30'
                  }`}
                >
                  <img src={mediaUrl(img)} alt={`Skrinshot ${i + 1}`} className="h-full w-full object-cover" />
                </button>
              ))}
            </div>
          )}

          <h1 className="m-0 mb-3.5 font-display text-[clamp(22px,5.5vw,36px)] font-bold leading-[1.15] tracking-[-.02em]">
            {listing.title}
          </h1>

          {/* Telefonda narx sarlavha ostida turadi — pastki paneldagi summa bilan bir xil. */}
          <div className="mb-5 font-display text-[28px] font-extrabold tracking-[-.02em] text-brand lg:hidden">
            {money(listing.price)} <span className="text-[15px] text-muted">UZS</span>
          </div>

          <div className="mb-6 flex flex-wrap gap-2 sm:mb-[26px] sm:gap-2.5">
            {listing.rankLevel && (
              <span className="chip border border-brand/30 bg-brand/[.12] text-brand-300">{listing.rankLevel}</span>
            )}
            <span className="chip bg-white/[.06] font-normal text-soft">Server: {listing.serverRegion}</span>
            {listing.isVerified && (
              <span className="chip border border-success/30 bg-success/[.12] text-success-fg">✓ Tekshirilgan</span>
            )}
            <span className="chip bg-white/[.06] font-normal text-dim">
              {listing.viewCount} ko'rish · {relativeTime(listing.createdAt)}
            </span>
          </div>

          <p className="m-0 mb-8 max-w-[720px] whitespace-pre-line text-[15px] leading-[1.75] text-soft-2 text-pretty sm:text-base">
            {listing.description}
          </p>

          {Object.keys(listing.stats).length > 0 && (
            <>
              <h3 className="m-0 mb-4 font-display text-[19px] font-bold">O'yin ichidagi vositalar</h3>
              <div className="mb-9 grid grid-cols-2 gap-3 sm:grid-cols-4 sm:gap-3.5">
                {Object.entries(listing.stats).map(([key, value]) => (
                  <div key={key} className="rounded-[14px] border border-white/[.08] bg-surface p-[18px]">
                    <div className="mb-1.5 text-[13px] text-dim">{key}</div>
                    <div className="font-display text-[19px] font-bold">{value}</div>
                  </div>
                ))}
              </div>
            </>
          )}

          {listing.inGameItems.length > 0 && (
            <div className="mb-9 flex flex-wrap gap-2.5">
              {listing.inGameItems.map((item) => (
                <span
                  key={item}
                  className="rounded-full border border-white/[.12] px-[17px] py-2 text-sm font-semibold text-soft"
                >
                  {item}
                </span>
              ))}
            </div>
          )}

          {listing.similar.length > 0 && (
            <>
              <h3 className="m-0 mb-4 font-display text-[19px] font-bold">O'xshash e'lonlar</h3>
              <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 sm:gap-4">
                {listing.similar.map((s) => (
                  <ListingCard key={s.id} listing={s} variant="compact" />
                ))}
              </div>
            </>
          )}
        </div>

        {/* ── Yon panel ────────────────────────────────────────────── */}
        <div className="flex flex-col gap-4 lg:sticky lg:top-[92px]">
          {/* Telefonda narx va "Sotib olish" pastdagi yopishgan panelda — bu blok yashiriladi. */}
          <div className="hidden rounded-[20px] border border-white/[.10] bg-surface p-[26px] lg:block">
            <div className="mb-1.5 text-[13px] text-muted">Narx</div>
            <div className="mb-5 font-display text-[38px] font-extrabold tracking-[-.02em] text-brand">
              {money(listing.price)} <span className="text-[17px] text-muted">UZS</span>
            </div>

            {isOwn ? (
              <Link to="/profile" className="btn-ghost block w-full py-3.5 text-center">
                Bu sizning e'loningiz
              </Link>
            ) : !isAvailable ? (
              <div className="rounded-[14px] border border-white/[.12] bg-white/[.03] py-4 text-center text-sm font-semibold text-muted">
                {listing.status === 'Sold' ? 'Sotilgan' : 'Hozir sotuvda emas'}
              </div>
            ) : (
              <>
                <button
                  onClick={handleBuy}
                  disabled={buying}
                  className="btn-primary mb-3 flex w-full items-center justify-center gap-2 py-4 text-base shadow-brand-sm"
                >
                  {buying ? <Spinner size={18} /> : 'Sotib olish'}
                </button>

                <button
                  onClick={() =>
                    toast.info("Sotuvchi bilan yozishma bitim ochilgandan keyin faollashadi — bu escrow talabi.")
                  }
                  className="btn-ghost w-full py-3.5 text-[15px]"
                >
                  💬 Sotuvchiga xabar yozish
                </button>
              </>
            )}

            <div className="mt-5">
              <EscrowNote>
                Bitim Escrow kafolati bilan himoyalangan. Pulingiz siz tasdiqlamaguningizcha sotuvchiga
                o'tmaydi.
              </EscrowNote>
            </div>
          </div>

          <div className="card p-[22px]">
            <div className="label-caps mb-4">SOTUVCHI</div>

            <Link
              to={`/u/${listing.seller.username}`}
              className="mb-[18px] flex items-center gap-3.5 text-white hover:text-white"
            >
              <Avatar name={listing.seller.username} size={52} />
              <div>
                <div className="flex items-center gap-[7px] font-display text-base font-bold">
                  @{listing.seller.username}
                  {listing.seller.isVerified && <span className="text-[13px] text-success">✓</span>}
                </div>
                {listing.seller.fullName && (
                  <div className="mt-0.5 text-[13px] text-soft">{listing.seller.fullName}</div>
                )}
                <div className="mt-[3px]">
                  <Stars rating={listing.seller.rating} />
                  <span className="ml-1 text-[13px] text-muted">· {listing.seller.totalSales} bitim</span>
                </div>
              </div>
            </Link>

            <div className="grid grid-cols-2 gap-3 text-[13px]">
              <div className="rounded-[10px] bg-white/[.04] p-3">
                <div className="mb-1 text-dim">Javob vaqti</div>
                <div className="font-semibold">~{listing.seller.avgResponseMinutes} daqiqa</div>
              </div>
              <div className="rounded-[10px] bg-white/[.04] p-3">
                <div className="mb-1 text-dim">Ro'yxatdan</div>
                <div className="font-semibold">{new Date(listing.seller.memberSince).getFullYear()}-yil</div>
              </div>
            </div>
          </div>

          {/* Escrow eslatmasi telefonda ham ko'rinishi kerak. */}
          <div className="lg:hidden">
            <EscrowNote>
              Bitim Escrow kafolati bilan himoyalangan. Pulingiz siz tasdiqlamaguningizcha sotuvchiga o'tmaydi.
            </EscrowNote>
          </div>
        </div>
      </div>

      {/* ── Telefon uchun pastki harakat paneli ──────────────────────── */}
      <div className="safe-bottom fixed inset-x-0 bottom-0 z-40 border-t border-white/[.08] bg-ink/[.92] px-4 pt-3 backdrop-blur-[14px] lg:hidden">
        <div className="flex items-center gap-3">
          <div className="min-w-0 flex-1">
            <div className="text-[11px] text-dim">Narx</div>
            <div className="truncate font-display text-[19px] font-extrabold text-brand">
              {money(listing.price)} <span className="text-xs font-semibold text-muted">UZS</span>
            </div>
          </div>

          {isOwn ? (
            <Link to="/profile" className="btn-ghost tap-target flex-shrink-0 px-5 py-3 text-sm">
              E'lonlarim
            </Link>
          ) : !isAvailable ? (
            <div className="flex-shrink-0 rounded-[14px] border border-white/[.12] bg-white/[.03] px-5 py-3 text-sm font-semibold text-muted">
              {listing.status === 'Sold' ? 'Sotilgan' : 'Sotuvda emas'}
            </div>
          ) : (
            <>
              <button
                onClick={() =>
                  toast.info("Sotuvchi bilan yozishma bitim ochilgandan keyin faollashadi — bu escrow talabi.")
                }
                className="btn-ghost tap-target flex-shrink-0 px-4 py-3 text-lg"
                aria-label="Sotuvchiga xabar yozish"
              >
                💬
              </button>
              <button
                onClick={handleBuy}
                disabled={buying}
                className="btn-primary tap-target flex flex-shrink-0 items-center justify-center px-7 py-3.5 text-[15px]"
              >
                {buying ? <Spinner size={18} /> : 'Sotib olish'}
              </button>
            </>
          )}
        </div>
      </div>
    </div>
  )
}
