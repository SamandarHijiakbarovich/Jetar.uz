using Jetar.Core.Common;
using Jetar.Core.Contracts;
using Jetar.Core.Entities;
using Jetar.Core.Enums;
using Jetar.Core.Interfaces;
using Jetar.Core.Options;
using Jetar.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Jetar.Infrastructure.Services;

public class ListingService : IListingService
{
    private readonly AppDbContext _db;
    private readonly PlatformOptions _platform;
    private readonly IClock _clock;

    public ListingService(AppDbContext db, IOptions<PlatformOptions> platform, IClock clock)
    {
        _db = db;
        _platform = platform.Value;
        _clock = clock;
    }

    public async Task<PagedResult<ListingCardDto>> SearchAsync(ListingQuery q, CancellationToken ct = default)
    {
        var page = Math.Max(1, q.Page);
        var size = Math.Clamp(q.PageSize, 1, 60);

        var query = _db.Listings.Include(l => l.Seller).AsNoTracking().AsQueryable();

        // Sotuvchining o'z sahifasida barcha holatlar ko'rinadi, ommaviy ro'yxatda faqat aktiv.
        query = q.SellerId.HasValue
            ? query.Where(l => l.SellerId == q.SellerId.Value)
            : query.Where(l => l.Status == ListingStatus.Active);

        var game = GameCatalog.FromSlug(q.Game);
        if (game.HasValue) query = query.Where(l => l.GameType == game.Value);

        var type = ListingTypeCatalog.FromSlug(q.Type);
        if (type.HasValue) query = query.Where(l => l.Type == type.Value);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            // ToLower().Contains — provayderdan mustaqil (Postgres'da LOWER(...) LIKE ga tushadi).
            var term = q.Search.Trim().ToLowerInvariant();
            query = query.Where(l => l.Title.ToLower().Contains(term) || l.Description.ToLower().Contains(term));
        }

        if (q.MinPrice.HasValue) query = query.Where(l => l.Price >= q.MinPrice.Value);
        if (q.MaxPrice.HasValue) query = query.Where(l => l.Price <= q.MaxPrice.Value);

        if (!string.IsNullOrWhiteSpace(q.Region))
            query = query.Where(l => l.ServerRegion == q.Region);

        if (q.VerifiedOnly == true) query = query.Where(l => l.IsVerified);

        var total = await query.CountAsync(ct);

        // Boost qilingan e'lonlar har doim yuqorida.
        var now = _clock.UtcNow;
        query = q.Sort switch
        {
            "price_asc" => query.OrderByDescending(l => l.BoostedUntil > now).ThenBy(l => l.Price),
            "price_desc" => query.OrderByDescending(l => l.BoostedUntil > now).ThenByDescending(l => l.Price),
            "popular" => query.OrderByDescending(l => l.BoostedUntil > now).ThenByDescending(l => l.ViewCount),
            _ => query.OrderByDescending(l => l.BoostedUntil > now).ThenByDescending(l => l.CreatedAt)
        };

        var items = await query
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        return PagedResult<ListingCardDto>.Create(items.Select(l => l.ToCardDto()).ToList(), page, size, total);
    }

    public async Task<ListingDetailDto> GetAsync(Guid id, Guid? viewerId, CancellationToken ct = default)
    {
        var listing = await _db.Listings
            .Include(l => l.Seller)
            .FirstOrDefaultAsync(l => l.Id == id, ct)
            ?? throw AppException.NotFound("E'lon");

        if (viewerId != listing.SellerId)
        {
            listing.ViewCount++;
            await _db.SaveChangesAsync(ct);
        }

        var similar = await _db.Listings
            .Include(l => l.Seller)
            .AsNoTracking()
            .Where(l => l.GameType == listing.GameType && l.Id != listing.Id && l.Status == ListingStatus.Active)
            .OrderByDescending(l => l.CreatedAt)
            .Take(3)
            .ToListAsync(ct);

        return listing.ToDetailDto(similar.Select(l => l.ToCardDto()).ToList());
    }

    public async Task<ListingDetailDto> CreateAsync(Guid sellerId, CreateListingRequest r, CancellationToken ct = default)
    {
        Validate(r.Title, r.Description, r.Price, r.Images?.Count ?? 0);

        var listing = new Listing
        {
            SellerId = sellerId,
            GameType = r.GameType,
            Type = r.Type,
            Title = r.Title.Trim(),
            Description = r.Description.Trim(),
            Price = decimal.Round(r.Price, 0),
            ServerRegion = string.IsNullOrWhiteSpace(r.ServerRegion) ? "ASIA" : r.ServerRegion.Trim().ToUpperInvariant(),
            RankLevel = r.RankLevel?.Trim() ?? string.Empty,
            InGameItems = r.InGameItems ?? new List<string>(),
            Images = r.Images ?? new List<string>(),
            Stats = r.Stats ?? new Dictionary<string, string>(),
            Status = _platform.AutoApproveListings ? ListingStatus.Active : ListingStatus.Pending,
            CreatedAt = _clock.UtcNow
        };

        _db.Listings.Add(listing);
        await _db.SaveChangesAsync(ct);

        return await GetAsync(listing.Id, sellerId, ct);
    }

    public async Task<ListingDetailDto> UpdateAsync(Guid id, Guid userId, bool isModerator, UpdateListingRequest r, CancellationToken ct = default)
    {
        var listing = await _db.Listings.FirstOrDefaultAsync(l => l.Id == id, ct)
                      ?? throw AppException.NotFound("E'lon");

        if (listing.SellerId != userId && !isModerator)
            throw AppException.Forbidden("Bu e'lon sizga tegishli emas.");

        if (listing.Status == ListingStatus.Reserved)
            throw AppException.Conflict("Bitim ochilgan e'lonni tahrirlab bo'lmaydi.");

        if (r.Type.HasValue) listing.Type = r.Type.Value;
        if (r.Title != null) listing.Title = r.Title.Trim();
        if (r.Description != null) listing.Description = r.Description.Trim();
        if (r.Price.HasValue) listing.Price = decimal.Round(r.Price.Value, 0);
        if (r.ServerRegion != null) listing.ServerRegion = r.ServerRegion.Trim().ToUpperInvariant();
        if (r.RankLevel != null) listing.RankLevel = r.RankLevel.Trim();
        if (r.InGameItems != null) listing.InGameItems = r.InGameItems;
        if (r.Images != null) listing.Images = r.Images;
        if (r.Stats != null) listing.Stats = r.Stats;

        if (r.Status.HasValue)
        {
            // Sotuvchi faqat yashirish/qayta ochish qila oladi; Blocked va Sold — moderator ishi.
            var allowed = isModerator
                ? new[] { ListingStatus.Active, ListingStatus.Hidden, ListingStatus.Blocked, ListingStatus.Pending, ListingStatus.Sold }
                : new[] { ListingStatus.Active, ListingStatus.Hidden };

            if (!allowed.Contains(r.Status.Value))
                throw AppException.Forbidden("Bu holatga o'tkazishga ruxsat yo'q.");

            listing.Status = r.Status.Value;
        }

        Validate(listing.Title, listing.Description, listing.Price, listing.Images.Count);

        listing.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);

        return await GetAsync(listing.Id, listing.SellerId, ct);
    }

    public async Task DeleteAsync(Guid id, Guid userId, bool isModerator, CancellationToken ct = default)
    {
        var listing = await _db.Listings.Include(l => l.Transactions).FirstOrDefaultAsync(l => l.Id == id, ct)
                      ?? throw AppException.NotFound("E'lon");

        if (listing.SellerId != userId && !isModerator)
            throw AppException.Forbidden("Bu e'lon sizga tegishli emas.");

        // Bitim tarixi bor e'lon o'chirilmaydi — audit uchun saqlanadi, faqat yashiriladi.
        if (listing.Transactions.Count > 0)
        {
            listing.Status = ListingStatus.Hidden;
            listing.UpdatedAt = _clock.UtcNow;
        }
        else
        {
            _db.Listings.Remove(listing);
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<GameSummaryDto>> GetGameSummariesAsync(CancellationToken ct = default)
    {
        var counts = await _db.Listings
            .AsNoTracking()
            .Where(l => l.Status == ListingStatus.Active)
            .GroupBy(l => l.GameType)
            .Select(g => new { Game = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return GameCatalog.All
            .Select(g => new GameSummaryDto(
                g.Slug, g.Name, g.Glyph, g.Color,
                counts.FirstOrDefault(c => c.Game == g.Type)?.Count ?? 0))
            .ToList();
    }

    public async Task<CategoriesDto> GetCategoriesAsync(CancellationToken ct = default)
    {
        var typeCounts = await _db.Listings
            .AsNoTracking()
            .Where(l => l.Status == ListingStatus.Active)
            .GroupBy(l => l.Type)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var types = ListingTypeCatalog.All
            .Select(t => new ListingTypeSummaryDto(
                t.Slug, t.Name, t.Glyph, t.Description,
                typeCounts.FirstOrDefault(c => c.Type == t.Type)?.Count ?? 0))
            .ToList();

        return new CategoriesDto(types, await GetGameSummariesAsync(ct));
    }

    private void Validate(string title, string description, decimal price, int imageCount)
    {
        if (title.Trim().Length < 10)
            throw new AppException("Sarlavha kamida 10 belgidan iborat bo'lsin.", 400, "invalid_title");

        if (description.Trim().Length < 20)
            throw new AppException("Tavsif kamida 20 belgidan iborat bo'lsin.", 400, "invalid_description");

        if (price < _platform.MinListingPrice || price > _platform.MaxListingPrice)
            throw new AppException(
                $"Narx {_platform.MinListingPrice:N0} – {_platform.MaxListingPrice:N0} so'm oralig'ida bo'lsin.",
                400, "invalid_price");

        if (imageCount > _platform.MaxImagesPerListing)
            throw new AppException($"Maksimal {_platform.MaxImagesPerListing} ta rasm.", 400, "too_many_images");
    }
}
