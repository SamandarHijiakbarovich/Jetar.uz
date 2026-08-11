# Tekshiruv va topshiriq hujjati

Bu faylda **nima sinovdan o'tgan**, **nima o'tmagan** va o'tmaganini **qanday tekshirish** yozilgan.

**Yangilandi: 2026-08-09.** Docker demoni bu mashinada ishlamagani uchun PostgreSQL 16
to'g'ridan-to'g'ri o'rnatildi (`winget`, Windows xizmati sifatida). Shundan keyin
to'liq oqim haqiqiy bazada uchidan-uchiga sinovdan o'tkazildi — quyidagi jadvallarga qarang.

---

## 1. Nima sinalgan, nima yo'q

### ✅ Sinalgan

| Nima | Qanday sinalgan |
| --- | --- |
| Escrow biznes mantig'i | 23 ta unit test: holat o'tishlari, 8% komissiya, avto-release, nizo ssenariylari, reyting hisobi, chat huquqlari |
| To'liq HTTP oqimi | 10 ta integratsiya testi (`WebApplicationFactory`): ro'yxatdan o'tish → e'lon → escrow → to'lov → chat → tasdiqlash → baho, hamda 401/403/409 xatolari |
| Backend kompilyatsiyasi | `dotnet build` — 0 xato |
| Frontend kompilyatsiyasi | `tsc -b && vite build` — 0 xato |
| API ishga tushishi | `/health` va `/swagger` javob berdi, 40 ta endpoint ro'yxatda |
| Sahifalar tartibi | Bosh sahifa, e'lonlar, e'lon tafsiloti, kirish, ro'yxatdan o'tish — brauzerda 375px va 768px da |
| Mobil moslashuv | Gorizontal scroll yo'q, input'lar 16px (iOS zoom bo'lmaydi), tap-target ≥44px |

### ✅ Haqiqiy PostgreSQL 16 da sinalgan (2026-08-09)

| Nima | Natija |
| --- | --- |
| EF migratsiyasi | `InitialCreate` qo'llandi, 8 jadval yaratildi |
| `jsonb` ustunlari | `InGameItems`, `Images`, `Stats` — `information_schema` bo'yicha haqiqatan `jsonb` |
| DbSeeder | 8 foydalanuvchi, 11 e'lon, 6 bitim, 3 baho, 1 nizo yozildi |
| To'liq escrow oqimi | Kirish → e'lon → sotib olish → Click sandbox to'lovi → chat → ma'lumot yuborish → tasdiqlash → baho |
| Komissiya hisobi | 850 000 → komissiya 68 000, sotuvchiga 782 000 |
| Holat o'tishlari | `Initiated → AwaitingPayment → EscrowHeld → CredentialsSent → Completed` |
| Statistika yangilanishi | Sotuvchi sotuvlari 47→48, xaridor xaridlari 8→9, reyting qayta hisoblandi |
| E'lon holati | Bitim yakunlangach `Sold` ga o'tdi |
| **SignalR real-time chat** | Sotuvchi API orqali yozgan xabar xaridor brauzerida **sahifa yangilanmasdan** paydo bo'ldi |
| Avto-release taymeri | `AutoReleaseAt` to'ldirildi, UI da "2 kun 23 soat" ko'rindi |
| Admin panel | Statistika, bitimlar jadvali, nizolar ro'yxati — haqiqiy ma'lumot bilan |
| Nizoni hal qilish | Xaridor foydasiga → bitim `Refunded`, `ResolvedAt` va izoh saqlandi |

### ❌ Hali sinalmagan

| Nima | Nega muhim |
| --- | --- |
| **Docker image'lari** | `Dockerfile` lar va `docker-compose.yml` bir marta ham yig'ilmagan (Docker demoni ishlamadi) |
| **Redis ulanishi** | Kod bor, lekin ulanmagan (bo'sh bo'lsa xotira keshiga tushadi — bu yo'l ishlayapti) |
| **MinIO** | Compose'da bor, lekin **ilova uni umuman ishlatmaydi** — rasm local diskka saqlanadi |
| **Rasm yuklash** | `/api/listings/upload` hali chaqirilmagan |
| **E'lon joylash formasi** | `/create` sahifasi haqiqiy ma'lumot bilan to'ldirilmagan |
| **Haqiqiy telefon** | Faqat brauzer emulyatsiyasi; haqiqiy iOS/Android'da ko'rilmagan |
| **Click / Payme jonli rejimi** | Imzo tekshiruvi hujjat bo'yicha yozilgan, provayder sandbox'ida sinalmagan |
| **Uzum / Apelsin callback** | Faqat checkout URL quriladi; callback protokoli hali yo'q (shartnomadan keyin) |
| **Telegram bildirishnomalar** | Kod bor, bot tokeni bo'lmagani uchun sinalmagan |
| **HTTPS / Nginx / domen** | Umuman sozlanmagan |
| **Git** | Loyiha git repozitoriya emas, birorta commit yo'q |

---

## 2. Bazani ko'tarish — uch variant

Ilova qattiq bog'langan yagona narsa — **PostgreSQL**. Redis ixtiyoriy, MinIO ishlatilmayapti.

### Variant A — Docker'da faqat Postgres (tavsiya, tez)

Docker Desktop ochiq va yashil "Engine running" bo'lsin, keyin:

```bash
docker run -d --name jetar-postgres -e POSTGRES_DB=jetar -e POSTGRES_USER=jetar -e POSTGRES_PASSWORD=jetar -p 5432:5432 -v jetar-pgdata:/var/lib/postgresql/data postgres:15-alpine
```

Bu qiymatlar `appsettings.json` dagi ulanish satriga aynan mos — hech narsa sozlash kerak emas.

Keyingi safar shunchaki:

```bash
docker start jetar-postgres
```

### Variant B — PostgreSQL'ni o'rnatish (Docker umuman kerak emas)

1. https://www.postgresql.org/download/windows/ dan installerni yuklab o'rnating (versiya 15 yoki 16).
2. O'rnatishda `postgres` superuser paroli so'raladi — eslab qoling.
3. pgAdmin yoki Navicat orqali ulanib, `jetar` bazasi va `jetar` foydalanuvchisini yarating:

```sql
CREATE USER jetar WITH PASSWORD 'jetar';
CREATE DATABASE jetar OWNER jetar;
```

### Variant C — To'liq Docker Compose

```bash
cd /d/jetar && cp .env.example .env
```

`.env` da `JWT_SECRET` ni to'ldiring (kamida 32 belgi), keyin:

```bash
docker compose up -d --build
```

> Bu variant hech qachon sinalmagan. Birinchi urinishda xato chiqsa, 5-bo'limdagi jadvalga qarang.

---

## 3. Ishga tushirish va tekshirish

### 3.1 Backend

```bash
cd /d/jetar/backend && dotnet run --project src/Jetar.API
```

**Kutilgan natija** — konsolda ketma-ket:

```
Migratsiyalar qo'llanmoqda...
Demo ma'lumotlar yuklanmoqda...
Demo ma'lumotlar yuklandi: 8 foydalanuvchi, 11 e'lon, 6 bitim. Demo parol: jetar123
Now listening on: http://localhost:5080
```

**Tekshirish:**

```bash
curl http://localhost:5080/health
```

`{"status":"ok","service":"jetar-api"}` qaytishi kerak.

```bash
curl "http://localhost:5080/api/listings?pageSize=3"
```

`totalCount` 9 atrofida bo'lgan JSON qaytishi kerak (aktiv e'lonlar).

**Bazani ko'zdan kechirish** (Navicat / pgAdmin): `localhost:5432`, baza `jetar`,
foydalanuvchi `jetar`, parol `jetar`. Ichida 8 ta jadval bo'lishi kerak:
`users`, `listings`, `transactions`, `payments`, `messages`, `ratings`, `disputes`,
`__EFMigrationsHistory`.

### 3.2 Frontend

Yangi terminalda:

```bash
cd /d/jetar/frontend && npm run dev
```

http://localhost:5173 ni oching. Bosh sahifada e'lonlar kartochkalari va o'yinlar
ro'yxati **soni bilan** ko'rinishi kerak (bo'sh emas).

### 3.3 To'liq escrow oqimini qo'lda o'tish

Bu eng muhim tekshiruv — shu yerda hali ko'rilmagan sahifalar ochiladi.

| # | Amal | Kutilgan natija |
| --- | --- | --- |
| 1 | `alisher_uz` / `jetar123` bilan kiring | Yuqorida "@alisher_uz" chiqadi |
| 2 | E'lonlardan `valisher` ning eFootball akkauntini oching | Narx, sotuvchi, o'xshash e'lonlar |
| 3 | "Sotib olish" | `/checkout/...` ga o'tadi, escrow indikatori 1-bosqichda |
| 4 | Click'ni tanlab "To'lovni amalga oshirish" | Sandbox darhol tasdiqlaydi, bitim sahifasiga o'tadi, holat **Escrow** |
| 5 | Bazada tekshiring | `transactions.status = 'EscrowHeld'`, `payments.status = 'Paid'` |
| 6 | Chatga xabar yozing | Xabar darhol chiqadi (SignalR) |
| 7 | Boshqa brauzerda `valisher` bilan kiring, shu bitimni oching | Xabar real vaqtda ko'rinishi kerak — **SignalR shu yerda sinaladi** |
| 8 | `valisher` da "Ma'lumotlar yuborildi" | Holat "Ma'lumot yuborildi" ga o'tadi |
| 9 | `alisher_uz` da "✓ Tasdiqlayman" | Holat **Yakunlandi**, e'lon "Sotilgan" |
| 10 | Baho qoldiring | `valisher` reytingi qayta hisoblanadi |
| 11 | `jetar_admin` / `jetar123` bilan kiring, `/admin` | Statistika, bitimlar jadvali, 1 ta ochiq nizo |
| 12 | Nizoni "Xaridor foydasiga" hal qiling | Bitim `Refunded`, e'lon yana sotuvda |

### 3.4 Rasm yuklashni tekshirish

`/create` sahifasida e'lon yarating va skrinshot yuklang. Fayl
`backend/src/Jetar.API/wwwroot/uploads/YYYY/MM/` ichida paydo bo'lishi kerak,
e'lon sahifasida esa rasm ko'rinishi kerak.

---

## 4. Telefonda sinash (LAN orqali)

Mobil moslashuv faqat brauzer emulyatsiyasida tekshirilgan. Haqiqiy telefonda:

**1-qadam.** Kompyuter IP manzilini aniqlang:

```bash
ipconfig | findstr /i "IPv4"
```

Telefon va kompyuter **bir xil Wi-Fi'da** bo'lishi shart. (Bu mashinada `172.20.10.3`
ko'rindi — bu iPhone hotspot tarmog'iga o'xshaydi; hotspot ishlatsangiz o'sha to'g'ri keladi.)

**2-qadam.** `frontend/.env.local` fayl yarating (IP'ni o'zingiznikiga almashtiring):

```
VITE_API_URL=http://172.20.10.3:5080
```

**3-qadam.** Backend'ni tashqi ulanishlarga ochiq ishga tushiring:

```bash
cd /d/jetar/backend && dotnet run --project src/Jetar.API --urls http://0.0.0.0:5080
```

**4-qadam.** Frontend'ni ham tashqariga oching:

```bash
cd /d/jetar/frontend && npm run dev -- --host
```

**5-qadam.** CORS ruxsati. `backend/src/Jetar.API/appsettings.Development.json` ichiga
qo'shing (IP'ni almashtiring):

```json
"Cors": { "AllowedOrigins": [ "http://localhost:5173", "http://172.20.10.3:5173" ] }
```

**6-qadam.** Windows Firewall. PowerShell'ni **administrator** sifatida ochib:

```powershell
New-NetFirewallRule -DisplayName "Jetar dev" -Direction Inbound -Protocol TCP -LocalPort 5080,5173 -Action Allow
```

**7-qadam.** Telefon brauzerida `http://172.20.10.3:5173` ni oching.

Nimalarga e'tibor bering:
- Input'ga bosganda sahifa kattalashib ketmasligi (iOS)
- E'lon sahifasida pastdagi "Sotib olish" paneli barmoq ostida qulay turishi
- Pastki panel iPhone'ning uy chizig'i ostida qolib ketmasligi
- Filtr tugmasi va yig'iladigan panel ishlashi

---

## 5. Ehtimoliy muammolar

| Xato | Sabab | Yechim |
| --- | --- | --- |
| `docker daemon is not running` | Docker Desktop yopiq yoki hali ko'tarilmagan | Docker Desktop'ni oching, yashil "Engine running" ni kuting |
| `port is already allocated` (5432) | Boshqa postgres konteyneri ishlayapti | `docker ps` bilan toping va to'xtating, yoki portni `-p 5433:5432` ga o'zgartirib ulanish satrini moslang |
| `Npgsql...ConnectionRefused` | Postgres ko'tarilmagan | `docker ps` da `jetar-postgres` `Up` ekanini tekshiring |
| `password authentication failed` | Ulanish satri mos emas | `appsettings.json` dagi `ConnectionStrings:Postgres` ni tekshiring |
| API konteyneri qayta-qayta o'chadi | Migratsiya xatosi | `docker compose logs api` bilan haqiqiy xatoni ko'ring |
| `JWT_SECRET ... belgilanishi shart` | `.env` da bo'sh | Kamida 32 belgi kiriting |
| Frontend'da `ERR_CONNECTION_REFUSED` | Backend ishlamayapti | `curl http://localhost:5080/health` bilan tekshiring |
| Frontend'da CORS xatosi | Origin ro'yxatda yo'q | `Cors:AllowedOrigins` ga manzilni qo'shing |
| `npm ci` xatosi (Docker build) | `package-lock.json` mos emas | `cd frontend && npm install` qilib qayta yig'ing |
| MinIO healthcheck qizil | `mc` buyrug'i image'da yo'q bo'lishi mumkin | E'tiborsiz qoldiring — ilova MinIO'ni ishlatmaydi. Kerak bo'lmasa: `docker compose up -d postgres redis api web` |
| Sahifa oq/qora bo'sh | Dev serverda eskirgan HMR holati | `Ctrl+Shift+R` |

---

## 6. Tashqi hisob talab qiladigan ishlar

Bularni men umuman qila olmayman — sizning hisoblaringiz kerak.

| Ish | Kerak narsa |
| --- | --- |
| Click jonli to'lov | Click merchant hisobi, `service_id`, `merchant_id`, `SECRET_KEY`; kabinetda callback URL: `https://<domen>/api/payments/click` |
| Payme jonli to'lov | Payme merchant hisobi, `merchant_id`, `SECRET_KEY`; callback: `https://<domen>/api/payments/payme` |
| Uzum / Apelsin | Shartnoma + protokol hujjati (kod hali yozilmagan) |
| Telegram bot | @BotFather'dan token, `.env` da `TELEGRAM_ENABLED=true` |
| Domen va HTTPS | Domen sotib olish, DNS, Let's Encrypt sertifikati |
| Server | VPS (Ubuntu 22.04 tavsiya etilgan) |

---

## 7. Ishga tushirishdan oldin (production)

- [ ] `JWT_SECRET` ni yangi tasodifiy qiymatga almashtiring
- [ ] `POSTGRES_PASSWORD` va `MINIO_PASSWORD` ni o'zgartiring
- [ ] `PAYMENTS_SANDBOX=false` va provayder kalitlarini kiriting
- [ ] `Cors:AllowedOrigins` ni faqat haqiqiy domenga cheklang
- [ ] Nginx oldida HTTPS sozlang
- [ ] `Database:Seed` ni `false` qiling (demo ma'lumotlar production'ga tushmasin)
- [ ] Bazani zaxiralash jadvalini sozlang
- [ ] Git repozitoriya yarating va kodni saqlang
