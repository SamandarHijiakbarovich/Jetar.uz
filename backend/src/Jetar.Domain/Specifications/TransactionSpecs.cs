using System.Linq.Expressions;
using Jetar.Domain.Abstractions;
using Jetar.Domain.Entities;
using Jetar.Domain.Enums;

namespace Jetar.Domain.Specifications;

/// <summary>Bitta bitim — DTO uchun zarur barcha navigatsiyalar bilan.</summary>
public sealed class TransactionWithDetailsSpec : Specification<Transaction>
{
    public TransactionWithDetailsSpec(Guid id) : base(t => t.Id == id)
    {
        AddDetailIncludes(this);
    }

    /// <summary>Sahifalangan ro'yxat uchun: filtr + Include + saralash + sahifalash.</summary>
    public TransactionWithDetailsSpec(Expression<Func<Transaction, bool>> predicate, int page, int pageSize)
        : base(predicate)
    {
        AddDetailIncludes(this);
        AddOrderByDescending(t => t.CreatedAt);
        ApplyPaging(page, pageSize);
        ApplyNoTracking();
    }

    private static void AddDetailIncludes(TransactionWithDetailsSpec spec)
    {
        spec.AddInclude(t => t.Listing!);
        spec.AddInclude("Listing.Seller");
        spec.AddInclude(t => t.Buyer!);
        spec.AddInclude(t => t.Seller!);
        spec.AddInclude(t => t.Dispute!);
    }
}

/// <summary>Muddati o'tgan, avto-release'ga tayyor bitimlar (kuzatiladi — o'zgartiriladi).</summary>
public sealed class DueForAutoReleaseSpec : Specification<Transaction>
{
    public DueForAutoReleaseSpec(DateTimeOffset now)
        : base(t => (t.Status == TransactionStatus.CredentialsSent || t.Status == TransactionStatus.BuyerVerifying)
                    && t.AutoReleaseAt != null && t.AutoReleaseAt <= now)
    {
        AddInclude(t => t.Listing!);
        AddInclude(t => t.Seller!);
    }
}

/// <summary>To'lanmagan, muddati o'tgan bitimlar (kuzatiladi — bekor qilinadi).</summary>
public sealed class StaleUnpaidSpec : Specification<Transaction>
{
    public StaleUnpaidSpec(DateTimeOffset cutoff)
        : base(t => (t.Status == TransactionStatus.Initiated || t.Status == TransactionStatus.AwaitingPayment)
                    && t.CreatedAt <= cutoff)
        => AddInclude(t => t.Listing!);
}
