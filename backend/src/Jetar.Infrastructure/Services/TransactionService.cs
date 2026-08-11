using Jetar.Core.Common;
using Jetar.Core.Contracts;
using Jetar.Core.Interfaces;
using Jetar.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jetar.Infrastructure.Services;

public class TransactionService : ITransactionService
{
    private readonly AppDbContext _db;

    public TransactionService(AppDbContext db) => _db = db;

    public async Task<TransactionDto> GetAsync(Guid id, Guid viewerId, bool isModerator, CancellationToken ct = default)
    {
        var tx = await BaseQuery().FirstOrDefaultAsync(t => t.Id == id, ct)
                 ?? throw AppException.NotFound("Bitim");

        if (tx.BuyerId != viewerId && tx.SellerId != viewerId && !isModerator)
            throw AppException.Forbidden("Bu bitim sizga tegishli emas.");

        var rated = await _db.Ratings.AnyAsync(r => r.TransactionId == id && r.FromUserId == viewerId, ct);
        return tx.ToDto(viewerId, rated);
    }

    public async Task<PagedResult<TransactionDto>> ListForUserAsync(
        Guid userId, string? role, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = BaseQuery().AsNoTracking();

        query = role?.ToLowerInvariant() switch
        {
            "buyer" => query.Where(t => t.BuyerId == userId),
            "seller" => query.Where(t => t.SellerId == userId),
            _ => query.Where(t => t.BuyerId == userId || t.SellerId == userId)
        };

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var ids = items.Select(t => t.Id).ToList();
        var ratedIds = await _db.Ratings
            .Where(r => ids.Contains(r.TransactionId) && r.FromUserId == userId)
            .Select(r => r.TransactionId)
            .ToListAsync(ct);

        var dtos = items.Select(t => t.ToDto(userId, ratedIds.Contains(t.Id))).ToList();
        return PagedResult<TransactionDto>.Create(dtos, page, pageSize, total);
    }

    private IQueryable<Core.Entities.Transaction> BaseQuery() =>
        _db.Transactions
            .Include(t => t.Listing)!.ThenInclude(l => l!.Seller)
            .Include(t => t.Buyer)
            .Include(t => t.Seller)
            .Include(t => t.Dispute);
}
