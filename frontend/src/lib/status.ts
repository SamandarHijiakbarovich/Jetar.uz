import type { DisputeStatus, ListingStatus, TransactionStatus } from './types'

export interface Tone {
  label: string
  bg: string
  fg: string
}

/** Dizayndagi holat badge ranglariga aynan mos. */
export const TRANSACTION_TONES: Record<TransactionStatus, Tone> = {
  Initiated: { label: 'Ochildi', bg: 'rgba(255,255,255,.08)', fg: '#C5C5E0' },
  AwaitingPayment: { label: "To'lov kutilmoqda", bg: 'rgba(245,158,11,.14)', fg: '#FCD34D' },
  EscrowHeld: { label: 'Escrow', bg: 'rgba(59,130,246,.14)', fg: '#93C5FD' },
  CredentialsSent: { label: "Ma'lumot yuborildi", bg: 'rgba(59,130,246,.14)', fg: '#93C5FD' },
  BuyerVerifying: { label: 'Tekshirilmoqda', bg: 'rgba(59,130,246,.14)', fg: '#93C5FD' },
  Completed: { label: 'Yakunlandi', bg: 'rgba(16,185,129,.14)', fg: '#6EE7B7' },
  Disputed: { label: 'Nizo', bg: 'rgba(255,107,53,.16)', fg: '#FFB08A' },
  Refunded: { label: 'Qaytarildi', bg: 'rgba(148,163,184,.16)', fg: '#CBD5E1' },
  Cancelled: { label: 'Bekor qilindi', bg: 'rgba(239,68,68,.14)', fg: '#FCA5A5' },
}

export const LISTING_TONES: Record<ListingStatus, Tone> = {
  Pending: { label: 'Tekshiruvda', bg: 'rgba(245,158,11,.9)', fg: '#3B2606' },
  Active: { label: 'Aktiv', bg: 'rgba(16,185,129,.9)', fg: '#052E22' },
  Reserved: { label: 'Band', bg: 'rgba(59,130,246,.9)', fg: '#0A1E3D' },
  Sold: { label: 'Sotilgan', bg: 'rgba(120,120,150,.9)', fg: '#12121F' },
  Hidden: { label: 'Yashirilgan', bg: 'rgba(120,120,150,.9)', fg: '#12121F' },
  Blocked: { label: 'Bloklangan', bg: 'rgba(239,68,68,.9)', fg: '#2B0707' },
}

export const DISPUTE_TONES: Record<DisputeStatus, Tone> = {
  Open: { label: 'Ochiq', bg: 'rgba(239,68,68,.14)', fg: '#FCA5A5' },
  UnderReview: { label: "Ko'rib chiqilmoqda", bg: 'rgba(245,158,11,.14)', fg: '#FCD34D' },
  ResolvedForBuyer: { label: 'Xaridor foydasiga', bg: 'rgba(16,185,129,.14)', fg: '#6EE7B7' },
  ResolvedForSeller: { label: 'Sotuvchi foydasiga', bg: 'rgba(16,185,129,.14)', fg: '#6EE7B7' },
  Rejected: { label: 'Rad etildi', bg: 'rgba(148,163,184,.16)', fg: '#CBD5E1' },
}

/**
 * Escrow qadamining vizual holati — dizayndagi 3 bosqichli indikator uchun.
 * 0 — hali kelmagan, 1 — hozirgi, 2 — o'tilgan.
 */
export function escrowStepStates(status: TransactionStatus): [number, number, number] {
  switch (status) {
    case 'Initiated':
    case 'AwaitingPayment':
      return [1, 0, 0]
    case 'EscrowHeld':
    case 'CredentialsSent':
    case 'BuyerVerifying':
    case 'Disputed':
      return [2, 1, 0]
    case 'Completed':
      return [2, 2, 2]
    case 'Refunded':
    case 'Cancelled':
      return [2, 0, 0]
    default:
      return [1, 0, 0]
  }
}
