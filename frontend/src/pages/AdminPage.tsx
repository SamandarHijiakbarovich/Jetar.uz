import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { PageLoader, Spinner, StatusPill } from '../components/ui'
import { useAuth } from '../context/auth-context'
import { useToast } from '../context/toast-context'
import { ApiError, api } from '../lib/api'
import { money, relativeTime, shortDate } from '../lib/format'
import { mediaUrl } from '../lib/media'
import { LISTING_TONES } from '../lib/status'
import type { AdminStats, BoostRequest, ListingCard, PlatformSettings } from '../lib/types'
import AdminUsers from './AdminUsers'

type AdminTab = 'boosts' | 'dashboard' | 'users'

export default function AdminPage() {
  const { user } = useAuth()
  const toast = useToast()

  const [stats, setStats] = useState<AdminStats | null>(null)
  const [listings, setListings] = useState<ListingCard[]>([])
  const [settings, setSettings] = useState<PlatformSettings | null>(null)
  const [tab, setTab] = useState<AdminTab>('boosts')

  const load = useCallback(async () => {
    const [s, recent, cfg] = await Promise.all([
      api.admin.stats(),
      api.listings.search({ sort: 'newest', pageSize: 10 }),
      api.admin.settings(),
    ])

    setStats(s)
    setListings(recent.items)
    setSettings(cfg)
  }, [])

  useEffect(() => {
    load().catch(() => toast.error('Admin ma\'lumotlari yuklanmadi.'))
  }, [load, toast])

  async function toggleVerify(listing: ListingCard) {
    try {
      await api.admin.verifyListing(listing.id, !listing.isVerified)
      setListings((c) => c.map((l) => (l.id === listing.id ? { ...l, isVerified: !l.isVerified } : l)))
      toast.success(listing.isVerified ? 'Belgi olib tashlandi.' : "E'lon tekshirilgan deb belgilandi.")
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : 'Amal bajarilmadi.')
    }
  }

  if (!stats || !settings) return <PageLoader label="Admin panel yuklanmoqda…" />

  return (
    <div className="page pb-24 pt-11">
      <div className="mb-5">
        <h1 className="m-0 mb-1.5 font-display text-[clamp(24px,6vw,32px)] font-bold tracking-[-.02em]">
          Admin panel
        </h1>
        <p className="m-0 text-sm text-muted sm:text-[15px]">
          {user?.role === 'Admin' ? 'Administrator' : 'Moderator'}: @{user?.username} · {stats.activeListings} aktiv
          e'lon, {stats.pendingListings} tekshiruvda
        </p>
      </div>

      {/* ── Tablar ───────────────────────────────────────────────────── */}
      <div className="mb-7 flex gap-1 border-b border-white/[.08]">
        {(
          [
            ['boosts', 'Ko\'tarish so\'rovlari'],
            ['dashboard', 'Boshqaruv paneli'],
            ['users', 'Foydalanuvchilar'],
          ] as [AdminTab, string][]
        ).map(([key, label]) => (
          <button
            key={key}
            onClick={() => setTab(key)}
            className={`-mb-px border-b-2 px-4 py-2.5 text-sm font-semibold transition-colors ${
              tab === key ? 'border-brand text-white' : 'border-transparent text-muted hover:text-white'
            }`}
          >
            {label}
          </button>
        ))}
      </div>

      {tab === 'boosts' && <BoostsTab />}

      {tab === 'users' && <AdminUsers />}

      {tab === 'dashboard' && (
        <>
          {/* ── Ko'rsatkichlar ───────────────────────────────────────── */}
          <div className="mb-6 grid grid-cols-2 gap-3 sm:gap-[18px] lg:grid-cols-4">
            <StatCard
              label="Jami foydalanuvchilar"
              value={money(stats.totalUsers)}
              delta={`${stats.usersGrowthPercent >= 0 ? '↑' : '↓'} ${Math.abs(stats.usersGrowthPercent)}% oxirgi oyda`}
            />
            <StatCard
              label="Faol e'lonlar"
              value={money(stats.activeListings)}
              delta={`${stats.pendingListings} tekshiruvda · jami ${stats.totalListings}`}
              deltaColor="#94A3B8"
            />
            <StatCard
              label="Kutilayotgan boostlar"
              value={String(stats.boostsPending)}
              color={stats.boostsPending > 0 ? '#F59E0B' : '#fff'}
              delta={`${stats.boostsApproved} ta tasdiqlangan`}
              deltaColor={stats.boostsPending > 0 ? '#FCD79A' : '#10B981'}
            />
            <StatCard
              label="Boost daromadi (UZS)"
              value={money(stats.boostRevenue)}
              color="#FF6B35"
              delta={`${stats.boostRevenueGrowthPercent >= 0 ? '↑' : '↓'} ${Math.abs(stats.boostRevenueGrowthPercent)}% oxirgi oyda`}
            />
          </div>

          <div className="grid items-start gap-5 lg:grid-cols-[minmax(0,1.5fr)_minmax(0,1fr)]">
            {/* ── So'nggi e'lonlar ─────────────────────────────────────── */}
            <div className="card overflow-hidden">
              <div className="border-b border-white/[.06] px-[22px] py-5 font-display text-[17px] font-bold">
                So'nggi e'lonlar
              </div>

              {listings.length === 0 && (
                <div className="px-[22px] py-10 text-center text-sm text-dim">E'lon yo'q.</div>
              )}

              {listings.map((l) => (
                <div
                  key={l.id}
                  className="flex items-center gap-3 border-t border-white/[.06] px-4 py-3 sm:px-[22px]"
                >
                  <Link to={`/listings/${l.id}`} className="flex min-w-0 flex-1 items-center gap-3 text-white hover:text-white">
                    <span className="grid h-9 w-9 flex-shrink-0 place-items-center rounded-[9px] bg-white/[.05] text-lg">
                      {l.gameGlyph}
                    </span>
                    <span className="min-w-0">
                      <span className="line-clamp-1 text-[13.5px] font-semibold">{l.title}</span>
                      <span className="text-xs text-dim">
                        @{l.sellerUsername} · {money(l.price)} · {relativeTime(l.createdAt)}
                      </span>
                    </span>
                  </Link>

                  <StatusPill tone={LISTING_TONES[l.status]} className="!hidden !text-[11px] sm:!inline-block" />

                  <button
                    onClick={() => toggleVerify(l)}
                    className={`flex-shrink-0 rounded-[9px] border px-2.5 py-1.5 text-xs font-semibold transition-colors ${
                      l.isVerified
                        ? 'border-success/40 bg-success/[.10] text-success-fg'
                        : 'border-white/[.14] text-muted hover:border-brand hover:text-brand'
                    }`}
                    title={l.isVerified ? 'Tekshirilgan' : 'Tekshirilgan deb belgilash'}
                  >
                    {l.isVerified ? '✓ Tekshirilgan' : 'Tasdiqlash'}
                  </button>
                </div>
              ))}
            </div>

            {/* ── Sozlamalar ────────────────────────────────────────── */}
            <div className="card p-6">
              <div className="mb-[18px] font-display text-[17px] font-bold">Platforma sozlamalari</div>

              <div className="flex flex-col gap-4 text-sm">
                <SettingRow
                  label="Escrow (to'lov)"
                  value={settings.escrowEnabled ? 'Yoqilgan' : "O'chirilgan"}
                  valueColor={settings.escrowEnabled ? '#F59E0B' : '#10B981'}
                />
                <SettingRow
                  label="Kontakt uchun login"
                  value={settings.contactRequiresLogin ? 'Talab qilinadi' : 'Ochiq'}
                />
                <SettingRow label="Boost kartasi" value={settings.boostCardNumber} />
                <SettingRow label="Boost tariflari" value={`${settings.boostTierCount} ta`} valueColor="#FF6B35" />
                <SettingRow label="Eng past narx" value={`${money(settings.minListingPrice)} so'm`} />
                <SettingRow label="E'lon avto-tasdiq" value={settings.autoApproveListings ? 'Yoqilgan' : "O'chirilgan"} />
              </div>

              <p className="mt-5 border-t border-white/[.08] pt-4 text-xs leading-relaxed text-dim">
                Sozlamalar <span className="font-mono text-soft">appsettings.json</span> va{' '}
                <span className="font-mono text-soft">.env</span> orqali boshqariladi.
              </p>
            </div>
          </div>

          <p className="mt-8 text-center text-xs text-dim">
            Oxirgi yangilanish: {shortDate(new Date().toISOString())}
          </p>
        </>
      )}
    </div>
  )
}

function BoostsTab() {
  const toast = useToast()
  const [items, setItems] = useState<BoostRequest[] | null>(null)
  const [busy, setBusy] = useState<string | null>(null)

  function load() {
    api.admin
      .boosts()
      .then(setItems)
      .catch(() => toast.error('Ko\'tarish so\'rovlari yuklanmadi.'))
  }

  useEffect(load, []) // eslint-disable-line react-hooks/exhaustive-deps

  async function approve(id: string) {
    setBusy(id)
    try {
      await api.admin.approveBoost(id)
      toast.success('E\'lon ko\'tarildi.')
      setItems((c) => (c ?? []).filter((b) => b.id !== id))
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : 'Amal bajarilmadi.')
    } finally {
      setBusy(null)
    }
  }

  async function reject(id: string) {
    const note = window.prompt('Rad etish sababi (ixtiyoriy):') ?? undefined
    setBusy(id)
    try {
      await api.admin.rejectBoost(id, note)
      toast.success('So\'rov rad etildi.')
      setItems((c) => (c ?? []).filter((b) => b.id !== id))
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : 'Amal bajarilmadi.')
    } finally {
      setBusy(null)
    }
  }

  if (items === null) return <PageLoader label="Yuklanmoqda…" />

  if (items.length === 0) {
    return (
      <div className="card px-6 py-16 text-center">
        <div className="mb-3 text-4xl">✅</div>
        <h3 className="font-display text-xl font-bold">Kutilayotgan so'rov yo'q</h3>
        <p className="mt-2 text-sm text-muted">Yangi ko'tarish so'rovlari shu yerda paydo bo'ladi.</p>
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-3">
      {items.map((b) => (
        <div key={b.id} className="card flex flex-col gap-4 p-4 sm:flex-row sm:items-center sm:justify-between sm:p-5">
          <div className="min-w-0 flex-1">
            <Link to={`/listings/${b.listingId}`} className="line-clamp-1 font-semibold hover:text-brand">
              {b.listingTitle}
            </Link>
            <div className="mt-1 text-[13px] text-muted">
              @{b.sellerUsername} · {b.days} kun ·{' '}
              <strong className="font-display text-brand">{money(b.amount)} so'm</strong> · {relativeTime(b.createdAt)}
            </div>
          </div>

          <div className="flex items-center gap-2">
            {b.screenshotUrl && (
              <a
                href={mediaUrl(b.screenshotUrl) ?? '#'}
                target="_blank"
                rel="noopener noreferrer"
                className="rounded-[9px] border border-white/[.14] px-3 py-2 text-[13px] font-semibold text-muted hover:border-brand hover:text-brand"
              >
                📎 Chek
              </a>
            )}
            <button
              onClick={() => reject(b.id)}
              disabled={busy === b.id}
              className="rounded-[9px] border border-danger/40 px-3.5 py-2 text-[13px] font-semibold text-danger-fg transition-colors hover:bg-danger hover:text-white"
            >
              Rad etish
            </button>
            <button
              onClick={() => approve(b.id)}
              disabled={busy === b.id}
              className="rounded-[9px] border border-success/40 px-3.5 py-2 text-[13px] font-semibold text-success-fg transition-colors hover:bg-success hover:text-white"
            >
              {busy === b.id ? <Spinner size={14} /> : 'Tasdiqlash'}
            </button>
          </div>
        </div>
      ))}
    </div>
  )
}

function StatCard({
  label,
  value,
  delta,
  color = '#fff',
  deltaColor = '#10B981',
}: {
  label: string
  value: string
  delta: string
  color?: string
  deltaColor?: string
}) {
  return (
    <div className="card p-4 sm:p-6">
      <div className="mb-2 text-xs text-muted sm:mb-3 sm:text-[13px]">{label}</div>
      <div
        className="font-display text-[clamp(20px,5.5vw,30px)] font-extrabold tracking-[-.02em]"
        style={{ color }}
      >
        {value}
      </div>
      <div className="mt-1.5 text-[11px] sm:mt-2 sm:text-[13px]" style={{ color: deltaColor }}>
        {delta}
      </div>
    </div>
  )
}

function SettingRow({ label, value, valueColor = '#fff' }: { label: string; value: string; valueColor?: string }) {
  return (
    <div className="flex items-center justify-between gap-3">
      <span className="text-muted">{label}</span>
      <span className="truncate font-display font-bold" style={{ color: valueColor }}>
        {value}
      </span>
    </div>
  )
}
