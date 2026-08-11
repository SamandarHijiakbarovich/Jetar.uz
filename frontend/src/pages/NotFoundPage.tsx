import { Link } from 'react-router-dom'

export default function NotFoundPage() {
  return (
    <div className="flex min-h-[60vh] flex-col items-center justify-center px-6 text-center">
      <div className="mb-4 font-display text-[clamp(64px,14vw,120px)] font-extrabold leading-none text-brand">
        404
      </div>

      <h1 className="m-0 mb-3 font-display text-2xl font-bold">Sahifa topilmadi</h1>
      <p className="m-0 mb-8 max-w-md leading-relaxed text-muted">
        Manzil noto'g'ri yoki sahifa ko'chirilgan. Bosh sahifadan davom eting.
      </p>

      <div className="flex flex-wrap justify-center gap-3">
        <Link to="/" className="btn-primary px-7 py-3.5">
          Bosh sahifa
        </Link>
        <Link to="/listings" className="btn-ghost px-7 py-3.5">
          E'lonlar
        </Link>
      </div>
    </div>
  )
}
