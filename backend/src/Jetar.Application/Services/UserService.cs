using Jetar.Domain.Abstractions;
using Jetar.Domain.Common;
using Jetar.Domain.Enums;
using Jetar.Application.Contracts;
using Jetar.Application.Interfaces;

namespace Jetar.Application.Services;

/// <summary>
/// Foydalanuvchi profili bilan bog'liq amallar. Ilgari kontroller <c>DbContext</c>ni
/// bevosita ishlatardi — endi barcha ma'lumot repository orqali shu servisda.
/// </summary>
public class UserService : IUserService
{
    private readonly IUnitOfWork _uow;
    private readonly IListingService _listings;
    private readonly IRatingService _ratings;
    private readonly IClock _clock;

    public UserService(IUnitOfWork uow, IListingService listings, IRatingService ratings, IClock clock)
    {
        _uow = uow;
        _listings = listings;
        _ratings = ratings;
        _clock = clock;
    }

    public async Task<UserDto?> GetByIdAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _uow.Users.GetByIdAsync(userId, ct);
        return user?.ToDto();
    }

    public async Task<PublicProfileDto> GetPublicProfileAsync(string username, CancellationToken ct = default)
    {
        var normalized = username.TrimStart('@').ToLowerInvariant();

        var user = await _uow.Users.FirstOrDefaultAsync(u => u.Username == normalized, ct)
                   ?? throw AppException.NotFound("Foydalanuvchi");

        var listings = await _listings.SearchAsync(new ListingQuery { SellerId = user.Id, PageSize = 24 }, ct);
        var ratings = await _ratings.GetForUserAsync(user.Id, 10, ct);

        return new PublicProfileDto(user.ToDto(), listings.Items, ratings);
    }

    public async Task<UserDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await _uow.Users.GetByIdAsync(userId, ct)
                   ?? throw AppException.NotFound("Foydalanuvchi");

        if (!string.IsNullOrWhiteSpace(request.FirstName)) user.FirstName = request.FirstName.Trim();
        if (!string.IsNullOrWhiteSpace(request.LastName)) user.LastName = request.LastName.Trim();

        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            var newName = request.Username.Trim().TrimStart('@').ToLowerInvariant();

            if (newName != user.Username && await _uow.Users.AnyAsync(u => u.Username == newName, ct))
                throw AppException.Conflict("Bu foydalanuvchi nomi band.");

            user.Username = newName;
        }

        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            var phone = AuthService.NormalizePhone(request.Phone);

            if (phone != user.Phone && await _uow.Users.AnyAsync(u => u.Phone == phone, ct))
                throw AppException.Conflict("Bu telefon raqami band.");

            user.Phone = phone;
        }

        if (request.TelegramUsername != null)
            user.TelegramUsername = string.IsNullOrWhiteSpace(request.TelegramUsername)
                ? null
                : "@" + request.TelegramUsername.Trim().TrimStart('@');

        if (request.City != null) user.City = request.City.Trim();

        await _uow.SaveChangesAsync(ct);
        return user.ToDto();
    }

    public async Task<UserDto> UpdateNotificationsAsync(Guid userId, NotificationSettingsRequest request, CancellationToken ct = default)
    {
        var user = await _uow.Users.GetByIdAsync(userId, ct)
                   ?? throw AppException.NotFound("Foydalanuvchi");

        user.NotifyTelegram = request.NotifyTelegram;
        user.NotifyNewMessage = request.NotifyNewMessage;
        user.NotifyMarketing = request.NotifyMarketing;

        await _uow.SaveChangesAsync(ct);
        return user.ToDto();
    }

    public async Task<UserSummaryDto> GetSummaryAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _uow.Users.GetByIdAsync(userId, ct)
                   ?? throw AppException.NotFound("Foydalanuvchi");

        var activeListings = await _uow.Listings.CountAsync(
            l => l.SellerId == userId && l.Status == ListingStatus.Active, ct);

        var openTransactions = await _uow.Transactions.CountAsync(
            t => (t.BuyerId == userId || t.SellerId == userId)
                 && t.Status != TransactionStatus.Completed
                 && t.Status != TransactionStatus.Cancelled
                 && t.Status != TransactionStatus.Refunded, ct);

        var earned = await _uow.Transactions.TotalEarnedAsync(userId, ct);

        var unread = await _uow.Messages.CountAsync(m => m.ReceiverId == userId && !m.IsRead, ct);

        return new UserSummaryDto(
            user.ToDto(),
            activeListings,
            openTransactions,
            earned,
            unread,
            (int)(_clock.UtcNow - user.CreatedAt).TotalDays);
    }
}
