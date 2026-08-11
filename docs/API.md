# Jetar API — qo'llanma

Baza manzil: `http://localhost:5080`
Interaktiv hujjat: `http://localhost:5080/swagger` (Development)

Barcha javoblar JSON. Enum'lar matn ko'rinishida (`"EscrowHeld"`, `"Click"`).

## Xatolar

Har qanday xato bir xil shaklda qaytadi:

```json
{
  "code": "conflict",
  "message": "Bu e'lon hozir sotuvda emas.",
  "traceId": "0HN7A9..."
}
```

| Status | `code` misollari | Ma'nosi |
| --- | --- | --- |
| 400 | `invalid_price`, `self_purchase`, `weak_password` | So'rov noto'g'ri |
| 401 | `unauthorized` | Token yo'q yoki muddati o'tgan |
| 403 | `forbidden` | Huquq yetarli emas |
| 404 | `not_found` | Obyekt topilmadi |
| 409 | `conflict` | Holat mos kelmaydi |
| 502 | `payment_init_failed` | To'lov provayderi javob bermadi |

## Autentifikatsiya

```http
POST /api/auth/register
Content-Type: application/json

{ "username": "alisher_uz", "phone": "+998901234567", "password": "jetar123" }
```

Javob:

```json
{
  "accessToken": "eyJhbGciOi...",
  "refreshToken": "eyJhbGciOi...",
  "expiresAt": "2026-08-08T12:00:00+00:00",
  "user": { "id": "...", "username": "alisher_uz", "rating": 0, "role": "User" }
}
```

Keyingi so'rovlarda:

```http
Authorization: Bearer <accessToken>
```

Access token muddati tugasa `POST /api/auth/refresh` ga `refreshToken` yuboriladi.
Frontend buni avtomatik bajaradi (`src/lib/api.ts`).

## E'lonlar

```http
GET /api/listings?game=efootball&minPrice=300000&maxPrice=1500000&sort=price_asc&page=1&pageSize=12
```

Filtrlar: `game`, `search`, `minPrice`, `maxPrice`, `region`, `verifiedOnly`,
`sellerId`. Saralash: `newest` (default), `popular`, `price_asc`, `price_desc`.
Boost qilingan e'lonlar har doim yuqorida.

```http
POST /api/listings
Authorization: Bearer <token>

{
  "gameType": "EFootball",
  "title": "eFootball 2025 — Legend akkaunt, 250+ o'yinchi",
  "description": "Legend darajali akkaunt, Konami ID orqali bog'langan...",
  "price": 850000,
  "serverRegion": "ASIA",
  "rankLevel": "Dream League",
  "inGameItems": ["GP", "Coins"],
  "images": ["/uploads/2026/08/abc.png"],
  "stats": { "GP": "1.2M", "Coins": "4 800" }
}
```

## Escrow bitimi

```http
POST /api/transactions/initiate
{ "listingId": "..." }
```

Javobda `escrowCode`, `amount`, `commissionAmount`, `sellerPayout` qaytadi.
E'lon `Reserved` holatiga o'tadi.

Keyin to'lov:

```http
POST /api/payments
{ "transactionId": "...", "method": "Click" }
```

Javobdagi `checkoutUrl` ga o'tiladi. Sandbox rejimida:

```http
POST /api/payments/sandbox/confirm
{ "providerPaymentId": "click_9f2a...", "success": true }
```

To'lov tasdiqlangach bitim avtomatik `EscrowHeld` holatiga o'tadi va
`autoReleaseAt` belgilanadi.

Sotuvchi ma'lumot yuborgach:

```http
POST /api/transactions/{id}/credentials-sent
```

Xaridor tasdiqlagach:

```http
POST /api/transactions/{id}/release
```

## Nizolar

```http
POST /api/transactions/{id}/dispute
{ "reason": "Akkaunt ma'lumotlari noto'g'ri — login ishlamayapti.", "evidenceUrls": [] }
```

Moderator qarori:

```http
POST /api/admin/disputes/{id}/resolve
{ "favourBuyer": true, "note": "Dalillar xaridor foydasiga." }
```

`favourBuyer: true` → pul qaytariladi, e'lon yana sotuvga chiqadi.
`favourBuyer: false` → pul sotuvchiga chiqariladi, komissiya ushlanadi.

## Chat (SignalR)

```javascript
const conn = new signalR.HubConnectionBuilder()
  .withUrl('http://localhost:5080/hubs/chat', {
    accessTokenFactory: () => token,
  })
  .withAutomaticReconnect()
  .build()

conn.on('ReceiveMessage', (message) => { /* ... */ })

await conn.start()
await conn.invoke('JoinTransaction', transactionId)
await conn.invoke('SendMessage', transactionId, 'Akkaunt logini: ...')
```

SignalR ulanmasa REST orqali ishlaydi: `GET /api/chat/{transactionId}`,
`POST /api/chat/send`.

Escrow holati o'zgarganda tizim avtomatik xabar qo'shadi (`isSystem: true`).

## To'lov callback'lari

Provayderlar quyidagi manzillarga POST yuboradi:

| Provayder | Endpoint | Tekshiruv |
| --- | --- | --- |
| Click | `/api/payments/click` | `md5` imzo (`sign_string`) |
| Payme | `/api/payments/payme` | `Basic base64("Paycom:SECRET")` |
| Uzum | `/api/payments/uzum` | shartnomadan keyin |
| Apelsin | `/api/payments/apelsin` | shartnomadan keyin |

Javob: `{ "error": 0, "error_note": "OK" }` yoki `error: -1`.
