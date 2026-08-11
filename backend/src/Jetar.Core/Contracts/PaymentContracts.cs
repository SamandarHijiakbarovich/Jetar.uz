using Jetar.Core.Enums;

namespace Jetar.Core.Contracts;

public record CreatePaymentRequest(Guid TransactionId, PaymentMethod Method);

public record PaymentDto(
    Guid Id,
    Guid TransactionId,
    decimal Amount,
    PaymentMethod Method,
    PaymentStatus Status,
    string? CheckoutUrl,
    string? ProviderPaymentId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PaidAt);

/// <summary>To'lov provayderi qaytaradigan natija.</summary>
public record PaymentInitResult(bool Success, string ProviderPaymentId, string? CheckoutUrl, string? Error);

/// <summary>Provayder callback'i normallashtirilgan ko'rinishda.</summary>
public record PaymentCallbackResult(bool IsPaid, bool IsCancelled, string ProviderPaymentId, string? Error);

/// <summary>Sandbox rejimida to'lovni qo'lda tasdiqlash uchun.</summary>
public record SandboxConfirmRequest(string ProviderPaymentId, bool Success);
