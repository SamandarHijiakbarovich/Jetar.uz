using Jetar.Core.Common;
using Jetar.Core.Contracts;
using Jetar.Core.Entities;
using Jetar.Core.Enums;
using Jetar.Core.Interfaces;
using Jetar.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jetar.Infrastructure.Services;

/// <summary>
/// To'lov oqimi: Payment yozuvi yaratiladi → provayder checkout URL beradi →
/// callback (yoki sandbox tasdiq) kelgach escrow "Hold" holatiga o'tadi.
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly AppDbContext _db;
    private readonly IEnumerable<IPaymentGateway> _gateways;
    private readonly IEscrowService _escrow;
    private readonly IClock _clock;
    private readonly ILogger<PaymentService> _log;

    public PaymentService(
        AppDbContext db,
        IEnumerable<IPaymentGateway> gateways,
        IEscrowService escrow,
        IClock clock,
        ILogger<PaymentService> log)
    {
        _db = db;
        _gateways = gateways;
        _escrow = escrow;
        _clock = clock;
        _log = log;
    }

    public async Task<PaymentDto> CreateAsync(Guid transactionId, Guid userId, PaymentMethod method, CancellationToken ct = default)
    {
        var tx = await _db.Transactions.FirstOrDefaultAsync(t => t.Id == transactionId, ct)
                 ?? throw AppException.NotFound("Bitim");

        if (tx.BuyerId != userId)
            throw AppException.Forbidden("To'lovni faqat xaridor amalga oshiradi.");

        if (tx.Status is not (TransactionStatus.Initiated or TransactionStatus.AwaitingPayment))
            throw AppException.Conflict($"Bitim holati '{StatusLabels.Get(tx.Status)}' — to'lov qabul qilinmaydi.");

        // Tugallanmagan urinish qolgan bo'lsa, uni bekor qilamiz.
        var stale = await _db.Payments
            .Where(p => p.TransactionId == transactionId && (p.Status == PaymentStatus.Created || p.Status == PaymentStatus.Pending))
            .ToListAsync(ct);

        foreach (var p in stale) p.Status = PaymentStatus.Cancelled;

        var payment = new Payment
        {
            TransactionId = tx.Id,
            UserId = userId,
            Amount = tx.Amount,
            Method = method,
            Status = PaymentStatus.Created,
            CreatedAt = _clock.UtcNow
        };

        var gateway = Resolve(method);
        var result = await gateway.InitiateAsync(payment, ct);

        if (!result.Success)
        {
            payment.Status = PaymentStatus.Failed;
            payment.FailureReason = result.Error;
            _db.Payments.Add(payment);
            await _db.SaveChangesAsync(ct);

            throw new AppException(result.Error ?? "To'lovni boshlab bo'lmadi.", 502, "payment_init_failed");
        }

        payment.ProviderPaymentId = result.ProviderPaymentId;
        payment.CheckoutUrl = result.CheckoutUrl;
        payment.Status = PaymentStatus.Pending;

        tx.Status = TransactionStatus.AwaitingPayment;

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync(ct);

        _log.LogInformation("To'lov boshlandi {Method} {ProviderId} bitim={Code}",
            method, result.ProviderPaymentId, tx.EscrowCode);

        return payment.ToDto();
    }

    public async Task<bool> HandleCallbackAsync(
        PaymentMethod method, string rawPayload, IDictionary<string, string> headers, CancellationToken ct = default)
    {
        var gateway = Resolve(method);
        var result = await gateway.HandleCallbackAsync(rawPayload, headers, ct);

        if (!string.IsNullOrEmpty(result.Error))
        {
            _log.LogWarning("{Method} callback rad etildi: {Error}", method, result.Error);
            return false;
        }

        var payment = await _db.Payments
            .FirstOrDefaultAsync(p => p.ProviderPaymentId == result.ProviderPaymentId, ct);

        if (payment == null)
        {
            _log.LogWarning("{Method} callback: to'lov topilmadi {Id}", method, result.ProviderPaymentId);
            return false;
        }

        payment.ProviderPayload = Truncate(rawPayload, 4000);

        if (result.IsPaid) await MarkPaidAsync(payment, ct);
        else if (result.IsCancelled) await MarkFailedAsync(payment, "Provayder bekor qildi.", ct);
        else await _db.SaveChangesAsync(ct);

        return true;
    }

    public async Task<PaymentDto> SandboxConfirmAsync(string providerPaymentId, bool success, CancellationToken ct = default)
    {
        var payment = await _db.Payments.FirstOrDefaultAsync(p => p.ProviderPaymentId == providerPaymentId, ct)
                      ?? throw AppException.NotFound("To'lov");

        if (payment.Status == PaymentStatus.Paid) return payment.ToDto();

        if (success) await MarkPaidAsync(payment, ct);
        else await MarkFailedAsync(payment, "Sandbox rejimida rad etildi.", ct);

        return payment.ToDto();
    }

    public async Task<bool> RefundAsync(Guid transactionId, CancellationToken ct = default)
    {
        var payments = await _db.Payments
            .Where(p => p.TransactionId == transactionId && p.Status == PaymentStatus.Paid)
            .ToListAsync(ct);

        var allOk = true;

        foreach (var payment in payments)
        {
            var ok = await Resolve(payment.Method).RefundAsync(payment, ct);
            allOk &= ok;

            if (!ok) continue;

            payment.Status = PaymentStatus.Refunded;
            payment.RefundedAt = _clock.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        return allOk;
    }

    private async Task MarkPaidAsync(Payment payment, CancellationToken ct)
    {
        payment.Status = PaymentStatus.Paid;
        payment.PaidAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);

        // 04 — pul BLOKLANADI.
        await _escrow.HoldAsync(payment.TransactionId, ct);
    }

    private async Task MarkFailedAsync(Payment payment, string reason, CancellationToken ct)
    {
        payment.Status = PaymentStatus.Failed;
        payment.FailureReason = reason;
        await _db.SaveChangesAsync(ct);
    }

    private IPaymentGateway Resolve(PaymentMethod method)
        => _gateways.FirstOrDefault(g => g.Method == method)
           ?? throw new AppException($"{method} to'lov usuli qo'llab-quvvatlanmaydi.", 400, "unsupported_method");

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max];
}
