using Jetar.Domain.Enums;

namespace Jetar.Application.Contracts;

/// <summary>Ko'tarish sahifasi uchun: karta rekvizitlari va tariflar.</summary>
public record BoostConfigDto(
    string CardNumber,
    string CardHolder,
    IReadOnlyList<BoostTierDto> Tiers);

public record BoostTierDto(int Days, decimal Price, string Label);

/// <summary>Sotuvchi boost so'rovi yuboradi.</summary>
public record CreateBoostRequest(Guid ListingId, int Days, string? ScreenshotUrl);

/// <summary>Admin rad etganda sabab.</summary>
public record RejectBoostRequest(string? Note);

public record BoostRequestDto(
    Guid Id,
    Guid ListingId,
    string ListingTitle,
    string SellerUsername,
    int Days,
    decimal Amount,
    string? ScreenshotUrl,
    BoostStatus Status,
    string? ReviewNote,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReviewedAt,
    DateTimeOffset? BoostedUntil);
