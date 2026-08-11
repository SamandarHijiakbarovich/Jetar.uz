import { useEffect, useState } from 'react'
import { Link, NavLink, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/auth-context'
import { Avatar } from './ui'

const NAV = [
  { to: '/', label: 'Bosh sahifa', end: true },
  { to: '/listings', label: "E'lonlar" },
  { to: '/transactions', label: 'Bitimlarim', auth: true },
  { to: '/create', label: "E'lon joylash", auth: true },
]

export function Logo({ size = 38 }: { size?: number }) {
  return (
    <Link to="/" className="flex flex-shrink-0 items-center gap-2.5 text-white hover:text-white">
      <span
        className="grid place-items-center rounded-[11px] bg-gradient-to-br from-brand to-brand-400 font-display font-extrabold text-ink shadow-brand-sm"
        style={{ width: size, height: size, fontSize: Math.round(size * 0.55) }}
      >
        J
      </span>
      <span className="font-display text-xl font-bold tracking-[-.02em]">Jetar</span>
    </Link>
  )
}

export default function Header() {
  const { user, logout, isModerator } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [menuOpen, setMenuOpen] = useState(false)

  // Sahifa almashganda mobil menyu yopiladi.
  useEffect(() => setMenuOpen(false), [location.pathname])

  const links = NAV.filter((item) => !item.auth || user)
  if (isModerator) links.push({ to: '/admin', label: 'Admin' })

  const linkClass = ({ isActive }: { isActive: boolean }) =>
    [
      'whitespace-nowrap rounded-[10px] border px-[15px] py-[9px] text-sm font-semibold transition-colors',
      isActive
        ? 'border-brand/40 bg-brand/[.12] text-brand'
        : 'border-transparent text-muted hover:text-white',
    ].join(' ')

  function handleLogout() {
    logout()
    navigate('/')
  }

  return (
    <header className="sticky top-0 z-50 border-b border-white/[.08] bg-ink/[.82] backdrop-blur-[14px]">
      <div className="mx-auto flex max-w-[1200px] items-center gap-4 px-4 py-3 sm:px-6 sm:py-3.5 lg:gap-7">
        <Logo />

        <nav className="hidden flex-1 gap-1.5 overflow-x-auto lg:flex" aria-label="Asosiy menyu">
          {links.map((item) => (
            <NavLink key={item.to} to={item.to} end={item.end} className={linkClass}>
              {item.label}
            </NavLink>
          ))}
        </nav>

        <div className="ml-auto hidden flex-shrink-0 items-center gap-2.5 lg:flex">
          {user ? (
            <>
              <Link
                to="/profile"
                className="flex items-center gap-2.5 rounded-[10px] border border-white/[.12] px-3 py-1.5 text-white transition-colors hover:border-brand/50 hover:text-white"
              >
                <Avatar name={user.username} size={28} />
                <span className="text-sm font-semibold">@{user.username}</span>
              </Link>
              <button
                onClick={handleLogout}
                className="rounded-[10px] border border-white/[.16] px-4 py-2.5 text-sm font-semibold text-muted transition-colors hover:border-danger/50 hover:text-danger-fg"
              >
                Chiqish
              </button>
            </>
          ) : (
            <>
              <Link
                to="/login"
                className="rounded-[10px] border border-white/[.16] px-[18px] py-[9px] text-sm font-semibold text-white transition-colors hover:border-brand hover:text-brand"
              >
                Kirish
              </Link>
              <Link
                to="/register"
                className="btn-primary px-[18px] py-[9px] text-sm"
              >
                Ro'yxatdan o'tish
              </Link>
            </>
          )}
        </div>

        <div className="ml-auto flex items-center gap-2 lg:hidden">
          {/* Kirmagan foydalanuvchi uchun asosiy tugma menyu ichida ko'milib qolmasin. */}
          {!user && (
            <Link to="/register" className="btn-primary tap-target px-4 py-2.5 text-[13px]">
              Ro'yxatdan o'tish
            </Link>
          )}

          <button
            onClick={() => setMenuOpen((v) => !v)}
            className="tap-target grid h-11 w-11 place-items-center rounded-[10px] border border-white/[.16] text-lg"
            aria-label="Menyu"
            aria-expanded={menuOpen}
          >
            {menuOpen ? '✕' : '☰'}
          </button>
        </div>
      </div>

      {menuOpen && (
        <div className="max-h-[calc(100dvh-64px)] overflow-y-auto border-t border-white/[.08] bg-ink px-4 py-4 sm:px-6 lg:hidden">
          <nav className="flex flex-col gap-1.5" aria-label="Mobil menyu">
            {links.map((item) => (
              <NavLink key={item.to} to={item.to} end={item.end} className={linkClass}>
                {item.label}
              </NavLink>
            ))}
          </nav>

          <div className="mt-4 flex flex-col gap-2.5 border-t border-white/[.08] pt-4">
            {user ? (
              <>
                <Link to="/profile" className="btn-ghost px-4 py-3 text-center">
                  @{user.username}
                </Link>
                <button onClick={handleLogout} className="btn-ghost px-4 py-3">
                  Chiqish
                </button>
              </>
            ) : (
              <>
                <Link to="/login" className="btn-ghost px-4 py-3 text-center">
                  Kirish
                </Link>
                <Link to="/register" className="btn-primary px-4 py-3 text-center">
                  Ro'yxatdan o'tish
                </Link>
              </>
            )}
          </div>
        </div>
      )}
    </header>
  )
}
