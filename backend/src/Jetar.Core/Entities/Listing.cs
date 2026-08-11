using Jetar.Core.Enums;

namespace Jetar.Core.Entities;

/// <summary>O'yin akkaunti e'loni.</summary>
public class Listing
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SellerId { get; set; }
    public User? Seller { get; set; }

    public GameType GameType { get; set; }

    /// <summary>Akkaunt / valyuta / skin / xizmat.</summary>
    public ListingType Type { get; set; } = ListingType.Account;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>Narx so'mda, tiyinsiz (numeric(12,0)).</summary>
    public decimal Price { get; set; }

    /// <summary>ASIA / EU / NA / CIS.</summary>
    public string ServerRegion { get; set; } = "ASIA";

    /// <summary>Dream League, Conqueror, Immortal ...</summary>
    public string RankLevel { get; set; } = string.Empty;

    /// <summary>O'yin ichidagi vositalar: ["Skin","Coins"] — jsonb.</summary>
    public List<string> InGameItems { get; set; } = new();

    /// <summary>Skrinshot URL'lari — jsonb.</summary>
    public List<string> Images { get; set; } = new();

    /// <summary>Sonli ko'rsatkichlar: {"GP":"1.2M","Coins":"4 800"} — jsonb.</summary>
    public Dictionary<string, string> Stats { get; set; } = new();

    public ListingStatus Status { get; set; } = ListingStatus.Pending;

    /// <summary>Moderator "Tekshirilgan" belgisini qo'ygan.</summary>
    public bool IsVerified { get; set; }

    /// <summary>Pullik ko'tarish (boost) tugash vaqti.</summary>
    public DateTimeOffset? BoostedUntil { get; set; }

    public int ViewCount { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? SoldAt { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
