using Jetar.Domain.Enums;

namespace Jetar.Application.Contracts;

public record AdminStatsDto(
    int TotalUsers,
    int ActiveListings,
    int PendingListings,
    int TotalListings,
    int BoostsPending,
    int BoostsApproved,
    decimal BoostRevenue,
    double UsersGrowthPercent,
    double ListingsGrowthPercent,
    double BoostRevenueGrowthPercent);

public record AdminTransactionRowDto(
    Guid Id,
    int Number,
    string BuyerUsername,
    string SellerUsername,
    decimal Amount,
    TransactionStatus Status,
    string StatusLabel,
    DateTimeOffset CreatedAt);

public record AdminUserRowDto(
    Guid Id,
    string Username,
    string FullName,
    string Phone,
    UserRole Role,
    decimal Rating,
    int RatingCount,
    int TotalSales,
    int TotalPurchases,
    bool IsVerified,
    bool IsBlocked,
    DateTimeOffset CreatedAt);

public record AdminDisputeRowDto(
    Guid Id,
    Guid TransactionId,
    int TransactionNumber,
    string Reason,
    DisputeStatus Status,
    double AgeHours,
    DateTimeOffset CreatedAt);

public record PlatformSettingsDto(
    bool EscrowEnabled,
    bool ContactRequiresLogin,
    decimal MinListingPrice,
    decimal MaxListingPrice,
    bool AutoApproveListings,
    string BoostCardNumber,
    int BoostTierCount);
