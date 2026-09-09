namespace Jetar.Application.Options;

/// <summary>JWT sozlamalari — appsettings.json "Jwt" bo'limi.</summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "jetar";
    public string Audience { get; set; } = "jetar-clients";

    /// <summary>Kamida 32 belgi. Ishlab chiqarishda .env orqali beriladi.</summary>
    public string Secret { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 60;
    public int RefreshTokenDays { get; set; } = 30;
}

/// <summary>Platforma biznes sozlamalari — appsettings.json "Platform" bo'limi.</summary>
public class PlatformOptions
{
    public const string SectionName = "Platform";

    /// <summary>
    /// Escrow (kafolatli to'lov) rejimi. Yangi modelda platforma pulga aralashmaydi —
    /// shu bayroq false bo'lsa barcha escrow/to'lov/bitim endpoint'lari 410 qaytaradi.
    /// </summary>
    public bool EscrowEnabled { get; set; } = false;

    /// <summary>Sotuvchi kontaktini (telefon/Telegram) faqat tizimga kirganlar ko'radimi.</summary>
    public bool ContactRequiresLogin { get; set; } = true;

    /// <summary>Escrow komissiyasi. Dizaynda 8% ko'rsatilgan. (Escrow o'chirilganda ishlatilmaydi.)</summary>
    public decimal CommissionRate { get; set; } = 0.08m;

    /// <summary>Xaridor javob bermasa, escrow shuncha soatdan keyin avtomatik sotuvchiga chiqadi.</summary>
    public int AutoReleaseHours { get; set; } = 72;

    /// <summary>To'lov qilinmagan bitim shuncha daqiqadan keyin bekor qilinadi.</summary>
    public int UnpaidTransactionTimeoutMinutes { get; set; } = 60;

    public decimal MinListingPrice { get; set; } = 10_000m;
    public decimal MaxListingPrice { get; set; } = 100_000_000m;

    public int MaxImagesPerListing { get; set; } = 8;
    public long MaxImageBytes { get; set; } = 5 * 1024 * 1024;

    /// <summary>Yangi e'lon moderator tasdig'isiz darhol efirga chiqsinmi (MVP uchun true).</summary>
    public bool AutoApproveListings { get; set; } = true;

    /// <summary>Pullik ko'tarish (TOP/VIP) sozlamalari.</summary>
    public BoostSettings Boost { get; set; } = new();

    public class BoostSettings
    {
        /// <summary>Sotuvchi to'lovni shu kartaga tashlaydi.</summary>
        public string CardNumber { get; set; } = "8600 0000 0000 0000";
        public string CardHolder { get; set; } = "JETAR";

        /// <summary>
        /// Ko'tarish tariflari — muddat va narx. Qiymatlar appsettings.json "Platform:Boost:Tiers"
        /// dan keladi. (Bu yerda ro'yxatni oldindan to'ldirmaymiz: .NET config bog'lovchisi
        /// ro'yxatga qo'shib yuboradi va tariflar takrorlanib qoladi.)
        /// </summary>
        public List<BoostTier> Tiers { get; set; } = new();
    }

    public class BoostTier
    {
        public int Days { get; set; }
        public decimal Price { get; set; }
        public string Label { get; set; } = string.Empty;
    }
}

/// <summary>
/// Boshlang'ich admin akkaunti — appsettings.json "Admin" bo'limi. Har ishga tushishda
/// tekshiriladi: yo'q bo'lsa yaratiladi, mavjud bo'lsa Admin roliga ko'tariladi. Prodda
/// parol JETAR_Admin__Password muhit o'zgaruvchisi orqali beriladi.
/// </summary>
public class AdminSeedOptions
{
    public const string SectionName = "Admin";

    public bool Enabled { get; set; } = true;
    public string FirstName { get; set; } = "Admin";
    public string LastName { get; set; } = string.Empty;
    public string Username { get; set; } = "admin";
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }

    /// <summary>Bo'sh bo'lsa admin yaratilmaydi (xavfsizlik). Prodda env orqali bering.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// true bo'lsa har ishga tushishda admin paroli config'dagi qiymatga majburan
    /// o'rnatiladi (parolni tiklash uchun). Odatda false — foydalanuvchi o'zgartirgan
    /// parol saqlanib qolsin.
    /// </summary>
    public bool ForcePassword { get; set; }
}

/// <summary>To'lov provayderlari sozlamalari — appsettings.json "Payments" bo'limi.</summary>
public class PaymentOptions
{
    public const string SectionName = "Payments";

    /// <summary>true bo'lsa haqiqiy provayderga chiqmaydi, sandbox rejimida ishlaydi.</summary>
    public bool SandboxMode { get; set; } = true;

    /// <summary>
    /// true bo'lsa checkout to'g'ridan-to'g'ri provayder (Click/Payme/Uzum) sahifasiga
    /// yo'naltiradi — sandbox bo'lsa ham. Merchant ID qo'yilgach jonli to'lov shu yo'l
    /// bilan ishlaydi (kod o'zgarmaydi). Demo/ko'rgazma uchun qulay.
    /// </summary>
    public bool ProviderRedirect { get; set; }

    public ProviderCredentials Click { get; set; } = new();
    public ProviderCredentials Payme { get; set; } = new();
    public ProviderCredentials Uzum { get; set; } = new();
    public ProviderCredentials Apelsin { get; set; } = new();

    /// <summary>To'lovdan keyin foydalanuvchi qaytariladigan frontend manzili.</summary>
    public string ReturnUrl { get; set; } = "http://localhost:5173/checkout/result";

    public class ProviderCredentials
    {
        public bool Enabled { get; set; } = true;
        public string MerchantId { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
    }
}

/// <summary>Fayl saqlash sozlamalari — appsettings.json "Storage" bo'limi.</summary>
public class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>"local" yoki "s3" (MinIO ham S3-mos).</summary>
    public string Provider { get; set; } = "local";

    public string LocalRoot { get; set; } = "wwwroot/uploads";
    public string PublicBaseUrl { get; set; } = "/uploads";

    public string S3Endpoint { get; set; } = string.Empty;
    public string S3Bucket { get; set; } = "jetar";
    public string S3AccessKey { get; set; } = string.Empty;
    public string S3SecretKey { get; set; } = string.Empty;
}

/// <summary>Telegram bildirishnomalari — appsettings.json "Telegram" bo'limi.</summary>
public class TelegramOptions
{
    public const string SectionName = "Telegram";

    public bool Enabled { get; set; }
    public string BotToken { get; set; } = string.Empty;
}
