namespace Jetar.Application.Contracts;

/// <summary>Ommaviy profil javobi: foydalanuvchi, uning e'lonlari va oxirgi baholari.</summary>
public record PublicProfileDto(
    UserDto User,
    IReadOnlyList<ListingCardDto> Listings,
    IReadOnlyList<RatingDto> Ratings);

/// <summary>Profil sahifasidagi umumiy ko'rsatkichlar.</summary>
public record UserSummaryDto(
    UserDto User,
    int ActiveListings,
    int OpenTransactions,
    decimal TotalEarned,
    int UnreadMessages,
    int MemberForDays);
