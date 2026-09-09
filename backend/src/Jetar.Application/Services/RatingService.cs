using Jetar.Domain.Abstractions;
using Jetar.Domain.Common;
using Jetar.Application.Contracts;
using Jetar.Domain.Entities;
using Jetar.Domain.Enums;
using Jetar.Application.Interfaces;

namespace Jetar.Application.Services;

public class RatingService : IRatingService
{
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;

    public RatingService(IUnitOfWork uow, IClock clock)
    {
        _uow = uow;
        _clock = clock;
    }

    public async Task<RatingDto> CreateAsync(Guid transactionId, Guid fromUserId, CreateRatingRequest request, CancellationToken ct = default)
    {
        if (request.Score is < 1 or > 5)
            throw new AppException("Baho 1 dan 5 gacha bo'lishi kerak.", 400, "invalid_score");

        var tx = await _uow.Transactions.GetByIdAsync(transactionId, ct)
                 ?? throw AppException.NotFound("Bitim");

        if (tx.BuyerId != fromUserId && tx.SellerId != fromUserId)
            throw AppException.Forbidden("Bu bitim sizga tegishli emas.");

        if (tx.Status != TransactionStatus.Completed)
            throw AppException.Conflict("Baho faqat yakunlangan bitimga qoldiriladi.");

        if (await _uow.Ratings.AnyAsync(r => r.TransactionId == transactionId && r.FromUserId == fromUserId, ct))
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

        await _uow.Ratings.AddAsync(rating, ct);
        await _uow.SaveChangesAsync(ct);

        await RecalculateAsync(toUserId, ct);

        rating.FromUser = await _uow.Users.GetByIdAsync(fromUserId, ct);
        return rating.ToDto();
    }

    public async Task<RatingDto> RateUserAsync(Guid toUserId, Guid fromUserId, CreateRatingRequest request, CancellationToken ct = default)
    {
        if (request.Score is < 1 or > 5)
            throw new AppException("Baho 1 dan 5 gacha bo'lishi kerak.", 400, "invalid_score");

        if (toUserId == fromUserId)
            throw new AppException("O'zingizga baho qo'yolmaysiz.", 400, "self_rating");

        var target = await _uow.Users.GetByIdAsync(toUserId, ct)
                     ?? throw AppException.NotFound("Foydalanuvchi");

        // To'g'ridan-to'g'ri baho: bir foydalanuvchi bir sotuvchiga bir marta.
        if (await _uow.Ratings.AnyAsync(
                r => r.ToUserId == toUserId && r.FromUserId == fromUserId && r.TransactionId == null, ct))
            throw AppException.Conflict("Siz bu sotuvchiga allaqachon baho qoldirgansiz.");

        var rating = new Rating
        {
            TransactionId = null,
            FromUserId = fromUserId,
            ToUserId = toUserId,
            Score = request.Score,
            Comment = request.Comment?.Trim(),
            CreatedAt = _clock.UtcNow
        };

        await _uow.Ratings.AddAsync(rating, ct);
        await _uow.SaveChangesAsync(ct);

        await RecalculateAsync(toUserId, ct);

        rating.FromUser = await _uow.Users.GetByIdAsync(fromUserId, ct);
        return rating.ToDto();
    }

    public async Task<IReadOnlyList<RatingDto>> GetForUserAsync(Guid userId, int limit, CancellationToken ct = default)
    {
        var ratings = await _uow.Ratings.ListForUserAsync(userId, limit, ct);
        return ratings.Select(r => r.ToDto()).ToList();
    }

    /// <summary>Foydalanuvchining o'rtacha reytingini qayta hisoblaydi.</summary>
    private async Task RecalculateAsync(Guid userId, CancellationToken ct)
    {
        var scores = await _uow.Ratings.ScoresForUserAsync(userId, ct);

        var user = await _uow.Users.GetByIdAsync(userId, ct);
        if (user == null) return;

        user.RatingCount = scores.Count;
        user.Rating = scores.Count == 0 ? 0 : Math.Round((decimal)scores.Average(), 2);

        await _uow.SaveChangesAsync(ct);
    }
}
