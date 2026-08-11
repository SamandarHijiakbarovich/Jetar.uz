# Jetar — o'yin akkauntlari uchun Escrow savdo platformasi

> Tez va ishonchli — Jetar! eFootball, PUBG Mobile, Free Fire va boshqa o'yinlar
> akkauntlarining Escrow kafolati bilan xavfsiz savdosi.

MVP to'liq ishlaydi: ro'yxatdan o'tish → e'lon joylash → escrow bitimi → to'lov →
chat → tasdiqlash → reyting → nizolarni moderatsiya qilish.

---

## Texnologiyalar

| Qatlam | Texnologiya | Vazifa |
| --- | --- | --- |
| Backend | C# .NET 9 + ASP.NET Core | API, Escrow, to'lovlar |
| Frontend | React 19 + TypeScript + Tailwind + Vite (PWA) | Veb-ilova interfeysi |
| Ma'lumotlar bazasi | PostgreSQL 15 + EF Core 9 | Asosiy ma'lumotlar, jsonb |
| Kesh | Redis 7 | Sessiya, tezkor xotira |
| Fayl saqlash | Local disk / MinIO (S3-mos) | Skrinshotlar |
| Real-time | SignalR | Bitim ichidagi chat |
| Fon vazifalar | `BackgroundService` | Avto-release, muddat nazorati |
| To'lov | Click, Payme, Uzum, Apelsin | Mahalliy to'lov tizimlari |
| Deploy | Docker Compose + Nginx | Ishga tushirish |

> Arxitektura hujjatida `Hangfire` ko'rsatilgan; MVP'da uning o'rnini ichki
> `EscrowBackgroundWorker` bajaradi — bir xil vazifa, kamroq bog'liqlik.
> Yuklama oshganda Hangfire'ga o'tish uchun `IEscrowService` interfeysi tayyor.

---

## Tez ishga tushirish

### Variant 1 — Docker Compose (tavsiya etiladi)

```bash
cp .env.example .env
```

`.env` faylida **`JWT_SECRET`** ni to'ldiring (kamida 32 belgi):

```bash
openssl rand -base64 48
```

Keyin:

```bash
docker compose up -d --build
```

| Xizmat | Manzil |
| --- | --- |
| Veb-ilova | http://localhost:5173 |
| API | http://localhost:5080 |
| Swagger | http://localhost:5080/swagger |
| Health | http://localhost:5080/health |
| MinIO konsoli | http://localhost:9001 |

### Variant 2 — mahalliy ishlab chiqish

**1. Baza va kesh:**

```bash
docker compose up -d postgres redis
```

**2. Backend:**

```bash
cd backend && dotnet run --project src/Jetar.API
```

API `http://localhost:5080` da ko'tariladi, migratsiyalar avtomatik qo'llanadi va
demo ma'lumotlar yuklanadi.

**3. Frontend:**

```bash
cd frontend && npm install && npm run dev
```

---

## Demo hisoblar

Baza birinchi marta ko'tarilganda quyidagi hisoblar yaratiladi.
Barchasining paroli — **`jetar123`**.

| Login | Rol | Izoh |
| --- | --- | --- |
| `alisher_uz` | Foydalanuvchi | 12 sotuv, 8 xarid, reyting 4.8 |
| `valisher` | Sotuvchi | 47 bitim, reyting 4.9 |
| `sardor_pubg` | Sotuvchi | PUBG akkauntlari |
| `jetar_mod` | Moderator | Admin panelga kirish, nizolar |
| `jetar_admin` | Admin | To'liq huquqlar |

---

## Escrow bitimining oqimi

Arxitektura hujjatidagi 01–11 qadamlar `EscrowService` ichida amalga oshirilgan.
Bitim holatini faqat shu servis o'zgartiradi.

```
01  Xaridor "Sotib olish" bosadi          GET  /api/listings/{id}
02  Bitim yaratiladi, escrow kodi         POST /api/transactions/initiate
03  Xaridor pulni o'tkazadi               POST /api/payments
04  Pul BLOKLANADI (EscrowHeld)           EscrowService.HoldAsync()
05  Sotuvchi ma'lumot yuboradi            POST /api/chat/send  (SignalR)
                                          POST /api/transactions/{id}/credentials-sent
06  Xaridor akkauntni tekshiradi          —
07  Xaridor "Tasdiqlayman" bosadi         POST /api/transactions/{id}/release
08  Pul chiqariladi, komissiya ushlanadi  EscrowService.ReleaseAsync()

Nizo bo'lsa:
09  Nizo ochiladi, pul muzlatiladi        POST /api/transactions/{id}/dispute
10  Moderator dalillarni tekshiradi       GET  /api/admin/disputes
11  Qaror: qaytarish yoki chiqarish       POST /api/admin/disputes/{id}/resolve
```

**Xavfsizlik kafolatlari:**

- Xaridor javob bermasa, pul `AUTO_RELEASE_HOURS` (default 72 soat) dan keyin
  avtomatik sotuvchiga o'tadi — sotuvchi cheksiz kutib qolmaydi.
- Nizo ochilganda avto-release **to'xtaydi**, pul moderator qaroriga qadar muzlaydi.
- Pul escrowda turganida bitim oddiy "bekor qilish" bilan yopilmaydi — faqat nizo
  yoki moderator qarori orqali.
- To'lanmagan bitim `UnpaidTransactionTimeoutMinutes` (default 60 daqiqa) dan keyin
  bekor qilinadi va e'lon yana sotuvga qaytadi.

---

## Loyiha strukturasi

Backend **Clean Architecture** (4 qatlam) asosida qurilgan. Bog'liqliklar faqat
ichkariga yo'naladi: `API → Infrastructure → Application → Domain`. Domen qatlami
hech kimga bog'liq emas.

```
jetar/
├── backend/
│   ├── src/
│   │   ├── Jetar.Domain/            Yadro — hech qanday tashqi bog'liqlik yo'q
│   │   │   ├── Entities/            User, Listing, Transaction, Payment, ...
│   │   │   ├── Enums/               GameType, TransactionStatus, ...
│   │   │   ├── Abstractions/        IRepository<T>, IUnitOfWork, ISpecification<T>
│   │   │   ├── Specifications/      So'rov spetsifikatsiyalari (Ardalis uslubi)
│   │   │   └── Common/              AppException, PagedResult, GameCatalog
│   │   ├── Jetar.Application/       Biznes-mantiq (faqat Domain'ga bog'liq)
│   │   │   ├── Services/            Escrow, Payment, Auth, Listing, Chat,
│   │   │   │                        Rating, Admin, User — repository orqali
│   │   │   ├── Contracts/           So'rov va javob DTO'lari
│   │   │   ├── Interfaces/          IEscrowService, IPaymentGateway,
│   │   │   │                        IPasswordHasher, ...
│   │   │   └── Options/             Konfiguratsiya modellari
│   │   ├── Jetar.Infrastructure/    Texnik detallar (Application + Domain)
│   │   │   ├── Data/                AppDbContext, konfiguratsiya, migratsiya,
│   │   │   │   └── Repositories/    EfRepository<T>, UnitOfWork, evaluator
│   │   │   ├── Services/            Token, FileStorage, Notification,
│   │   │   │                        BCryptPasswordHasher, fon vazifasi
│   │   │   └── External/            Click, Payme, Uzum, Apelsin gateway'lari
│   │   └── Jetar.API/
│   │       ├── Controllers/         Faqat servislarga bog'liq (DbContext'siz)
│   │       ├── Hubs/                ChatHub (SignalR)
│   │       ├── Middlewares/         Xatolarni bir shaklga keltirish
│   │       └── Validators/          FluentValidation
│   └── tests/Jetar.Tests/           Unit + integratsiya testlari
├── frontend/
│   └── src/
│       ├── pages/                   Sahifalar
│       ├── components/              Umumiy komponentlar
│       ├── context/                 Auth va Toast
│       └── lib/                     API klient, turlar, formatlash
├── docs/                            Qo'shimcha hujjatlar
└── docker-compose.yml
```

**Qo'llanilgan pattern'lar:** Clean Architecture (4 qatlam) · Repository ·
Unit of Work · Specification · Options · Dependency Injection.

---

## API

To'liq hujjat: `http://localhost:5080/swagger` (Development muhitida).

### Asosiy endpointlar

| Metod | Yo'l | Tavsif |
| --- | --- | --- |
| POST | `/api/auth/register` | Ro'yxatdan o'tish |
| POST | `/api/auth/login` | Kirish |
| POST | `/api/auth/refresh` | Tokenni yangilash |
| GET | `/api/listings` | E'lonlar — filtr, qidiruv, saralash |
| GET | `/api/listings/{id}` | E'lon tafsiloti |
| POST | `/api/listings` | E'lon joylash |
| POST | `/api/listings/upload` | Skrinshot yuklash |
| POST | `/api/transactions/initiate` | Escrow ochish |
| POST | `/api/transactions/{id}/release` | Pulni chiqarish |
| POST | `/api/transactions/{id}/dispute` | Nizo ochish |
| POST | `/api/payments` | To'lovni boshlash |
| POST | `/api/payments/click` | Click callback |
| POST | `/api/payments/payme` | Payme callback |
| GET | `/api/chat/{transactionId}` | Yozishma |
| GET | `/api/admin/stats` | Admin ko'rsatkichlari |
| POST | `/api/admin/disputes/{id}/resolve` | Nizo bo'yicha qaror |

SignalR hub: `/hubs/chat` (token `?access_token=` orqali uzatiladi).

---

## To'lovlar

MVP **sandbox rejimida** ishlaydi (`PAYMENTS_SANDBOX=true`): haqiqiy pul harakati
bo'lmaydi, to'lov `/api/payments/sandbox/confirm` orqali tasdiqlanadi.

Jonli rejimga o'tish:

1. `.env` da `PAYMENTS_SANDBOX=false`.
2. `CLICK_MERCHANT_ID`, `CLICK_SECRET_KEY`, `PAYME_MERCHANT_ID`, `PAYME_SECRET_KEY`
   qiymatlarini to'ldiring.
3. Provayder kabinetida callback manzillarini ko'rsating:
   - Click → `https://<domen>/api/payments/click`
   - Payme → `https://<domen>/api/payments/payme`

Click imzosi (`md5`) va Payme Basic-avtorizatsiyasi allaqachon tekshiriladi.
Uzum va Apelsin uchun checkout URL quriladi; shartnoma tuzilgach ularning
callback protokoli `PaymentGatewayBase.HandleCallbackAsync` da to'ldiriladi.

---

## Testlar

```bash
cd backend && dotnet test
```

Qamrov:

- **Unit** — escrow holat o'tishlari, komissiya hisobi, avto-release, nizo
  ssenariylari, reyting, chat huquqlari, telefon normalizatsiyasi.
- **Integratsiya** — `WebApplicationFactory` orqali haqiqiy HTTP quvuri:
  ro'yxatdan o'tish → e'lon → escrow → to'lov → chat → tasdiqlash → baho,
  hamda 401/403/409 xatolari.

---

## Konfiguratsiya

Barcha sozlamalar `.env` (Docker) yoki `appsettings.json` orqali beriladi.
Muhit o'zgaruvchilari `JETAR_` prefiksi bilan yoziladi, ichma-ich kalitlar
ikki pastki chiziq bilan ajratiladi:

```
JETAR_Platform__CommissionRate=0.08
JETAR_ConnectionStrings__Postgres=Host=...
```

| Kalit | Default | Tavsif |
| --- | --- | --- |
| `Platform:CommissionRate` | `0.08` | Escrow komissiyasi |
| `Platform:AutoReleaseHours` | `72` | Avto-release muddati |
| `Platform:UnpaidTransactionTimeoutMinutes` | `60` | To'lov kutish muddati |
| `Platform:MinListingPrice` | `10000` | Eng past narx |
| `Platform:AutoApproveListings` | `true` | E'lon moderatorsiz efirga chiqadi |
| `Payments:SandboxMode` | `true` | Sinov rejimi |
| `Jwt:AccessTokenMinutes` | `120` | Access token muddati |

---

## Xavfsizlik

- JWT (Bearer) autentifikatsiya, refresh token bilan
- `bcrypt` parol hashlash
- Rollar: `User`, `Moderator`, `Admin`
- EF Core parametrlangan so'rovlar — SQL injection himoyasi
- CORS oq ro'yxati (`Cors:AllowedOrigins`)
- Fayl yuklashda kengaytma va hajm tekshiruvi, papkadan chiqib ketish himoyasi
- Escrow — pul va aktiv himoyasining asosiy mexanizmi

**Ishlab chiqarishga chiqishdan oldin:**

- [ ] `JWT_SECRET` ni yangi tasodifiy qiymatga almashtiring
- [ ] `POSTGRES_PASSWORD` va `MINIO_PASSWORD` ni o'zgartiring
- [ ] `PAYMENTS_SANDBOX=false` va provayder kalitlarini kiriting
- [ ] Nginx oldida HTTPS (Let's Encrypt) sozlang
- [ ] `Cors:AllowedOrigins` ni haqiqiy domenga cheklang

---

## Keyingi bosqichlar

MVP doirasidan tashqarida qoldirilgan, lekin asosi tayyor:

- Telegram bot (`NotificationService` interfeysi tayyor, token berilsa ishlaydi)
- MinIO/S3 saqlash (`IFileStorage` interfeysi tayyor, hozir local disk)
- Hangfire (hozir `BackgroundService`)
- E'lonlarni pullik ko'tarish (`Listing.BoostedUntil` maydoni bor)
- Premium obuna va tekshiruv (verification) tariflari
- Mobil ilova
