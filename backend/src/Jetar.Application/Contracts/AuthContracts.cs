using Jetar.Domain.Enums;

namespace Jetar.Application.Contracts;

public record RegisterRequest(
    string FirstName,
    string LastName,
    string Username,
    string Phone,
    string Email,
    string Password,
    string? TelegramUsername);

/// <summary>Ro'yxatdan o'tish boshlandi — emailga kod yuborildi (akkaunt hali yaratilmagan).</summary>
public record RegistrationStartResponse(string Email, bool EmailSent, string? DevCode, int ExpiresInMinutes);

/// <summary>Emailga kelgan kodni tasdiqlash — akkaunt yaratiladi.</summary>
public record VerifyEmailRequest(string Email, string Code);

/// <summary>Kodni qayta yuborish.</summary>
public record ResendCodeRequest(string Email);

/// <summary>Kesh'da vaqtincha saqlanadigan tasdiqlanmagan ro'yxat (ichki — API'ga chiqmaydi).</summary>
public record PendingRegistration(
    string FirstName,
    string LastName,
    string Username,
    string Phone,
    string Email,
    string PasswordHash,
    string? TelegramUsername,
    string Code,
    int Attempts);

/// <summary>Kirish uchun faqat login (yoki telefon) va parol talab qilinadi.</summary>
public record LoginRequest(string Login, string Password);

public record RefreshRequest(string RefreshToken);

public record AuthResponse(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt, UserDto User);

public record UserDto(
    Guid Id,
    string Username,
    string FirstName,
    string LastName,
    string FullName,
    string Phone,
    string? TelegramUsername,
    string? City,
    string? AvatarUrl,
    decimal Rating,
    int RatingCount,
    int TotalSales,
    int TotalPurchases,
    bool IsVerified,
    UserRole Role,
    int AvgResponseMinutes,
    DateTimeOffset CreatedAt);

public record UpdateProfileRequest(
    string? FirstName,
    string? LastName,
    string? Username,
    string? Phone,
    string? TelegramUsername,
    string? City);

public record NotificationSettingsRequest(bool NotifyTelegram, bool NotifyNewMessage, bool NotifyMarketing);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
