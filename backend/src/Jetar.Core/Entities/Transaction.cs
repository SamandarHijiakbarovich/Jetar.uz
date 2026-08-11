using Jetar.Core.Enums;

namespace Jetar.Core.Entities;

/// <summary>
/// Escrow bitimi — tizimning yadrosi. E'lon, xaridor va sotuvchini bog'laydi;
/// to'lov, chat, reyting va nizo shu jadvalga osiladi.
/// </summary>
public class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Inson o'qiy oladigan qisqa raqam: #8842.</summary>
    public int Number { get; set; }

    public Guid ListingId { get; set; }
    public Listing? Listing { get; set; }

    public Guid BuyerId { get; set; }
    public User? Buyer { get; set; }

    public Guid SellerId { get; set; }
    public User? Seller { get; set; }

    /// <summary>Xaridor to'laydigan to'liq summa.</summary>
    public decimal Amount { get; set; }

    /// <summary>Platforma komissiyasi (Amount * CommissionRate).</summary>
    public decimal CommissionAmount { get; set; }

    /// <summary>Sotuvchiga chiqariladigan summa = Amount - CommissionAmount.</summary>
    public decimal SellerPayout { get; set; }

    public TransactionStatus Status { get; set; } = TransactionStatus.Initiated;

    /// <summary>Bitimni identifikatsiya qiluvchi kod: JT-8842-KX3P.</summary>
    public string EscrowCode { get; set; } = string.Empty;

    public bool BuyerConfirmed { get; set; }
    public bool SellerConfirmed { get; set; }

    /// <summary>Escrow ushlab turilgan vaqt — avto-release taymeri shundan hisoblanadi.</summary>
    public DateTimeOffset? EscrowHeldAt { get; set; }

    /// <summary>Xaridor javob bermasa, shu vaqtdan keyin pul avtomatik sotuvchiga o'tadi.</summary>
    public DateTimeOffset? AutoReleaseAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }

    public string? CancelReason { get; set; }

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
    public Dispute? Dispute { get; set; }
}
