using Jetar.Domain.Enums;

namespace Jetar.Application.Contracts;

/// <summary>Ro'yxatdagi qisqartirilgan e'lon kartochkasi.</summary>
public record ListingCardDto(
    Guid Id,
    string Title,
    GameType GameType,
    string GameName,
    string GameGlyph,
    string GameColor,
    ListingType Type,
    string TypeName,
    string TypeGlyph,
    decimal Price,
    string RankLevel,
    string ServerRegion,
    string? CoverImage,
    bool IsVerified,
    bool IsBoosted,
    ListingStatus Status,
    string SellerUsername,
    decimal SellerRating,
    DateTimeOffset CreatedAt);

/// <summary>To'liq e'lon sahifasi.</summary>
public record ListingDetailDto(
    Guid Id,
    string Title,
    string Description,
    GameType GameType,
    string GameName,
    string GameGlyph,
    string GameColor,
    ListingType Type,
    string TypeName,
    string TypeGlyph,
    decimal Price,
    string RankLevel,
    string ServerRegion,
    IReadOnlyList<string> InGameItems,
    IReadOnlyList<string> Images,
    IReadOnlyDictionary<string, string> Stats,
    bool IsVerified,
    bool IsBoosted,
    ListingStatus Status,
    int ViewCount,
    DateTimeOffset CreatedAt,
    SellerDto Seller,
    IReadOnlyList<ListingCardDto> Similar);

public record SellerDto(
    Guid Id,
    string Username,
    string FullName,
    string? AvatarUrl,
    decimal Rating,
    int RatingCount,
    int TotalSales,
    bool IsVerified,
    int AvgResponseMinutes,
    DateTimeOffset MemberSince,
    string? City = null,
    // Kontakt faqat tizimga kirgan foydalanuvchiga ko'rsatiladi (spamga qarshi).
    string? Phone = null,
    string? TelegramUsername = null);

public record CreateListingRequest(
    GameType GameType,
    ListingType Type,
    string Title,
    string Description,
    decimal Price,
    string ServerRegion,
    string RankLevel,
    List<string>? InGameItems,
    List<string>? Images,
    Dictionary<string, string>? Stats);

public record UpdateListingRequest(
    ListingType? Type,
    string? Title,
    string? Description,
    decimal? Price,
    string? ServerRegion,
    string? RankLevel,
    List<string>? InGameItems,
    List<string>? Images,
    Dictionary<string, string>? Stats,
    ListingStatus? Status);

/// <summary>GET /api/listings filtrlari.</summary>
public class ListingQuery
{
    public string? Game { get; set; }

    /// <summary>E'lon turi slug'i: akkaunt | valyuta | buyum | xizmat.</summary>
    public string? Type { get; set; }

    public string? Search { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? Region { get; set; }
    public bool? VerifiedOnly { get; set; }
    public Guid? SellerId { get; set; }

    /// <summary>newest | price_asc | price_desc | popular</summary>
    public string Sort { get; set; } = "newest";

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}

public record GameSummaryDto(string Slug, string Name, string Glyph, string Color, int ListingCount);

public record ListingTypeSummaryDto(string Slug, string Name, string Glyph, string Description, int ListingCount);

/// <summary>Bosh sahifadagi kategoriya bloklari uchun — ikkala o'lchov birga.</summary>
public record CategoriesDto(IReadOnlyList<ListingTypeSummaryDto> Types, IReadOnlyList<GameSummaryDto> Games);
