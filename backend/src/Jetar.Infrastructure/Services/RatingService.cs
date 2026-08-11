using Jetar.Core.Common;
using Jetar.Core.Contracts;
using Jetar.Core.Entities;
using Jetar.Core.Enums;
using Jetar.Core.Interfaces;
using Jetar.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jetar.Infrastructure.Services;

public class RatingService : IRatingService
{
    private readonly AppDbContext _db;
    private readonly IClock _clock;

    public RatingService(AppDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<RatingDto> CreateAsync(Guid transactionId, Guid fromUserId, CreateRatingRequest request, CancellationToken ct = default)
    {
        if (request.Score is < 1 or > 5)
            throw new AppException("Baho 1 dan 5 gacha bo'lishi kerak.", 400, "invalid_score");

        var tx = await _db.Transactions.FirstOrDefaultAsync(t => t.Id == transactionId, ct)
                 ?? throw AppException.NotFound("Bitim");

        if (tx.BuyerId != fromUserId && tx.SellerId != fromUserId)
            throw AppException.Forbidden("Bu bitim sizga tegishli emas.");

        if (tx.Status != TransactionStatus.Completed)
            throw AppException.Conflict("Baho faqat yakunlangan bitimga qoldiriladi.");

        if (await _db.Ratings.AnyAsync(r => r.TransactionId == transactionId && r.FromUserId == fromUserId, ct))
            throw AppException.Conflict("Siz bu bitimga allaqachon baho qoldirgansiz.");

        var toUserId = tx.BuyerId == fromUserId ? tx.SellerId : tx.BuyerId;

        var rating = new Rating
        {
            TransactionId = transactionId,
            FromUserId = fromUserId,
            ToUserId = toUserId,
            Score = request.Score,
            Comment = request.Comment?.Trim(),
            CreatedAt = _clock.UtcNow
        };

        _db.Ratings.Add(rating);
        await _db.SaveChangesAsync(ct);

        await RecalculateAsync(toUserId, ct);

        rating.FromUser = await _db.Users.FindAsync(new object?[] { fromUserId }, ct);
        return rating.ToDto();
    }

    public async Task<IReadOnlyList<RatingDto>> GetForUserAsync(Guid userId, int limit, CancellationToken ct = default)
    {
        var ratings = await _db.Ratings
            .Include(r => r.FromUser)
            .AsNoTracking()
            .Where(r => r.ToUserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync(ct);

        return ratings.Select(r => r.ToDto()).ToList();
    }

    /// <summary>Foydalanuvchining o'rtacha reytingini qayta hisoblaydi.</summary>
    private async Task RecalculateAsync(Guid userId, CancellationToken ct)
    {
        var scores = await _db.Ratings.Where(r => r.ToUserId == userId).Select(r => (int)r.Score).ToListAsync(ct);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null) return;

        user.RatingCount = scores.Count;
        user.Rating = scores.Count == 0 ? 0 : Math.Round((decimal)scores.Average(), 2);

        await _db.SaveChangesAsync(ct);
    }
}
