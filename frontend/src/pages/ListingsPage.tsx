import { useCallback, useEffect, useMemo, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import ListingCard from '../components/ListingCard'
import { EmptyState, SkeletonCard, Spinner } from '../components/ui'
import { api } from '../lib/api'
import { GAMES, LISTING_TYPES, REGIONS } from '../lib/games'
import { formatPriceInput, parsePriceInput } from '../lib/format'
import type { ListingCard as ListingCardType, ListingQuery } from '../lib/types'

const SORTS: { key: NonNullable<ListingQuery['sort']>; label: string }[] = [
  { key: 'newest', label: 'Yangi' },
  { key: 'popular', label: 'Ommabop' },
  { key: 'price_asc', label: 'Arzon' },
  { key: 'price_desc', label: 'Qimmat' },
]

export default function ListingsPage() {
  const [params, setParams] = useSearchParams()

  const [items, setItems] = useState<ListingCardType[]>([])
  const [total, setTotal] = useState(0)
  const [hasNext, setHasNext] = useState(false)
  const [loading, setLoading] = useState(true)
  const [loadingMore, setLoadingMore] = useState(false)
  const [page, setPage] = useState(1)

  // Telefonda filtrlar yig'ilgan holda turadi — aks holda ular ekranning
  // yarmini egallab, e'lonlarni pastga surib yuboradi.
  const [filtersOpen, setFiltersOpen] = useState(false)

  // URL — yagona haqiqat manbai, shunda filtrlar bilan link ulashish mumkin.
  const query = useMemo<ListingQuery>(
    () => ({
      game: params.get('game') ?? undefined,
      type: params.get('type') ?? undefined,
      search: params.get('search') ?? undefined,
      region: params.get('region') ?? undefined,
      minPrice: params.get('minPrice') ? Number(params.get('minPrice')) : undefined,
      maxPrice: params.get('maxPrice') ? Number(params.get('maxPrice')) : undefined,
      verifiedOnly: params.get('verifiedOnly') === '1' ? true : undefined,
      sort: (params.get('sort') as ListingQuery['sort']) ?? 'newest',
    }),
    [params],
  )

  const [searchDraft, setSearchDraft] = useState(query.search ?? '')
  const [minDraft, setMinDraft] = useState(query.minPrice ? formatPriceInput(String(query.minPrice)) : '')
  const [maxDraft, setMaxDraft] = useState(query.maxPrice ? formatPriceInput(String(query.maxPrice)) : '')

  const patch = useCallback(
    (changes: Record<string, string | undefined>) => {
      const next = new URLSearchParams(params)

      for (const [key, value] of Object.entries(changes)) {
        if (value === undefined || value === '') next.delete(key)
        else next.set(key, value)
      }

      setParams(next, { replace: true })
    },
    [params, setParams],
  )

  useEffect(() => {
    let alive = true
    setLoading(true)
    setPage(1)

    api.listings
      .search({ ...query, page: 1, pageSize: 12 })
      .then((res) => {
        if (!alive) return
        setItems(res.items)
        setTotal(res.totalCount)
        setHasNext(res.hasNext)
      })
      .finally(() => alive && setLoading(false))

    return () => {
      alive = false
    }
  }, [query])

  async function loadMore() {
    setLoadingMore(true)
    try {
      const res = await api.listings.search({ ...query, page: page + 1, pageSize: 12 })
      setItems((current) => [...current, ...res.items])
      setHasNext(res.hasNext)
      setPage((p) => p + 1)
    } finally {
      setLoadingMore(false)
    }
  }

  // "type" bu yerda hisoblanmaydi — u yuqoridagi alohida tablarda ko'rinib turadi.
  const activeCount = ['game', 'search', 'region', 'minPrice', 'maxPrice', 'verifiedOnly'].filter((k) =>
    params.has(k),
  ).length

  function clearFilters() {
    setSearchDraft('')
    setMinDraft('')
    setMaxDraft('')

    // Kategoriya tabini saqlaymiz — u filtr emas, navigatsiya.
    const keep: Record<string, string> = {}
    if (query.type) keep.type = query.type
    if (query.sort && query.sort !== 'newest') keep.sort = query.sort
    setParams(keep, { replace: true })
  }

  const activeType = LISTING_TYPES.find((t) => t.slug === query.type)

  return (
    <div className="page py-8 sm:py-11">
      <h1 className="m-0 mb-2 font-display text-[clamp(24px,6vw,34px)] font-bold tracking-[-.02em]">
        {activeType ? activeType.name : "Barcha e'lonlar"}
      </h1>
      <p className="m-0 mb-5 text-[15px] text-muted sm:mb-6 sm:text-base">
        {loading ? 'Qidirilmoqda…' : `${total} ta e'lon topildi`}
      </p>

      {/* ── Kategoriya tablari ────────────────────────────────────── */}
      <div className="no-scrollbar -mx-4 mb-6 flex gap-2 overflow-x-auto px-4 sm:mx-0 sm:px-0">
        <TypeTab active={!query.type} onClick={() => patch({ type: undefined })} glyph="✨" label="Hammasi" />
        {LISTING_TYPES.map((t) => (
          <TypeTab
            key={t.slug}
            active={query.type === t.slug}
            onClick={() => patch({ type: query.type === t.slug ? undefined : t.slug })}
            glyph={t.glyph}
            label={t.name}
          />
        ))}
      </div>

      {/* Qidiruv — telefonda ham har doim ko'rinib turadi. */}
      <form
        onSubmit={(e) => {
          e.preventDefault()
          patch({ search: searchDraft.trim() || undefined })
        }}
        className="mb-3 flex gap-2 lg:hidden"
      >
        <input
          value={searchDraft}
          onChange={(e) => setSearchDraft(e.target.value)}
          placeholder="Akkaunt qidirish…"
          className="field flex-1"
          aria-label="Qidiruv"
          type="search"
        />
        <button
          type="button"
          onClick={() => setFiltersOpen((v) => !v)}
          className={[
            'tap-target flex flex-shrink-0 items-center gap-2 rounded-[11px] border px-4 text-sm font-semibold transition-colors',
            activeCount > 0 || filtersOpen
              ? 'border-brand bg-brand/[.12] text-brand'
              : 'border-white/[.12] text-muted',
          ].join(' ')}
          aria-expanded={filtersOpen}
          aria-controls="filtrlar"
        >
          Filtr
          {activeCount > 0 && (
            <span className="grid h-5 min-w-[20px] place-items-center rounded-full bg-brand px-1 text-[11px] font-bold text-white">
              {activeCount}
            </span>
          )}
        </button>
      </form>

      <div className="grid gap-6 lg:grid-cols-[260px_minmax(0,1fr)] lg:items-start">
        {/* ── Filtrlar ─────────────────────────────────────────────── */}
        <aside
          id="filtrlar"
          className={[
            'card space-y-6 p-5 sm:p-6 lg:sticky lg:top-[92px] lg:!block',
            filtersOpen ? 'block' : 'hidden',
          ].join(' ')}
        >
          {/* Katta ekranda qidiruv shu yerda turadi. */}
          <div className="hidden lg:block">
            <div className="label-caps mb-3">QIDIRUV</div>
            <form
              onSubmit={(e) => {
                e.preventDefault()
                patch({ search: searchDraft.trim() || undefined })
              }}
            >
              <input
                value={searchDraft}
                onChange={(e) => setSearchDraft(e.target.value)}
                placeholder="Masalan: Legend akkaunt"
                className="field"
                aria-label="Qidiruv"
              />
            </form>
          </div>

          <div>
            <div className="label-caps mb-3">O'YIN</div>
            <div className="flex flex-wrap gap-2">
              <FilterChip active={!query.game} onClick={() => patch({ game: undefined })}>
                Barchasi
              </FilterChip>
              {GAMES.map((g) => (
                <FilterChip
                  key={g.slug}
                  active={query.game === g.slug}
                  onClick={() => patch({ game: query.game === g.slug ? undefined : g.slug })}
                >
                  {g.glyph} {g.name}
                </FilterChip>
              ))}
            </div>
          </div>

          <div>
            <div className="label-caps mb-3">SERVER</div>
            <div className="flex flex-wrap gap-2">
              {REGIONS.map((r) => (
                <FilterChip
                  key={r}
                  active={query.region === r}
                  onClick={() => patch({ region: query.region === r ? undefined : r })}
                >
                  {r}
                </FilterChip>
              ))}
            </div>
          </div>

          <div>
            <div className="label-caps mb-3">NARX (UZS)</div>
            <div className="flex items-center gap-2">
              <input
                value={minDraft}
                onChange={(e) => setMinDraft(formatPriceInput(e.target.value))}
                onBlur={() => patch({ minPrice: minDraft ? String(parsePriceInput(minDraft)) : undefined })}
                placeholder="dan"
                inputMode="numeric"
                className="field !px-3 !py-2.5 text-sm"
                aria-label="Eng past narx"
              />
              <span className="text-dim">—</span>
              <input
                value={maxDraft}
                onChange={(e) => setMaxDraft(formatPriceInput(e.target.value))}
                onBlur={() => patch({ maxPrice: maxDraft ? String(parsePriceInput(maxDraft)) : undefined })}
                placeholder="gacha"
                inputMode="numeric"
                className="field !px-3 !py-2.5 text-sm"
                aria-label="Eng yuqori narx"
              />
            </div>
          </div>

          <label className="flex cursor-pointer items-center gap-3 py-1 text-sm">
            <input
              type="checkbox"
              checked={query.verifiedOnly === true}
              onChange={(e) => patch({ verifiedOnly: e.target.checked ? '1' : undefined })}
              className="h-5 w-5 accent-brand"
            />
            <span className="text-soft">Faqat tekshirilgan e'lonlar</span>
          </label>

          <div className="flex gap-2">
            {activeCount > 0 && (
              <button
                onClick={clearFilters}
                className="tap-target flex-1 rounded-[11px] border border-white/[.12] py-2.5 text-sm font-semibold text-muted transition-colors hover:border-brand hover:text-brand"
              >
                Tozalash
              </button>
            )}
            <button
              onClick={() => setFiltersOpen(false)}
              className="btn-primary tap-target flex-1 py-2.5 text-sm lg:hidden"
            >
              Ko'rsatish
            </button>
          </div>
        </aside>

        {/* ── Natijalar ────────────────────────────────────────────── */}
        <div>
          <div className="no-scrollbar -mx-4 mb-5 flex gap-2 overflow-x-auto px-4 sm:mx-0 sm:px-0 sm:mb-6">
            {SORTS.map((s) => (
              <FilterChip key={s.key} active={query.sort === s.key} onClick={() => patch({ sort: s.key })}>
                {s.label}
              </FilterChip>
            ))}
          </div>

          {loading ? (
            <div className="grid gap-5 sm:grid-cols-2 sm:gap-6 xl:grid-cols-3">
              {Array.from({ length: 6 }, (_, i) => (
                <SkeletonCard key={i} />
              ))}
            </div>
          ) : items.length === 0 ? (
            <EmptyState
              title="Hech narsa topilmadi"
              text="Filtrlarni o'zgartirib ko'ring yoki boshqa o'yinni tanlang."
              action={
                activeCount > 0 ? (
                  <button onClick={clearFilters} className="btn-ghost px-6 py-3">
                    Filtrlarni tozalash
                  </button>
                ) : undefined
              }
            />
          ) : (
            <>
              <div className="grid gap-5 sm:grid-cols-2 sm:gap-6 xl:grid-cols-3">
                {items.map((l) => (
                  <ListingCard key={l.id} listing={l} />
                ))}
              </div>

              {hasNext && (
                <div className="mt-8 flex justify-center sm:mt-10">
                  <button onClick={loadMore} disabled={loadingMore} className="btn-ghost w-full px-8 py-3.5 sm:w-auto">
                    {loadingMore ? <Spinner size={18} /> : 'Yana yuklash'}
                  </button>
                </div>
              )}
            </>
          )}
        </div>
      </div>
    </div>
  )
}

/** Kategoriya tabi — filtrdan kattaroq va ko'zga tashlanadigan. */
function TypeTab({
  active,
  onClick,
  glyph,
  label,
}: {
  active: boolean
  onClick: () => void
  glyph: string
  label: string
}) {
  return (
    <button
      onClick={onClick}
      aria-pressed={active}
      className={[
        'tap-target flex flex-shrink-0 items-center gap-2 whitespace-nowrap rounded-2xl border px-4 py-2.5 text-sm font-semibold transition-all duration-200',
        active
          ? 'border-brand/50 bg-brand/[.12] text-brand shadow-brand-sm'
          : 'border-white/[.08] bg-surface text-muted hover:border-white/25 hover:text-white',
      ].join(' ')}
    >
      <span className="text-base leading-none">{glyph}</span>
      {label}
    </button>
  )
}

function FilterChip({
  active,
  onClick,
  children,
}: {
  active: boolean
  onClick: () => void
  children: React.ReactNode
}) {
  return (
    <button
      onClick={onClick}
      className={[
        'whitespace-nowrap rounded-full border px-[15px] py-2 text-[13px] font-semibold transition-colors',
        active
          ? 'border-brand bg-brand/[.12] text-brand'
          : 'border-white/[.12] bg-transparent text-muted hover:border-white/30 hover:text-white',
      ].join(' ')}
      aria-pressed={active}
    >
      {children}
    </button>
  )
}
