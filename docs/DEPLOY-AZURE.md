# Jetar — Azure'ga joylash (Docker Compose)

Bu qo'llanma **Azure VM (Ubuntu) + Docker Compose** yo'lini tavsiflaydi — botni serverga
qo'ygandek, hammasi bitta serverda (Postgres + Redis + MinIO + API + Web).

> Yangi model: platforma **to'lovga aralashmaydi**. Escrow o'chirilgan
> (`ESCROW_ENABLED=false`). Daromad — pullik ko'tarish (TOP).

---

## 1. Azure VM yaratish

- **Azure Portal → Virtual machines → Create**
- Image: **Ubuntu Server 22.04 LTS**, o'lcham: kamida **2 vCPU / 4 GB** (B2s)
- **Networking → Inbound ports**: `22` (SSH), `80`, `443` ni oching
- VM tayyor bo'lgach SSH orqali kiring:
  ```bash
  ssh azureuser@<VM_PUBLIC_IP>
  ```

## 2. Docker o'rnatish

```bash
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker $USER
newgrp docker
```

## 3. Loyihani olib kelish

```bash
git clone https://github.com/SamandarHijiakbarovich/Jetar.uz.git
cd Jetar.uz
```

## 4. `.env` faylini tayyorlash

```bash
cp .env.example .env
nano .env
```

Quyidagilarni **albatta** to'ldiring:

| O'zgaruvchi | Nima |
|---|---|
| `POSTGRES_PASSWORD` | Kuchli DB paroli |
| `JWT_SECRET` | `openssl rand -base64 48` natijasini qo'ying |
| `ADMIN_PASSWORD` | Admin (Samandar) uchun parol — **majburiy** |
| `FRONTEND_URL` | `https://sizning-domeningiz.uz` |
| `PUBLIC_API_URL` | `https://sizning-domeningiz.uz/api` yoki API domeni |
| `BOOST_CARD_NUMBER` | To'lov qabul qilinadigan karta |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `DB_SEED` | `false` (demo ma'lumot yuklanmasin) |

`JWT_SECRET` yaratish:
```bash
openssl rand -base64 48
```

## 5. Ishga tushirish

```bash
docker compose up -d --build
```

Holatni ko'rish:
```bash
docker compose ps
docker compose logs -f api
```

API ko'tarilganda log'da ko'rinadi:
`Admin bootstrap: @samandar (+998937650083) yaratildi.`

## 6. Domen + HTTPS

Eng oson yo'l — oldiga **Caddy** yoki **Nginx + Certbot** qo'yish. Caddy misoli
(`Caddyfile`):

```
sizning-domeningiz.uz {
    reverse_proxy /api/* jetar-api:8080
    reverse_proxy /* jetar-web:80
}
```

Caddy avtomatik Let's Encrypt sertifikat oladi. (Alternativa: Azure Front Door / Application Gateway.)

## 7. Admin panelga kirish

- Manzil: `https://sizning-domeningiz.uz/admin`
- Login: **telefon** `+998937650083` (yoki username `samandar`)
- Parol: `.env` dagi `ADMIN_PASSWORD`
- Kirgach **Profil → Sozlamalar → Parolni o'zgartirish** dan parolni yangilang.

---

## Yangilash (keyingi deploylar)

```bash
cd Jetar.uz
git pull
docker compose up -d --build
```

## Backup (tavsiya etiladi)

```bash
# Bazani zaxiralash
docker exec jetar-postgres pg_dump -U jetar jetar > backup_$(date +%F).sql
```

## Eslatmalar

- Yuklangan rasmlar `uploads` Docker volume'ida saqlanadi (VM diskida turadi) — o'chib ketmaydi.
- `PAYMENTS_*`, `CLICK_*`, `PAYME_*` o'zgaruvchilari endi ishlatilmaydi (escrow o'chirilgan) — bo'sh qoldiring.
- Boshqa yo'l (kengayadigan): Azure Container Apps + Azure Database for PostgreSQL + Azure Blob Storage.
