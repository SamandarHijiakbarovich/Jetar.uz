using System.Linq.Expressions;
using Jetar.Domain.Abstractions;
using Jetar.Domain.Common;
using Jetar.Application.Contracts;
using Jetar.Domain.Entities;
using Jetar.Application.Interfaces;
using Jetar.Domain.Specifications;

namespace Jetar.Application.Services;

public class TransactionService : ITransactionService
{
    private readonly IUnitOfWork _uow;

    public TransactionService(IUnitOfWork uow) => _uow = uow;

    public async Task<TransactionDto> GetAsync(Guid id, Guid viewerId, bool isModerator, CancellationToken ct = default)
    {
        var tx = await _uow.Transactions.FirstOrDefaultAsync(new TransactionWithDetailsSpec(id), ct)
                 ?? throw AppException.NotFound("Bitim");

        if (tx.BuyerId != viewerId && tx.SellerId != viewerId && !isModerator)
            throw AppException.Forbidden("Bu bitim sizga tegishli emas.");

        var rated = await _uow.Ratings.AnyAsync(r => r.TransactionId == id && r.FromUserId == viewerId, ct);
        return tx.ToDto(viewerId, rated);
    }

    public async Task<PagedResult<TransactionDto>> ListForUserAsync(
        Guid userId, string? role, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        Expression<Func<Transaction, bool>> filter = role?.ToLowerInvariant() switch
        {
            "buyer" => t => t.BuyerId == userId,
            "seller" => t => t.SellerId == userId,
            _ => t => t.BuyerId == userId || t.SellerId == userId
        };

        var total = await _uow.Transactions.CountAsync(filter, ct);

        var items = await _uow.Transactions.ListAsync(new TransactionWithDetailsSpec(filter, page, pageSize), ct);

        var ids = items.Select(t => t.Id).ToList();
        var ratedIds = await _uow.Ratings.RatedTransactionIdsAsync(ids, userId, ct);

        var dtos = items.Select(t => t.ToDto(userId, ratedIds.Contains(t.Id))).ToList();
        return PagedResult<TransactionDto>.Create(dtos, page, pageSize, total);
    }
}
