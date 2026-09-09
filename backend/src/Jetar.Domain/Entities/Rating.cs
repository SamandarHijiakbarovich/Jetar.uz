namespace Jetar.Domain.Entities;

/// <summary>
/// Sotuvchiga qoldiriladigan baho. Ikki manba bor:
/// eski escrow bitimidan (<see cref="TransactionId"/> to'ldirilgan) yoki
/// yangi modelda to'g'ridan-to'g'ri sotuvchi profiliga (TransactionId = null).
/// </summary>
public class Rating
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Escrow bitimidan kelgan baho uchun to'ldiriladi; to'g'ridan-to'g'ri bahoda null.</summary>
    public Guid? TransactionId { get; set; }
    public Transaction? Transaction { get; set; }

    public Guid FromUserId { get; set; }
    public User? FromUser { get; set; }

    public Guid ToUserId { get; set; }
    public User? ToUser { get; set; }

    /// <summary>1–5.</summary>
    public short Score { get; set; }

    public string? Comment { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
