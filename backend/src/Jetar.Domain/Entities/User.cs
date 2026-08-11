using Jetar.Domain.Enums;

namespace Jetar.Domain.Entities;

/// <summary>Platforma foydalanuvchisi — bir vaqtning o'zida ham xaridor, ham sotuvchi bo'lishi mumkin.</summary>
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Username { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Ism — ro'yxatdan o'tishda majburiy.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Familiya — ro'yxatdan o'tishda majburiy.</summary>
    public string LastName { get; set; } = string.Empty;

    public string? Email { get; set; }
    public string? TelegramUsername { get; set; }
    public long? TelegramChatId { get; set; }
    public string? City { get; set; }
    public string? AvatarUrl { get; set; }

    /// <summary>0.0 – 5.0 oralig'idagi o'rtacha reyting.</summary>
    public decimal Rating { get; set; }
    public int RatingCount { get; set; }
    public int TotalSales { get; set; }
    public int TotalPurchases { get; set; }

    /// <summary>Telefon tasdiqlangan va hujjat tekshirilgan sotuvchi.</summary>
    public bool IsVerified { get; set; }
    public bool IsBlocked { get; set; }
    public UserRole Role { get; set; } = UserRole.User;

    /// <summary>Sotuvchi javob berish o'rtacha vaqti (daqiqada) — profil kartochkasida ko'rsatiladi.</summary>
    public int AvgResponseMinutes { get; set; } = 15;

    public bool NotifyTelegram { get; set; } = true;
    public bool NotifyNewMessage { get; set; } = true;
    public bool NotifyMarketing { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastLoginAt { get; set; }

    public ICollection<Listing> Listings { get; set; } = new List<Listing>();
    public ICollection<Transaction> Purchases { get; set; } = new List<Transaction>();
    public ICollection<Transaction> Sales { get; set; } = new List<Transaction>();
}
