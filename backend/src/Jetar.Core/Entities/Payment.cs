using Jetar.Core.Enums;

namespace Jetar.Core.Entities;

/// <summary>Tashqi to'lov tizimi orqali amalga oshirilgan to'lov yozuvi.</summary>
public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TransactionId { get; set; }
    public Transaction? Transaction { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public decimal Amount { get; set; }

    public PaymentMethod Method { get; set; }

    /// <summary>Provayder tomonidagi to'lov identifikatori.</summary>
    public string? ProviderPaymentId { get; set; }

    /// <summary>Foydalanuvchi yo'naltiriladigan to'lov sahifasi.</summary>
    public string? CheckoutUrl { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Created;

    /// <summary>Callback'da kelgan xom javob — audit uchun.</summary>
    public string? ProviderPayload { get; set; }

    public string? FailureReason { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset? RefundedAt { get; set; }
}
