import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import ListingCard from '../components/ListingCard'
import { Avatar, EmptyState, PageLoader, Stars } from '../components/ui'
import { api } from '../lib/api'
import { relativeTime } from '../lib/format'
import type { ListingCard as ListingCardType, Rating, User } from '../lib/types'

export default function PublicProfilePage() {
  const { username = '' } = useParams()

  const [data, setData] = useState<{ user: User; listings: ListingCardType[]; ratings: Rating[] } | null>(null)
  const [notFound, setNotFound] = useState(false)

  useEffect(() => {
    let alive = true
    setData(null)
    setNotFound(false)

    api.users
      .profile(username)
      .then((res) => alive && setData(res))
      .catch(() => alive && setNotFound(true))

    return () => {
      alive = false
    }
  }, [username])

  if (notFound) {
    return (
      <div className="page py-24 text-center">
        <h1 className="font-display text-3xl font-bold">Foydalanuvchi topilmadi</h1>
        <Link to="/listings" className="btn-primary mt-6 inline-block px-7 py-3.5">
          E'lonlarga qaytish
        </Link>
      </div>
    )
  }

  if (!data) return <PageLoader />

  const { user, listings, ratings } = data
  const active = listings.filter((l) => l.status === 'Active')

  return (
    <div className="page pb-20 pt-8 sm:pb-24 sm:pt-11">
      <div className="mb-8 flex flex-wrap items-center gap-5 rounded-hero border border-white/[.08] bg-gradient-to-br from-[#1C1C36] to-[#141428] p-5 sm:gap-[26px] sm:p-8">
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

          <Stars rating={user.rating} count={user.ratingCount} />
          <div className="mt-1 text-[13px] text-muted">
            {user.city ? `${user.city} · ` : ''}
            {new Date(user.createdAt).getFullYear()}-yildan · javob vaqti ~{user.avgResponseMinutes} daqiqa
          </div>
        </div>

        <div className="flex w-full justify-between gap-4 border-t border-white/[.08] pt-4 text-center sm:w-auto sm:justify-start sm:gap-7 sm:border-0 sm:pt-0">
          <div>
            <div className="font-display text-[22px] font-extrabold sm:text-[26px]">{user.totalSales}</div>
            <div className="text-xs text-muted sm:text-[13px]">Sotilgan</div>
          </div>
          <div>
            <div className="font-display text-[22px] font-extrabold text-brand sm:text-[26px]">{active.length}</div>
            <div className="text-xs text-muted sm:text-[13px]">Aktiv e'lon</div>
          </div>
        </div>
      </div>

      <h2 className="m-0 mb-5 font-display text-xl font-bold sm:mb-6 sm:text-2xl">Sotuvdagi akkauntlar</h2>

      {active.length === 0 ? (
        <EmptyState glyph="📭" title="Hozircha aktiv e'lonlar yo'q" />
      ) : (
        <div className="mb-12 grid gap-5 sm:grid-cols-2 sm:gap-6 lg:grid-cols-3">
          {active.map((l) => (
            <ListingCard key={l.id} listing={l} />
          ))}
        </div>
      )}

      {ratings.length > 0 && (
        <>
          <h2 className="m-0 mb-5 font-display text-xl font-bold sm:mb-6 sm:text-2xl">Sharhlar</h2>

          <div className="grid gap-4 sm:gap-5 md:grid-cols-2 lg:grid-cols-3">
            {ratings.map((r) => (
              <div key={r.id} className="card p-5 sm:p-6">
                <div className="mb-3 text-sm text-warning">{'★'.repeat(r.score)}</div>
                {r.comment && (
                  <p className="m-0 mb-4 text-[15px] leading-[1.65] text-soft-3 text-pretty">{r.comment}</p>
                )}
                <div className="flex items-center gap-2.5">
                  <Avatar name={r.fromUsername} size={32} />
                  <div>
                    <div className="text-[13px] font-semibold">@{r.fromUsername}</div>
                    <div className="text-xs text-dim">{relativeTime(r.createdAt)}</div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </>
      )}
    </div>
  )
}
