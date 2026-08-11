import { Link } from 'react-router-dom'

const COLUMNS = [
  {
    title: 'PLATFORMA',
    links: [
      { label: 'Bosh sahifa', to: '/' },
      { label: "E'lonlar", to: '/listings' },
      { label: "Sotuvchi bo'lish", to: '/create' },
      { label: 'Narxlar', to: '/#tariflar' },
    ],
  },
  {
    title: 'YORDAM',
    links: [
      { label: 'Escrow qanday ishlaydi', to: '/#qanday-ishlaydi' },
      { label: "Ko'p so'raladigan savollar", to: '/#savollar' },
      { label: 'Nizolar', to: '/transactions' },
      { label: 'Aloqa', to: '/#aloqa' },
    ],
  },
  {
    title: 'HUJJATLAR',
    links: [
      { label: 'Foydalanish shartlari', to: '/#shartlar' },
      { label: 'Maxfiylik siyosati', to: '/#maxfiylik' },
      { label: 'Ommaviy oferta', to: '/#oferta' },
    ],
  },
]

const SOCIALS = [
  { glyph: '✈', label: 'Telegram' },
  { glyph: '◎', label: 'Instagram' },
  { glyph: '▶', label: 'YouTube' },
]

export default function Footer() {
  return (
    <footer className="safe-bottom border-t border-white/[.08] bg-[#0D0D1A] px-4 pb-8 pt-12 sm:px-6 sm:pt-14">
      <div className="mx-auto grid max-w-[1200px] grid-cols-2 gap-8 sm:gap-10 lg:grid-cols-[minmax(0,1.4fr)_repeat(3,minmax(0,.8fr))]">
        <div className="col-span-2 lg:col-span-1">
          <div className="mb-3.5 flex items-center gap-2.5">
            <span className="grid h-[34px] w-[34px] place-items-center rounded-[10px] bg-gradient-to-br from-brand to-brand-400 font-display text-[19px] font-extrabold text-ink">
              J
            </span>
            <span className="font-display text-[19px] font-bold">Jetar</span>
          </div>

          <p className="mb-[18px] max-w-[280px] text-sm leading-relaxed text-muted">
            Tez va ishonchli — Jetar! O'yin akkauntlari va vositalarining xavfsiz savdo platformasi.
          </p>

          <div className="flex gap-2.5">
            {SOCIALS.map((s) => (
              <button
                key={s.label}
                aria-label={s.label}
                className="tap-target grid h-11 w-11 place-items-center rounded-[10px] bg-white/[.06] text-base transition-colors hover:bg-brand sm:h-[38px] sm:w-[38px]"
              >
                {s.glyph}
              </button>
            ))}
          </div>
        </div>

        {COLUMNS.map((col) => (
          <div key={col.title}>
            <div className="label-caps mb-4">{col.title}</div>
            <div className="flex flex-col gap-[11px] text-sm text-soft-2">
              {col.links.map((link) => (
                <Link key={link.label} to={link.to} className="text-soft-2 transition-colors hover:text-brand">
                  {link.label}
                </Link>
              ))}
            </div>
          </div>
        ))}
      </div>

      <div className="mx-auto mt-8 flex max-w-[1200px] flex-wrap justify-between gap-2 border-t border-white/[.06] pt-6 text-xs text-dim sm:mt-9 sm:gap-3 sm:text-[13px]">
        <span>© {new Date().getFullYear()} Jetar. Barcha huquqlar himoyalangan.</span>
        <span>Toshkent, O'zbekiston · support@jetar.uz</span>
      </div>
    </footer>
  )
}
