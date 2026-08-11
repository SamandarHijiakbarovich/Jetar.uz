using Jetar.Domain.Abstractions;
using Jetar.Domain.Entities;
using Jetar.Domain.Enums;

namespace Jetar.Domain.Specifications;

/// <summary>Bitim bo'yicha to'langan to'lovlar (qaytarish uchun, kuzatiladi).</summary>
public sealed class PaidPaymentsForTransactionSpec : Specification<Payment>
{
    public PaidPaymentsForTransactionSpec(Guid transactionId)
        : base(p => p.TransactionId == transactionId && p.Status == PaymentStatus.Paid) { }
}

/// <summary>Bitim bo'yicha tugallanmagan to'lov urinishlari (bekor qilish uchun, kuzatiladi).</summary>
public sealed class PendingPaymentsForTransactionSpec : Specification<Payment>
{
    public PendingPaymentsForTransactionSpec(Guid transactionId)
        : base(p => p.TransactionId == transactionId
                    && (p.Status == PaymentStatus.Created || p.Status == PaymentStatus.Pending)) { }
}
