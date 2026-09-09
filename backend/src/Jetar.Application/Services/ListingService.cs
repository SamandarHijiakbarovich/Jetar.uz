using Jetar.Domain.Abstractions;
using Jetar.Domain.Common;
using Jetar.Application.Contracts;
using Jetar.Domain.Entities;
using Jetar.Domain.Enums;
using Jetar.Application.Interfaces;
using Jetar.Application.Options;
using Jetar.Domain.Specifications;
using Microsoft.Extensions.Options;

namespace Jetar.Application.Services;

public class ListingService : IListingService
{
    private readonly IUnitOfWork _uow;
    private readonly PlatformOptions _platform;
    private readonly IClock _clock;

    public ListingService(IUnitOfWork uow, IOptions<PlatformOptions> platform, IClock clock)
    {
        _uow = uow;
        _platform = platform.Value;
        _clock = clock;
    }

    public async Task<PagedResult<ListingCardDto>> SearchAsync(ListingQuery q, CancellationToken ct = default)
    {
        var page = Math.Max(1, q.Page);
        var size = Math.Clamp(q.PageSize, 1, 60);

        var spec = new ListingSearchSpec(
            q.SellerId,
            GameCatalog.FromSlug(q.Game),
            ListingTypeCatalog.FromSlug(q.Type),
            q.Search,
            q.MinPrice,
            q.MaxPrice,
            string.IsNullOrWhiteSpace(q.Region) ? null : q.Region,
            q.VerifiedOnly == true,
            q.Sort,
            _clock.UtcNow,
            page,
            size);

        var total = await _uow.Listings.CountAsync(spec, ct);
        var items = await _uow.Listings.ListAsync(spec, ct);

        return PagedResult<ListingCardDto>.Create(items.Select(l => l.ToCardDto()).ToList(), page, size, total);
    }

    public async Task<ListingDetailDto> GetAsync(Guid id, Guid? viewerId, CancellationToken ct = default)
    {
        var listing = await _uow.Listings.FirstOrDefaultAsync(new ListingWithSellerSpec(id), ct)
            ?? throw AppException.NotFound("E'lon");

        if (viewerId != listing.SellerId)
        {
            listing.ViewCount++;
            await _uow.SaveChangesAsync(ct);
        }

        var similar = await _uow.Listings.ListAsync(new SimilarListingsSpec(listing.GameType, listing.Id, 3), ct);

        // Kontakt (telefon/Telegram) spamga qarshi faqat tizimga kirganlarga ko'rsatiladi.
        var includeContact = viewerId.HasValue || !_platform.ContactRequiresLogin;

        return listing.ToDetailDto(similar.Select(l => l.ToCardDto()).ToList(), includeContact);
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

        await _uow.Listings.AddAsync(listing, ct);
        await _uow.SaveChangesAsync(ct);

        return await GetAsync(listing.Id, sellerId, ct);
    }

    public async Task<ListingDetailDto> UpdateAsync(Guid id, Guid userId, bool isModerator, UpdateListingRequest r, CancellationToken ct = default)
    {
        var listing = await _uow.Listings.FirstOrDefaultAsync(l => l.Id == id, ct)
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
        await _uow.SaveChangesAsync(ct);

        return await GetAsync(listing.Id, listing.SellerId, ct);
    }

    public async Task DeleteAsync(Guid id, Guid userId, bool isModerator, CancellationToken ct = default)
    {
        var listing = await _uow.Listings.FirstOrDefaultAsync(new ListingWithTransactionsSpec(id), ct)
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
            _uow.Listings.Remove(listing);
        }

        await _uow.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<GameSummaryDto>> GetGameSummariesAsync(CancellationToken ct = default)
    {
        var counts = await _uow.Listings.CountActiveByGameAsync(ct);

        return GameCatalog.All
            .Select(g => new GameSummaryDto(
                g.Slug, g.Name, g.Glyph, g.Color,
                counts.FirstOrDefault(c => c.Game == g.Type).Count))
            .ToList();
    }

    public async Task<CategoriesDto> GetCategoriesAsync(CancellationToken ct = default)
    {
        var typeCounts = await _uow.Listings.CountActiveByTypeAsync(ct);

        var types = ListingTypeCatalog.All
            .Select(t => new ListingTypeSummaryDto(
                t.Slug, t.Name, t.Glyph, t.Description,
                typeCounts.FirstOrDefault(c => c.Type == t.Type).Count))
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
