using Jetar.Domain.Abstractions;
using Jetar.Domain.Entities;
using Jetar.Domain.Enums;

namespace Jetar.Domain.Specifications;

/// <summary>Admin panel: bitimlarni qidirish (raqam, escrow kodi yoki username bo'yicha).</summary>
public sealed class AdminTransactionSearchSpec : Specification<Transaction>
{
    public AdminTransactionSearchSpec(string? search, int page, int pageSize)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().TrimStart('#', '@').ToLowerInvariant();
            var asNumber = int.TryParse(term, out var num) ? num : (int?)null;

            Where(t =>
                (asNumber != null && t.Number == asNumber) ||
                t.EscrowCode.ToLower().Contains(term) ||
                t.Buyer!.Username.Contains(term) ||
                t.Seller!.Username.Contains(term));
        }

        AddInclude(t => t.Buyer!);
        AddInclude(t => t.Seller!);
        ApplyNoTracking();
        AddOrderByDescending(t => t.CreatedAt);
        ApplyPaging(page, pageSize);
    }
}

/// <summary>Admin panel: nizolar ro'yxati (ixtiyoriy — faqat ochiqlari).</summary>
public sealed class DisputeListSpec : Specification<Dispute>
{
    public DisputeListSpec(bool openOnly)
    {
        if (openOnly)
            Where(d => d.Status == DisputeStatus.Open || d.Status == DisputeStatus.UnderReview);

        AddInclude(d => d.Transaction!);
        ApplyNoTracking();
        AddOrderBy(d => d.CreatedAt);
    }
}
