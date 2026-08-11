using Jetar.Core.Enums;

namespace Jetar.Core.Contracts;

public record AdminStatsDto(
    int TotalUsers,
    int TotalTransactions,
    decimal TotalRevenue,
    int OpenDisputes,
    int DisputesOver24h,
    double UsersGrowthPercent,
    double TransactionsGrowthPercent,
    double RevenueGrowthPercent,
    int ActiveListings,
    int PendingListings);

public record AdminTransactionRowDto(
    Guid Id,
    int Number,
    string BuyerUsername,
    string SellerUsername,
    decimal Amount,
    TransactionStatus Status,
    string StatusLabel,
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
    decimal CommissionRate,
    int AutoReleaseHours,
    decimal MinListingPrice,
    decimal MaxListingPrice,
    bool AutoApproveListings,
    bool SandboxPayments);
