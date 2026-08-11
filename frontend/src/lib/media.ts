import { API_BASE } from './api'

/**
 * Yuklangan rasm manzilini to'liq URL'ga aylantiradi.
 *
 * Local saqlashda backend nisbiy yo'l qaytaradi ("/uploads/2026/08/x.png").
 * Frontend boshqa portda (yoki domenda) ishlagani uchun bu yo'lni to'g'ridan-to'g'ri
 * <img src> ga bersak, brauzer uni frontend manziliga nisbatan qidiradi va topolmaydi.
 * Shuning uchun nisbiy yo'llarni API manziliga bog'laymiz.
 *
 * MinIO / S3 ga o'tilganda storage to'liq "https://..." qaytaradi — u holda o'zgartirmaymiz.
 */
export function mediaUrl(url: string | null | undefined): string | undefined {
  if (!url) return undefined
  if (/^https?:\/\//i.test(url) || url.startsWith('data:')) return url
  return `${API_BASE}${url.startsWith('/') ? '' : '/'}${url}`
}
