import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { Avatar, EmptyState, Spinner, Stars } from '../components/ui'
import { useAuth } from '../context/auth-context'
import { useToast } from '../context/toast-context'
import { ApiError, api } from '../lib/api'
import { shortDate } from '../lib/format'
import type { AdminUserRow, UserRole } from '../lib/types'

type StatusFilter = 'all' | 'blocked' | 'verified'

const ROLE_BADGE: Record<UserRole, { label: string; className: string }> = {
  User: { label: 'Foydalanuvchi', className: 'bg-white/[.06] text-dim' },
  Moderator: { label: 'Moderator', className: 'bg-info/15 text-info-fg' },
  Admin: { label: 'Admin', className: 'bg-brand/20 text-brand' },
}

export default function AdminUsers() {
  const { user: me } = useAuth()
  const toast = useToast()
  const isAdmin = me?.role === 'Admin'

  const [rows, setRows] = useState<AdminUserRow[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [role, setRole] = useState<UserRole | ''>('')
  const [status, setStatus] = useState<StatusFilter>('all')
  const [loading, setLoading] = useState(true)
  const [acting, setActing] = useState<string | null>(null)

  const pageSize = 20

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const res = await api.admin.users({
        search: search.trim() || undefined,
        role: role || undefined,
        blocked: status === 'blocked' ? true : undefined,
        verified: status === 'verified' ? true : undefined,
        page,
        pageSize,
      })
      setRows(res.items)
      setTotal(res.totalCount)
    } catch {
      toast.error('Foydalanuvchilar yuklanmadi.')
    } finally {
      setLoading(false)
    }
  }, [search, role, status, page, toast])

  useEffect(() => {
    load()
  }, [load])

  async function act(id: string, fn: () => Promise<unknown>, ok: string) {
    setActing(id)
    try {
      await fn()
      toast.success(ok)
      await load()
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : 'Amal bajarilmadi.')
    } finally {
      setActing(null)
    }
  }

  const totalPages = Math.max(1, Math.ceil(total / pageSize))

  return (
    <div>
      {/* ── Filtrlar ─────────────────────────────────────────────────── */}
      <form
        onSubmit={(e) => {
          e.preventDefault()
          setPage(1)
          load()
        }}
        className="mb-5 flex flex-wrap items-center gap-2.5"
      >
        <input
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Username, telefon yoki ism..."
          className="field !py-2.5 text-sm sm:w-[260px]"
          aria-label="Foydalanuvchi qidiruvi"
          type="search"
        />

        <select
          value={role}
          onChange={(e) => {
            setRole(e.target.value as UserRole | '')
            setPage(1)
          }}
          className="field !py-2.5 !w-auto text-sm"
          aria-label="Rol bo'yicha filtr"
        >
          <option value="">Barcha rollar</option>
          <option value="User">Foydalanuvchi</option>
          <option value="Moderator">Moderator</option>
          <option value="Admin">Admin</option>
        </select>

        <div className="flex overflow-hidden rounded-[11px] border border-white/10 text-sm">
          {(['all', 'verified', 'blocked'] as StatusFilter[]).map((s) => (
            <button
              key={s}
              type="button"
              onClick={() => {
                setStatus(s)
                setPage(1)
              }}
              className={`px-3.5 py-2.5 transition-colors ${
                status === s ? 'bg-brand text-white' : 'text-muted hover:bg-white/[.04]'
              }`}
            >
              {s === 'all' ? 'Hammasi' : s === 'verified' ? 'Tasdiqlangan' : 'Bloklangan'}
            </button>
          ))}
        </div>

        <button type="submit" className="tap-target rounded-[11px] bg-brand px-5 py-2.5 text-sm font-semibold text-white">
          Qidirish
        </button>

        <span className="ml-auto text-sm text-dim">{total} ta foydalanuvchi</span>
      </form>

      {/* ── Ro'yxat ──────────────────────────────────────────────────── */}
      <div className="card overflow-hidden">
        <div className="hidden grid-cols-[1.6fr_1fr_110px_120px_130px_1.4fr] gap-3 bg-white/[.03] px-[22px] py-3.5 text-[11px] font-bold tracking-[.08em] text-dim lg:grid">
          <div>FOYDALANUVCHI</div>
          <div>TELEFON</div>
          <div>ROL</div>
          <div>REYTING</div>
          <div>SAVDO</div>
          <div className="text-right">AMALLAR</div>
        </div>

        {loading && (
          <div className="flex justify-center py-16">
            <Spinner />
          </div>
        )}

        {!loading && rows.length === 0 && (
          <EmptyState title="Foydalanuvchi topilmadi" text="Filtrlarni o'zgartirib ko'ring." />
        )}

        {!loading &&
          rows.map((u) => {
            const badge = ROLE_BADGE[u.role]
            const busy = acting === u.id
            return (
              <div
                key={u.id}
                className="grid grid-cols-1 items-center gap-3 border-t border-white/[.06] px-4 py-3.5 text-sm lg:grid-cols-[1.6fr_1fr_110px_120px_130px_1.4fr] lg:px-[22px]"
              >
                {/* Foydalanuvchi */}
                <div className="flex min-w-0 items-center gap-3">
                  <Avatar name={u.fullName || u.username} size={38} />
                  <div className="min-w-0">
                    <Link to={`/u/${u.username}`} className="block truncate font-semibold text-white hover:text-brand">
                      {u.fullName || u.username}
                    </Link>
                    <div className="truncate text-xs text-dim">
                      @{u.username} · {shortDate(u.createdAt)}
                      {u.isBlocked && <span className="ml-1.5 font-semibold text-danger-fg">· bloklangan</span>}
                    </div>
                  </div>
                </div>

                {/* Telefon */}
                <div className="font-mono text-[13px] text-muted">{u.phone}</div>

                {/* Rol */}
                <div>
                  <span className={`inline-flex rounded-full px-2.5 py-[3px] text-[11.5px] font-semibold ${badge.className}`}>
                    {badge.label}
                  </span>
                </div>

                {/* Reyting */}
                <div>
                  {u.ratingCount > 0 ? (
                    <Stars rating={u.rating} count={u.ratingCount} />
                  ) : (
                    <span className="text-xs text-dim">baho yo'q</span>
                  )}
                </div>

                {/* Savdo */}
                <div className="text-[13px] text-muted">
                  <span className="text-success-fg">{u.totalSales}</span> sotdi ·{' '}
                  <span className="text-info-fg">{u.totalPurchases}</span> oldi
                </div>

                {/* Amallar */}
                <div className="flex flex-wrap items-center justify-start gap-2 lg:justify-end">
                  <button
                    onClick={() =>
                      act(
                        u.id,
                        () => api.admin.verifyUser(u.id, !u.isVerified),
                        u.isVerified ? 'Tasdiq olib tashlandi.' : 'Sotuvchi tasdiqlandi.',
                      )
                    }
                    disabled={busy}
                    className={`rounded-[9px] border px-3 py-[6px] text-[12.5px] font-semibold transition-colors ${
                      u.isVerified
                        ? 'border-success/40 text-success-fg hover:bg-success hover:text-white'
                        : 'border-white/15 text-muted hover:bg-white/[.06]'
                    }`}
                  >
                    {busy ? <Spinner size={13} /> : u.isVerified ? '✓ Tasdiqlangan' : 'Tasdiqlash'}
                  </button>

                  {isAdmin && (
                    <>
                      <select
                        value={u.role}
                        disabled={busy || u.id === me?.id}
                        onChange={(e) =>
                          act(u.id, () => api.admin.setUserRole(u.id, e.target.value as UserRole), 'Rol o‘zgartirildi.')
                        }
                        className="field !w-auto !py-[6px] text-[12.5px] disabled:opacity-40"
                        aria-label="Rolni o'zgartirish"
                        title={u.id === me?.id ? "O'z rolingizni o'zgartirib bo'lmaydi" : 'Rolni o‘zgartirish'}
                      >
                        <option value="User">Foydalanuvchi</option>
                        <option value="Moderator">Moderator</option>
                        <option value="Admin">Admin</option>
                      </select>

                      <button
                        onClick={() =>
                          act(
                            u.id,
                            () => api.admin.blockUser(u.id, !u.isBlocked),
                            u.isBlocked ? 'Blokdan chiqarildi.' : 'Bloklandi.',
                          )
                        }
                        disabled={busy || u.id === me?.id}
                        className={`rounded-[9px] border px-3 py-[6px] text-[12.5px] font-semibold transition-colors disabled:opacity-40 ${
                          u.isBlocked
                            ? 'border-warning/40 text-warning-fg hover:bg-warning hover:text-white'
                            : 'border-danger/40 text-danger-fg hover:bg-danger hover:text-white'
                        }`}
                      >
                        {u.isBlocked ? 'Blokdan chiqarish' : 'Bloklash'}
                      </button>
                    </>
                  )}
                </div>
              </div>
            )
          })}
      </div>

      {/* ── Sahifalash ───────────────────────────────────────────────── */}
      {totalPages > 1 && (
        <div className="mt-5 flex items-center justify-center gap-3 text-sm">
          <button
            onClick={() => setPage((p) => Math.max(1, p - 1))}
            disabled={page <= 1}
            className="rounded-[9px] border border-white/12 px-4 py-2 text-muted disabled:opacity-40 enabled:hover:bg-white/[.05]"
          >
            ← Oldingi
          </button>
          <span className="text-dim">
            {page} / {totalPages}
          </span>
          <button
            onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
            disabled={page >= totalPages}
            className="rounded-[9px] border border-white/12 px-4 py-2 text-muted disabled:opacity-40 enabled:hover:bg-white/[.05]"
          >
            Keyingi →
          </button>
        </div>
      )}
    </div>
  )
}
