using Jetar.Domain.Abstractions;
using Jetar.Domain.Entities;

namespace Jetar.Domain.Specifications;

/// <summary>Nizo — bitimi va uning e'loni bilan (moderator qarori uchun, kuzatiladi).</summary>
public sealed class DisputeWithTransactionSpec : Specification<Dispute>
{
    public DisputeWithTransactionSpec(Guid id) : base(d => d.Id == id)
    {
        AddInclude(d => d.Transaction!);
        AddInclude("Transaction.Listing");
    }
}
