/** Dizayndagi ko'rinish: "850 000" — probel bilan ajratilgan, tiyinsiz. */
export function money(value: number): string {
  return Math.round(value)
    .toString()
    .replace(/\B(?=(\d{3})+(?!\d))/g, ' ')
}

export function moneyUzs(value: number): string {
  return `${money(value)} so'm`
}

const dateFormatter = new Intl.DateTimeFormat('ru-RU', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
})

export function shortDate(iso: string): string {
  return dateFormatter.format(new Date(iso))
}

export function dateTime(iso: string): string {
  const d = new Date(iso)
  return `${dateFormatter.format(d)} ${d.getHours().toString().padStart(2, '0')}:${d
    .getMinutes()
    .toString()
    .padStart(2, '0')}`
}

export function timeOnly(iso: string): string {
  const d = new Date(iso)
  return `${d.getHours().toString().padStart(2, '0')}:${d.getMinutes().toString().padStart(2, '0')}`
}

/** "3 kun oldin", "2 soat oldin" — sharhlar va nizolar uchun. */
export function relativeTime(iso: string): string {
  const diffMs = Date.now() - new Date(iso).getTime()
  const minutes = Math.floor(diffMs / 60000)

  if (minutes < 1) return 'hozirgina'
  if (minutes < 60) return `${minutes} daqiqa oldin`

  const hours = Math.floor(minutes / 60)
  if (hours < 24) return `${hours} soat oldin`

  const days = Math.floor(hours / 24)
  if (days < 7) return `${days} kun oldin`

  const weeks = Math.floor(days / 7)
  if (weeks < 5) return `${weeks} hafta oldin`

  const months = Math.floor(days / 30)
  if (months < 12) return `${months} oy oldin`

  return `${Math.floor(days / 365)} yil oldin`
}

/** Qolgan vaqt: avto-release taymeri uchun. */
export function timeLeft(iso: string): string {
  const diffMs = new Date(iso).getTime() - Date.now()
  if (diffMs <= 0) return 'muddat tugadi'

  const hours = Math.floor(diffMs / 3_600_000)
  if (hours >= 24) return `${Math.floor(hours / 24)} kun ${hours % 24} soat`
  if (hours >= 1) return `${hours} soat`

  return `${Math.max(1, Math.floor(diffMs / 60_000))} daqiqa`
}

/** 4.9 → "★★★★★", 4.4 → "★★★★☆" */
export function stars(rating: number): string {
  const full = Math.round(rating)
  return '★'.repeat(Math.min(5, full)) + '☆'.repeat(Math.max(0, 5 - full))
}

export function initial(name: string): string {
  return (name.replace(/^@/, '')[0] ?? '?').toUpperCase()
}

/** Kirituvdagi raqamni "850 000" ko'rinishida ko'rsatish. */
export function formatPriceInput(raw: string): string {
  const digits = raw.replace(/\D/g, '')
  return digits ? money(Number(digits)) : ''
}

export function parsePriceInput(raw: string): number {
  return Number(raw.replace(/\D/g, '')) || 0
}
