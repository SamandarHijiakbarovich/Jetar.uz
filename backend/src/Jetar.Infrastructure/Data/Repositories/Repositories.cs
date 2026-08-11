using Jetar.Domain.Abstractions;
using Jetar.Domain.Entities;
using Jetar.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Jetar.Infrastructure.Data.Repositories;

// Aggregate'ga xos repository'lar. Hozircha generic amallarni meros oladi; maxsus
// so'rov metodlari kerak bo'lganda shu sinflarga qo'shiladi.

public class UserRepository : EfRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext db) : base(db) { }
}

public class ListingRepository : EfRepository<Listing>, IListingRepository
{
    public ListingRepository(AppDbContext db) : base(db) { }

    public async Task<IReadOnlyList<(GameType Game, int Count)>> CountActiveByGameAsync(CancellationToken ct = default)
        => await Set.AsNoTracking()
            .Where(l => l.Status == ListingStatus.Active)
            .GroupBy(l => l.GameType)
            .Select(g => new ValueTuple<GameType, int>(g.Key, g.Count()))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<(ListingType Type, int Count)>> CountActiveByTypeAsync(CancellationToken ct = default)
        => await Set.AsNoTracking()
            .Where(l => l.Status == ListingStatus.Active)
            .GroupBy(l => l.Type)
            .Select(g => new ValueTuple<ListingType, int>(g.Key, g.Count()))
            .ToListAsync(ct);
}

public class TransactionRepository : EfRepository<Transaction>, ITransactionRepository
{
    public TransactionRepository(AppDbContext db) : base(db) { }

    public async Task<decimal> SumCommissionAsync(
        System.Linq.Expressions.Expression<Func<Transaction, bool>> predicate, CancellationToken ct = default)
        => await Set.Where(predicate).SumAsync(t => (decimal?)t.CommissionAmount, ct) ?? 0m;

    public async Task<decimal> TotalEarnedAsync(Guid sellerId, CancellationToken ct = default)
        => await Set
            .Where(t => t.SellerId == sellerId && t.Status == TransactionStatus.Completed)
            .SumAsync(t => (decimal?)t.SellerPayout, ct) ?? 0m;
}

public class PaymentRepository : EfRepository<Payment>, IPaymentRepository
{
    public PaymentRepository(AppDbContext db) : base(db) { }
}

public class MessageRepository : EfRepository<Message>, IMessageRepository
{
    public MessageRepository(AppDbContext db) : base(db) { }

    public async Task<IReadOnlyList<Message>> ListThreadAsync(Guid transactionId, CancellationToken ct = default)
        => await Set
            .Include(m => m.Sender)
            .AsNoTracking()
            .Where(m => m.TransactionId == transactionId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Message>> ListUnreadAsync(Guid transactionId, Guid receiverId, CancellationToken ct = default)
        => await Set
            .Where(m => m.TransactionId == transactionId && m.ReceiverId == receiverId && !m.IsRead)
            .ToListAsync(ct);
}

public class RatingRepository : EfRepository<Rating>, IRatingRepository
{
    public RatingRepository(AppDbContext db) : base(db) { }

    public async Task<IReadOnlyList<Rating>> ListForUserAsync(Guid userId, int limit, CancellationToken ct = default)
        => await Set
            .Include(r => r.FromUser)
            .AsNoTracking()
            .Where(r => r.ToUserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<int>> ScoresForUserAsync(Guid userId, CancellationToken ct = default)
        => await Set.Where(r => r.ToUserId == userId).Select(r => (int)r.Score).ToListAsync(ct);

    public async Task<IReadOnlyList<Guid>> RatedTransactionIdsAsync(
        IReadOnlyCollection<Guid> transactionIds, Guid fromUserId, CancellationToken ct = default)
        => await Set
            .Where(r => transactionIds.Contains(r.TransactionId) && r.FromUserId == fromUserId)
            .Select(r => r.TransactionId)
            .ToListAsync(ct);
}

public class DisputeRepository : EfRepository<Dispute>, IDisputeRepository
{
    public DisputeRepository(AppDbContext db) : base(db) { }
}
