import { useCallback, useEffect, useRef, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import EscrowSteps from '../components/EscrowSteps'
import { Avatar, EscrowNote, PageLoader, Spinner, StatusPill } from '../components/ui'
import { useAuth } from '../context/auth-context'
import { useToast } from '../context/toast-context'
import { API_BASE, ApiError, api, tokenStore } from '../lib/api'
import { dateTime, money, timeLeft, timeOnly } from '../lib/format'
import { TRANSACTION_TONES } from '../lib/status'
import type { Message, Transaction } from '../lib/types'

/** Escrow bitimi sahifasi: holat, amallar va real vaqtdagi chat. */
export default function TransactionPage() {
  const { id = '' } = useParams()
  const { user } = useAuth()
  const toast = useToast()

  const [tx, setTx] = useState<Transaction | null>(null)
  const [messages, setMessages] = useState<Message[]>([])
  const [draft, setDraft] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const [disputeOpen, setDisputeOpen] = useState(false)
  const [disputeReason, setDisputeReason] = useState('')
  const [ratingScore, setRatingScore] = useState(5)
  const [ratingComment, setRatingComment] = useState('')

  const threadRef = useRef<HTMLDivElement>(null)

  const reload = useCallback(async () => {
    const [transaction, thread] = await Promise.all([api.transactions.get(id), api.chat.thread(id)])
    setTx(transaction)
    setMessages(thread)
  }, [id])

  useEffect(() => {
    reload().catch((err) => setError(err instanceof ApiError ? err.message : 'Bitim yuklanmadi.'))
  }, [reload])

  // SignalR — yangi xabarlar darhol keladi; ulanmasa 10 soniyada bir marta so'raymiz.
  useEffect(() => {
    if (!id || !tokenStore.access) return

    let disposed = false
    let connection: { stop: () => Promise<void> } | null = null
    let poll: ReturnType<typeof setInterval> | null = null

    async function connect() {
      try {
        const signalr = await import('@microsoft/signalr')

        const conn = new signalr.HubConnectionBuilder()
          .withUrl(`${API_BASE}/hubs/chat`, { accessTokenFactory: () => tokenStore.access ?? '' })
          .withAutomaticReconnect()
          .configureLogging(signalr.LogLevel.Warning)
          .build()

        conn.on('ReceiveMessage', (message: Message) => {
          setMessages((current) =>
            current.some((m) => m.id === message.id) ? current : [...current, message],
          )
          // Tizim xabari escrow holati o'zgarganini bildiradi.
          if (message.isSystem) api.transactions.get(id).then(setTx).catch(() => {})
        })

        await conn.start()
        await conn.invoke('JoinTransaction', id)

        if (disposed) {
          await conn.stop()
          return
        }

        connection = conn
      } catch {
        // Real-time ulanmadi — davriy so'rovga o'tamiz.
        poll = setInterval(() => {
          api.chat.thread(id).then(setMessages).catch(() => {})
        }, 10_000)
      }
    }

    connect()

    return () => {
      disposed = true
      connection?.stop().catch(() => {})
      if (poll) clearInterval(poll)
    }
  }, [id])

  useEffect(() => {
    threadRef.current?.scrollTo({ top: threadRef.current.scrollHeight, behavior: 'smooth' })
  }, [messages])

  useEffect(() => {
    if (tx) api.chat.markRead(tx.id).catch(() => {})
  }, [tx])

  async function run(action: () => Promise<unknown>, successText: string) {
    setBusy(true)
    try {
      await action()
      await reload()
      toast.success(successText)
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : 'Amal bajarilmadi.')
    } finally {
      setBusy(false)
    }
  }

  async function sendMessage(e: React.FormEvent) {
    e.preventDefault()
    const text = draft.trim()
    if (!text || !tx) return

    setDraft('')
    try {
      const sent = await api.chat.send(tx.id, text)
      setMessages((current) => (current.some((m) => m.id === sent.id) ? current : [...current, sent]))
    } catch (err) {
      setDraft(text)
      toast.error(err instanceof ApiError ? err.message : 'Xabar yuborilmadi.')
    }
  }

  if (error) {
    return (
      <div className="page py-24 text-center">
        <h1 className="font-display text-3xl font-bold">Bitim ochilmadi</h1>
        <p className="mt-3 text-muted">{error}</p>
        <Link to="/transactions" className="btn-primary mt-6 inline-block px-7 py-3.5">
          Bitimlarim
        </Link>
      </div>
    )
  }

  if (!tx || !user) return <PageLoader />

  const isBuyer = tx.viewerIsBuyer
  const other = isBuyer ? tx.seller : tx.buyer
  const canPay = tx.status === 'Initiated' || tx.status === 'AwaitingPayment'
  const canSendCredentials = !isBuyer && tx.status === 'EscrowHeld'
  const canRelease = isBuyer && ['EscrowHeld', 'CredentialsSent', 'BuyerVerifying'].includes(tx.status)
  const canDispute = !tx.hasDispute && !['Completed', 'Refunded', 'Cancelled'].includes(tx.status)
  const canCancel = canPay
  const canRate = tx.status === 'Completed' && !tx.viewerHasRated

  return (
    <div className="page pb-16 pt-6 sm:pb-24 sm:pt-11">
      <Link to="/transactions" className="mb-5 inline-block text-sm font-semibold text-muted hover:text-brand sm:mb-6">
        ← Bitimlarim
      </Link>

      <div className="mb-5 flex flex-wrap items-center gap-3 sm:mb-6 sm:gap-4">
        <h1 className="m-0 font-display text-[clamp(22px,6vw,32px)] font-bold tracking-[-.02em]">
          Bitim #{tx.number}
        </h1>
        <StatusPill tone={TRANSACTION_TONES[tx.status]} />
        <span className="font-mono text-xs text-dim sm:text-sm">{tx.escrowCode}</span>
      </div>

      <div className="mb-5 rounded-[20px] border border-white/[.08] bg-surface p-4 sm:mb-6 sm:p-7">
        <div className="label-caps mb-5">ESCROW HOLATI</div>
        <EscrowSteps status={tx.status} />

        {tx.autoReleaseAt && ['CredentialsSent', 'BuyerVerifying'].includes(tx.status) && (
          <p className="mt-6 text-center text-[13px] leading-relaxed text-muted">
            Siz tasdiqlamasangiz, pul <strong className="text-brand">{timeLeft(tx.autoReleaseAt)}</strong> dan
            keyin avtomatik sotuvchiga o'tadi.
          </p>
        )}
      </div>

      {/*
       * Telefonda amallar paneli chatdan oldin turadi (order-1): "Tasdiqlayman"
       * va "To'lovga o'tish" — sahifadagi eng muhim tugmalar.
       */}
      <div className="grid items-start gap-5 sm:gap-6 lg:grid-cols-[minmax(0,1.3fr)_minmax(0,.7fr)]">
        {/* ── Chat ─────────────────────────────────────────────────── */}
        <div className="card order-2 flex h-[min(70vh,560px)] flex-col overflow-hidden lg:order-1">
          <div className="flex items-center gap-3 border-b border-white/[.06] px-6 py-4">
            <Avatar name={other.username} size={38} />
            <div>
              <div className="font-display text-[15px] font-bold">@{other.username}</div>
              <div className="text-xs text-dim">{isBuyer ? 'Sotuvchi' : 'Xaridor'}</div>
            </div>
          </div>

          <div ref={threadRef} className="flex-1 space-y-3 overflow-y-auto px-6 py-5">
            {messages.length === 0 && (
              <p className="py-10 text-center text-sm text-dim">Hozircha xabar yo'q.</p>
            )}

            {messages.map((m) =>
              m.isSystem ? (
                <div key={m.id} className="my-4 text-center">
                  <span className="inline-block rounded-full border border-white/[.08] bg-white/[.04] px-4 py-2 text-xs leading-relaxed text-muted">
                    {m.text}
                  </span>
                </div>
              ) : (
                <div
                  key={m.id}
                  className={`flex ${m.senderId === user.id ? 'justify-end' : 'justify-start'}`}
                >
                  <div
                    className={[
                      'max-w-[78%] rounded-2xl px-4 py-2.5 text-sm leading-relaxed',
                      m.senderId === user.id
                        ? 'rounded-br-md bg-brand/[.18] text-white'
                        : 'rounded-bl-md bg-white/[.06] text-soft-3',
                    ].join(' ')}
                  >
                    <div className="whitespace-pre-wrap break-words">{m.text}</div>
                    <div className="mt-1 text-right text-[11px] text-dim">{timeOnly(m.createdAt)}</div>
                  </div>
                </div>
              ),
            )}
          </div>

          <form onSubmit={sendMessage} className="flex gap-2.5 border-t border-white/[.06] p-3 sm:p-4">
            <input
              value={draft}
              onChange={(e) => setDraft(e.target.value)}
              placeholder="Xabar yozing…"
              className="field min-w-0 flex-1 !py-3"
              maxLength={2000}
              disabled={tx.status === 'Cancelled'}
              aria-label="Xabar matni"
              enterKeyHint="send"
            />
            <button
              type="submit"
              disabled={!draft.trim() || tx.status === 'Cancelled'}
              className="btn-primary tap-target flex-shrink-0 px-4 py-3 sm:px-6"
              aria-label="Yuborish"
            >
              <span className="hidden sm:inline">Yuborish</span>
              <span className="sm:hidden">➤</span>
            </button>
          </form>
        </div>

        {/* ── Amallar ──────────────────────────────────────────────── */}
        <div className="order-1 flex flex-col gap-4 lg:order-2">
          <div className="card p-6">
            <Link
              to={`/listings/${tx.listing.id}`}
              className="mb-5 flex gap-3 text-white hover:text-white"
            >
              <div
                className="grid h-[52px] w-[74px] flex-shrink-0 place-items-center rounded-[10px] text-[22px]"
                style={{ background: `linear-gradient(135deg, ${tx.listing.gameColor}, #12122A)` }}
              >
                {tx.listing.gameGlyph}
              </div>
              <div>
                <div className="line-clamp-2 text-sm font-semibold leading-[1.35]">{tx.listing.title}</div>
                <div className="mt-1 font-display text-base font-bold text-brand">{money(tx.amount)}</div>
              </div>
            </Link>

            <div className="space-y-1.5 border-t border-white/[.08] pt-4 text-[13px] text-muted">
              <div className="flex justify-between">
                <span>Ochilgan</span>
                <span className="text-soft">{dateTime(tx.createdAt)}</span>
              </div>
              {!isBuyer && (
                <div className="flex justify-between">
                  <span>Sizga o'tadi</span>
                  <span className="font-semibold text-success-fg">{money(tx.sellerPayout)} so'm</span>
                </div>
              )}
              {!isBuyer && (
                <div className="flex justify-between">
                  <span>Komissiya</span>
                  <span className="text-soft">{money(tx.commissionAmount)} so'm</span>
                </div>
              )}
            </div>
          </div>

          <div className="card space-y-3 p-6">
            <div className="label-caps mb-1">AMALLAR</div>

            {canPay && isBuyer && (
              <Link to={`/checkout/${tx.id}`} className="btn-primary block w-full py-3.5 text-center">
                To'lovga o'tish
              </Link>
            )}

            {canSendCredentials && (
              <button
                onClick={() =>
                  run(
                    () => api.transactions.credentialsSent(tx.id),
                    "Xaridorga xabar berildi — endi u akkauntni tekshiradi.",
                  )
                }
                disabled={busy}
                className="btn-primary w-full py-3.5"
              >
                {busy ? <Spinner size={18} /> : "Ma'lumotlar yuborildi"}
              </button>
            )}

            {canRelease && (
              <button
                onClick={() =>
                  run(() => api.transactions.release(tx.id), 'Bitim yakunlandi — pul sotuvchiga chiqarildi.')
                }
                disabled={busy}
                className="btn-primary w-full bg-gradient-to-br from-success to-[#059669] py-3.5 shadow-none"
              >
                {busy ? <Spinner size={18} /> : '✓ Tasdiqlayman'}
              </button>
            )}

            {canCancel && (
              <button
                onClick={() => run(() => api.transactions.cancel(tx.id), 'Bitim bekor qilindi.')}
                disabled={busy}
                className="btn-ghost w-full py-3"
              >
                Bekor qilish
              </button>
            )}

            {canDispute && !disputeOpen && (
              <button
                onClick={() => setDisputeOpen(true)}
                className="w-full rounded-[14px] border border-danger/40 bg-transparent py-3 text-sm font-semibold text-danger-fg transition-colors hover:bg-danger hover:text-white"
              >
                Nizo ochish
              </button>
            )}

            {disputeOpen && (
              <div className="space-y-3 rounded-[14px] border border-danger/25 bg-danger/5 p-4">
                <textarea
                  value={disputeReason}
                  onChange={(e) => setDisputeReason(e.target.value)}
                  placeholder="Muammoni batafsil yozing — moderator shu matn asosida qaror qabul qiladi."
                  rows={4}
                  className="field resize-y text-sm"
                />
                <div className="flex gap-2.5">
                  <button
                    onClick={() =>
                      run(
                        () => api.transactions.dispute(tx.id, disputeReason.trim()),
                        'Nizo ochildi — moderator 24 soat ichida ko\'rib chiqadi.',
                      ).then(() => {
                        setDisputeOpen(false)
                        setDisputeReason('')
                      })
                    }
                    disabled={busy || disputeReason.trim().length < 15}
                    className="btn-primary flex-1 bg-gradient-to-br from-danger to-[#B91C1C] py-2.5 text-sm shadow-none"
                  >
                    Yuborish
                  </button>
                  <button onClick={() => setDisputeOpen(false)} className="btn-ghost px-4 py-2.5 text-sm">
                    Bekor
                  </button>
                </div>
              </div>
            )}

            {canRate && (
              <div className="space-y-3 rounded-[14px] border border-white/[.10] bg-white/[.03] p-4">
                <div className="text-sm font-semibold">Bitimni baholang</div>

                <div className="flex gap-1.5">
                  {[1, 2, 3, 4, 5].map((n) => (
                    <button
                      key={n}
                      onClick={() => setRatingScore(n)}
                      className={`text-2xl transition-colors ${n <= ratingScore ? 'text-warning' : 'text-white/20'}`}
                      aria-label={`${n} yulduz`}
                    >
                      ★
                    </button>
                  ))}
                </div>

                <textarea
                  value={ratingComment}
                  onChange={(e) => setRatingComment(e.target.value)}
                  placeholder="Izoh (ixtiyoriy)"
                  rows={3}
                  className="field resize-y text-sm"
                />

                <button
                  onClick={() =>
                    run(
                      () => api.transactions.rate(tx.id, ratingScore, ratingComment.trim() || undefined),
                      'Rahmat! Bahoyingiz qabul qilindi.',
                    )
                  }
                  disabled={busy}
                  className="btn-primary w-full py-2.5 text-sm"
                >
                  Baho qoldirish
                </button>
              </div>
            )}

            {tx.status === 'Disputed' && (
              <div className="rounded-xl border border-danger/25 bg-danger/[.06] p-3.5 text-[13px] leading-relaxed text-danger-fg">
                Nizo ochilgan. Pul escrowda muzlatildi, moderator qarorini kuting.
              </div>
            )}
          </div>

          <EscrowNote>
            {isBuyer
              ? "Pul Jetar hisobida. Akkauntni tekshirmaguningizcha sotuvchiga o'tmaydi."
              : "Xaridor tasdiqlagach pul hisobingizga chiqariladi."}
          </EscrowNote>
        </div>
      </div>
    </div>
  )
}
