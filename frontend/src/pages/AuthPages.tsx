import { useState } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { Logo } from '../components/Header'
import { Spinner } from '../components/ui'
import { useAuth } from '../context/auth-context'
import { useToast } from '../context/toast-context'
import { ApiError } from '../lib/api'

function AuthShell({ title, subtitle, children, footer }: {
  title: string
  subtitle: string
  children: React.ReactNode
  footer: React.ReactNode
}) {
  return (
    <div className="relative flex min-h-[calc(100dvh-64px)] items-center justify-center overflow-hidden px-4 py-10 sm:px-6 sm:py-16">
      <div
        className="pointer-events-none absolute inset-0"
        style={{
          background:
            'radial-gradient(700px 400px at 80% 10%, rgba(255,107,53,.16), transparent 62%), radial-gradient(600px 380px at 15% 90%, rgba(74,74,106,.28), transparent 60%)',
        }}
      />

      <div className="relative w-full max-w-[440px]">
        <div className="mb-8 flex justify-center">
          <Logo size={44} />
        </div>

        <div className="rounded-hero border border-white/[.10] bg-surface p-6 shadow-panel sm:p-8">
          <h1 className="m-0 mb-2 font-display text-[24px] font-bold tracking-[-.02em] sm:text-[26px]">{title}</h1>
          <p className="m-0 mb-6 text-sm leading-relaxed text-muted sm:mb-7">{subtitle}</p>

          {children}
        </div>

        <p className="mt-6 text-center text-sm text-muted">{footer}</p>
      </div>
    </div>
  )
}

export function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const toast = useToast()

  const [form, setForm] = useState({ login: '', password: '' })
  const [busy, setBusy] = useState(false)

  const from = (location.state as { from?: string } | null)?.from ?? '/'

  async function submit(e: React.FormEvent) {
    e.preventDefault()
    setBusy(true)

    try {
      const user = await login(form.login.trim(), form.password)
      toast.success(`Xush kelibsiz, @${user.username}!`)
      navigate(from, { replace: true })
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : 'Kirishda xato yuz berdi.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <AuthShell
      title="Kirish"
      subtitle="Foydalanuvchi nomi yoki telefon raqamingiz bilan kiring."
      footer={
        <>
          Hisobingiz yo'qmi?{' '}
          <Link to="/register" className="font-semibold text-brand">
            Ro'yxatdan o'ting
          </Link>
        </>
      }
    >
      <form onSubmit={submit} className="flex flex-col gap-4">
        <label className="block">
          <span className="mb-[7px] block text-[13px] text-muted">Login yoki telefon</span>
          <input
            value={form.login}
            onChange={(e) => setForm({ ...form, login: e.target.value })}
            placeholder="alisher_uz yoki +998 90 123 45 67"
            autoComplete="username"
            required
            className="field"
          />
        </label>

        <label className="block">
          <span className="mb-[7px] block text-[13px] text-muted">Parol</span>
          <input
            type="password"
            value={form.password}
            onChange={(e) => setForm({ ...form, password: e.target.value })}
            autoComplete="current-password"
            required
            className="field"
          />
        </label>

        <button type="submit" disabled={busy} className="btn-primary mt-2 flex items-center justify-center py-4">
          {busy ? <Spinner size={18} /> : 'Kirish'}
        </button>
      </form>

      <div className="mt-6 rounded-xl border border-white/[.08] bg-white/[.03] p-3.5 text-xs leading-relaxed text-dim">
        Demo hisoblar: <span className="font-mono text-soft">alisher_uz</span>,{' '}
        <span className="font-mono text-soft">valisher</span>,{' '}
        <span className="font-mono text-soft">jetar_admin</span> — parol{' '}
        <span className="font-mono text-soft">jetar123</span>
      </div>
    </AuthShell>
  )
}

export function RegisterPage() {
  const { register } = useAuth()
  const navigate = useNavigate()
  const toast = useToast()

  const [form, setForm] = useState({
    firstName: '',
    lastName: '',
    username: '',
    phone: '+998 ',
    password: '',
    telegramUsername: '',
  })
  const [busy, setBusy] = useState(false)

  async function submit(e: React.FormEvent) {
    e.preventDefault()

    if (form.firstName.trim().length < 2) {
      toast.error('Ismingizni kiriting.')
      return
    }

    if (form.lastName.trim().length < 2) {
      toast.error('Familiyangizni kiriting.')
      return
    }

    if (form.password.length < 6) {
      toast.error("Parol kamida 6 belgidan iborat bo'lsin.")
      return
    }

    setBusy(true)
    try {
      const user = await register({
        firstName: form.firstName.trim(),
        lastName: form.lastName.trim(),
        username: form.username.trim(),
        phone: form.phone.trim(),
        password: form.password,
        telegramUsername: form.telegramUsername.trim() || undefined,
      })

      toast.success(`Hisob yaratildi. Xush kelibsiz, @${user.username}!`)
      navigate('/')
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "Ro'yxatdan o'tishda xato.")
    } finally {
      setBusy(false)
    }
  }

  return (
    <AuthShell
      title="Ro'yxatdan o'tish"
      subtitle="Bir daqiqada hisob oching va escrow himoyasi bilan savdo qiling."
      footer={
        <>
          Hisobingiz bormi?{' '}
          <Link to="/login" className="font-semibold text-brand">
            Kirish
          </Link>
        </>
      }
    >
      <form onSubmit={submit} className="flex flex-col gap-4">
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <label className="block">
            <span className="mb-[7px] block text-[13px] text-muted">Ism</span>
            <input
              value={form.firstName}
              onChange={(e) => setForm({ ...form, firstName: e.target.value })}
              placeholder="Alisher"
              autoComplete="given-name"
              required
              minLength={2}
              maxLength={50}
              className="field"
            />
          </label>

          <label className="block">
            <span className="mb-[7px] block text-[13px] text-muted">Familiya</span>
            <input
              value={form.lastName}
              onChange={(e) => setForm({ ...form, lastName: e.target.value })}
              placeholder="Karimov"
              autoComplete="family-name"
              required
              minLength={2}
              maxLength={50}
              className="field"
            />
          </label>
        </div>

        <label className="block">
          <span className="mb-[7px] block text-[13px] text-muted">Foydalanuvchi nomi</span>
          <input
            value={form.username}
            onChange={(e) => setForm({ ...form, username: e.target.value })}
            placeholder="alisher_uz"
            autoComplete="username"
            required
            className="field"
          />
        </label>

        <label className="block">
          <span className="mb-[7px] block text-[13px] text-muted">Telefon</span>
          <input
            value={form.phone}
            onChange={(e) => setForm({ ...form, phone: e.target.value })}
            placeholder="+998 90 123 45 67"
            autoComplete="tel"
            required
            className="field"
          />
        </label>

        <label className="block">
          <span className="mb-[7px] block text-[13px] text-muted">
            Telegram <span className="text-dim">(ixtiyoriy)</span>
          </span>
          <input
            value={form.telegramUsername}
            onChange={(e) => setForm({ ...form, telegramUsername: e.target.value })}
            placeholder="@alisher_uz"
            className="field"
          />
        </label>

        <label className="block">
          <span className="mb-[7px] block text-[13px] text-muted">Parol</span>
          <input
            type="password"
            value={form.password}
            onChange={(e) => setForm({ ...form, password: e.target.value })}
            autoComplete="new-password"
            required
            minLength={6}
            className="field"
          />
        </label>

        <button type="submit" disabled={busy} className="btn-primary mt-2 flex items-center justify-center py-4">
          {busy ? <Spinner size={18} /> : "Ro'yxatdan o'tish"}
        </button>
      </form>

      <p className="mt-5 text-center text-xs leading-relaxed text-dim">
        Ro'yxatdan o'tish orqali siz foydalanish shartlari va maxfiylik siyosatiga rozilik bildirasiz.
      </p>
    </AuthShell>
  )
}
