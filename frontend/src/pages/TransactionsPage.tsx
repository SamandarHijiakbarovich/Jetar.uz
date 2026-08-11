import { useEffect, useState } from 'react'
import TransactionTable from '../components/TransactionTable'
import { PageLoader } from '../components/ui'
import { api } from '../lib/api'
import type { Transaction } from '../lib/types'

const ROLES = [
  { key: undefined, label: 'Barchasi' },
  { key: 'buyer' as const, label: 'Xaridor sifatida' },
  { key: 'seller' as const, label: 'Sotuvchi sifatida' },
]

export default function TransactionsPage() {
  const [role, setRole] = useState<'buyer' | 'seller' | undefined>(undefined)
  const [items, setItems] = useState<Transaction[] | null>(null)

  useEffect(() => {
    let alive = true
    setItems(null)

    api.transactions
      .list(role, 1, 50)
      .then((res) => alive && setItems(res.items))
      .catch(() => alive && setItems([]))

    return () => {
      alive = false
    }
  }, [role])

  return (
    <div className="page pb-20 pt-8 sm:pb-24 sm:pt-11">
      <h1 className="m-0 mb-2 font-display text-[clamp(24px,6vw,34px)] font-bold tracking-[-.02em]">
        Bitimlarim
      </h1>
      <p className="m-0 mb-6 text-[15px] text-muted sm:mb-7 sm:text-base">
        Escrow bitimlari tarixi va joriy holati.
      </p>

      <div className="no-scrollbar -mx-4 mb-6 flex gap-2 overflow-x-auto px-4 sm:mx-0 sm:flex-wrap sm:px-0">
        {ROLES.map((r) => (
          <button
            key={r.label}
            onClick={() => setRole(r.key)}
            className={[
              'whitespace-nowrap rounded-full border px-[15px] py-2 text-[13px] font-semibold transition-colors',
              role === r.key
                ? 'border-brand bg-brand/[.12] text-brand'
                : 'border-white/[.12] text-muted hover:border-white/30 hover:text-white',
            ].join(' ')}
          >
            {r.label}
          </button>
        ))}
      </div>

      {items === null ? <PageLoader label="Bitimlar yuklanmoqda…" /> : <TransactionTable items={items} />}
    </div>
  )
}
