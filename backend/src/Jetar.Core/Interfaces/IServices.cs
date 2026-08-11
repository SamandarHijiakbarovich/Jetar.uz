using Jetar.Core.Common;
using Jetar.Core.Contracts;
using Jetar.Core.Entities;

namespace Jetar.Core.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken ct = default);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default);
}

public interface ITokenService
{
    (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(User user);
    string CreateRefreshToken(User user);
    Guid? ValidateRefreshToken(string token);
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
    Task<RatingDto> CreateAsync(Guid transactionId, Guid fromUserId, CreateRatingRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<RatingDto>> GetForUserAsync(Guid userId, int limit, CancellationToken ct = default);
}

public interface IAdminService
{
    Task<AdminStatsDto> GetStatsAsync(CancellationToken ct = default);
    Task<PagedResult<AdminTransactionRowDto>> GetTransactionsAsync(string? search, int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<AdminDisputeRowDto>> GetDisputesAsync(bool openOnly, CancellationToken ct = default);
    Task<PlatformSettingsDto> GetSettingsAsync(CancellationToken ct = default);
    Task SetUserBlockedAsync(Guid userId, bool blocked, CancellationToken ct = default);
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
