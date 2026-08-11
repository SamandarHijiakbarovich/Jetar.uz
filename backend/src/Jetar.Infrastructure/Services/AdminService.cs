using Jetar.Core.Common;
using Jetar.Core.Contracts;
using Jetar.Core.Enums;
using Jetar.Core.Interfaces;
using Jetar.Core.Options;
using Jetar.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Jetar.Infrastructure.Services;

public class AdminService : IAdminService
{
    private readonly AppDbContext _db;
    private readonly PlatformOptions _platform;
    private readonly PaymentOptions _payments;
    private readonly IClock _clock;

    public AdminService(
        AppDbContext db,
        IOptions<PlatformOptions> platform,
        IOptions<PaymentOptions> payments,
        IClock clock)
    {
        _db = db;
        _platform = platform.Value;
        _payments = payments.Value;
        _clock = clock;
    }

    public async Task<AdminStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var prevMonthStart = monthStart.AddMonths(-1);

        var totalUsers = await _db.Users.CountAsync(ct);
        var totalTransactions = await _db.Transactions.CountAsync(ct);

        var completed = _db.Transactions.Where(t => t.Status == TransactionStatus.Completed);

        // Platforma daromadi — ushlangan komissiyalar yig'indisi.
        var totalRevenue = await completed.SumAsync(t => (decimal?)t.CommissionAmount, ct) ?? 0m;

        var openDisputes = await _db.Disputes
            .CountAsync(d => d.Status == DisputeStatus.Open || d.Status == DisputeStatus.UnderReview, ct);

        var over24hCutoff = now.AddHours(-24);
        var disputesOver24h = await _db.Disputes
            .CountAsync(d => (d.Status == DisputeStatus.Open || d.Status == DisputeStatus.UnderReview)
                             && d.CreatedAt <= over24hCutoff, ct);

        var usersThisMonth = await _db.Users.CountAsync(u => u.CreatedAt >= monthStart, ct);
        var usersPrevMonth = await _db.Users.CountAsync(u => u.CreatedAt >= prevMonthStart && u.CreatedAt < monthStart, ct);

        var txThisMonth = await _db.Transactions.CountAsync(t => t.CreatedAt >= monthStart, ct);
        var txPrevMonth = await _db.Transactions.CountAsync(t => t.CreatedAt >= prevMonthStart && t.CreatedAt < monthStart, ct);

        var revThisMonth = await completed.Where(t => t.CompletedAt >= monthStart)
            .SumAsync(t => (decimal?)t.CommissionAmount, ct) ?? 0m;
        var revPrevMonth = await completed.Where(t => t.CompletedAt >= prevMonthStart && t.CompletedAt < monthStart)
            .SumAsync(t => (decimal?)t.CommissionAmount, ct) ?? 0m;

        return new AdminStatsDto(
            totalUsers,
            totalTransactions,
            totalRevenue,
            openDisputes,
            disputesOver24h,
            Growth(usersThisMonth, usersPrevMonth),
            Growth(txThisMonth, txPrevMonth),
            Growth((double)revThisMonth, (double)revPrevMonth),
            await _db.Listings.CountAsync(l => l.Status == ListingStatus.Active, ct),
            await _db.Listings.CountAsync(l => l.Status == ListingStatus.Pending, ct));
    }

    public async Task<PagedResult<AdminTransactionRowDto>> GetTransactionsAsync(string? search, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Transactions
            .Include(t => t.Buyer)
            .Include(t => t.Seller)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().TrimStart('#', '@').ToLowerInvariant();
            var asNumber = int.TryParse(term, out var num) ? num : (int?)null;

            query = query.Where(t =>
                (asNumber != null && t.Number == asNumber) ||
                t.EscrowCode.ToLower().Contains(term) ||
                t.Buyer!.Username.Contains(term) ||
                t.Seller!.Username.Contains(term));
        }

        var total = await query.CountAsync(ct);

        var rows = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new AdminTransactionRowDto(
                t.Id,
                t.Number,
                t.Buyer!.Username,
                t.Seller!.Username,
                t.Amount,
                t.Status,
                "",
                t.CreatedAt))
            .ToListAsync(ct);

        var labelled = rows.Select(r => r with { StatusLabel = StatusLabels.Get(r.Status) }).ToList();
        return PagedResult<AdminTransactionRowDto>.Create(labelled, page, pageSize, total);
    }

    public async Task<IReadOnlyList<AdminDisputeRowDto>> GetDisputesAsync(bool openOnly, CancellationToken ct = default)
    {
        var query = _db.Disputes.Include(d => d.Transaction).AsNoTracking().AsQueryable();

        if (openOnly)
            query = query.Where(d => d.Status == DisputeStatus.Open || d.Status == DisputeStatus.UnderReview);

        var disputes = await query.OrderBy(d => d.CreatedAt).ToListAsync(ct);
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
            _platform.CommissionRate,
            _platform.AutoReleaseHours,
            _platform.MinListingPrice,
            _platform.MaxListingPrice,
            _platform.AutoApproveListings,
            _payments.SandboxMode));

    public async Task SetUserBlockedAsync(Guid userId, bool blocked, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw AppException.NotFound("Foydalanuvchi");

        user.IsBlocked = blocked;
        await _db.SaveChangesAsync(ct);
    }

    public async Task SetListingVerifiedAsync(Guid listingId, bool verified, CancellationToken ct = default)
    {
        var listing = await _db.Listings.FirstOrDefaultAsync(l => l.Id == listingId, ct)
                      ?? throw AppException.NotFound("E'lon");

        listing.IsVerified = verified;
        if (verified && listing.Status == ListingStatus.Pending) listing.Status = ListingStatus.Active;

        listing.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private static double Growth(double current, double previous)
    {
        if (previous <= 0) return current > 0 ? 100 : 0;
        return Math.Round((current - previous) / previous * 100, 1);
    }
}
