using System.Text.RegularExpressions;
using Jetar.Core.Common;
using Jetar.Core.Contracts;
using Jetar.Core.Entities;
using Jetar.Core.Interfaces;
using Jetar.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jetar.Infrastructure.Services;

public partial class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokens;
    private readonly IClock _clock;

    public AuthService(AppDbContext db, ITokenService tokens, IClock clock)
    {
        _db = db;
        _tokens = tokens;
        _clock = clock;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var username = request.Username.Trim().TrimStart('@').ToLowerInvariant();
        var phone = NormalizePhone(request.Phone);
        var firstName = request.FirstName?.Trim() ?? string.Empty;
        var lastName = request.LastName?.Trim() ?? string.Empty;

        if (!PersonNameRegex().IsMatch(firstName))
            throw new AppException("Ismni to'g'ri kiriting (2–50 harf).", 400, "invalid_first_name");

        if (!PersonNameRegex().IsMatch(lastName))
            throw new AppException("Familiyani to'g'ri kiriting (2–50 harf).", 400, "invalid_last_name");

        if (!UsernameRegex().IsMatch(username))
            throw new AppException("Foydalanuvchi nomi 3–32 belgi: harf, raqam va pastki chiziq.", 400, "invalid_username");

        if (!PhoneRegex().IsMatch(phone))
            throw new AppException("Telefon raqami +998XXXXXXXXX ko'rinishida bo'lishi kerak.", 400, "invalid_phone");

        if (request.Password.Length < 6)
            throw new AppException("Parol kamida 6 belgidan iborat bo'lsin.", 400, "weak_password");

        if (await _db.Users.AnyAsync(u => u.Username == username, ct))
            throw AppException.Conflict("Bu foydalanuvchi nomi band.");

        if (await _db.Users.AnyAsync(u => u.Phone == phone, ct))
            throw AppException.Conflict("Bu telefon raqami allaqachon ro'yxatdan o'tgan.");

        var user = new User
        {
            Username = username,
            Phone = phone,
            FirstName = firstName,
            LastName = lastName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            TelegramUsername = string.IsNullOrWhiteSpace(request.TelegramUsername)
                ? null
                : "@" + request.TelegramUsername.Trim().TrimStart('@'),
            CreatedAt = _clock.UtcNow,
            LastLoginAt = _clock.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return BuildResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var login = request.Login.Trim().TrimStart('@').ToLowerInvariant();
        var phone = NormalizePhone(request.Login);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == login || u.Phone == phone, ct);

        // Foydalanuvchi topilmasa ham bir xil xato — hisob mavjudligini oshkor qilmaymiz.
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw AppException.Unauthorized("Login yoki parol noto'g'ri.");

        if (user.IsBlocked)
            throw AppException.Forbidden("Hisobingiz bloklangan. Qo'llab-quvvatlash bilan bog'laning.");

        user.LastLoginAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);

        return BuildResponse(user);
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var userId = _tokens.ValidateRefreshToken(refreshToken)
                     ?? throw AppException.Unauthorized("Refresh token yaroqsiz yoki muddati o'tgan.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw AppException.Unauthorized("Foydalanuvchi topilmadi.");

        if (user.IsBlocked) throw AppException.Forbidden("Hisobingiz bloklangan.");

        return BuildResponse(user);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw AppException.NotFound("Foydalanuvchi");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            throw AppException.Unauthorized("Joriy parol noto'g'ri.");

        if (request.NewPassword.Length < 6)
            throw new AppException("Yangi parol kamida 6 belgidan iborat bo'lsin.", 400, "weak_password");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _db.SaveChangesAsync(ct);
    }

    private AuthResponse BuildResponse(User user)
    {
        var (access, expires) = _tokens.CreateAccessToken(user);
        return new AuthResponse(access, _tokens.CreateRefreshToken(user), expires, user.ToDto());
    }

    /// <summary>+998 90 123 45 67 → +998901234567.</summary>
    public static string NormalizePhone(string raw)
    {
        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.Length == 9) digits = "998" + digits;            // 901234567
        if (digits.Length == 12 && digits.StartsWith("998")) return "+" + digits;
        return raw.StartsWith('+') ? "+" + digits : digits;
    }

    [GeneratedRegex(@"^[a-z0-9_]{3,32}$")]
    private static partial Regex UsernameRegex();

    /// <summary>Ism/familiya: harflar, apostrof, defis va bo'shliq. Lotin va kirill qo'llab-quvvatlanadi.</summary>
    [GeneratedRegex(@"^[\p{L}][\p{L}'’\- ]{1,49}$")]
    private static partial Regex PersonNameRegex();

    [GeneratedRegex(@"^\+998\d{9}$")]
    private static partial Regex PhoneRegex();
}
