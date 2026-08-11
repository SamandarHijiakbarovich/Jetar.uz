import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { PageLoader, Spinner } from '../components/ui'
import { api } from '../lib/api'

/**
 * To'lov provayderi foydalanuvchini shu sahifaga qaytaradi.
 * Sandbox rejimida to'lovni shu yerdan tasdiqlash mumkin.
 */
export default function CheckoutResultPage() {
  const [params] = useSearchParams()
  const paymentId = params.get('paymentId')
  const isSandbox = params.get('sandbox') === '1'

  const [state, setState] = useState<'idle' | 'confirming' | 'done' | 'failed'>('idle')
  const [transactionId, setTransactionId] = useState<string | null>(null)

  useEffect(() => {
    if (!isSandbox || !paymentId) return

    setState('confirming')
    api.payments
      .sandboxConfirm(paymentId, true)
      .then((payment) => {
        setTransactionId(payment.transactionId)
        setState('done')
      })
      .catch(() => setState('failed'))
  }, [isSandbox, paymentId])

  if (state === 'confirming') return <PageLoader label="To'lov tasdiqlanmoqda…" />

  const ok = state === 'done'

  return (
    <div className="mx-auto max-w-[520px] px-6 py-24 text-center">
      <div className="mb-6 text-6xl">{ok ? '✅' : state === 'failed' ? '⚠️' : '💳'}</div>

      <h1 className="m-0 mb-3 font-display text-3xl font-bold tracking-[-.02em]">
        {ok ? "To'lov qabul qilindi" : state === 'failed' ? "To'lov tasdiqlanmadi" : "To'lov holati"}
      </h1>

      <p className="m-0 mb-8 leading-relaxed text-muted">
        {ok
          ? "Pul Escrow hisobida bloklandi. Sotuvchi akkaunt ma'lumotlarini yuborganidan keyin tekshirib tasdiqlaysiz."
          : state === 'failed'
            ? "To'lovni tasdiqlab bo'lmadi. Bitim sahifasidan qayta urinib ko'ring."
            : "To'lov holatini bitim sahifasida ko'rishingiz mumkin."}
      </p>

      <div className="flex flex-wrap justify-center gap-3">
        {transactionId ? (
          <Link to={`/transactions/${transactionId}`} className="btn-primary px-7 py-3.5">
            Bitimga o'tish
          </Link>
        ) : (
          <Link to="/transactions" className="btn-primary px-7 py-3.5">
            Bitimlarim
          </Link>
        )}
        <Link to="/listings" className="btn-ghost px-7 py-3.5">
          E'lonlar
        </Link>
      </div>

      {state === 'idle' && !isSandbox && (
        <p className="mt-6 flex items-center justify-center gap-2 text-xs text-dim">
          <Spinner size={14} /> Provayder tasdiqlashini kutmoqda…
        </p>
      )}
    </div>
  )
}
