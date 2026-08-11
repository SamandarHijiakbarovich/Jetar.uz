using Jetar.Domain.Entities;
using Jetar.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jetar.Infrastructure.Data;

/// <summary>
/// Demo ma'lumotlar — dizayn maketidagi e'lonlar, sotuvchilar va bitimlar.
/// Faqat baza bo'sh bo'lgandagina ishlaydi.
/// </summary>
public static class DbSeeder
{
    public const string DemoPassword = "jetar123";

    public static async Task SeedAsync(AppDbContext db, ILogger logger, CancellationToken ct = default)
    {
        if (await db.Users.AnyAsync(ct))
        {
            logger.LogInformation("Baza allaqachon to'ldirilgan — seed o'tkazib yuborildi.");
            return;
        }

        logger.LogInformation("Demo ma'lumotlar yuklanmoqda...");

        var hash = BCrypt.Net.BCrypt.HashPassword(DemoPassword);
        var now = DateTimeOffset.UtcNow;

        User NewUser(string username, string firstName, string lastName, string phone, string city,
            decimal rating, int ratingCount, int sales, int purchases, bool verified, UserRole role,
            int months, int responseMin) => new()
        {
            Username = username,
            FirstName = firstName,
            LastName = lastName,
            Phone = phone,
            PasswordHash = hash,
            TelegramUsername = "@" + username,
            City = city,
            Rating = rating,
            RatingCount = ratingCount,
            TotalSales = sales,
            TotalPurchases = purchases,
            IsVerified = verified,
            Role = role,
            AvgResponseMinutes = responseMin,
            CreatedAt = now.AddMonths(-months)
        };

        var admin = NewUser("jetar_admin", "Jasur", "Rahimov", "+998900000000", "Toshkent", 5.0m, 0, 0, 0, true, UserRole.Admin, 24, 5);
        var valisher = NewUser("valisher", "Valisher", "To'xtayev", "+998901112233", "Toshkent", 4.9m, 47, 47, 3, true, UserRole.User, 20, 8);
        var alisher = NewUser("alisher_uz", "Alisher", "Karimov", "+998901234567", "Toshkent", 4.8m, 20, 12, 8, true, UserRole.User, 19, 12);
        var sardor = NewUser("sardor_pubg", "Sardor", "Ergashev", "+998903334455", "Samarqand", 4.9m, 31, 24, 5, true, UserRole.User, 14, 10);
        var madina = NewUser("madina_ff", "Madina", "Yusupova", "+998905556677", "Farg'ona", 4.6m, 9, 4, 6, false, UserRole.User, 9, 25);
        var gamerTash = NewUser("gamer_tash", "Bekzod", "Nazarov", "+998907778899", "Toshkent", 4.7m, 18, 15, 2, true, UserRole.User, 12, 14);
        var skinMarket = NewUser("skin_market", "Otabek", "Sultonov", "+998909990011", "Toshkent", 4.8m, 26, 22, 1, true, UserRole.User, 16, 6);
        var moderator = NewUser("jetar_mod", "Nodira", "Islomova", "+998900000001", "Toshkent", 5.0m, 0, 0, 0, true, UserRole.Moderator, 18, 5);

        var users = new[] { admin, moderator, valisher, alisher, sardor, madina, gamerTash, skinMarket };
        db.Users.AddRange(users);
        await db.SaveChangesAsync(ct);

        Listing NewListing(User seller, GameType game, string title, string description, decimal price,
            string region, string rank, string[] items, Dictionary<string, string> stats,
            bool verified, int daysAgo, ListingStatus status = ListingStatus.Active,
            ListingType type = ListingType.Account) => new()
        {
            SellerId = seller.Id,
            GameType = game,
            Type = type,
            Title = title,
            Description = description,
            Price = price,
            ServerRegion = region,
            RankLevel = rank,
            InGameItems = items.ToList(),
            Images = new List<string>(),
            Stats = stats,
            Status = status,
            IsVerified = verified,
            ViewCount = Random.Shared.Next(40, 900),
            CreatedAt = now.AddDays(-daysAgo)
        };

        var listings = new List<Listing>
        {
            NewListing(valisher, GameType.EFootball,
                "eFootball 2025 — Legend akkaunt, 250+ o'yinchi",
                "2019-yildan beri o'ynalgan akkaunt. Legend darajali 250+ o'yinchi, shu jumladan Messi (102 daraja), " +
                "Ronaldo (101 daraja), Mbappe (100 daraja). Akkaunt Konami ID orqali bog'langan, pochta to'liq beriladi. " +
                "Hech qanday ban yoki ogohlantirish yo'q.",
                850_000, "ASIA", "Dream League",
                new[] { "GP", "Coins", "Skin" },
                new() { ["GP"] = "1.2M", ["Coins"] = "4 800", ["Skinlar"] = "36", ["Daraja"] = "92" },
                true, 3),

            NewListing(sardor, GameType.PubgMobile,
                "PUBG Mobile — Conqueror, 40+ mifik skin",
                "Conqueror darajasiga 4 mavsum ketma-ket chiqqan akkaunt. 40 dan ortiq mifik skin, " +
                "M416 Glacier 7-daraja, AKM Ghillie. Facebook va Twitter bog'lanishi olib tashlangan, " +
                "faqat gost akkaunt orqali beriladi.",
                1_200_000, "ASIA", "Conqueror",
                new[] { "UC", "Skin" },
                new() { ["UC"] = "2 400", ["Skinlar"] = "41", ["Daraja"] = "78", ["Mavsum"] = "S12" },
                true, 5),

            NewListing(madina, GameType.FreeFire,
                "Free Fire — Grandmaster, 60+ bundle",
                "Grandmaster darajasi, 60 dan ortiq to'liq bundle, elit pass barcha mavsumlar. " +
                "Akkaunt Google orqali bog'langan, pochta ma'lumotlari to'liq topshiriladi. " +
                "3 yillik faol o'yin tarixi.",
                450_000, "CIS", "Grandmaster",
                new[] { "Diamond", "Skin" },
                new() { ["Diamond"] = "1 100", ["Bundle"] = "62", ["Daraja"] = "65" },
                false, 7),

            NewListing(skinMarket, GameType.CsGo,
                "CS:GO Prime — Legendary Eagle, inventar $400",
                "Prime status, Legendary Eagle Master. Inventar qiymati taxminan 400 dollar: " +
                "AWP Asiimov, AK-47 Redline, nozlar. Steam Guard 15 kundan ortiq faol, " +
                "trade ban yo'q. Pochta va telefon to'liq beriladi.",
                2_100_000, "EU", "LEM",
                new[] { "Skin" },
                new() { ["Inventar"] = "$400", ["Soat"] = "1 850", ["Prime"] = "Ha" },
                true, 9),

            NewListing(gamerTash, GameType.Dota2,
                "Dota 2 — Immortal, 6.2k MMR, Arcana x4",
                "Immortal daraja, 6200 MMR. To'rtta Arcana: Juggernaut, Pudge, Phantom Assassin, Legion Commander. " +
                "Behavior score 10000, kalibrovka to'liq o'tgan. Steam akkaunt to'liq topshiriladi.",
                1_750_000, "EU", "Immortal",
                new[] { "Skin" },
                new() { ["MMR"] = "6 200", ["Arcana"] = "4", ["Behavior"] = "10 000" },
                true, 12),

            NewListing(valisher, GameType.Valorant,
                "Valorant — Ascendant 3, 30+ skin",
                "Ascendant 3 daraja, 30 dan ortiq qurol skini, shu jumladan Reaver va Prime to'plamlari. " +
                "Riot akkaunt pochtasi to'liq beriladi, hech qanday ogohlantirish yo'q.",
                990_000, "EU", "Ascendant",
                new[] { "Skin" },
                new() { ["Daraja"] = "112", ["Skinlar"] = "31", ["Agent"] = "18" },
                true, 2),

            NewListing(valisher, GameType.EFootball,
                "eFootball — FIFA Champion, kuchli hujum liniyasi",
                "FIFA Champion darajasi, kuchli hujum liniyasi va yaxshi himoya. " +
                "Konami ID bog'langan, pochta to'liq beriladi.",
                620_000, "ASIA", "FIFA Champion",
                new[] { "GP", "Coins" },
                new() { ["GP"] = "740k", ["Coins"] = "1 950", ["Daraja"] = "71" },
                false, 4),

            NewListing(alisher, GameType.EFootball,
                "eFootball — 180+ o'yinchi, arzon narx",
                "Kollektsiyada 180 dan ortiq o'yinchi. Boshlovchilar uchun juda mos akkaunt. " +
                "Pochta to'liq topshiriladi, hech qanday cheklov yo'q.",
                410_000, "ASIA", "Superstar",
                new[] { "GP" },
                new() { ["GP"] = "320k", ["Daraja"] = "58" },
                false, 6),

            NewListing(sardor, GameType.PubgMobile,
                "PUBG Mobile — Ace Master, 20+ skin",
                "Ace Master darajasi, 20 dan ortiq qurol skini va 3 ta to'liq kostyum. " +
                "Akkaunt gost orqali beriladi, barcha bog'lanishlar olib tashlangan.",
                640_000, "ASIA", "Ace Master",
                new[] { "UC", "Skin" },
                new() { ["UC"] = "820", ["Skinlar"] = "23", ["Daraja"] = "61" },
                true, 21, ListingStatus.Sold),

            NewListing(madina, GameType.FreeFire,
                "Free Fire — Heroic, 24 bundle to'plami",
                "Heroic darajasi, 24 ta bundle. Elit pass 5 mavsum. " +
                "Google akkaunt bog'langan, ma'lumotlar to'liq beriladi.",
                380_000, "CIS", "Heroic",
                new[] { "Diamond", "Skin" },
                new() { ["Diamond"] = "540", ["Bundle"] = "24", ["Daraja"] = "52" },
                false, 34, ListingStatus.Sold),

            NewListing(gamerTash, GameType.CodMobile,
                "COD Mobile — Legendary, mifik qurollar",
                "Legendary daraja, uchta mifik qurol va 40 dan ortiq skin. " +
                "Akkaunt Activision ID orqali bog'langan, to'liq topshiriladi.",
                780_000, "ASIA", "Legendary",
                new[] { "Skin", "Coins" },
                new() { ["Daraja"] = "150", ["Mifik"] = "3", ["Skinlar"] = "44" },
                false, 1),

            // ── O'yin valyutasi ────────────────────────────────────────────
            NewListing(sardor, GameType.PubgMobile,
                "PUBG Mobile — 660 UC, ID orqali to'ldirish",
                "660 UC to'g'ridan-to'g'ri o'yin ID raqamingizga to'ldiriladi. " +
                "Akkaunt ma'lumotlari kerak emas, faqat ID. To'ldirish 5–15 daqiqa ichida.",
                95_000, "ASIA", "",
                new[] { "UC" },
                new() { ["Miqdor"] = "660 UC", ["Yetkazish"] = "5–15 daqiqa" },
                true, 1, ListingStatus.Active, ListingType.Currency),

            NewListing(madina, GameType.FreeFire,
                "Free Fire — 1080 Diamond, tezkor to'ldirish",
                "1080 olmos o'yin ID orqali to'ldiriladi. Akkaunt parolini berish shart emas. " +
                "Kuniga 100 dan ortiq buyurtma bajaraman.",
                78_000, "CIS", "",
                new[] { "Diamond" },
                new() { ["Miqdor"] = "1080 💎", ["Yetkazish"] = "10 daqiqa" },
                true, 2, ListingStatus.Active, ListingType.Currency),

            NewListing(valisher, GameType.EFootball,
                "eFootball — 1 000 000 GP paketi",
                "Bir million GP akkauntingizga o'tkaziladi. Konami ID kerak. " +
                "Xavfsiz usul, ban xavfi yo'q, 2 yildan beri shu xizmatni ko'rsataman.",
                120_000, "ASIA", "",
                new[] { "GP" },
                new() { ["Miqdor"] = "1M GP", ["Yetkazish"] = "1 soat" },
                false, 3, ListingStatus.Active, ListingType.Currency),

            // ── Skin va buyumlar ───────────────────────────────────────────
            NewListing(skinMarket, GameType.CsGo,
                "CS:GO — AWP Dragon Lore, Field-Tested",
                "AWP | Dragon Lore, Field-Tested holatida, float 0.24. " +
                "Steam trade orqali topshiriladi, trade ban yo'q.",
                14_500_000, "EU", "",
                new[] { "Skin" },
                new() { ["Float"] = "0.24", ["Holat"] = "Field-Tested" },
                true, 4, ListingStatus.Active, ListingType.Item),

            NewListing(sardor, GameType.PubgMobile,
                "PUBG Mobile — M416 Glacier 7-daraja",
                "M416 Glacier to'liq 7-darajagacha yangilangan. " +
                "Akkaunt bilan birga yoki alohida gift orqali beriladi.",
                890_000, "ASIA", "",
                new[] { "Skin" },
                new() { ["Daraja"] = "7", ["Qurol"] = "M416" },
                true, 5, ListingStatus.Active, ListingType.Item),

            NewListing(gamerTash, GameType.Dota2,
                "Dota 2 — Arcana to'plami (4 ta)",
                "Juggernaut, Pudge, Phantom Assassin va Legion Commander arcana'lari. " +
                "Gift orqali topshiriladi, 30 kunlik do'stlik shart.",
                2_400_000, "EU", "",
                new[] { "Skin" },
                new() { ["Soni"] = "4", ["Turi"] = "Arcana" },
                false, 6, ListingStatus.Active, ListingType.Item),

            // ── Xizmatlar ──────────────────────────────────────────────────
            NewListing(sardor, GameType.PubgMobile,
                "PUBG Mobile — Conqueror darajasiga ko'tarish",
                "Akkauntingizni joriy darajadan Conqueror'ga ko'taraman. " +
                "Muddat 5–10 kun, mavsum tugashiga qarab. VPN ishlatmayman, ban xavfi minimal.",
                1_500_000, "ASIA", "Conqueror",
                Array.Empty<string>(),
                new() { ["Muddat"] = "5–10 kun", ["Kafolat"] = "Bor" },
                true, 2, ListingStatus.Active, ListingType.Service),

            NewListing(gamerTash, GameType.Valorant,
                "Valorant — Immortal darajasiga boosting",
                "Ascendant'dan Immortal'ga ko'tarish. Duo yoki solo rejim tanlanadi. " +
                "Kunlik hisobot beraman, jarayonni kuzatib borasiz.",
                980_000, "EU", "Immortal",
                Array.Empty<string>(),
                new() { ["Muddat"] = "7–14 kun", ["Rejim"] = "Solo / Duo" },
                false, 7, ListingStatus.Active, ListingType.Service)
        };

        db.Listings.AddRange(listings);
        await db.SaveChangesAsync(ct);

        // ── Bitim tarixi: admin panelidagi jadval uchun ─────────────────────
        var efootballLegend = listings[0];
        var pubgSold = listings[8];
        var ffSold = listings[9];

        Transaction NewTx(Listing listing, User buyer, TransactionStatus status, int daysAgo, decimal rate = 0.08m)
        {
            var commission = Math.Round(listing.Price * rate, 0, MidpointRounding.AwayFromZero);
            var created = now.AddDays(-daysAgo);

            return new Transaction
            {
                ListingId = listing.Id,
                BuyerId = buyer.Id,
                SellerId = listing.SellerId,
                Amount = listing.Price,
                CommissionAmount = commission,
                SellerPayout = listing.Price - commission,
                Status = status,
                EscrowCode = $"JT-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
                BuyerConfirmed = status == TransactionStatus.Completed,
                SellerConfirmed = status is TransactionStatus.Completed or TransactionStatus.CredentialsSent,
                EscrowHeldAt = status is TransactionStatus.EscrowHeld or TransactionStatus.CredentialsSent
                    or TransactionStatus.BuyerVerifying or TransactionStatus.Completed or TransactionStatus.Disputed
                    ? created.AddMinutes(12)
                    : null,
                CompletedAt = status == TransactionStatus.Completed ? created.AddHours(6) : null,
                CreatedAt = created
            };
        }

        var transactions = new List<Transaction>
        {
            NewTx(pubgSold, alisher, TransactionStatus.Completed, 20),
            NewTx(ffSold, alisher, TransactionStatus.Completed, 33),
            NewTx(efootballLegend, sardor, TransactionStatus.Disputed, 2),
            NewTx(listings[5], madina, TransactionStatus.EscrowHeld, 1),
            NewTx(listings[7], gamerTash, TransactionStatus.Completed, 45),
            NewTx(listings[3], sardor, TransactionStatus.Cancelled, 12)
        };

        db.Transactions.AddRange(transactions);
        await db.SaveChangesAsync(ct);

        // Nizo ochilgan bitimga nizo yozuvi
        var disputed = transactions[2];
        db.Disputes.Add(new Dispute
        {
            TransactionId = disputed.Id,
            OpenedById = disputed.BuyerId,
            Reason = "Akkaunt ma'lumotlari noto'g'ri — login ishlamayapti.",
            Status = DisputeStatus.Open,
            CreatedAt = now.AddHours(-31)
        });

        // Yakunlangan bitimlarga reyting
        db.Ratings.AddRange(
            new Rating
            {
                TransactionId = transactions[0].Id,
                FromUserId = transactions[0].BuyerId,
                ToUserId = transactions[0].SellerId,
                Score = 5,
                Comment = "Jetar orqali akkaunt sotib oldim, juda xavfsiz va tez! Pul escrowda turdi, tekshirib tasdiqladim.",
                CreatedAt = now.AddDays(-20)
            },
            new Rating
            {
                TransactionId = transactions[1].Id,
                FromUserId = transactions[1].BuyerId,
                ToUserId = transactions[1].SellerId,
                Score = 5,
                Comment = "Ilgari Telegramda aldangan edim. Bu yerda kafil tizimi bor — endi faqat Jetar orqali oldi-sotdi qilaman.",
                CreatedAt = now.AddDays(-33)
            },
            new Rating
            {
                TransactionId = transactions[4].Id,
                FromUserId = transactions[4].BuyerId,
                ToUserId = transactions[4].SellerId,
                Score = 5,
                Comment = "Sotuvchi sifatida ishlayapman. Komissiya adolatli, pul bir kunda kartaga tushdi.",
                CreatedAt = now.AddDays(-45)
            });

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Demo ma'lumotlar yuklandi: {Users} foydalanuvchi, {Listings} e'lon, {Tx} bitim. Demo parol: {Password}",
            users.Length, listings.Count, transactions.Count, DemoPassword);
    }
}
