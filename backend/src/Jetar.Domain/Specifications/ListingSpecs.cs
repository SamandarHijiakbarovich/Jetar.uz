using Jetar.Domain.Abstractions;
using Jetar.Domain.Entities;
using Jetar.Domain.Enums;

namespace Jetar.Domain.Specifications;

/// <summary>E'lonlar katalogini qidirish: dinamik filtr + boost-birinchi saralash + sahifalash.</summary>
public sealed class ListingSearchSpec : Specification<Listing>
{
    public ListingSearchSpec(
        Guid? sellerId,
        GameType? game,
        ListingType? type,
        string? search,
        decimal? minPrice,
        decimal? maxPrice,
        string? region,
        bool verifiedOnly,
        string? sort,
        DateTimeOffset now,
        int page,
        int size)
    {
        // Sotuvchining o'z sahifasida barcha holatlar, ommaviy ro'yxatda faqat aktiv.
        if (sellerId.HasValue) Where(l => l.SellerId == sellerId.Value);
        else Where(l => l.Status == ListingStatus.Active);

        if (game.HasValue) Where(l => l.GameType == game.Value);
        if (type.HasValue) Where(l => l.Type == type.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            Where(l => l.Title.ToLower().Contains(term) || l.Description.ToLower().Contains(term));
        }

        if (minPrice.HasValue) Where(l => l.Price >= minPrice.Value);
        if (maxPrice.HasValue) Where(l => l.Price <= maxPrice.Value);
        if (!string.IsNullOrWhiteSpace(region)) Where(l => l.ServerRegion == region);
        if (verifiedOnly) Where(l => l.IsVerified);

        AddInclude(l => l.Seller!);
        ApplyNoTracking();

        // Boost qilingan e'lonlar har doim yuqorida.
        AddOrderByDescending(l => l.BoostedUntil > now);
        switch (sort)
        {
            case "price_asc": AddOrderBy(l => l.Price); break;
            case "price_desc": AddOrderByDescending(l => l.Price); break;
            case "popular": AddOrderByDescending(l => l.ViewCount); break;
            default: AddOrderByDescending(l => l.CreatedAt); break;
        }

        ApplyPaging(page, size);
    }
}

/// <summary>Bitta e'lon — sotuvchisi bilan (kuzatiladi, ViewCount oshirish mumkin bo'lsin).</summary>
public sealed class ListingWithSellerSpec : Specification<Listing>
{
    public ListingWithSellerSpec(Guid id) : base(l => l.Id == id)
        => AddInclude(l => l.Seller!);
}

/// <summary>E'longa o'xshash aktiv e'lonlar (bir xil o'yin), eng yangisidan.</summary>
public sealed class SimilarListingsSpec : Specification<Listing>
{
    public SimilarListingsSpec(GameType game, Guid excludeId, int take)
    {
        Where(l => l.GameType == game && l.Id != excludeId && l.Status == ListingStatus.Active);
        AddInclude(l => l.Seller!);
        ApplyNoTracking();
        AddOrderByDescending(l => l.CreatedAt);
        ApplyPaging(1, take);
    }
}

/// <summary>Bitim tarixi bilan yuklangan e'lon (o'chirish qoidasi uchun).</summary>
public sealed class ListingWithTransactionsSpec : Specification<Listing>
{
    public ListingWithTransactionsSpec(Guid id) : base(l => l.Id == id)
        => AddInclude(l => l.Transactions);
}
