import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { PageLoader, Spinner, StatusPill } from '../components/ui'
import { useAuth } from '../context/auth-context'
import { useToast } from '../context/toast-context'
import { ApiError, api } from '../lib/api'
import { money, shortDate } from '../lib/format'
import { TRANSACTION_TONES } from '../lib/status'
import type { AdminDisputeRow, AdminStats, AdminTransactionRow, PlatformSettings } from '../lib/types'

export default function AdminPage() {
  const { user } = useAuth()
  const toast = useToast()

  const [stats, setStats] = useState<AdminStats | null>(null)
  const [rows, setRows] = useState<AdminTransactionRow[]>([])
  const [disputes, setDisputes] = useState<AdminDisputeRow[]>([])
  const [settings, setSettings] = useState<PlatformSettings | null>(null)
  const [search, setSearch] = useState('')
  const [resolving, setResolving] = useState<string | null>(null)

  const load = useCallback(
    async (term?: string) => {
      const [s, tx, d, cfg] = await Promise.all([
        api.admin.stats(),
        api.admin.transactions(term, 1, 20),
        api.admin.disputes(true),
        api.admin.settings(),
      ])

      setStats(s)
      setRows(tx.items)
      setDisputes(d)
      setSettings(cfg)
    },
    [],
  )

  useEffect(() => {
    load().catch(() => toast.error('Admin ma\'lumotlari yuklanmadi.'))
  }, [load, toast])

  async function resolve(dispute: AdminDisputeRow, favourBuyer: boolean) {
    setResolving(dispute.id)
    try {
      await api.admin.resolveDispute(
        dispute.id,
        favourBuyer,
        favourBuyer ? 'Dalillar xaridor foydasiga.' : 'Dalillar sotuvchi foydasiga.',
      )
      toast.success(favourBuyer ? 'Pul xaridorga qaytarildi.' : 'Pul sotuvchiga chiqarildi.')
      await load(search || undefined)
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : 'Qaror qabul qilinmadi.')
    } finally {
      setResolving(null)
    }
  }

  if (!stats || !settings) return <PageLoader label="Admin panel yuklanmoqda…" />

  return (
    <div className="page pb-24 pt-11">
      <div className="mb-7 flex flex-col gap-4 sm:flex-row sm:flex-wrap sm:items-center sm:justify-between">
        <div>
          <h1 className="m-0 mb-1.5 font-display text-[clamp(24px,6vw,32px)] font-bold tracking-[-.02em]">
            Admin panel
          </h1>
          <p className="m-0 text-sm text-muted sm:text-[15px]">
            Moderator: @{user?.username} · {stats.activeListings} aktiv e'lon, {stats.pendingListings} tekshiruvda
          </p>
        </div>

        <form
          onSubmit={(e) => {
            e.preventDefault()
            load(search || undefined)
          }}
          className="flex gap-2.5"
        >
          <input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Qidirish..."
            className="field !py-2.5 text-sm sm:w-[220px]"
            aria-label="Bitimlar bo'yicha qidiruv"
            type="search"
          />
          <button
            type="submit"
            className="tap-target flex-shrink-0 rounded-[11px] bg-brand px-5 py-2.5 text-sm font-semibold text-white"
          >
            Qidirish
          </button>
        </form>
      </div>

      {/* ── Ko'rsatkichlar ───────────────────────────────────────────── */}
      <div className="mb-6 grid grid-cols-2 gap-3 sm:gap-[18px] lg:grid-cols-4">
        <StatCard
          label="Jami foydalanuvchilar"
          value={money(stats.totalUsers)}
          delta={`${stats.usersGrowthPercent >= 0 ? '↑' : '↓'} ${Math.abs(stats.usersGrowthPercent)}% oxirgi oyda`}
        />
        <StatCard
          label="Jami bitimlar"
          value={money(stats.totalTransactions)}
          delta={`${stats.transactionsGrowthPercent >= 0 ? '↑' : '↓'} ${Math.abs(stats.transactionsGrowthPercent)}% oxirgi oyda`}
        />
        <StatCard
          label="Daromad (UZS)"
          value={money(stats.totalRevenue)}
          color="#FF6B35"
          delta={`${stats.revenueGrowthPercent >= 0 ? '↑' : '↓'} ${Math.abs(stats.revenueGrowthPercent)}% oxirgi oyda`}
        />
        <StatCard
          label="Kutilayotgan nizolar"
          value={String(stats.openDisputes)}
          color={stats.openDisputes > 0 ? '#EF4444' : '#fff'}
          delta={`${stats.disputesOver24h} tasi 24 soatdan oshgan`}
          deltaColor={stats.disputesOver24h > 0 ? '#FCA5A5' : '#10B981'}
        />
      </div>

      <div className="grid items-start gap-5 lg:grid-cols-[minmax(0,1.5fr)_minmax(0,1fr)]">
        {/* ── So'nggi bitimlar ─────────────────────────────────────── */}
        <div className="card overflow-hidden">
          <div className="border-b border-white/[.06] px-[22px] py-5 font-display text-[17px] font-bold">
            So'nggi bitimlar
          </div>

          <div className="hidden grid-cols-[80px_1fr_1fr_130px_120px] gap-3 bg-white/[.03] px-[22px] py-3.5 text-[11px] font-bold tracking-[.08em] text-dim md:grid">
            <div>ID</div>
            <div>XARIDOR</div>
            <div>SOTUVCHI</div>
            <div>MIQDOR</div>
            <div>HOLAT</div>
          </div>

          {rows.length === 0 && <div className="px-[22px] py-10 text-center text-sm text-dim">Bitim yo'q.</div>}

          {rows.map((r) => (
            <Link
              key={r.id}
              to={`/transactions/${r.id}`}
              className="block border-t border-white/[.06] text-[13.5px] text-white transition-colors hover:bg-white/[.02] hover:text-white"
            >
              {/* Telefonda ustun sarlavhalari ko'rinmaydi, shuning uchun
                  qator o'zi tushunarli kartochkaga aylanadi. */}
              <div className="flex flex-col gap-1.5 px-4 py-3.5 md:hidden">
                <div className="flex items-center justify-between gap-3">
                  <span className="font-mono text-dim">#{r.number}</span>
                  <StatusPill tone={TRANSACTION_TONES[r.status]} className="!text-[11.5px]" />
                </div>
                <div className="flex items-center justify-between gap-3">
                  <span className="truncate text-xs text-muted">
                    @{r.buyerUsername} → @{r.sellerUsername}
                  </span>
                  <span className="flex-shrink-0 font-display font-semibold">{money(r.amount)}</span>
                </div>
              </div>

              <div className="hidden grid-cols-[80px_1fr_1fr_130px_120px] items-center gap-3 px-[22px] py-[15px] md:grid">
                <div className="font-mono text-dim">#{r.number}</div>
                <div className="truncate">@{r.buyerUsername}</div>
                <div className="truncate">@{r.sellerUsername}</div>
                <div className="font-display font-semibold">{money(r.amount)}</div>
                <div>
                  <StatusPill tone={TRANSACTION_TONES[r.status]} className="!text-[11.5px]" />
                </div>
              </div>
            </Link>
          ))}
        </div>

        <div className="flex flex-col gap-5">
          {/* ── Nizolar ───────────────────────────────────────────── */}
          <div className="overflow-hidden rounded-card border border-danger/25 bg-danger/[.05]">
            <div className="flex items-center justify-between px-[22px] py-5 font-display text-[17px] font-bold">
              Nizolar
              <span className="rounded-full bg-danger px-2.5 py-[3px] text-xs">{disputes.length}</span>
            </div>

            {disputes.length === 0 && (
              <div className="px-[22px] pb-6 text-sm text-muted">Ochiq nizolar yo'q. 🎉</div>
            )}

            {disputes.map((d) => (
              <div key={d.id} className="border-t border-danger/15 px-[22px] py-4">
                <div className="mb-1.5 flex items-center justify-between">
                  <Link to={`/transactions/${d.transactionId}`} className="font-mono text-[13px] text-muted hover:text-brand">
                    #{d.transactionNumber}
                  </Link>
                  <span className={`text-xs ${d.ageHours > 24 ? 'text-danger-fg' : 'text-muted'}`}>
                    {Math.round(d.ageHours)} soat
                  </span>
                </div>

                <div className="mb-2.5 text-sm leading-relaxed">{d.reason}</div>

                <div className="flex flex-wrap gap-2">
                  <button
                    onClick={() => resolve(d, true)}
                    disabled={resolving === d.id}
                    className="rounded-[9px] border border-info/40 px-3.5 py-[7px] text-[13px] font-semibold text-info-fg transition-colors hover:bg-info hover:text-white"
                  >
                    {resolving === d.id ? <Spinner size={14} /> : 'Xaridor foydasiga'}
                  </button>
                  <button
                    onClick={() => resolve(d, false)}
                    disabled={resolving === d.id}
                    className="rounded-[9px] border border-success/40 px-3.5 py-[7px] text-[13px] font-semibold text-success-fg transition-colors hover:bg-success hover:text-white"
                  >
                    Sotuvchi foydasiga
                  </button>
                </div>
              </div>
            ))}
          </div>

          {/* ── Sozlamalar ────────────────────────────────────────── */}
          <div className="card p-6">
            <div className="mb-[18px] font-display text-[17px] font-bold">Platforma sozlamalari</div>

            <div className="flex flex-col gap-4 text-sm">
              <SettingRow label="Escrow komissiyasi" value={`${(settings.commissionRate * 100).toFixed(0)}%`} valueColor="#FF6B35" />
              <SettingRow label="Avto-release muddati" value={`${settings.autoReleaseHours} soat`} />
              <SettingRow label="Eng past narx" value={`${money(settings.minListingPrice)} so'm`} />
              <SettingRow label="E'lon avto-tasdiq" value={settings.autoApproveListings ? 'Yoqilgan' : "O'chirilgan"} />
              <SettingRow
                label="To'lov rejimi"
                value={settings.sandboxPayments ? 'Sandbox' : 'Jonli'}
                valueColor={settings.sandboxPayments ? '#F59E0B' : '#10B981'}
              />
            </div>

            <p className="mt-5 border-t border-white/[.08] pt-4 text-xs leading-relaxed text-dim">
              Sozlamalar <span className="font-mono text-soft">appsettings.json</span> va{' '}
              <span className="font-mono text-soft">.env</span> orqali boshqariladi.
            </p>
          </div>
        </div>
      </div>

      <p className="mt-8 text-center text-xs text-dim">
        Oxirgi yangilanish: {shortDate(new Date().toISOString())}
      </p>
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
    <div className="flex items-center justify-between">
      <span className="text-muted">{label}</span>
      <span className="font-display font-bold" style={{ color: valueColor }}>
        {value}
      </span>
    </div>
  )
}
