using Jetar.Core.Enums;

namespace Jetar.Core.Contracts;

public record InitiateTransactionRequest(Guid ListingId);

public record TransactionDto(
    Guid Id,
    int Number,
    string EscrowCode,
    TransactionStatus Status,
    string StatusLabel,
    decimal Amount,
    decimal CommissionAmount,
    decimal SellerPayout,
    bool BuyerConfirmed,
    bool SellerConfirmed,
    DateTimeOffset CreatedAt,
    DateTimeOffset? EscrowHeldAt,
    DateTimeOffset? AutoReleaseAt,
    DateTimeOffset? CompletedAt,
    ListingCardDto Listing,
    PartyDto Buyer,
    PartyDto Seller,
    bool HasDispute,
    // ViewerIsBuyer — joriy foydalanuvchi xaridormi (aks holda sotuvchi).
    bool ViewerIsBuyer,
    bool ViewerHasRated);

public record PartyDto(Guid Id, string Username, string? AvatarUrl, decimal Rating, bool IsVerified);

public record CancelTransactionRequest(string? Reason);

public record OpenDisputeRequest(string Reason, List<string>? EvidenceUrls);

public record DisputeDto(
    Guid Id,
    Guid TransactionId,
    int TransactionNumber,
    string Reason,
    IReadOnlyList<string> EvidenceUrls,
    DisputeStatus Status,
    string OpenedByUsername,
    string? ResolutionNote,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ResolvedAt);

public record ResolveDisputeRequest(bool FavourBuyer, string? Note);

public record CreateRatingRequest(short Score, string? Comment);

public record RatingDto(
    Guid Id,
    short Score,
    string? Comment,
    string FromUsername,
    string? FromAvatarUrl,
    DateTimeOffset CreatedAt);
