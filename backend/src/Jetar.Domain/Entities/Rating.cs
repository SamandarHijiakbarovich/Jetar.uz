namespace Jetar.Domain.Entities;

/// <summary>Bitim yakunlangach qoldiriladigan baho. Bir bitim ichida har tomon bir marta baho beradi.</summary>
public class Rating
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TransactionId { get; set; }
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
