import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { Avatar, GameCover, PageLoader, SkeletonCard, Spinner, StatusPill } from '../components/ui'
import { useAuth } from '../context/auth-context'
import { useToast } from '../context/toast-context'
import { ApiError, api } from '../lib/api'
import { money, relativeTime } from '../lib/format'
import { LISTING_TONES } from '../lib/status'
import type { BoostRequest, ListingCard } from '../lib/types'

type Tab = 'listings' | 'boosts' | 'settings'

const TABS: { key: Tab; label: string }[] = [
  { key: 'listings', label: "Mening e'lonlarim" },
  { key: 'boosts', label: 'Ko\'tarishlarim' },
  { key: 'settings', label: 'Sozlamalar' },
]

export default function ProfilePage() {
  const { user, refreshUser } = useAuth()
  const [params, setParams] = useSearchParams()
  const tab = (params.get('tab') as Tab) ?? 'listings'

  if (!user) return <PageLoader />

  return (
    <div className="page pb-20 pt-8 sm:pb-24 sm:pt-11">
      {/* ── Profil sarlavhasi ────────────────────────────────────────── */}
      <div className="mb-6 flex flex-wrap items-center gap-5 rounded-hero border border-white/[.08] bg-gradient-to-br from-[#1C1C36] to-[#141428] p-5 sm:gap-[26px] sm:p-8">
        <Avatar name={user.username} size={64} />

        <div className="min-w-[180px] flex-1">
          <div className="mb-2 flex flex-wrap items-center gap-2 sm:gap-3">
            <h1 className="m-0 font-display text-[clamp(20px,5.5vw,30px)] font-bold tracking-[-.02em]">
              @{user.username}
            </h1>
            {user.isVerified && (
              <span className="rounded-full border border-success/[.32] bg-success/[.14] px-3 py-[5px] text-xs font-semibold text-success-fg">
                ✓ Tasdiqlangan
              </span>
            )}
          </div>

          {user.fullName && (
            <div className="mb-1.5 font-display text-[15px] font-semibold text-soft sm:text-base">
              {user.fullName}
            </div>
          )}

          <div className="text-[13px] text-warning sm:text-sm">
            {'★'.repeat(Math.round(user.rating) || 0) || '☆☆☆☆☆'}{' '}
            <span className="text-muted">
              {user.rating.toFixed(1)} reyting
              {user.city ? ` · ${user.city}` : ''} · {new Date(user.createdAt).getFullYear()}-yildan
            </span>
          </div>
        </div>

        <div className="flex w-full justify-between gap-4 border-t border-white/[.08] pt-4 sm:w-auto sm:justify-start sm:gap-7 sm:border-0 sm:pt-0">
          <Metric value={String(user.totalSales)} label="Sotilgan" />
          <Metric value={String(user.ratingCount)} label="Baholar" />
          <Metric value={user.rating.toFixed(1)} label="Reyting" color="#FF6B35" />
        </div>
      </div>

      {/* ── Tablar ──────────────────────────────────────────────────── */}
      <div className="no-scrollbar -mx-4 mb-6 flex gap-2 overflow-x-auto border-b border-white/[.08] px-4 sm:mx-0 sm:px-0">
        {TABS.map((t) => (
          <button
            key={t.key}
            onClick={() => setParams(t.key === 'listings' ? {} : { tab: t.key }, { replace: true })}
            className={[
              'whitespace-nowrap border-b-2 px-[18px] py-3.5 text-[15px] font-semibold transition-colors',
              tab === t.key ? 'border-brand text-white' : 'border-transparent text-dim hover:text-white',
            ].join(' ')}
          >
            {t.label}
          </button>
        ))}
      </div>

      {tab === 'listings' && <MyListingsTab />}
      {tab === 'boosts' && <MyBoostsTab />}
      {tab === 'settings' && <SettingsTab onSaved={refreshUser} />}
    </div>
  )
}

function Metric({ value, label, color = '#fff' }: { value: string; label: string; color?: string }) {
  return (
    <div className="text-center">
      <div className="font-display text-[22px] font-extrabold sm:text-[26px]" style={{ color }}>
        {value}
      </div>
      <div className="text-xs text-muted sm:text-[13px]">{label}</div>
    </div>
  )
}

function MyListingsTab() {
  const toast = useToast()
  const [items, setItems] = useState<ListingCard[] | null>(null)

  useEffect(() => {
    api.users
      .myListings()
      .then((res) => setItems(res.items))
      .catch(() => setItems([]))
  }, [])

  async function toggleHidden(listing: ListingCard) {
    const next = listing.status === 'Hidden' ? 'Active' : 'Hidden'
    try {
      await api.listings.update(listing.id, { status: next })
      setItems((current) =>
        (current ?? []).map((l) => (l.id === listing.id ? { ...l, status: next as ListingCard['status'] } : l)),
      )
      toast.success(next === 'Hidden' ? "E'lon yashirildi." : "E'lon yana sotuvda.")
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : 'Amal bajarilmadi.')
    }
  }

  if (items === null) {
    return (
      <div className="grid grid-cols-2 gap-3 sm:gap-5 lg:grid-cols-4">
        {Array.from({ length: 4 }, (_, i) => (
          <SkeletonCard key={i} />
        ))}
      </div>
    )
  }

  return (
    <div className="grid grid-cols-2 gap-3 sm:gap-5 lg:grid-cols-4">
      {items.map((l) => (
        <div key={l.id} className="card overflow-hidden">
          <Link to={`/listings/${l.id}`}>
            <GameCover glyph={l.gameGlyph} color={l.gameColor} image={l.coverImage} glyphSize={32}>
              <StatusPill tone={LISTING_TONES[l.status]} className="absolute right-2.5 top-2.5 !text-[11px]" />
            </GameCover>
          </Link>

          <div className="p-3.5">
            <div className="mb-2.5 line-clamp-2 min-h-[38px] text-sm font-semibold leading-[1.35]">
              {l.title}
            </div>
            <div className="mb-3 font-display text-[17px] font-bold text-brand">{money(l.price)}</div>

            {l.status !== 'Sold' && l.status !== 'Reserved' && (
              <button
                onClick={() => toggleHidden(l)}
                className="w-full rounded-[10px] border border-white/[.12] py-2 text-xs font-semibold text-muted transition-colors hover:border-brand hover:text-brand"
              >
                {l.status === 'Hidden' ? 'Sotuvga qaytarish' : 'Yashirish'}
              </button>
            )}
          </div>
        </div>
      ))}

      <Link
        to="/create"
        className="grid min-h-[170px] cursor-pointer place-items-center rounded-2xl border-[1.5px] border-dashed border-brand/40 bg-brand/[.04] p-5 text-center transition-colors hover:bg-brand/[.09] sm:min-h-[220px]"
      >
        <div>
          <div className="mb-2.5 text-[32px] text-brand">+</div>
          <div className="font-display text-[15px] font-semibold text-brand">Yangi e'lon qo'shish</div>
        </div>
      </Link>
    </div>
  )
}

const BOOST_TONE: Record<BoostRequest['status'], { label: string; cls: string }> = {
  Pending: { label: 'Kutilmoqda', cls: 'border-warning/30 bg-warning/[.10] text-[#F5D08C]' },
  Approved: { label: 'Tasdiqlangan', cls: 'border-success/30 bg-success/[.10] text-success-fg' },
  Rejected: { label: 'Rad etilgan', cls: 'border-danger/30 bg-danger/[.10] text-danger-fg' },
}

function MyBoostsTab() {
  const [items, setItems] = useState<BoostRequest[] | null>(null)

  useEffect(() => {
    api.boosts
      .mine()
      .then(setItems)
      .catch(() => setItems([]))
  }, [])

  if (items === null) return <PageLoader label="Yuklanmoqda…" />

  if (items.length === 0) {
    return (
      <div className="card flex flex-col items-center gap-3 px-6 py-16 text-center">
        <div className="text-4xl">🚀</div>
        <h3 className="font-display text-xl font-bold">Hali ko'tarish so'rovi yo'q</h3>
        <p className="max-w-md text-[15px] leading-relaxed text-muted">
          E'loningizni ro'yxat tepasiga chiqarish uchun uning sahifasidagi "TOP ga chiqarish" tugmasini bosing.
        </p>
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-3">
      {items.map((b) => {
        const tone = BOOST_TONE[b.status]
        return (
          <div key={b.id} className="card flex flex-wrap items-center justify-between gap-3 p-4 sm:p-5">
            <div className="min-w-0 flex-1">
              <Link to={`/listings/${b.listingId}`} className="line-clamp-1 font-semibold hover:text-brand">
                {b.listingTitle}
              </Link>
              <div className="mt-1 text-[13px] text-muted">
                {b.days} kun · {money(b.amount)} so'm · {relativeTime(b.createdAt)}
              </div>
              {b.status === 'Rejected' && b.reviewNote && (
                <div className="mt-1 text-[13px] text-danger-fg">Sabab: {b.reviewNote}</div>
              )}
              {b.status === 'Approved' && b.boostedUntil && (
                <div className="mt-1 text-[13px] text-success-fg">
                  {new Date(b.boostedUntil).toLocaleDateString('uz')} gacha TOP da
                </div>
              )}
            </div>
            <span className={`rounded-full border px-3 py-1 text-xs font-semibold ${tone.cls}`}>{tone.label}</span>
          </div>
        )
      })}
    </div>
  )
}

function SettingsTab({ onSaved }: { onSaved: () => Promise<void> }) {
  const { user } = useAuth()
  const toast = useToast()

  const [form, setForm] = useState({
    firstName: user?.firstName ?? '',
    lastName: user?.lastName ?? '',
    username: user?.username ?? '',
    phone: user?.phone ?? '',
    telegramUsername: user?.telegramUsername ?? '',
    city: user?.city ?? '',
  })

  const [notify, setNotify] = useState({ telegram: true, newMessage: true, marketing: false })
  const [saving, setSaving] = useState(false)
  const [passwords, setPasswords] = useState({ current: '', next: '' })

  async function saveProfile() {
    setSaving(true)
    try {
      await api.users.updateProfile({
        firstName: form.firstName,
        lastName: form.lastName,
        username: form.username,
        phone: form.phone,
        telegramUsername: form.telegramUsername,
        city: form.city,
      })
      await onSaved()
      toast.success('Profil yangilandi.')
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : 'Saqlab bo\'lmadi.')
    } finally {
      setSaving(false)
    }
  }

  async function saveNotifications(next: typeof notify) {
    setNotify(next)
    try {
      await api.users.updateNotifications({
        notifyTelegram: next.telegram,
        notifyNewMessage: next.newMessage,
        notifyMarketing: next.marketing,
      })
    } catch {
      toast.error('Sozlama saqlanmadi.')
    }
  }

  async function changePassword() {
    try {
      await api.auth.changePassword({ currentPassword: passwords.current, newPassword: passwords.next })
      setPasswords({ current: '', next: '' })
      toast.success("Parol o'zgartirildi.")
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "Parolni o'zgartirib bo'lmadi.")
    }
  }

  return (
    <div className="flex max-w-[620px] flex-col gap-[18px]">
      <div className="card p-5 sm:p-[26px]">
        <div className="label-caps mb-[18px]">SHAXSIY MA'LUMOTLAR</div>

        <div className="flex flex-col gap-3.5">
          <div className="grid grid-cols-1 gap-3.5 sm:grid-cols-2">
            <Field label="Ism" value={form.firstName} onChange={(v) => setForm({ ...form, firstName: v })} />
            <Field label="Familiya" value={form.lastName} onChange={(v) => setForm({ ...form, lastName: v })} />
          </div>
          <Field label="Foydalanuvchi nomi" value={form.username} onChange={(v) => setForm({ ...form, username: v })} />
          <Field label="Telefon" value={form.phone} onChange={(v) => setForm({ ...form, phone: v })} />
          <Field
            label="Telegram"
            value={form.telegramUsername ?? ''}
            onChange={(v) => setForm({ ...form, telegramUsername: v })}
          />
          <Field label="Shahar" value={form.city ?? ''} onChange={(v) => setForm({ ...form, city: v })} />
        </div>

        <button onClick={saveProfile} disabled={saving} className="btn-primary mt-5 w-full py-3.5">
          {saving ? <Spinner size={18} /> : 'Saqlash'}
        </button>
      </div>

      <div className="card p-5 sm:p-[26px]">
        <div className="label-caps mb-[18px]">BILDIRISHNOMALAR</div>

        <div className="flex flex-col gap-3.5">
          <Toggle
            label="Telegram bildirishnomalar"
            on={notify.telegram}
            onChange={(v) => saveNotifications({ ...notify, telegram: v })}
          />
          <Toggle
            label="Yangi xabar haqida"
            on={notify.newMessage}
            onChange={(v) => saveNotifications({ ...notify, newMessage: v })}
          />
          <Toggle
            label="Marketing xabarlari"
            on={notify.marketing}
            onChange={(v) => saveNotifications({ ...notify, marketing: v })}
          />
        </div>
      </div>

      <div className="card p-5 sm:p-[26px]">
        <div className="label-caps mb-[18px]">PAROLNI O'ZGARTIRISH</div>

        <div className="flex flex-col gap-3.5">
          <Field
            label="Joriy parol"
            type="password"
            value={passwords.current}
            onChange={(v) => setPasswords({ ...passwords, current: v })}
          />
          <Field
            label="Yangi parol"
            type="password"
            value={passwords.next}
            onChange={(v) => setPasswords({ ...passwords, next: v })}
          />
        </div>

        <button
          onClick={changePassword}
          disabled={!passwords.current || passwords.next.length < 6}
          className="btn-ghost mt-5 w-full py-3.5"
        >
          Parolni o'zgartirish
        </button>
      </div>
    </div>
  )
}

function Field({
  label,
  value,
  onChange,
  type = 'text',
}: {
  label: string
  value: string
  onChange: (value: string) => void
  type?: string
}) {
  return (
    <label className="block">
      <span className="mb-[7px] block text-[13px] text-muted">{label}</span>
      <input type={type} value={value} onChange={(e) => onChange(e.target.value)} className="field" />
    </label>
  )
}

function Toggle({ label, on, onChange }: { label: string; on: boolean; onChange: (on: boolean) => void }) {
  return (
    <button
      onClick={() => onChange(!on)}
      className="flex items-center justify-between text-left"
      role="switch"
      aria-checked={on}
    >
      <span className={`text-[15px] ${on ? 'text-white' : 'text-muted'}`}>{label}</span>
      <span
        className={`flex h-[26px] w-[46px] items-center rounded-full p-[3px] transition-colors ${
          on ? 'justify-end bg-brand' : 'justify-start bg-white/[.14]'
        }`}
      >
        <span className={`h-5 w-5 rounded-full ${on ? 'bg-white' : 'bg-dim'}`} />
      </span>
    </button>
  )
}
