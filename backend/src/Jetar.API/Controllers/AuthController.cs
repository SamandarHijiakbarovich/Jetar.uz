using Jetar.Core.Contracts;
using Jetar.Core.Interfaces;
using Jetar.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jetar.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly ICurrentUser _user;
    private readonly AppDbContext _db;

    public AuthController(IAuthService auth, ICurrentUser user, AppDbContext db)
    {
        _auth = auth;
        _user = user;
        _db = db;
    }

    /// <summary>Ro'yxatdan o'tish.</summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct)
        => Ok(await _auth.RegisterAsync(request, ct));

    /// <summary>Kirish — login sifatida foydalanuvchi nomi yoki telefon raqami.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
        => Ok(await _auth.LoginAsync(request, ct));

    /// <summary>Access tokenni yangilash.</summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest request, CancellationToken ct)
        => Ok(await _auth.RefreshAsync(request.RefreshToken, ct));

    /// <summary>Joriy foydalanuvchi ma'lumotlari.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me(CancellationToken ct)
    {
        var id = _user.RequireId();
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);

        // Token yaroqli, lekin hisob o'chirilgan — klient uni chiqib ketgan deb qabul qilsin.
        return user == null
            ? Unauthorized(new { code = "unauthorized", message = "Hisob topilmadi. Qaytadan kiring." })
            : Ok(Map(user));
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        await _auth.ChangePasswordAsync(_user.RequireId(), request, ct);
        return NoContent();
    }

    internal static UserDto Map(Core.Entities.User u) => new(
        u.Id, u.Username, u.FirstName, u.LastName, $"{u.FirstName} {u.LastName}".Trim(),
        u.Phone, u.TelegramUsername, u.City, u.AvatarUrl,
        u.Rating, u.RatingCount, u.TotalSales, u.TotalPurchases,
        u.IsVerified, u.Role, u.AvgResponseMinutes, u.CreatedAt);
}
