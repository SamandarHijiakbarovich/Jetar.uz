using Jetar.Domain.Enums;

namespace Jetar.Domain.Entities;

/// <summary>
/// Pullik ko'tarish (TOP/VIP) so'rovi. Sotuvchi kartaga to'lov qilib chek yuboradi,
/// admin qo'lda tasdiqlaydi va e'lon <see cref="Listing.BoostedUntil"/> gacha tepada turadi.
/// Platforma bu yerda faqat o'z reklama xizmatini sotadi — begona pulni ushlab turmaydi.
/// </summary>
public class BoostRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ListingId { get; set; }
    public Listing? Listing { get; set; }

    /// <summary>So'rov yuborgan sotuvchi.</summary>
    public Guid UserId { get; set; }
    public User? User { get; set; }

    /// <summary>Necha kunga ko'tarish (tarifdan).</summary>
    public int Days { get; set; }

    /// <summary>Tarif narxi so'mda.</summary>
    public decimal Amount { get; set; }

    /// <summary>To'lov cheki / skrinshot URL'i (ixtiyoriy).</summary>
    public string? ScreenshotUrl { get; set; }

    public BoostStatus Status { get; set; } = BoostStatus.Pending;

    /// <summary>Admin izohi (rad etilganda sabab).</summary>
    public string? ReviewNote { get; set; }

    public Guid? ReviewedById { get; set; }
    public User? ReviewedBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReviewedAt { get; set; }
}
