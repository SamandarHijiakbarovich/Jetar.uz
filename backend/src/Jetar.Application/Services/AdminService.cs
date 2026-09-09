using Jetar.Domain.Abstractions;
using Jetar.Domain.Common;
using Jetar.Application.Contracts;
using Jetar.Domain.Enums;
using Jetar.Application.Interfaces;
using Jetar.Application.Options;
using Jetar.Domain.Specifications;
using Microsoft.Extensions.Options;

namespace Jetar.Application.Services;

public class AdminService : IAdminService
{
    private readonly IUnitOfWork _uow;
    private readonly PlatformOptions _platform;
    private readonly IClock _clock;

    public AdminService(
        IUnitOfWork uow,
        IOptions<PlatformOptions> platform,
        IClock clock)
    {
        _uow = uow;
        _platform = platform.Value;
        _clock = clock;
    }

    public async Task<AdminStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var prevMonthStart = monthStart.AddMonths(-1);

        var totalUsers = await _uow.Users.CountAsync(ct);

        var activeListings = await _uow.Listings.CountAsync(l => l.Status == ListingStatus.Active, ct);
        var pendingListings = await _uow.Listings.CountAsync(l => l.Status == ListingStatus.Pending, ct);
        var totalListings = await _uow.Listings.CountAsync(ct);

        var boostsPending = await _uow.BoostRequests.CountAsync(b => b.Status == BoostStatus.Pending, ct);
        var boostsApproved = await _uow.BoostRequests.CountAsync(b => b.Status == BoostStatus.Approved, ct);

        // Platforma daromadi — tasdiqlangan ko'tarishlar (o'z reklama xizmati).
        var boostRevenue = await _uow.BoostRequests.SumApprovedAmountAsync(null, ct);
        var revThisMonth = await _uow.BoostRequests.SumApprovedAmountAsync(monthStart, ct);
        var revPrevMonth = boostRevenue - revThisMonth; // taxminiy: oldingi barcha davr

        var usersThisMonth = await _uow.Users.CountAsync(u => u.CreatedAt >= monthStart, ct);
        var usersPrevMonth = await _uow.Users.CountAsync(u => u.CreatedAt >= prevMonthStart && u.CreatedAt < monthStart, ct);

        var listingsThisMonth = await _uow.Listings.CountAsync(l => l.CreatedAt >= monthStart, ct);
        var listingsPrevMonth = await _uow.Listings.CountAsync(l => l.CreatedAt >= prevMonthStart && l.CreatedAt < monthStart, ct);

        return new AdminStatsDto(
            totalUsers,
            activeListings,
            pendingListings,
            totalListings,
            boostsPending,
            boostsApproved,
            boostRevenue,
            Growth(usersThisMonth, usersPrevMonth),
            Growth(listingsThisMonth, listingsPrevMonth),
            Growth((double)revThisMonth, (double)revPrevMonth));
    }

    public async Task<PagedResult<AdminTransactionRowDto>> GetTransactionsAsync(string? search, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var spec = new AdminTransactionSearchSpec(search, page, pageSize);

        var total = await _uow.Transactions.CountAsync(spec, ct);
        var items = await _uow.Transactions.ListAsync(spec, ct);

        var rows = items.Select(t => new AdminTransactionRowDto(
            t.Id,
            t.Number,
            t.Buyer!.Username,
            t.Seller!.Username,
            t.Amount,
            t.Status,
            StatusLabels.Get(t.Status),
            t.CreatedAt)).ToList();

        return PagedResult<AdminTransactionRowDto>.Create(rows, page, pageSize, total);
    }

    public async Task<IReadOnlyList<AdminDisputeRowDto>> GetDisputesAsync(bool openOnly, CancellationToken ct = default)
    {
        var disputes = await _uow.Disputes.ListAsync(new DisputeListSpec(openOnly), ct);
        var now = _clock.UtcNow;

        return disputes.Select(d => new AdminDisputeRowDto(
            d.Id,
            d.TransactionId,
            d.Transaction?.Number ?? 0,
            d.Reason,
            d.Status,
            Math.Round((now - d.CreatedAt).TotalHours, 1),
            d.CreatedAt)).ToList();
    }

    public Task<PlatformSettingsDto> GetSettingsAsync(CancellationToken ct = default)
        => Task.FromResult(new PlatformSettingsDto(
            _platform.EscrowEnabled,
            _platform.ContactRequiresLogin,
            _platform.MinListingPrice,
            _platform.MaxListingPrice,
            _platform.AutoApproveListings,
            _platform.Boost.CardNumber,
            _platform.Boost.Tiers.Count));

    public async Task<PagedResult<AdminUserRowDto>> GetUsersAsync(
        string? search, UserRole? role, bool? blocked, bool? verified, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var spec = new AdminUserSearchSpec(search, role, blocked, verified, page, pageSize);

        var total = await _uow.Users.CountAsync(spec, ct);
        var users = await _uow.Users.ListAsync(spec, ct);

        var rows = users.Select(u => new AdminUserRowDto(
            u.Id,
            u.Username,
            $"{u.FirstName} {u.LastName}".Trim(),
            u.Phone,
            u.Role,
            u.Rating,
            u.RatingCount,
            u.TotalSales,
            u.TotalPurchases,
            u.IsVerified,
            u.IsBlocked,
            u.CreatedAt)).ToList();

        return PagedResult<AdminUserRowDto>.Create(rows, page, pageSize, total);
    }

    public async Task SetUserBlockedAsync(Guid userId, bool blocked, CancellationToken ct = default)
    {
        var user = await _uow.Users.GetByIdAsync(userId, ct)
                   ?? throw AppException.NotFound("Foydalanuvchi");

        user.IsBlocked = blocked;
        await _uow.SaveChangesAsync(ct);
    }

    public async Task SetUserVerifiedAsync(Guid userId, bool verified, CancellationToken ct = default)
    {
        var user = await _uow.Users.GetByIdAsync(userId, ct)
                   ?? throw AppException.NotFound("Foydalanuvchi");

        user.IsVerified = verified;
        await _uow.SaveChangesAsync(ct);
    }

    public async Task SetUserRoleAsync(Guid userId, UserRole role, CancellationToken ct = default)
    {
        var user = await _uow.Users.GetByIdAsync(userId, ct)
                   ?? throw AppException.NotFound("Foydalanuvchi");

        user.Role = role;
        await _uow.SaveChangesAsync(ct);
    }

    public async Task SetListingVerifiedAsync(Guid listingId, bool verified, CancellationToken ct = default)
    {
        var listing = await _uow.Listings.GetByIdAsync(listingId, ct)
                      ?? throw AppException.NotFound("E'lon");

        listing.IsVerified = verified;
        if (verified && listing.Status == ListingStatus.Pending) listing.Status = ListingStatus.Active;

        listing.UpdatedAt = _clock.UtcNow;
        await _uow.SaveChangesAsync(ct);
    }

    private static double Growth(double current, double previous)
    {
        if (previous <= 0) return current > 0 ? 100 : 0;
        return Math.Round((current - previous) / previous * 100, 1);
    }
}
