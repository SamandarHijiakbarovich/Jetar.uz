/** Backend DTO'lariga mos turlar (Jetar.Core/Contracts). */

export type GameType =
  | 'EFootball'
  | 'PubgMobile'
  | 'FreeFire'
  | 'CsGo'
  | 'Dota2'
  | 'Valorant'
  | 'CodMobile'

export type ListingStatus = 'Pending' | 'Active' | 'Reserved' | 'Sold' | 'Hidden' | 'Blocked'

/** E'lon turi — o'yindan mustaqil ikkinchi kategoriya o'lchovi. */
export type ListingType = 'Account' | 'Currency' | 'Item' | 'Service'

export type TransactionStatus =
  | 'Initiated'
  | 'AwaitingPayment'
  | 'EscrowHeld'
  | 'CredentialsSent'
  | 'BuyerVerifying'
  | 'Completed'
  | 'Disputed'
  | 'Refunded'
  | 'Cancelled'

export type PaymentStatus = 'Created' | 'Pending' | 'Paid' | 'Failed' | 'Refunded' | 'Cancelled'
export type PaymentMethod = 'Click' | 'Payme' | 'Uzum' | 'Apelsin'
export type DisputeStatus = 'Open' | 'UnderReview' | 'ResolvedForBuyer' | 'ResolvedForSeller' | 'Rejected'
export type UserRole = 'User' | 'Moderator' | 'Admin'

export interface User {
  id: string
  username: string
  firstName: string
  lastName: string
  /** "Alisher Karimov" — backend tomonda birlashtiriladi. */
  fullName: string
  phone: string
  telegramUsername?: string | null
  city?: string | null
  avatarUrl?: string | null
  rating: number
  ratingCount: number
  totalSales: number
  totalPurchases: number
  isVerified: boolean
  role: UserRole
  avgResponseMinutes: number
  createdAt: string
}

export interface AuthResponse {
  accessToken: string
  refreshToken: string
  expiresAt: string
  user: User
}

export interface ListingCard {
  id: string
  title: string
  gameType: GameType
  gameName: string
  gameGlyph: string
  gameColor: string
  type: ListingType
  typeName: string
  typeGlyph: string
  price: number
  rankLevel: string
  serverRegion: string
  coverImage?: string | null
  isVerified: boolean
  isBoosted: boolean
  status: ListingStatus
  sellerUsername: string
  sellerRating: number
  createdAt: string
}

export interface Seller {
  id: string
  username: string
  fullName: string
  avatarUrl?: string | null
  rating: number
  ratingCount: number
  totalSales: number
  isVerified: boolean
  avgResponseMinutes: number
  memberSince: string
}

export interface ListingDetail {
  id: string
  title: string
  description: string
  gameType: GameType
  gameName: string
  gameGlyph: string
  gameColor: string
  type: ListingType
  typeName: string
  typeGlyph: string
  price: number
  rankLevel: string
  serverRegion: string
  inGameItems: string[]
  images: string[]
  stats: Record<string, string>
  isVerified: boolean
  isBoosted: boolean
  status: ListingStatus
  viewCount: number
  createdAt: string
  seller: Seller
  similar: ListingCard[]
}

export interface Party {
  id: string
  username: string
  avatarUrl?: string | null
  rating: number
  isVerified: boolean
}

export interface Transaction {
  id: string
  number: number
  escrowCode: string
  status: TransactionStatus
  statusLabel: string
  amount: number
  commissionAmount: number
  sellerPayout: number
  buyerConfirmed: boolean
  sellerConfirmed: boolean
  createdAt: string
  escrowHeldAt?: string | null
  autoReleaseAt?: string | null
  completedAt?: string | null
  listing: ListingCard
  buyer: Party
  seller: Party
  hasDispute: boolean
  viewerIsBuyer: boolean
  viewerHasRated: boolean
}

export interface Payment {
  id: string
  transactionId: string
  amount: number
  method: PaymentMethod
  status: PaymentStatus
  checkoutUrl?: string | null
  providerPaymentId?: string | null
  createdAt: string
  paidAt?: string | null
}

export interface Message {
  id: string
  transactionId: string
  senderId: string
  senderUsername: string
  text: string
  attachmentUrl?: string | null
  isSystem: boolean
  isRead: boolean
  createdAt: string
}

export interface Rating {
  id: string
  score: number
  comment?: string | null
  fromUsername: string
  fromAvatarUrl?: string | null
  createdAt: string
}

export interface GameSummary {
  slug: string
  name: string
  glyph: string
  color: string
  listingCount: number
}

export interface ListingTypeSummary {
  slug: string
  name: string
  glyph: string
  description: string
  listingCount: number
}

export interface Categories {
  types: ListingTypeSummary[]
  games: GameSummary[]
}

export interface Paged<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasNext: boolean
}

export interface AdminStats {
  totalUsers: number
  totalTransactions: number
  totalRevenue: number
  openDisputes: number
  disputesOver24h: number
  usersGrowthPercent: number
  transactionsGrowthPercent: number
  revenueGrowthPercent: number
  activeListings: number
  pendingListings: number
}

export interface AdminTransactionRow {
  id: string
  number: number
  buyerUsername: string
  sellerUsername: string
  amount: number
  status: TransactionStatus
  statusLabel: string
  createdAt: string
}

export interface AdminDisputeRow {
  id: string
  transactionId: string
  transactionNumber: number
  reason: string
  status: DisputeStatus
  ageHours: number
  createdAt: string
}

export interface AdminUserRow {
  id: string
  username: string
  fullName: string
  phone: string
  role: UserRole
  rating: number
  ratingCount: number
  totalSales: number
  totalPurchases: number
  isVerified: boolean
  isBlocked: boolean
  createdAt: string
}

export interface PlatformSettings {
  commissionRate: number
  autoReleaseHours: number
  minListingPrice: number
  maxListingPrice: number
  autoApproveListings: boolean
  sandboxPayments: boolean
}

export interface PaymentMethodOption {
  key: PaymentMethod
  name: string
  brand: string
  note: string
  enabled: boolean
}

export interface ListingQuery {
  game?: string
  /** akkaunt | valyuta | buyum | xizmat */
  type?: string
  search?: string
  minPrice?: number
  maxPrice?: number
  region?: string
  verifiedOnly?: boolean
  sellerId?: string
  sort?: 'newest' | 'price_asc' | 'price_desc' | 'popular'
  page?: number
  pageSize?: number
}
