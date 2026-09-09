import { useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Spinner } from '../components/ui'
import { useToast } from '../context/toast-context'
import { ApiError, api } from '../lib/api'
import { formatPriceInput, parsePriceInput } from '../lib/format'
import { GAMES, IN_GAME_ITEMS, LISTING_TYPES, RANKS, REGIONS } from '../lib/games'
import { mediaUrl } from '../lib/media'
import type { GameType, ListingType } from '../lib/types'

const TIPS = [
  "Aniq va batafsil ma'lumot yozing — daraja, o'yinchilar, skinlar.",
  'Haqiqiy skrinshotlarni yuklang. Internetdan olingan rasmlar bloklanadi.',
  "Narxni bozorga mos qilib belgilang — o'xshash e'lonlarni ko'rib chiqing.",
]

export default function CreateListingPage() {
  const navigate = useNavigate()
  const toast = useToast()
  const fileInput = useRef<HTMLInputElement>(null)

  const [game, setGame] = useState<GameType>('EFootball')
  const [type, setType] = useState<ListingType>('Account')
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [price, setPrice] = useState('')
  const [region, setRegion] = useState<string>('ASIA')
  const [rank, setRank] = useState<string>(RANKS[0])
  const [items, setItems] = useState<string[]>(['Skin', 'Coins'])
  const [images, setImages] = useState<string[]>([])
  const [uploading, setUploading] = useState(false)
  const [saving, setSaving] = useState(false)

  function toggleItem(item: string) {
    setItems((current) => (current.includes(item) ? current.filter((i) => i !== item) : [...current, item]))
  }

  async function handleFiles(files: FileList | null) {
    if (!files?.length) return

    const room = 8 - images.length
    if (room <= 0) {
      toast.error('Maksimal 8 ta rasm.')
      return
    }

    setUploading(true)
    try {
      const uploaded: string[] = []

      for (const file of Array.from(files).slice(0, room)) {
        if (file.size > 5 * 1024 * 1024) {
          toast.error(`${file.name} — 5 MB dan katta.`)
          continue
        }
        const res = await api.listings.upload(file)
        uploaded.push(res.url)
      }

      if (uploaded.length) {
        setImages((current) => [...current, ...uploaded])
        toast.success(`${uploaded.length} ta rasm yuklandi.`)
      }
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : 'Rasm yuklanmadi.')
    } finally {
      setUploading(false)
      if (fileInput.current) fileInput.current.value = ''
    }
  }

  async function submit() {
    const numericPrice = parsePriceInput(price)

    if (title.trim().length < 10) return toast.error('Sarlavha kamida 10 belgidan iborat bo\'lsin.')
    if (description.trim().length < 20) return toast.error('Tavsif kamida 20 belgidan iborat bo\'lsin.')
    if (numericPrice < 10_000) return toast.error("Narx kamida 10 000 so'm bo'lsin.")

    setSaving(true)
    try {
      const created = await api.listings.create({
        gameType: game,
        type,
        title: title.trim(),
        description: description.trim(),
        price: numericPrice,
        serverRegion: region,
        rankLevel: rank,
        inGameItems: items,
        images,
      })

      toast.success("E'lon joylashtirildi!")
      navigate(`/listings/${created.id}`)
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "E'lonni saqlab bo'lmadi.")
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="mx-auto w-full max-w-[1060px] px-4 pb-16 pt-8 sm:px-6 sm:pb-24 sm:pt-11">
      <h1 className="m-0 mb-2 font-display text-[clamp(24px,6vw,34px)] font-bold tracking-[-.02em]">
        E'lon joylashtirish
      </h1>
      <p className="m-0 mb-7 text-[15px] leading-relaxed text-muted sm:mb-[34px] sm:text-base">
        Akkauntingiz haqida to'liq ma'lumot bering — bu tezroq sotilishga yordam beradi.
      </p>

      <div className="grid items-start gap-5 sm:gap-6 lg:grid-cols-[minmax(0,1.4fr)_minmax(0,.6fr)]">
        <div className="flex flex-col gap-5">
          {/* Kategoriya */}
          <section className="rounded-[20px] border border-white/[.08] bg-surface p-5 shadow-rim sm:p-[26px]">
            <div className="label-caps mb-4">NIMA SOTMOQCHISIZ?</div>

            <div className="grid grid-cols-2 gap-2.5 lg:grid-cols-4">
              {LISTING_TYPES.map((t) => {
                const on = type === t.type
                return (
                  <button
                    key={t.type}
                    onClick={() => setType(t.type)}
                    aria-pressed={on}
                    className={[
                      'rounded-2xl border p-3.5 text-left transition-all duration-200',
                      on
                        ? 'border-brand/50 bg-brand/[.10] shadow-brand-sm'
                        : 'border-white/[.10] bg-field hover:border-white/25',
                    ].join(' ')}
                  >
                    <div className="mb-1.5 text-[22px] leading-none">{t.glyph}</div>
                    <div className={`text-[13px] font-semibold ${on ? 'text-brand' : 'text-white'}`}>
                      {t.name}
                    </div>
                    <div className="mt-0.5 text-[11px] leading-snug text-dim">{t.description}</div>
                  </button>
                )
              })}
            </div>
          </section>

          {/* O'yin */}
          <section className="rounded-[20px] border border-white/[.08] bg-surface p-5 shadow-rim sm:p-[26px]">
            <div className="label-caps mb-4">O'YINNI TANLANG</div>

            <div className="grid grid-cols-3 gap-2.5 sm:grid-cols-4">
              {GAMES.map((g) => {
                const on = game === g.type
                return (
                  <button
                    key={g.type}
                    onClick={() => setGame(g.type)}
                    className={[
                      'cursor-pointer rounded-xl border px-2 py-3.5 text-center transition-colors',
                      on ? 'border-brand bg-brand/[.10] text-brand' : 'border-white/[.10] bg-field text-soft',
                    ].join(' ')}
                    aria-pressed={on}
                  >
                    <div className="mb-1.5 text-[22px]">{g.glyph}</div>
                    <div className="text-xs font-semibold">{g.name}</div>
                  </button>
                )
              })}
            </div>
          </section>

          {/* Ma'lumotlar */}
          <section className="flex flex-col gap-[18px] rounded-[20px] border border-white/[.08] bg-surface p-5 sm:p-[26px]">
            <div className="label-caps">E'LON MA'LUMOTLARI</div>

            <label className="block">
              <span className="mb-[7px] block text-[13px] text-muted">E'lon sarlavhasi</span>
              <input
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                placeholder="Masalan: eFootball 2025 — Legend akkaunt, 250+ o'yinchi"
                maxLength={160}
                className="field !py-3.5"
              />
              <span className="mt-1.5 block text-right text-xs text-dim">{title.length}/160</span>
            </label>

            <label className="block">
              <span className="mb-[7px] block text-[13px] text-muted">Tavsif</span>
              <textarea
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                rows={5}
                maxLength={4000}
                placeholder="Akkaunt darajasi, o'yinchilar, skinlar, bog'langan pochta va boshqa muhim ma'lumotlar"
                className="field resize-y !py-3.5"
              />
            </label>

            <div className="grid grid-cols-2 gap-3.5 sm:grid-cols-3">
              <label className="col-span-2 block sm:col-span-1">
                <span className="mb-[7px] block text-[13px] text-muted">Narx (UZS)</span>
                <input
                  value={price}
                  onChange={(e) => setPrice(formatPriceInput(e.target.value))}
                  placeholder="850 000"
                  inputMode="numeric"
                  className="field !py-3.5"
                />
              </label>

              <label className="block">
                <span className="mb-[7px] block text-[13px] text-muted">Server</span>
                <select value={region} onChange={(e) => setRegion(e.target.value)} className="field !py-3.5">
                  {REGIONS.map((r) => (
                    <option key={r} value={r} className="bg-field">
                      {r}
                    </option>
                  ))}
                </select>
              </label>

              <label className="block">
                <span className="mb-[7px] block text-[13px] text-muted">Daraja / rank</span>
                <select value={rank} onChange={(e) => setRank(e.target.value)} className="field !py-3.5">
                  {RANKS.map((r) => (
                    <option key={r} value={r} className="bg-field">
                      {r}
                    </option>
                  ))}
                </select>
              </label>
            </div>

            <div>
              <div className="mb-2.5 text-[13px] text-muted">O'yin ichidagi vositalar</div>
              <div className="flex flex-wrap gap-2.5">
                {IN_GAME_ITEMS.map((item) => {
                  const on = items.includes(item)
                  return (
                    <button
                      key={item}
                      onClick={() => toggleItem(item)}
                      className={[
                        'cursor-pointer rounded-full border px-[17px] py-2 text-sm font-semibold transition-colors',
                        on ? 'border-brand bg-brand/[.12] text-brand' : 'border-white/[.12] text-muted',
                      ].join(' ')}
                      aria-pressed={on}
                    >
                      {item}
                    </button>
                  )
                })}
              </div>
            </div>
          </section>

          {/* Skrinshotlar */}
          <section className="rounded-[20px] border border-white/[.08] bg-surface p-5 sm:p-[26px]">
            <div className="label-caps mb-4">SKRINSHOTLAR</div>

            <button
              onClick={() => fileInput.current?.click()}
              disabled={uploading || images.length >= 8}
              className="w-full cursor-pointer rounded-2xl border-[1.5px] border-dashed border-white/[.16] px-6 py-9 text-center transition-colors hover:border-brand hover:bg-brand/[.04] disabled:cursor-not-allowed disabled:opacity-50"
            >
              {uploading ? (
                <Spinner size={26} />
              ) : (
                <>
                  <div className="mb-3 text-3xl">📤</div>
                  <div className="mb-1.5 font-display text-base font-semibold">
                    Rasmlarni bu yerga tashlang
                  </div>
                  <div className="text-[13px] text-dim">
                    PNG yoki JPG · maksimal 8 ta · har biri 5 MB gacha
                  </div>
                </>
              )}
            </button>

            <input
              ref={fileInput}
              type="file"
              accept="image/png,image/jpeg,image/webp"
              multiple
              hidden
              onChange={(e) => handleFiles(e.target.files)}
            />

            <div className="mt-4 grid grid-cols-4 gap-3">
              {Array.from({ length: 4 }, (_, i) => {
                const url = images[i]
                return url ? (
                  <button
                    key={url}
                    onClick={() => setImages((c) => c.filter((x) => x !== url))}
                    className="group relative aspect-video overflow-hidden rounded-[10px]"
                    title="O'chirish"
                  >
                    <img src={mediaUrl(url)} alt="" className="h-full w-full object-cover" />
                    <span className="absolute inset-0 grid place-items-center bg-ink/70 text-sm font-semibold opacity-0 transition-opacity group-hover:opacity-100">
                      O'chirish
                    </span>
                  </button>
                ) : (
                  <div key={i} className="aspect-video rounded-[10px] bg-white/[.05]" />
                )
              })}
            </div>

            {images.length > 4 && (
              <p className="mt-3 text-[13px] text-dim">yana {images.length - 4} ta rasm yuklangan</p>
            )}
          </section>

          <button
            onClick={submit}
            disabled={saving}
            className="btn-primary flex items-center justify-center gap-2 py-[17px] text-base shadow-brand-sm"
          >
            {saving ? <Spinner size={18} /> : "E'lonni joylashtirish"}
          </button>
        </div>

        {/* Maslahatlar — telefonda forma ostida, katta ekranda yon panelda. */}
        <aside className="rounded-[20px] border border-brand/[.22] bg-brand/[.05] p-5 sm:p-6 lg:sticky lg:top-[92px]">
          <div className="mb-4 font-display text-[17px] font-bold">Sotuvchilar uchun maslahatlar</div>

          <div className="flex flex-col gap-4 text-sm leading-[1.6] text-soft-3">
            {TIPS.map((tip, i) => (
              <div key={tip} className="flex gap-2.5">
                <span className="text-brand">{String(i + 1).padStart(2, '0')}</span>
                <span>{tip}</span>
              </div>
            ))}
          </div>

          <div className="mt-5 border-t border-brand/20 pt-[18px] text-[13px] leading-[1.6] text-muted">
            E'lon joylash <strong className="text-brand">bepul</strong>. Xaridor siz bilan to'g'ridan-to'g'ri
            (Telegram/telefon) bog'lanadi. E'loningizni ro'yxat tepasiga chiqarish uchun{' '}
            <strong className="text-brand">TOP</strong> xizmatidan foydalaning.
          </div>
        </aside>
      </div>
    </div>
  )
}
