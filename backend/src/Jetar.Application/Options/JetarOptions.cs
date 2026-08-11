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

    /// <summary>Escrow komissiyasi. Dizaynda 8% ko'rsatilgan.</summary>
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
