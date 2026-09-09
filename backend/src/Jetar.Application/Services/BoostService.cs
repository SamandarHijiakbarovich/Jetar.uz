using Jetar.Domain.Abstractions;
using Jetar.Domain.Common;
using Jetar.Application.Contracts;
using Jetar.Domain.Entities;
using Jetar.Domain.Enums;
using Jetar.Application.Interfaces;
using Jetar.Application.Options;
using Jetar.Domain.Specifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jetar.Application.Services;

/// <summary>
/// Pullik ko'tarish. Sotuvchi kartaga to'lov qilib so'rov yuboradi, admin qo'lda
/// tasdiqlaydi va e'lon <see cref="Listing.BoostedUntil"/> gacha tepada turadi.
/// Platforma o'z reklama xizmatini sotadi — begona pulni ushlab turmaydi.
/// </summary>
public class BoostService : IBoostService
{
    private readonly IUnitOfWork _uow;
    private readonly PlatformOptions _platform;
    private readonly IClock _clock;
    private readonly INotificationService _notifications;
    private readonly ILogger<BoostService> _log;

    public BoostService(
        IUnitOfWork uow,
        IOptions<PlatformOptions> platform,
        IClock clock,
        INotificationService notifications,
        ILogger<BoostService> log)
    {
        _uow = uow;
        _platform = platform.Value;
        _clock = clock;
        _notifications = notifications;
        _log = log;
    }

    public BoostConfigDto GetConfig()
    {
        var b = _platform.Boost;
        return new BoostConfigDto(
            b.CardNumber,
            b.CardHolder,
            b.Tiers.Select(t => new BoostTierDto(t.Days, t.Price, t.Label)).ToList());
    }

    public async Task<BoostRequestDto> RequestAsync(Guid userId, CreateBoostRequest request, CancellationToken ct = default)
    {
        var tier = _platform.Boost.Tiers.FirstOrDefault(t => t.Days == request.Days)
                   ?? throw new AppException("Bunday tarif mavjud emas.", 400, "invalid_tier");

        var listing = await _uow.Listings.GetByIdAsync(request.ListingId, ct)
                      ?? throw AppException.NotFound("E'lon");

        if (listing.SellerId != userId)
            throw AppException.Forbidden("Bu e'lon sizga tegishli emas.");

        if (listing.Status is ListingStatus.Sold or ListingStatus.Blocked)
            throw AppException.Conflict("Bu e'lonni ko'tarib bo'lmaydi.");

        var hasPending = await _uow.BoostRequests.AnyAsync(
            r => r.ListingId == listing.Id && r.Status == BoostStatus.Pending, ct);
        if (hasPending)
            throw AppException.Conflict("Bu e'lon bo'yicha ko'rib chiqilayotgan so'rov bor.");

        var boost = new BoostRequest
        {
            ListingId = listing.Id,
            UserId = userId,
            Days = tier.Days,
            Amount = tier.Price,
            ScreenshotUrl = string.IsNullOrWhiteSpace(request.ScreenshotUrl) ? null : request.ScreenshotUrl.Trim(),
            Status = BoostStatus.Pending,
            CreatedAt = _clock.UtcNow
        };

        await _uow.BoostRequests.AddAsync(boost, ct);
        await _uow.SaveChangesAsync(ct);

        _log.LogInformation("Boost so'rovi yuborildi listing={ListingId} kun={Days} summa={Amount}",
            listing.Id, tier.Days, tier.Price);

        boost.Listing = listing;
        return boost.ToDto();
    }

    public async Task<IReadOnlyList<BoostRequestDto>> GetMineAsync(Guid userId, CancellationToken ct = default)
    {
        var items = await _uow.BoostRequests.ListAsync(new MyBoostsSpec(userId), ct);
        return items.Select(b => b.ToDto()).ToList();
    }

    public async Task<IReadOnlyList<BoostRequestDto>> GetPendingAsync(CancellationToken ct = default)
    {
        var items = await _uow.BoostRequests.ListAsync(new PendingBoostsSpec(), ct);
        return items.Select(b => b.ToDto()).ToList();
    }

    public async Task<BoostRequestDto> ApproveAsync(Guid id, Guid moderatorId, CancellationToken ct = default)
    {
        var boost = await _uow.BoostRequests.FirstOrDefaultAsync(new BoostWithDetailsSpec(id), ct)
                    ?? throw AppException.NotFound("Boost so'rovi");

        if (boost.Status != BoostStatus.Pending)
            throw AppException.Conflict("Bu so'rov allaqachon ko'rib chiqilgan.");

        var listing = boost.Listing ?? throw AppException.NotFound("E'lon");

        // Agar e'lon hozir ham ko'tarilgan bo'lsa — muddatni cho'zamiz, aks holda hozirdan boshlaymiz.
        var start = listing.BoostedUntil is { } until && until > _clock.UtcNow ? until : _clock.UtcNow;
        listing.BoostedUntil = start.AddDays(boost.Days);
        listing.UpdatedAt = _clock.UtcNow;

        boost.Status = BoostStatus.Approved;
        boost.ReviewedById = moderatorId;
        boost.ReviewedAt = _clock.UtcNow;

        await _uow.SaveChangesAsync(ct);

        _log.LogInformation("Boost tasdiqlandi listing={ListingId} to={BoostedUntil}",
            listing.Id, listing.BoostedUntil);

        await _notifications.NotifyAsync(boost.UserId, "E'loningiz ko'tarildi",
            $"\"{listing.Title}\" e'loningiz {boost.Days} kunga TOP ga chiqarildi.", ct);

        return boost.ToDto();
    }

    public async Task<BoostRequestDto> RejectAsync(Guid id, Guid moderatorId, string? note, CancellationToken ct = default)
    {
        var boost = await _uow.BoostRequests.FirstOrDefaultAsync(new BoostWithDetailsSpec(id), ct)
                    ?? throw AppException.NotFound("Boost so'rovi");

        if (boost.Status != BoostStatus.Pending)
            throw AppException.Conflict("Bu so'rov allaqachon ko'rib chiqilgan.");

        boost.Status = BoostStatus.Rejected;
        boost.ReviewNote = note?.Trim();
        boost.ReviewedById = moderatorId;
        boost.ReviewedAt = _clock.UtcNow;

        await _uow.SaveChangesAsync(ct);

        await _notifications.NotifyAsync(boost.UserId, "Boost so'rovi rad etildi",
            note ?? "To'lov tasdiqlanmadi. Iltimos, chekni tekshirib qayta yuboring.", ct);

        return boost.ToDto();
    }
}
