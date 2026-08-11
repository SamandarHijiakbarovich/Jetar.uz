import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import EscrowSteps from '../components/EscrowSteps'
import { Avatar, EscrowNote, PageLoader, Spinner } from '../components/ui'
import { useToast } from '../context/toast-context'
import { ApiError, api } from '../lib/api'
import { money } from '../lib/format'
import type { PaymentMethod, PaymentMethodOption, Transaction } from '../lib/types'

const SHORT: Record<PaymentMethod, string> = {
  Click: 'CLICK',
  Payme: 'PAYME',
  Uzum: 'UZUM',
  Apelsin: 'APLSN',
}

export default function CheckoutPage() {
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const toast = useToast()

  const [tx, setTx] = useState<Transaction | null>(null)
  const [methods, setMethods] = useState<PaymentMethodOption[]>([])
  const [sandbox, setSandbox] = useState(false)
  const [selected, setSelected] = useState<PaymentMethod>('Click')
  const [paying, setPaying] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let alive = true

    Promise.all([api.transactions.get(id), api.payments.methods()])
      .then(([transaction, payment]) => {
        if (!alive) return
        setTx(transaction)
        setMethods(payment.methods)
        setSandbox(payment.sandbox)
        if (payment.methods[0]) setSelected(payment.methods[0].key)
      })
      .catch((err) => alive && setError(err instanceof ApiError ? err.message : 'Bitim yuklanmadi.'))

    return () => {
      alive = false
    }
  }, [id])

  async function handlePay() {
    if (!tx) return

    setPaying(true)
    try {
      const payment = await api.payments.create(tx.id, selected)

      if (sandbox && payment.providerPaymentId) {
        // Sandbox rejimida provayder sahifasi o'rniga to'lovni darhol tasdiqlaymiz.
        await api.payments.sandboxConfirm(payment.providerPaymentId, true)
        toast.success("To'lov qabul qilindi — pul Escrow hisobida bloklandi.")
        navigate(`/transactions/${tx.id}`)
        return
      }

      if (payment.checkoutUrl) {
        window.location.href = payment.checkoutUrl
        return
      }

      toast.error("To'lov sahifasi manzili kelmadi.")
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "To'lovni amalga oshirib bo'lmadi.")
    } finally {
      setPaying(false)
    }
  }

  if (error) {
    return (
      <div className="page py-24 text-center">
        <h1 className="font-display text-3xl font-bold">Bitim ochilmadi</h1>
        <p className="mt-3 text-muted">{error}</p>
      </div>
    )
  }

  if (!tx) return <PageLoader />

  // To'langan bitim uchun checkout emas, bitim sahifasi kerak.
  if (tx.status !== 'Initiated' && tx.status !== 'AwaitingPayment') {
    navigate(`/transactions/${tx.id}`, { replace: true })
    return <PageLoader />
  }

  return (
    <div className="mx-auto w-full max-w-[920px] px-4 pb-16 pt-8 sm:px-6 sm:pb-24 sm:pt-11">
      <h1 className="m-0 mb-2 font-display text-[clamp(24px,6vw,34px)] font-bold tracking-[-.02em]">
        Bitimni rasmiylashtirish
      </h1>
      <p className="m-0 mb-7 text-[15px] leading-relaxed text-muted sm:mb-9 sm:text-base">
        Pul Escrow hisobida bloklanadi va siz akkauntni tasdiqlagach sotuvchiga o'tadi.
      </p>

      <div className="mb-5 rounded-[20px] border border-white/[.08] bg-surface p-4 sm:p-7">
        <div className="label-caps mb-5">ESCROW HOLATI</div>
        <EscrowSteps status={tx.status} />
      </div>

      {/* Telefonda avval nima uchun to'layotgani ko'rinsin, keyin to'lov usuli. */}
      <div className="grid items-start gap-5 lg:grid-cols-[minmax(0,1.15fr)_minmax(0,.85fr)]">
        <div className="order-2 rounded-[20px] border border-white/[.08] bg-surface p-5 sm:p-7 lg:order-1">
          <div className="label-caps mb-[18px]">TO'LOV USULI</div>

          <div className="flex flex-col gap-3">
            {methods.map((m) => {
              const on = selected === m.key
              return (
                <button
                  key={m.key}
                  onClick={() => setSelected(m.key)}
                  className={[
                    'flex cursor-pointer items-center gap-3 rounded-[13px] border px-3.5 py-4 text-left transition-colors sm:gap-3.5 sm:px-4 sm:py-[15px]',
                    on ? 'border-brand/[.45] bg-brand/[.07]' : 'border-white/[.10] bg-field hover:border-white/25',
                  ].join(' ')}
                  aria-pressed={on}
                >
                  <span
                    className="grid h-[22px] w-[22px] flex-shrink-0 place-items-center rounded-full border-2"
                    style={{ borderColor: on ? '#FF6B35' : 'rgba(255,255,255,.22)' }}
                  >
                    <span
                      className="h-2.5 w-2.5 rounded-full"
                      style={{ background: on ? '#FF6B35' : 'transparent' }}
                    />
                  </span>

                  <span
                    className="grid h-[30px] w-11 flex-shrink-0 place-items-center rounded-[7px] font-display text-[11px] font-bold text-white"
                    style={{ background: m.brand }}
                  >
                    {SHORT[m.key]}
                  </span>

                  <span className="flex-1">
                    <span className="block text-[15px] font-semibold">{m.name}</span>
                    <span className="block text-[13px] text-dim">{m.note}</span>
                  </span>
                </button>
              )
            })}
          </div>

          <button
            onClick={handlePay}
            disabled={paying || methods.length === 0}
            className="btn-primary mt-6 flex w-full items-center justify-center gap-2 py-[17px] text-base shadow-brand-sm"
          >
            {paying ? <Spinner size={18} /> : "To'lovni amalga oshirish"}
          </button>

          {sandbox && (
            <p className="mt-3 text-center text-xs text-dim">
              Sandbox rejimi: haqiqiy pul yechilmaydi, to'lov darhol tasdiqlanadi.
            </p>
          )}

          <div className="mt-4">
            <EscrowNote>
              Bitim Escrow kafolati bilan himoyalangan. Pulingiz bitim tugaguncha himoyalangan.
            </EscrowNote>
          </div>
        </div>

        {/* ── Bitim xulosasi ───────────────────────────────────────── */}
        <div className="order-1 rounded-[20px] border border-white/[.08] bg-surface p-5 sm:p-6 lg:order-2">
          <div className="label-caps mb-[18px]">BITIM XULOSASI</div>

          <div className="mb-5 flex gap-3">
            <div
              className="grid h-[52px] w-[74px] flex-shrink-0 place-items-center rounded-[10px] text-[22px]"
              style={{ background: `linear-gradient(135deg, ${tx.listing.gameColor}, #12122A)` }}
            >
              {tx.listing.gameGlyph}
            </div>
            <div>
              <div className="text-sm font-semibold leading-[1.35]">{tx.listing.title}</div>
              <div className="mt-1 text-[13px] text-dim">
                {tx.listing.rankLevel} · {tx.listing.serverRegion}
              </div>
            </div>
          </div>

          <div className="mb-5 flex items-center gap-2.5 rounded-[11px] bg-white/[.04] p-3">
            <Avatar name={tx.seller.username} size={32} />
            <div className="text-[13px]">
              <div className="font-semibold">@{tx.seller.username}</div>
              <div className="text-warning">★ {tx.seller.rating.toFixed(1)}</div>
            </div>
          </div>

          <Row label="Akkaunt narxi" value={`${money(tx.amount)} so'm`} />
          <Row label="Xizmat haqi" value="0 so'm" valueClass="text-success" />

          <div className="flex items-baseline justify-between border-t border-white/[.08] pt-4">
            <span className="text-[15px] font-semibold">Jami</span>
            <span className="font-display text-[26px] font-extrabold text-brand">{money(tx.amount)}</span>
          </div>

          <div className="mt-4 rounded-[11px] bg-white/[.03] p-3 text-xs leading-relaxed text-dim">
            Escrow kodi: <span className="font-mono text-soft">{tx.escrowCode}</span>
            <br />
            Sotuvchiga o'tadi: {money(tx.sellerPayout)} so'm (komissiya {money(tx.commissionAmount)} so'm)
          </div>
        </div>
      </div>
    </div>
  )
}

function Row({ label, value, valueClass = 'text-white' }: { label: string; value: string; valueClass?: string }) {
  return (
    <div className="mb-2.5 flex justify-between text-sm text-muted">
      <span>{label}</span>
      <span className={valueClass}>{value}</span>
    </div>
  )
}
