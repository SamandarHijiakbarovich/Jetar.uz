import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { GameTile, TypeCard } from '../components/CategoryRail'
import ListingCard from '../components/ListingCard'
import { SkeletonCard } from '../components/ui'
import { api } from '../lib/api'
import { relativeTime } from '../lib/format'
import type { Categories, ListingCard as ListingCardType, Rating } from '../lib/types'

const TRUST = [
  { glyph: '🆓', title: 'Bepul e’lon', text: 'E’lon joylash mutlaqo bepul' },
  { glyph: '💬', title: "To'g'ridan-to'g'ri", text: 'Sotuvchi bilan bevosita bog’laning' },
  { glyph: '🚀', title: 'TOP xizmati', text: 'E’loningizni tepaga chiqaring' },
]

const STEPS = [
  { num: '01', glyph: '🎯', title: 'Tanlang', text: "E'lonlarni filtrlab, o'zingizga mos variantni toping." },
  { num: '02', glyph: '💬', title: "Bog'laning", text: 'Sotuvchiga Telegram yoki telefon orqali to‘g‘ridan-to‘g‘ri yozing.' },
  { num: '03', glyph: '🤝', title: 'Kelishing', text: "Shartlarni kelishib, akkauntni bevosita o'zaro almashing." },
]

export default function HomePage() {
  const navigate = useNavigate()

  const [listings, setListings] = useState<ListingCardType[] | null>(null)
  const [categories, setCategories] = useState<Categories | null>(null)
  const [reviews, setReviews] = useState<Rating[]>([])
  const [total, setTotal] = useState(0)
  const [search, setSearch] = useState('')

  useEffect(() => {
    let alive = true

    async function load() {
      const [popular, cats] = await Promise.all([
        api.listings.search({ sort: 'popular', pageSize: 8 }),
        api.listings.categories(),
      ])

      if (!alive) return
      setListings(popular.items)
      setCategories(cats)
      setTotal(popular.totalCount)

      const sellers = [...new Set(popular.items.map((l) => l.sellerUsername))].slice(0, 3)
      const profiles = await Promise.allSettled(sellers.map((u) => api.users.profile(u)))
      if (!alive) return

      setReviews(
        profiles
          .filter((p) => p.status === 'fulfilled')
          .flatMap((p) => (p as PromiseFulfilledResult<{ ratings: Rating[] }>).value.ratings)
          .filter((r) => r.comment)
          .slice(0, 3),
      )
    }

    load().catch(() => alive && setListings([]))
    return () => {
      alive = false
    }
  }, [])

  function submitSearch(e: React.FormEvent) {
    e.preventDefault()
    navigate(search.trim() ? `/listings?search=${encodeURIComponent(search.trim())}` : '/listings')
  }

  const featured = listings?.slice(0, 2) ?? []
  const rest = listings?.slice(2) ?? []
  const sellerCount = new Set(listings?.map((l) => l.sellerUsername) ?? []).size

  return (
    <div>
      {/* ══ Hero ═══════════════════════════════════════════════════════ */}
      <section className="relative overflow-hidden px-4 pb-16 pt-10 sm:px-6 sm:pb-24 sm:pt-16">
        <div
          aria-hidden
          className="pointer-events-none absolute inset-0 -z-10"
          style={{
            background:
              'radial-gradient(900px 480px at 78% 0%, rgba(255,107,53,.20), transparent 62%), radial-gradient(700px 420px at 5% 95%, rgba(74,74,106,.30), transparent 60%)',
          }}
        />
        <div
          aria-hidden
          className="pointer-events-none absolute inset-0 -z-10 bg-grid-fade bg-grid opacity-[.6]"
          style={{ maskImage: 'radial-gradient(100% 60% at 50% 0%, #000 10%, transparent 90%)' }}
        />

        <div className="relative mx-auto grid max-w-[1200px] items-center gap-12 lg:grid-cols-[minmax(0,1.05fr)_minmax(0,.8fr)] lg:gap-16">
          {/* min-w-0 — grid elementi standart bo'yicha min-content'dan kichrayolmaydi
              va tor ekranda trekdan chiqib ketadi. */}
          <div className="min-w-0 animate-jrise">
            <div className="mb-5 inline-flex items-center gap-2 rounded-full border border-brand/30 bg-brand/[.08] px-3.5 py-[7px] text-xs font-semibold text-brand-300 backdrop-blur-sm sm:text-[13px]">
              <span className="h-[7px] w-[7px] animate-jpulse rounded-full bg-success" />
              Bepul e'lon · to'g'ridan-to'g'ri savdo
            </div>

            <h1 className="m-0 mb-5 font-display text-[clamp(36px,7.5vw,66px)] font-extrabold leading-[1.02] tracking-[-.038em]">
              O'yin dunyosining
              <br />
              <span className="bg-gradient-to-r from-brand via-[#FF9558] to-[#FFC79A] bg-clip-text text-transparent">
                ishonchli bozori
              </span>
            </h1>

            <p className="m-0 mb-7 max-w-[520px] text-base leading-relaxed text-muted text-pretty sm:text-lg">
              Akkaunt, o'yin valyutasi, skin va boosting xizmatlari — hammasi bir joyda. Sotuvchilar
              bilan to'g'ridan-to'g'ri bog'laning.
            </p>

            {/* Qidiruv — marketplace shundan boshlanadi */}
            <form onSubmit={submitSearch} className="mb-7 max-w-[560px]">
              <div className="flex gap-2 rounded-2xl border border-white/[.10] bg-surface/80 p-2 shadow-rim backdrop-blur-md transition-colors focus-within:border-brand/60">
                <input
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  placeholder="Nima qidiryapsiz? Masalan: Conqueror akkaunt"
                  aria-label="Qidiruv"
                  type="search"
                  className="min-w-0 flex-1 border-none bg-transparent px-3 text-[15px] text-white outline-none placeholder:text-dim"
                />
                <button type="submit" className="btn-primary tap-target flex-shrink-0 px-5 py-3 text-sm sm:px-7">
                  Qidirish
                </button>
              </div>
            </form>

            <div className="flex flex-wrap gap-x-10 gap-y-4">
              <Stat value={`${total || 19}+`} label="faol e'lon" color="#FF6B35" />
              <Stat value={`${sellerCount || 6}+`} label="sotuvchi" color="#fff" />
              <Stat value="98%" label="muvaffaqiyat" color="#10B981" />
            </div>
          </div>

          {/* Jonli bitim kartochkasi */}
          <div className="relative min-w-0 animate-jrise [animation-delay:.1s]">
            <div
              aria-hidden
              className="pointer-events-none absolute -inset-6 -z-10 rounded-full opacity-70 blur-3xl"
              style={{ background: 'radial-gradient(closest-side, rgba(255,107,53,.22), transparent)' }}
            />
            {listings === null ? (
              <div className="h-[420px] animate-pulse rounded-hero border border-white/10 bg-surface/60" />
            ) : (
              featured[0] && (
                <div className="rounded-hero border border-white/[.10] bg-gradient-to-b from-[#1E1E38f2] to-[#0F0F20f2] p-4 shadow-panel backdrop-blur-xl sm:p-5">
                  <div className="mb-3.5 flex items-center justify-between">
                    <span className="text-[11.5px] font-bold uppercase tracking-[.12em] text-muted">
                      Ommabop e'lon
                    </span>
                    <span className="flex items-center gap-1.5 text-xs font-semibold text-success">
                      <span className="h-1.5 w-1.5 animate-jpulse rounded-full bg-success" /> Faol
                    </span>
                  </div>

                  <ListingCard listing={featured[0]} variant="featured" />
                </div>
              )
            )}
          </div>
        </div>
      </section>

      {/* ══ Ishonch paneli ═════════════════════════════════════════════ */}
      <section className="border-y border-white/[.06] bg-surface-2/60">
        <div className="page grid gap-px sm:grid-cols-3">
          {TRUST.map((t) => (
            <div key={t.title} className="flex items-center gap-3.5 py-5 sm:py-6">
              <span className="grid h-11 w-11 flex-shrink-0 place-items-center rounded-xl border border-white/[.07] bg-white/[.03] text-xl">
                {t.glyph}
              </span>
              <div>
                <div className="font-display text-[15px] font-bold">{t.title}</div>
                <div className="text-[13px] text-muted">{t.text}</div>
              </div>
            </div>
          ))}
        </div>
      </section>

      {/* ══ Kategoriyalar ══════════════════════════════════════════════ */}
      <section className="page py-14 sm:py-20">
        <SectionHead title="Kategoriyalar" subtitle="Nima sotib olmoqchisiz?" />

        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {categories
            ? categories.types.map((t) => <TypeCard key={t.slug} type={t} />)
            : Array.from({ length: 4 }, (_, i) => (
                <div key={i} className="h-[190px] animate-pulse rounded-[18px] bg-white/[.04]" />
              ))}
        </div>
      </section>

      {/* ══ O'yinlar ═══════════════════════════════════════════════════ */}
      <section className="page pb-14 sm:pb-20">
        <SectionHead
          title="O'yin bo'yicha"
          subtitle="Sevimli o'yiningizni tanlang"
          action={
            <Link to="/listings" className="text-sm font-semibold text-brand hover:text-brand-400">
              Barchasi →
            </Link>
          }
        />

        <div className="no-scrollbar -mx-4 flex gap-3 overflow-x-auto px-4 pb-1 sm:mx-0 sm:grid sm:grid-cols-4 sm:px-0 lg:grid-cols-7">
          {categories?.games.map((g) => <GameTile key={g.slug} game={g} />) ??
            Array.from({ length: 7 }, (_, i) => (
              <div key={i} className="h-[124px] w-[136px] flex-shrink-0 animate-pulse rounded-2xl bg-white/[.04] sm:w-auto" />
            ))}
        </div>
      </section>

      {/* ══ Ommabop e'lonlar ═══════════════════════════════════════════ */}
      <section className="page pb-14 sm:pb-20">
        <SectionHead
          title="🔥 Ommabop e'lonlar"
          subtitle="Eng ko'p ko'rilgan takliflar"
          action={
            <Link to="/listings" className="text-sm font-semibold text-brand hover:text-brand-400">
              Barchasi →
            </Link>
          }
        />

        <div className="grid gap-4 sm:grid-cols-2 sm:gap-5 lg:grid-cols-3 xl:grid-cols-3">
          {listings === null
            ? Array.from({ length: 6 }, (_, i) => <SkeletonCard key={i} />)
            : rest.map((l) => <ListingCard key={l.id} listing={l} />)}
        </div>
      </section>

      {/* ══ Qanday ishlaydi ════════════════════════════════════════════ */}
      <section id="qanday-ishlaydi" className="relative overflow-hidden border-y border-white/[.06] bg-surface-2 px-4 py-16 sm:px-6 sm:py-24">
        <div
          aria-hidden
          className="pointer-events-none absolute inset-0 -z-10"
          style={{ background: 'radial-gradient(700px 300px at 50% 0%, rgba(255,107,53,.10), transparent 70%)' }}
        />

        <div className="mx-auto max-w-[1200px]">
          <div className="mb-12 text-center">
            <div className="mb-3 text-xs font-bold uppercase tracking-[.16em] text-brand">Qanday ishlaydi</div>
            <h2 className="m-0 font-display text-[clamp(26px,5.5vw,40px)] font-bold tracking-[-.025em]">
              Uch qadamda savdo
            </h2>
          </div>

          <div className="relative grid gap-8 md:grid-cols-3 md:gap-6">
            {/* Qadamlarni bog'lovchi chiziq — faqat keng ekranda */}
            <span
              aria-hidden
              className="pointer-events-none absolute left-[16%] right-[16%] top-[38px] hidden h-px md:block"
              style={{ background: 'linear-gradient(90deg, transparent, rgba(255,107,53,.35), transparent)' }}
            />

            {STEPS.map((s) => (
              <div key={s.num} className="relative text-center">
                <div className="mx-auto mb-5 grid h-[78px] w-[78px] place-items-center rounded-[24px] border border-brand/25 bg-gradient-to-br from-brand/[.18] to-brand/[.03] text-[32px] shadow-rim backdrop-blur-sm">
                  {s.glyph}
                </div>
                <div className="mb-2 font-mono text-[13px] font-bold tracking-[.12em] text-brand">{s.num}</div>
                <h3 className="m-0 mb-2 font-display text-xl font-bold">{s.title}</h3>
                <p className="mx-auto m-0 max-w-[280px] text-[15px] leading-relaxed text-muted">{s.text}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* ══ Sharhlar ═══════════════════════════════════════════════════ */}
      {reviews.length > 0 && (
        <section className="page py-14 sm:py-20">
          <SectionHead title="Foydalanuvchilar fikri" subtitle="Haqiqiy bitimlardan keyingi baholar" />

          <div className="grid gap-4 sm:gap-5 md:grid-cols-3">
            {reviews.map((r) => (
              <figure
                key={r.id}
                className="m-0 rounded-[18px] border border-white/[.07] bg-surface p-5 shadow-rim sm:p-6"
              >
                <div className="mb-3 text-sm tracking-wide text-warning">{'★'.repeat(r.score)}</div>
                <blockquote className="m-0 mb-5 text-[15px] leading-relaxed text-soft-3 text-pretty">
                  {r.comment}
                </blockquote>
                <figcaption className="flex items-center gap-3">
                  <span className="grid h-9 w-9 place-items-center rounded-full bg-gradient-to-br from-brand to-[#4A4A6A] font-display text-sm font-bold">
                    {r.fromUsername[0]?.toUpperCase()}
                  </span>
                  <span>
                    <span className="block text-[13px] font-semibold">@{r.fromUsername}</span>
                    <span className="block text-xs text-dim">{relativeTime(r.createdAt)}</span>
                  </span>
                </figcaption>
              </figure>
            ))}
          </div>
        </section>
      )}

      {/* ══ Sotuvchi bo'lish ═══════════════════════════════════════════ */}
      <section className="page pb-16 sm:pb-24">
        <div className="relative overflow-hidden rounded-[24px] border border-brand/25 bg-gradient-to-br from-brand/[.14] via-surface to-surface p-7 shadow-rim sm:p-12">
          <span
            aria-hidden
            className="pointer-events-none absolute -right-16 -top-16 h-64 w-64 rounded-full opacity-50 blur-3xl"
            style={{ background: 'radial-gradient(closest-side, rgba(255,107,53,.5), transparent)' }}
          />

          <div className="relative flex flex-col items-start justify-between gap-6 lg:flex-row lg:items-center">
            <div>
              <h2 className="m-0 mb-2.5 font-display text-[clamp(22px,4.5vw,32px)] font-bold tracking-[-.02em]">
                Sotuvchi bo'ling — e'lon bepul
              </h2>
              <p className="m-0 max-w-[560px] text-[15px] leading-relaxed text-soft-2">
                E'lon joylashtiring, xaridorlar siz bilan to'g'ridan-to'g'ri bog'lanadi. Ko'proq
                ko'rinish uchun e'loningizni TOP ga chiqaring.
              </p>
            </div>

            <Link to="/create" className="btn-primary tap-target flex-shrink-0 px-8 py-4 text-base shadow-brand">
              E'lon joylashtirish
            </Link>
          </div>
        </div>
      </section>
    </div>
  )
}

function SectionHead({ title, subtitle, action }: { title: string; subtitle?: string; action?: React.ReactNode }) {
  return (
    <div className="mb-6 flex flex-wrap items-end justify-between gap-3 sm:mb-8">
      <div>
        <h2 className="m-0 font-display text-[clamp(22px,4.5vw,32px)] font-bold tracking-[-.025em]">{title}</h2>
        {subtitle && <p className="m-0 mt-1.5 text-sm text-muted sm:text-[15px]">{subtitle}</p>}
      </div>
      {action}
    </div>
  )
}

function Stat({ value, label, color }: { value: string; label: string; color: string }) {
  return (
    <div>
      <div className="font-display text-[clamp(24px,5vw,32px)] font-extrabold leading-none" style={{ color }}>
        {value}
      </div>
      <div className="mt-1.5 text-[13px] text-muted">{label}</div>
    </div>
  )
}
