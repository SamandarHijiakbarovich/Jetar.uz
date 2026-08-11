using Jetar.Domain.Common;
using Jetar.Application.Contracts;
using Jetar.Domain.Entities;

namespace Jetar.Application.Services;

/// <summary>Entity → DTO o'girish. Bitta joyda saqlanadi, shunda javob shakli hamma yerda bir xil bo'ladi.</summary>
internal static class Mappers
{
    /// <summary>"Alisher Karimov" — bo'sh bo'lsa bo'sh satr qaytadi.</summary>
    public static string FullName(this User u) => $"{u.FirstName} {u.LastName}".Trim();

    public static UserDto ToDto(this User u) => new(
        u.Id, u.Username, u.FirstName, u.LastName, u.FullName(),
        u.Phone, u.TelegramUsername, u.City, u.AvatarUrl,
        u.Rating, u.RatingCount, u.TotalSales, u.TotalPurchases,
        u.IsVerified, u.Role, u.AvgResponseMinutes, u.CreatedAt);

    public static SellerDto ToSellerDto(this User u) => new(
        u.Id, u.Username, u.FullName(), u.AvatarUrl, u.Rating, u.RatingCount, u.TotalSales,
        u.IsVerified, u.AvgResponseMinutes, u.CreatedAt);

    public static PartyDto ToPartyDto(this User u) => new(u.Id, u.Username, u.AvatarUrl, u.Rating, u.IsVerified);

    public static ListingCardDto ToCardDto(this Listing l)
    {
        var game = GameCatalog.Get(l.GameType);
        var type = ListingTypeCatalog.Get(l.Type);
        return new ListingCardDto(
            l.Id, l.Title, l.GameType, game.Name, game.Glyph, game.Color,
            l.Type, type.Name, type.Glyph,
            l.Price, l.RankLevel, l.ServerRegion,
            l.Images.FirstOrDefault(),
            l.IsVerified,
            l.BoostedUntil.HasValue && l.BoostedUntil > DateTimeOffset.UtcNow,
            l.Status,
            l.Seller?.Username ?? "—",
            l.Seller?.Rating ?? 0,
            l.CreatedAt);
    }

    public static ListingDetailDto ToDetailDto(this Listing l, IReadOnlyList<ListingCardDto> similar)
    {
        var game = GameCatalog.Get(l.GameType);
        var type = ListingTypeCatalog.Get(l.Type);
        return new ListingDetailDto(
            l.Id, l.Title, l.Description, l.GameType, game.Name, game.Glyph, game.Color,
            l.Type, type.Name, type.Glyph,
            l.Price, l.RankLevel, l.ServerRegion,
            l.InGameItems, l.Images, l.Stats,
            l.IsVerified,
            l.BoostedUntil.HasValue && l.BoostedUntil > DateTimeOffset.UtcNow,
            l.Status, l.ViewCount, l.CreatedAt,
            l.Seller?.ToSellerDto() ?? new SellerDto(l.SellerId, "—", "", null, 0, 0, 0, false, 0, l.CreatedAt),
            similar);
    }

    public static TransactionDto ToDto(this Transaction t, Guid viewerId, bool viewerHasRated) => new(
        t.Id, t.Number, t.EscrowCode, t.Status, StatusLabels.Get(t.Status),
        t.Amount, t.CommissionAmount, t.SellerPayout,
        t.BuyerConfirmed, t.SellerConfirmed,
        t.CreatedAt, t.EscrowHeldAt, t.AutoReleaseAt, t.CompletedAt,
        t.Listing?.ToCardDto() ?? throw new InvalidOperationException("Listing yuklanmagan."),
        t.Buyer?.ToPartyDto() ?? new PartyDto(t.BuyerId, "—", null, 0, false),
        t.Seller?.ToPartyDto() ?? new PartyDto(t.SellerId, "—", null, 0, false),
        t.Dispute != null,
        t.BuyerId == viewerId,
        viewerHasRated);

    public static MessageDto ToDto(this Message m) => new(
        m.Id, m.TransactionId, m.SenderId, m.Sender?.Username ?? "Jetar",
        m.Text, m.AttachmentUrl, m.IsSystem, m.IsRead, m.CreatedAt);

    public static PaymentDto ToDto(this Payment p) => new(
        p.Id, p.TransactionId, p.Amount, p.Method, p.Status,
        p.CheckoutUrl, p.ProviderPaymentId, p.CreatedAt, p.PaidAt);

    public static RatingDto ToDto(this Rating r) => new(
        r.Id, r.Score, r.Comment, r.FromUser?.Username ?? "—", r.FromUser?.AvatarUrl, r.CreatedAt);

    public static DisputeDto ToDto(this Dispute d) => new(
        d.Id, d.TransactionId, d.Transaction?.Number ?? 0, d.Reason, d.EvidenceUrls,
        d.Status, d.OpenedBy?.Username ?? "—", d.ResolutionNote, d.CreatedAt, d.ResolvedAt);
}
