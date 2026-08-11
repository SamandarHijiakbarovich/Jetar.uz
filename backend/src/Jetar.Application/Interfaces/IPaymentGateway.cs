using Jetar.Application.Contracts;
using Jetar.Domain.Entities;
using Jetar.Domain.Enums;

namespace Jetar.Application.Interfaces;

/// <summary>
/// Bitta to'lov provayderi (Click, Payme, Uzum, Apelsin) bilan ishlash shartnomasi.
/// Sandbox rejimida haqiqiy tarmoq so'rovi yuborilmaydi.
/// </summary>
public interface IPaymentGateway
{
    PaymentMethod Method { get; }

    /// <summary>Provayder tomonida to'lov yaratadi va checkout URL qaytaradi.</summary>
    Task<PaymentInitResult> InitiateAsync(Payment payment, CancellationToken ct = default);

    /// <summary>Provayderdan kelgan callback'ni tekshiradi va normallashtiradi.</summary>
    Task<PaymentCallbackResult> HandleCallbackAsync(string rawPayload, IDictionary<string, string> headers, CancellationToken ct = default);

    /// <summary>Pulni xaridorga qaytaradi.</summary>
    Task<bool> RefundAsync(Payment payment, CancellationToken ct = default);
}

/// <summary>Provayderlarni tanlab, to'lov oqimini boshqaradi.</summary>
public interface IPaymentService
{
    Task<PaymentDto> CreateAsync(Guid transactionId, Guid userId, PaymentMethod method, CancellationToken ct = default);

    /// <summary>Provayder callback'i — muvaffaqiyatli bo'lsa escrow "Hold" holatiga o'tadi.</summary>
    Task<bool> HandleCallbackAsync(PaymentMethod method, string rawPayload, IDictionary<string, string> headers, CancellationToken ct = default);

    /// <summary>Sandbox rejimida to'lovni qo'lda tasdiqlash / rad etish.</summary>
    Task<PaymentDto> SandboxConfirmAsync(string providerPaymentId, bool success, CancellationToken ct = default);

    Task<bool> RefundAsync(Guid transactionId, CancellationToken ct = default);
}
