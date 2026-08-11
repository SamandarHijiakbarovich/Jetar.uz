import type {
  AdminDisputeRow,
  AdminStats,
  AdminTransactionRow,
  AuthResponse,
  Categories,
  GameSummary,
  ListingCard,
  ListingDetail,
  ListingQuery,
  Message,
  Paged,
  Payment,
  PaymentMethod,
  PaymentMethodOption,
  PlatformSettings,
  Rating,
  Transaction,
  User,
} from './types'

export const API_BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5080'

const ACCESS_KEY = 'jetar.access'
const REFRESH_KEY = 'jetar.refresh'

export const tokenStore = {
  get access() {
    return localStorage.getItem(ACCESS_KEY)
  },
  get refresh() {
    return localStorage.getItem(REFRESH_KEY)
  },
  set(access: string, refresh: string) {
    localStorage.setItem(ACCESS_KEY, access)
    localStorage.setItem(REFRESH_KEY, refresh)
  },
  clear() {
    localStorage.removeItem(ACCESS_KEY)
    localStorage.removeItem(REFRESH_KEY)
  },
}

/** Backend qaytargan xato: { code, message, traceId }. */
export class ApiError extends Error {
  status: number
  code: string

  constructor(status: number, code: string, message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.code = code
  }
}

/** 401 kelganda bir marta refresh qilamiz; parallel so'rovlar bitta urinishni kutadi. */
let refreshing: Promise<boolean> | null = null

async function tryRefresh(): Promise<boolean> {
  const token = tokenStore.refresh
  if (!token) return false

  refreshing ??= (async () => {
    try {
      const res = await fetch(`${API_BASE}/api/auth/refresh`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken: token }),
      })

      if (!res.ok) {
        tokenStore.clear()
        return false
      }

      const data: AuthResponse = await res.json()
      tokenStore.set(data.accessToken, data.refreshToken)
      return true
    } catch {
      return false
    } finally {
      // Keyingi 401 uchun yangi urinishga yo'l ochamiz.
      setTimeout(() => (refreshing = null), 0)
    }
  })()

  return refreshing
}

interface RequestOptions extends Omit<RequestInit, 'body'> {
  body?: unknown
  auth?: boolean
  retry?: boolean
}

async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const { body, auth = true, retry = true, headers, ...rest } = options

  const finalHeaders = new Headers(headers)
  const isFormData = body instanceof FormData

  if (body !== undefined && !isFormData) finalHeaders.set('Content-Type', 'application/json')

  const access = tokenStore.access
  if (auth && access) finalHeaders.set('Authorization', `Bearer ${access}`)

  const res = await fetch(`${API_BASE}${path}`, {
    ...rest,
    headers: finalHeaders,
    body: body === undefined ? undefined : isFormData ? (body as FormData) : JSON.stringify(body),
  })

  if (res.status === 401 && retry && auth && tokenStore.refresh) {
    if (await tryRefresh()) return request<T>(path, { ...options, retry: false })
  }

  if (res.status === 204) return undefined as T

  const text = await res.text()
  const payload = text ? safeParse(text) : null

  if (!res.ok) {
    const code = (payload as { code?: string })?.code ?? 'error'
    const message =
      (payload as { message?: string })?.message ??
      readValidationErrors(payload) ??
      `So'rov bajarilmadi (${res.status}).`

    throw new ApiError(res.status, code, message)
  }

  return payload as T
}

function safeParse(text: string): unknown {
  try {
    return JSON.parse(text)
  } catch {
    return text
  }
}

/** ASP.NET ValidationProblemDetails ichidan birinchi xabarni oladi. */
function readValidationErrors(payload: unknown): string | null {
  const errors = (payload as { errors?: Record<string, string[]> })?.errors
  if (!errors) return null

  const first = Object.values(errors).flat()[0]
  return first ?? null
}

function qs(params: Record<string, unknown>): string {
  const search = new URLSearchParams()

  for (const [key, value] of Object.entries(params)) {
    if (value === undefined || value === null || value === '') continue
    search.set(key, String(value))
  }

  const s = search.toString()
  return s ? `?${s}` : ''
}

export const api = {
  auth: {
    register: (body: {
      firstName: string
      lastName: string
      username: string
      phone: string
      password: string
      telegramUsername?: string
    }) => request<AuthResponse>('/api/auth/register', { method: 'POST', body, auth: false }),

    login: (body: { login: string; password: string }) =>
      request<AuthResponse>('/api/auth/login', { method: 'POST', body, auth: false }),

    me: () => request<User>('/api/auth/me'),

    changePassword: (body: { currentPassword: string; newPassword: string }) =>
      request<void>('/api/auth/change-password', { method: 'POST', body }),
  },

  listings: {
    search: (query: ListingQuery = {}) =>
      request<Paged<ListingCard>>(`/api/listings${qs(query as Record<string, unknown>)}`, { auth: false }),

    get: (id: string) => request<ListingDetail>(`/api/listings/${id}`),

    games: () => request<GameSummary[]>('/api/listings/games', { auth: false }),

    categories: () => request<Categories>('/api/listings/categories', { auth: false }),

    create: (body: {
      gameType: string
      type: string
      title: string
      description: string
      price: number
      serverRegion: string
      rankLevel: string
      inGameItems?: string[]
      images?: string[]
      stats?: Record<string, string>
    }) => request<ListingDetail>('/api/listings', { method: 'POST', body }),

    update: (id: string, body: Record<string, unknown>) =>
      request<ListingDetail>(`/api/listings/${id}`, { method: 'PATCH', body }),

    remove: (id: string) => request<void>(`/api/listings/${id}`, { method: 'DELETE' }),

    upload: (file: File) => {
      const form = new FormData()
      form.append('file', file)
      return request<{ url: string }>('/api/listings/upload', { method: 'POST', body: form })
    },
  },

  transactions: {
    list: (role?: 'buyer' | 'seller', page = 1, pageSize = 20) =>
      request<Paged<Transaction>>(`/api/transactions${qs({ role, page, pageSize })}`),

    get: (id: string) => request<Transaction>(`/api/transactions/${id}`),

    initiate: (listingId: string) =>
      request<Transaction>('/api/transactions/initiate', { method: 'POST', body: { listingId } }),

    credentialsSent: (id: string) =>
      request<Transaction>(`/api/transactions/${id}/credentials-sent`, { method: 'POST' }),

    release: (id: string) => request<Transaction>(`/api/transactions/${id}/release`, { method: 'POST' }),

    cancel: (id: string, reason?: string) =>
      request<Transaction>(`/api/transactions/${id}/cancel`, { method: 'POST', body: { reason } }),

    dispute: (id: string, reason: string, evidenceUrls?: string[]) =>
      request<unknown>(`/api/transactions/${id}/dispute`, { method: 'POST', body: { reason, evidenceUrls } }),

    rate: (id: string, score: number, comment?: string) =>
      request<Rating>(`/api/transactions/${id}/rating`, { method: 'POST', body: { score, comment } }),
  },

  payments: {
    methods: () =>
      request<{ sandbox: boolean; methods: PaymentMethodOption[] }>('/api/payments/methods', { auth: false }),

    create: (transactionId: string, method: PaymentMethod) =>
      request<Payment>('/api/payments', { method: 'POST', body: { transactionId, method } }),

    sandboxConfirm: (providerPaymentId: string, success = true) =>
      request<Payment>('/api/payments/sandbox/confirm', {
        method: 'POST',
        body: { providerPaymentId, success },
      }),
  },

  chat: {
    thread: (transactionId: string) => request<Message[]>(`/api/chat/${transactionId}`),

    send: (transactionId: string, text: string, attachmentUrl?: string) =>
      request<Message>('/api/chat/send', { method: 'POST', body: { transactionId, text, attachmentUrl } }),

    markRead: (transactionId: string) => request<void>(`/api/chat/${transactionId}/read`, { method: 'POST' }),
  },

  users: {
    profile: (username: string) =>
      request<{ user: User; listings: ListingCard[]; ratings: Rating[] }>(
        `/api/users/${encodeURIComponent(username)}`,
        { auth: false },
      ),

    myListings: (page = 1, pageSize = 24) =>
      request<Paged<ListingCard>>(`/api/users/me/listings${qs({ page, pageSize })}`),

    summary: () =>
      request<{
        user: User
        activeListings: number
        openTransactions: number
        totalEarned: number
        unreadMessages: number
        memberForDays: number
      }>('/api/users/me/summary'),

    updateProfile: (body: {
      firstName?: string
      lastName?: string
      username?: string
      phone?: string
      telegramUsername?: string
      city?: string
    }) => request<User>('/api/users/me', { method: 'PATCH', body }),

    updateNotifications: (body: {
      notifyTelegram: boolean
      notifyNewMessage: boolean
      notifyMarketing: boolean
    }) => request<User>('/api/users/me/notifications', { method: 'PATCH', body }),
  },

  admin: {
    stats: () => request<AdminStats>('/api/admin/stats'),

    transactions: (search?: string, page = 1, pageSize = 20) =>
      request<Paged<AdminTransactionRow>>(`/api/admin/transactions${qs({ search, page, pageSize })}`),

    disputes: (openOnly = true) => request<AdminDisputeRow[]>(`/api/admin/disputes${qs({ openOnly })}`),

    resolveDispute: (id: string, favourBuyer: boolean, note?: string) =>
      request<{ transactionId: string; status: string }>(`/api/admin/disputes/${id}/resolve`, {
        method: 'POST',
        body: { favourBuyer, note },
      }),

    settings: () => request<PlatformSettings>('/api/admin/settings'),

    verifyListing: (id: string, verified = true) =>
      request<void>(`/api/admin/listings/${id}/verify${qs({ verified })}`, { method: 'POST' }),

    blockUser: (id: string, blocked = true) =>
      request<void>(`/api/admin/users/${id}/block${qs({ blocked })}`, { method: 'POST' }),
  },
}
