import { useEffect, useState } from 'react'
import { useToast } from '../context/toast-context'
import { ApiError, api } from '../lib/api'
import { money } from '../lib/format'
import type { BoostConfig, BoostTier } from '../lib/types'
import { Spinner } from './ui'

/**
 * E'lonni TOP ga chiqarish oynasi. Sotuvchi tarifni tanlaydi, kartaga to'lov qilib
 * chek skrinshotini yuboradi, admin qo'lda tasdiqlaydi. Platforma o'z reklama
 * xizmatini sotadi — pul harakatiga aralashmaydi.
 */
export default function BoostModal({
  listingId,
  listingTitle,
  onClose,
}: {
  listingId: string
  listingTitle: string
  onClose: () => void
}) {
  const toast = useToast()

  const [config, setConfig] = useState<BoostConfig | null>(null)
  const [tier, setTier] = useState<BoostTier | null>(null)
  const [screenshot, setScreenshot] = useState<string | null>(null)
  const [uploading, setUploading] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [done, setDone] = useState(false)

  useEffect(() => {
    api.boosts
      .config()
      .then((cfg) => {
        setConfig(cfg)
        setTier(cfg.tiers[0] ?? null)
      })
      .catch(() => toast.error('Ko\'tarish sozlamalari yuklanmadi.'))
  }, [toast])

  async function handleFile(file: File | undefined) {
    if (!file) return
    if (file.size > 5 * 1024 * 1024) {
      toast.error('Rasm 5 MB dan katta.')
      return
    }
    setUploading(true)
    try {
      const res = await api.listings.upload(file)
      setScreenshot(res.url)
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : 'Chek yuklanmadi.')
    } finally {
      setUploading(false)
    }
  }

  async function submit() {
    if (!tier) return
    setSubmitting(true)
    try {
      await api.boosts.create(listingId, tier.days, screenshot ?? undefined)
      setDone(true)
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : 'So\'rov yuborilmadi.')
    } finally {
      setSubmitting(false)
    }
  }

  function copyCard() {
    if (config?.cardNumber) {
      navigator.clipboard?.writeText(config.cardNumber.replace(/\s/g, ''))
      toast.success('Karta raqami nusxalandi.')
    }
  }

  return (
    <div
      className="fixed inset-0 z-[100] flex items-end justify-center bg-ink/70 p-0 backdrop-blur-sm sm:items-center sm:p-4"
      onClick={onClose}
    >
      <div
        className="max-h-[92dvh] w-full max-w-[460px] overflow-y-auto rounded-t-[22px] border border-white/[.10] bg-surface p-6 shadow-panel sm:rounded-[22px]"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="mb-1 flex items-start justify-between gap-3">
          <h2 className="m-0 font-display text-xl font-bold">🚀 TOP ga chiqarish</h2>
          <button onClick={onClose} className="tap-target -mr-2 -mt-1 px-2 text-xl text-muted hover:text-white">
            ✕
          </button>
        </div>
        <p className="mb-5 line-clamp-1 text-[13px] text-muted">{listingTitle}</p>

        {done ? (
          <div className="py-6 text-center">
            <div className="mb-3 text-4xl">✅</div>
            <h3 className="m-0 mb-2 font-display text-lg font-bold">So'rov qabul qilindi</h3>
            <p className="m-0 mb-6 text-[14px] leading-relaxed text-muted">
              To'lovingiz tekshirilgach, admin e'loningizni tasdiqlaydi va u ro'yxat tepasiga chiqadi.
              Holatini profilingizdagi "Ko'tarishlarim" bo'limida ko'rasiz.
            </p>
            <button onClick={onClose} className="btn-primary w-full py-3.5">
              Yopish
            </button>
          </div>
        ) : !config ? (
          <div className="flex justify-center py-12">
            <Spinner size={28} />
          </div>
        ) : (
          <>
            {/* Tarif tanlash */}
            <div className="mb-5 grid grid-cols-2 gap-3">
              {config.tiers.map((t) => {
                const on = tier?.days === t.days
                return (
                  <button
                    key={t.days}
                    onClick={() => setTier(t)}
                    aria-pressed={on}
                    className={[
                      'rounded-2xl border p-4 text-left transition-all',
                      on ? 'border-brand/60 bg-brand/[.10] shadow-brand-sm' : 'border-white/[.10] bg-field hover:border-white/25',
                    ].join(' ')}
                  >
                    <div className={`font-display text-[15px] font-bold ${on ? 'text-brand' : 'text-white'}`}>
                      {t.days} kun
                    </div>
                    <div className="mt-1 font-display text-lg font-extrabold">{money(t.price)}</div>
                    <div className="text-[11px] text-dim">so'm</div>
                  </button>
                )
              })}
            </div>

            {/* Karta rekvizitlari */}
            <div className="mb-4 rounded-[14px] border border-white/[.10] bg-white/[.03] p-4">
              <div className="mb-1 text-[13px] text-muted">To'lovni shu kartaga o'tkazing:</div>
              <button onClick={copyCard} className="flex w-full items-center justify-between text-left">
                <span className="font-mono text-[17px] font-bold tracking-wide">{config.cardNumber}</span>
                <span className="text-xs text-brand">nusxa 📋</span>
              </button>
              <div className="mt-1 text-[13px] text-soft">{config.cardHolder}</div>
              {tier && (
                <div className="mt-3 border-t border-white/[.08] pt-3 text-sm">
                  To'lov summasi:{' '}
                  <strong className="font-display text-brand">{money(tier.price)} so'm</strong>
                </div>
              )}
            </div>

            {/* Chek yuklash */}
            <div className="mb-5">
              <div className="mb-2 text-[13px] text-muted">To'lov cheki / skrinshot (ixtiyoriy, tavsiya etiladi)</div>
              {screenshot ? (
                <div className="flex items-center justify-between rounded-[12px] border border-success/30 bg-success/[.08] px-4 py-3 text-[13px] text-success-fg">
                  ✓ Chek yuklandi
                  <button onClick={() => setScreenshot(null)} className="text-muted hover:text-white">
                    o'chirish
                  </button>
                </div>
              ) : (
                <label className="flex cursor-pointer items-center justify-center rounded-[12px] border-[1.5px] border-dashed border-white/[.16] px-4 py-4 text-[13px] text-dim transition-colors hover:border-brand hover:text-brand">
                  {uploading ? <Spinner size={18} /> : '📎 Chek rasmini tanlang'}
                  <input
                    type="file"
                    accept="image/png,image/jpeg,image/webp"
                    hidden
                    onChange={(e) => handleFile(e.target.files?.[0])}
                  />
                </label>
              )}
            </div>

            <button
              onClick={submit}
              disabled={submitting || !tier}
              className="btn-primary flex w-full items-center justify-center gap-2 py-4 text-base"
            >
              {submitting ? <Spinner size={18} /> : "To'lovni tasdiqlashga yuborish"}
            </button>
            <p className="mt-3 text-center text-[12px] leading-relaxed text-dim">
              Admin to'lovni tekshirgach e'loningiz {tier?.days ?? 0} kunga tepaga chiqadi.
            </p>
          </>
        )}
      </div>
    </div>
  )
}
