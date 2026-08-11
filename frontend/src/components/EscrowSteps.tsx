import { escrowStepStates } from '../lib/status'
import type { TransactionStatus } from '../lib/types'

const STEPS = ["Pul to'lanmoqda", 'Akkaunt tekshirilmoqda', 'Bitim yakunlandi']

/**
 * Uch bosqichli escrow indikatori — dizayndagi "ESCROW HOLATI" bloki.
 * Chiziqlar nuqtalar orasida ulanadi, o'tilgan qism to'q sariq.
 */
export default function EscrowSteps({ status }: { status: TransactionStatus }) {
  const states = escrowStepStates(status)
  const failed = status === 'Cancelled' || status === 'Refunded'
  const disputed = status === 'Disputed'

  return (
    <div className="relative grid grid-cols-3">
      {STEPS.map((label, index) => {
        const state = states[index]

        const dot =
          state === 0 ? '#2A2A48' : failed ? '#EF4444' : disputed && state === 1 ? '#F59E0B' : '#FF6B35'
        const fg = state === 0 ? '#9A9ABE' : '#fff'
        const labelFg = state === 0 ? '#9A9ABE' : '#fff'

        // Chiziqning chap yarmi oldingi qadamdan, o'ng yarmi keyingi qadamga.
        const leftDone = index > 0 && states[index - 1] >= 1
        const rightDone = state >= 2

        return (
          <div key={label} className="relative text-center">
            {index > 0 && (
              <span
                className="absolute left-0 right-1/2 top-[19px] z-0 h-[3px]"
                style={{ background: leftDone ? 'rgba(255,107,53,.5)' : 'rgba(255,255,255,.1)' }}
              />
            )}
            {index < STEPS.length - 1 && (
              <span
                className="absolute left-1/2 right-0 top-[19px] z-0 h-[3px]"
                style={{ background: rightDone ? 'rgba(255,107,53,.5)' : 'rgba(255,255,255,.1)' }}
              />
            )}

            <div
              className="relative z-[1] mx-auto mb-3.5 grid h-10 w-10 place-items-center rounded-full border-[3px] border-surface text-[15px] font-bold"
              style={{ background: dot, color: fg }}
            >
              {state === 2 ? '✓' : index + 1}
            </div>

            <div
              className="px-1 text-[11px] font-semibold leading-tight sm:text-sm"
              style={{ color: labelFg }}
            >
              {label}
            </div>
          </div>
        )
      })}
    </div>
  )
}
