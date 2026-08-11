import type { GameType, ListingType } from './types'

/** Backend ListingTypeCatalog bilan bir xil bo'lishi shart. */
export const LISTING_TYPES: {
  type: ListingType
  slug: string
  name: string
  glyph: string
  description: string
}[] = [
  { type: 'Account', slug: 'akkaunt', name: 'Akkauntlar', glyph: '🎮', description: "To'liq o'yin akkauntlari" },
  { type: 'Currency', slug: 'valyuta', name: "O'yin valyutasi", glyph: '💎', description: 'UC, Diamond, GP, Coins' },
  { type: 'Item', slug: 'buyum', name: 'Skin va buyum', glyph: '🎁', description: 'Skinlar, bundle va qurollar' },
  { type: 'Service', slug: 'xizmat', name: 'Xizmatlar', glyph: '🚀', description: "Rank ko'tarish va mashg'ulot" },
]

export function listingTypeBySlug(slug: string) {
  return LISTING_TYPES.find((t) => t.slug === slug)
}

/** Backend GameCatalog bilan bir xil — e'lon yaratish formasi shu ro'yxatdan foydalanadi. */
export const GAMES: { type: GameType; slug: string; name: string; glyph: string; color: string }[] = [
  { type: 'EFootball', slug: 'efootball', name: 'eFootball', glyph: '⚽', color: '#3B82F6' },
  { type: 'PubgMobile', slug: 'pubg-mobile', name: 'PUBG Mobile', glyph: '🎯', color: '#F59E0B' },
  { type: 'FreeFire', slug: 'free-fire', name: 'Free Fire', glyph: '🔥', color: '#EF4444' },
  { type: 'CsGo', slug: 'csgo', name: 'CS:GO', glyph: '💀', color: '#FF6B35' },
  { type: 'Dota2', slug: 'dota-2', name: 'Dota 2', glyph: '🗡️', color: '#7C3AED' },
  { type: 'Valorant', slug: 'valorant', name: 'Valorant', glyph: '🎯', color: '#DC2626' },
  { type: 'CodMobile', slug: 'cod-mobile', name: 'COD Mobile', glyph: '⚔️', color: '#0EA5E9' },
]

export const REGIONS = ['ASIA', 'EU', 'NA', 'CIS'] as const

export const RANKS = [
  'Dream League',
  'FIFA Champion',
  'Superstar',
  'Conqueror',
  'Ace Master',
  'Ace',
  'Grandmaster',
  'Heroic',
  'Legend',
  'Legendary',
  'Immortal',
  'Divine',
  'Ascendant',
  'LEM',
] as const

export const IN_GAME_ITEMS = ['Skin', 'Diamond', 'UC', 'Coins', 'GP'] as const

export function gameBySlug(slug: string) {
  return GAMES.find((g) => g.slug === slug)
}

export function gameByType(type: GameType) {
  return GAMES.find((g) => g.type === type) ?? GAMES[0]
}

/**
 * Har bir o'yin uchun muqova (hero) rasmi manzili.
 * Rasm fayllari `public/games/<slug>.jpg` ga qo'yiladi (masalan efootball.jpg).
 * Fayl bo'lmasa GameArt avtomatik generativ muqovaga qaytadi — sayt buzilmaydi.
 */
export function gameImage(type: GameType): string {
  return `/games/${gameByType(type).slug}.jpg`
}
