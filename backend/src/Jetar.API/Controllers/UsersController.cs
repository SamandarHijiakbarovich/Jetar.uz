using Jetar.Core.Common;
using Jetar.Core.Contracts;
using Jetar.Core.Interfaces;
using Jetar.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jetar.API.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IRatingService _ratings;
    private readonly IListingService _listings;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;

    public UsersController(
        AppDbContext db,
        IRatingService ratings,
        IListingService listings,
        ICurrentUser user,
        IClock clock)
    {
        _db = db;
        _ratings = ratings;
        _listings = listings;
        _user = user;
        _clock = clock;
    }

    /// <summary>Ommaviy profil.</summary>
    [HttpGet("{username}")]
    public async Task<ActionResult<object>> Profile(string username, CancellationToken ct)
    {
        var normalized = username.TrimStart('@').ToLowerInvariant();

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == normalized, ct)
                   ?? throw AppException.NotFound("Foydalanuvchi");

        var listings = await _listings.SearchAsync(
            new ListingQuery { SellerId = user.Id, PageSize = 24 }, ct);

        var ratings = await _ratings.GetForUserAsync(user.Id, 10, ct);

        return Ok(new
        {
            user = AuthController.Map(user),
            listings = listings.Items,
            ratings
        });
    }

    /// <summary>Mening e'lonlarim — barcha holatlar bilan.</summary>
    [HttpGet("me/listings")]
    [Authorize]
    public async Task<ActionResult<PagedResult<ListingCardDto>>> MyListings(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 24, CancellationToken ct = default)
        => Ok(await _listings.SearchAsync(
            new ListingQuery { SellerId = _user.RequireId(), Page = page, PageSize = pageSize }, ct));

    [HttpPatch("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> UpdateProfile(UpdateProfileRequest request, CancellationToken ct)
    {
        var id = _user.RequireId();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
                   ?? throw AppException.NotFound("Foydalanuvchi");

        if (!string.IsNullOrWhiteSpace(request.FirstName)) user.FirstName = request.FirstName.Trim();
        if (!string.IsNullOrWhiteSpace(request.LastName)) user.LastName = request.LastName.Trim();

        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            var newName = request.Username.Trim().TrimStart('@').ToLowerInvariant();

            if (newName != user.Username && await _db.Users.AnyAsync(u => u.Username == newName, ct))
                throw AppException.Conflict("Bu foydalanuvchi nomi band.");

            user.Username = newName;
        }

        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            var phone = Jetar.Infrastructure.Services.AuthService.NormalizePhone(request.Phone);

            if (phone != user.Phone && await _db.Users.AnyAsync(u => u.Phone == phone, ct))
                throw AppException.Conflict("Bu telefon raqami band.");

            user.Phone = phone;
        }

        if (request.TelegramUsername != null)
            user.TelegramUsername = string.IsNullOrWhiteSpace(request.TelegramUsername)
                ? null
                : "@" + request.TelegramUsername.Trim().TrimStart('@');

        if (request.City != null) user.City = request.City.Trim();

        await _db.SaveChangesAsync(ct);
        return Ok(AuthController.Map(user));
    }

    [HttpPatch("me/notifications")]
    [Authorize]
    public async Task<ActionResult<UserDto>> UpdateNotifications(NotificationSettingsRequest request, CancellationToken ct)
    {
        var id = _user.RequireId();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
                   ?? throw AppException.NotFound("Foydalanuvchi");

        user.NotifyTelegram = request.NotifyTelegram;
        user.NotifyNewMessage = request.NotifyNewMessage;
        user.NotifyMarketing = request.NotifyMarketing;

        await _db.SaveChangesAsync(ct);
        return Ok(AuthController.Map(user));
    }

    /// <summary>Profil sahifasidagi umumiy ko'rsatkichlar.</summary>
    [HttpGet("me/summary")]
    [Authorize]
    public async Task<ActionResult<object>> Summary(CancellationToken ct)
    {
        var id = _user.RequireId();

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct)
                   ?? throw AppException.NotFound("Foydalanuvchi");

        var activeListings = await _db.Listings
            .CountAsync(l => l.SellerId == id && l.Status == Core.Enums.ListingStatus.Active, ct);

        var openTransactions = await _db.Transactions
            .CountAsync(t => (t.BuyerId == id || t.SellerId == id)
                             && t.Status != Core.Enums.TransactionStatus.Completed
                             && t.Status != Core.Enums.TransactionStatus.Cancelled
                             && t.Status != Core.Enums.TransactionStatus.Refunded, ct);

        var earned = await _db.Transactions
            .Where(t => t.SellerId == id && t.Status == Core.Enums.TransactionStatus.Completed)
            .SumAsync(t => (decimal?)t.SellerPayout, ct) ?? 0m;

        var unread = await _db.Messages.CountAsync(m => m.ReceiverId == id && !m.IsRead, ct);

        return Ok(new
        {
            user = AuthController.Map(user),
            activeListings,
            openTransactions,
            totalEarned = earned,
            unreadMessages = unread,
            memberForDays = (int)(_clock.UtcNow - user.CreatedAt).TotalDays
        });
    }
}
