using Jetar.Application.Contracts;
using Jetar.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jetar.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly ICurrentUser _user;
    private readonly IUserService _users;

    public AuthController(IAuthService auth, ICurrentUser user, IUserService users)
    {
        _auth = auth;
        _user = user;
        _users = users;
    }

    /// <summary>Ro'yxatdan o'tish — 1-bosqich: emailga tasdiqlash kodi yuboriladi.</summary>
    [HttpPost("register")]
    public async Task<ActionResult<RegistrationStartResponse>> Register(RegisterRequest request, CancellationToken ct)
        => Ok(await _auth.StartRegistrationAsync(request, ct));

    /// <summary>Ro'yxatdan o'tish — 2-bosqich: kodни tasdiqlash, akkaunt yaratiladi.</summary>
    [HttpPost("verify-email")]
    public async Task<ActionResult<AuthResponse>> VerifyEmail(VerifyEmailRequest request, CancellationToken ct)
        => Ok(await _auth.VerifyEmailAsync(request, ct));

    /// <summary>Tasdiqlash kodini qayta yuborish.</summary>
    [HttpPost("resend-code")]
    public async Task<ActionResult<RegistrationStartResponse>> ResendCode(ResendCodeRequest request, CancellationToken ct)
        => Ok(await _auth.ResendCodeAsync(request, ct));

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
        var user = await _users.GetByIdAsync(_user.RequireId(), ct);

        // Token yaroqli, lekin hisob o'chirilgan — klient uni chiqib ketgan deb qabul qilsin.
        return user == null
            ? Unauthorized(new { code = "unauthorized", message = "Hisob topilmadi. Qaytadan kiring." })
            : Ok(user);
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        await _auth.ChangePasswordAsync(_user.RequireId(), request, ct);
        return NoContent();
    }
}
