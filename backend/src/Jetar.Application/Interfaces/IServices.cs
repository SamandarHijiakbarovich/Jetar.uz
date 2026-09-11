using Jetar.Domain.Common;
using Jetar.Application.Contracts;
using Jetar.Domain.Entities;
using Jetar.Domain.Enums;

namespace Jetar.Application.Interfaces;

public interface IAuthService
{
    /// <summary>1-bosqich: ma'lumot tekshiriladi, emailga kod yuboriladi (akkaunt hali yaratilmaydi).</summary>
    Task<RegistrationStartResponse> StartRegistrationAsync(RegisterRequest request, CancellationToken ct = default);

    /// <summary>Kodni qayta yuborish.</summary>
    Task<RegistrationStartResponse> ResendCodeAsync(ResendCodeRequest request, CancellationToken ct = default);

    /// <summary>2-bosqich: kod tasdiqlanadi, akkaunt yaratiladi va token qaytariladi.</summary>
    Task<AuthResponse> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken ct = default);

    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken ct = default);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default);
}

/// <summary>Email jo'natish (SMTP). Test rejimida (o'chirilgan) faqat log qiladi.</summary>
public interface IEmailSender
{
    /// <summary>false bo'lsa haqiqiy email yuborilmaydi (test rejimi) — kod javobda ko'rsatiladi.</summary>
    bool Enabled { get; }

    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default);
}

/// <summary>Tasdiqlanmagan ro'yxat ma'lumotini vaqtincha saqlash (kesh: Redis yoki xotira).</summary>
public interface IVerificationCodeStore
{
    Task SaveAsync(string email, PendingRegistration data, TimeSpan ttl, CancellationToken ct = default);
    Task<PendingRegistration?> GetAsync(string email, CancellationToken ct = default);
    Task RemoveAsync(string email, CancellationToken ct = default);
}

public interface ITokenService
{
    (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(User user);
    string CreateRefreshToken(User user);
    Guid? ValidateRefreshToken(string token);
}

/// <summary>Parolni hashlash va tekshirish. Implementatsiya (BCrypt) Infrastructure qatlamida.</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface IListingService
{
    Task<PagedResult<ListingCardDto>> SearchAsync(ListingQuery query, CancellationToken ct = default);
    /// <summary>Ko'rishlar soni faqat begona foydalanuvchi ochganda oshadi.</summary>
    Task<ListingDetailDto> GetAsync(Guid id, Guid? viewerId, CancellationToken ct = default);
    Task<ListingDetailDto> CreateAsync(Guid sellerId, CreateListingRequest request, CancellationToken ct = default);
    Task<ListingDetailDto> UpdateAsync(Guid id, Guid sellerId, bool isModerator, UpdateListingRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid sellerId, bool isModerator, CancellationToken ct = default);
    Task<IReadOnlyList<GameSummaryDto>> GetGameSummariesAsync(CancellationToken ct = default);

    /// <summary>Bosh sahifa uchun: e'lon turlari va o'yinlar, har biri sanog'i bilan.</summary>
    Task<CategoriesDto> GetCategoriesAsync(CancellationToken ct = default);
}

public interface ITransactionService
{
    Task<TransactionDto> GetAsync(Guid id, Guid viewerId, bool isModerator, CancellationToken ct = default);
    Task<PagedResult<TransactionDto>> ListForUserAsync(Guid userId, string? role, int page, int pageSize, CancellationToken ct = default);
}

public interface IChatService
{
    Task<IReadOnlyList<MessageDto>> GetThreadAsync(Guid transactionId, Guid viewerId, bool isModerator, CancellationToken ct = default);
    Task<MessageDto> SendAsync(Guid senderId, SendMessageRequest request, CancellationToken ct = default);
    Task MarkReadAsync(Guid transactionId, Guid viewerId, CancellationToken ct = default);

    /// <summary>Escrow holati o'zgarganda avtomatik tizim xabari qo'shadi.</summary>
    Task<MessageDto> SendSystemAsync(Guid transactionId, string text, CancellationToken ct = default);
}

public interface IRatingService
{
    /// <summary>Escrow bitimi yakunlangach baho (eski model). Escrow o'chirilgan bo'lsa ishlatilmaydi.</summary>
    Task<RatingDto> CreateAsync(Guid transactionId, Guid fromUserId, CreateRatingRequest request, CancellationToken ct = default);

    /// <summary>Sotuvchiga to'g'ridan-to'g'ri baho (yangi model) — bir foydalanuvchi bir sotuvchiga bir marta.</summary>
    Task<RatingDto> RateUserAsync(Guid toUserId, Guid fromUserId, CreateRatingRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<RatingDto>> GetForUserAsync(Guid userId, int limit, CancellationToken ct = default);
}

/// <summary>Pullik ko'tarish (TOP/VIP) — so'rov, admin tasdig'i.</summary>
public interface IBoostService
{
    /// <summary>Karta rekvizitlari va tariflar (ochiq).</summary>
    BoostConfigDto GetConfig();

    /// <summary>Sotuvchi to'lov qildim deb so'rov yuboradi.</summary>
    Task<BoostRequestDto> RequestAsync(Guid userId, CreateBoostRequest request, CancellationToken ct = default);

    /// <summary>Sotuvchining o'z so'rovlari.</summary>
    Task<IReadOnlyList<BoostRequestDto>> GetMineAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Admin uchun: ko'rib chiqilishi kutilayotgan so'rovlar.</summary>
    Task<IReadOnlyList<BoostRequestDto>> GetPendingAsync(CancellationToken ct = default);

    /// <summary>Admin tasdiqlaydi — e'lon BoostedUntil gacha ko'tariladi.</summary>
    Task<BoostRequestDto> ApproveAsync(Guid id, Guid moderatorId, CancellationToken ct = default);

    /// <summary>Admin rad etadi.</summary>
    Task<BoostRequestDto> RejectAsync(Guid id, Guid moderatorId, string? note, CancellationToken ct = default);
}

public interface IUserService
{
    /// <summary>Joriy foydalanuvchi ma'lumotlari. Hisob topilmasa null (token yaroqli, lekin hisob o'chirilgan).</summary>
    Task<UserDto?> GetByIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Username bo'yicha ommaviy profil: foydalanuvchi, e'lonlari va baholari.</summary>
    Task<PublicProfileDto> GetPublicProfileAsync(string username, CancellationToken ct = default);

    Task<UserDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct = default);
    Task<UserDto> UpdateNotificationsAsync(Guid userId, NotificationSettingsRequest request, CancellationToken ct = default);

    /// <summary>Profil sahifasidagi umumiy ko'rsatkichlar.</summary>
    Task<UserSummaryDto> GetSummaryAsync(Guid userId, CancellationToken ct = default);
}

public interface IAdminService
{
    Task<AdminStatsDto> GetStatsAsync(CancellationToken ct = default);
    Task<PagedResult<AdminTransactionRowDto>> GetTransactionsAsync(string? search, int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<AdminDisputeRowDto>> GetDisputesAsync(bool openOnly, CancellationToken ct = default);
    Task<PlatformSettingsDto> GetSettingsAsync(CancellationToken ct = default);

    /// <summary>Foydalanuvchilar ro'yxati — qidiruv va filtr bilan (admin panel).</summary>
    Task<PagedResult<AdminUserRowDto>> GetUsersAsync(
        string? search, UserRole? role, bool? blocked, bool? verified, int page, int pageSize, CancellationToken ct = default);

    Task SetUserBlockedAsync(Guid userId, bool blocked, CancellationToken ct = default);

    /// <summary>Sotuvchiga "Tekshirilgan" belgisini qo'yish/olib tashlash.</summary>
    Task SetUserVerifiedAsync(Guid userId, bool verified, CancellationToken ct = default);

    /// <summary>Foydalanuvchi rolini o'zgartirish (moderator tayinlash) — faqat admin.</summary>
    Task SetUserRoleAsync(Guid userId, UserRole role, CancellationToken ct = default);

    Task SetListingVerifiedAsync(Guid listingId, bool verified, CancellationToken ct = default);
}

/// <summary>Bildirishnomalar (Telegram bot, kelajakda push/SMS).</summary>
public interface INotificationService
{
    Task NotifyAsync(Guid userId, string title, string body, CancellationToken ct = default);
    Task NotifyTransactionAsync(Transaction transaction, string eventKey, CancellationToken ct = default);
}

/// <summary>Rasm va fayllarni saqlash (local disk yoki MinIO/S3).</summary>
public interface IFileStorage
{
    Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken ct = default);
    Task DeleteAsync(string url, CancellationToken ct = default);
}

/// <summary>Testlarda vaqtni boshqarish uchun.</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

/// <summary>Joriy so'rovdagi foydalanuvchi.</summary>
public interface ICurrentUser
{
    Guid? Id { get; }
    string? Username { get; }
    bool IsAuthenticated { get; }
    bool IsModerator { get; }
    bool IsAdmin { get; }

    /// <summary>Autentifikatsiya qilinmagan bo'lsa 401 tashlaydi.</summary>
    Guid RequireId();
}
