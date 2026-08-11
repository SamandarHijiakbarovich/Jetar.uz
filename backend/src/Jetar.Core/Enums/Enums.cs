namespace Jetar.Core.Enums;

/// <summary>Platformada qo'llab-quvvatlanadigan o'yinlar.</summary>
public enum GameType
{
    EFootball = 0,
    PubgMobile = 1,
    FreeFire = 2,
    CsGo = 3,
    Dota2 = 4,
    Valorant = 5,
    CodMobile = 6
}

/// <summary>
/// E'lon turi — o'yindan mustaqil ikkinchi kategoriya o'lchovi.
/// Platforma faqat akkaunt emas, o'yin ichidagi vositalarni ham sotadi.
/// </summary>
public enum ListingType
{
    /// <summary>To'liq o'yin akkaunti.</summary>
    Account = 0,

    /// <summary>O'yin valyutasi: UC, Diamond, GP, Coins.</summary>
    Currency = 1,

    /// <summary>Skin, bundle, qurol yoki boshqa predmet.</summary>
    Item = 2,

    /// <summary>Xizmat: rank ko'tarish (boosting), mashg'ulot.</summary>
    Service = 3
}

/// <summary>E'lon holati.</summary>
public enum ListingStatus
{
    /// <summary>Moderator tasdig'ini kutmoqda.</summary>
    Pending = 0,
    /// <summary>Sotuvda.</summary>
    Active = 1,
    /// <summary>Bitim ochilgan, boshqa xaridorlar uchun yopiq.</summary>
    Reserved = 2,
    Sold = 3,
    /// <summary>Sotuvchi o'zi yashirgan.</summary>
    Hidden = 4,
    /// <summary>Moderator bloklagan.</summary>
    Blocked = 5
}

/// <summary>Escrow bitimining hayotiy sikli. Arxitektura hujjati 03-bo'lim.</summary>
public enum TransactionStatus
{
    /// <summary>01–02: bitim ochildi, to'lov hali kelmagan.</summary>
    Initiated = 0,
    /// <summary>03: to'lov provayderiga yuborildi, tasdiq kutilmoqda.</summary>
    AwaitingPayment = 1,
    /// <summary>04: pul Jetar hisobida BLOKLANDI.</summary>
    EscrowHeld = 2,
    /// <summary>05: sotuvchi akkaunt ma'lumotlarini yubordi.</summary>
    CredentialsSent = 3,
    /// <summary>06: xaridor tekshirmoqda.</summary>
    BuyerVerifying = 4,
    /// <summary>08: pul sotuvchiga chiqarildi, komissiya ushlandi.</summary>
    Completed = 5,
    /// <summary>09: nizo ochildi, pul muzlatildi.</summary>
    Disputed = 6,
    /// <summary>11: pul xaridorga qaytarildi.</summary>
    Refunded = 7,
    Cancelled = 8
}

/// <summary>To'lov holati (Payme / Click / Uzum).</summary>
public enum PaymentStatus
{
    Created = 0,
    Pending = 1,
    Paid = 2,
    Failed = 3,
    Refunded = 4,
    Cancelled = 5
}

/// <summary>Nizo holati.</summary>
public enum DisputeStatus
{
    Open = 0,
    UnderReview = 1,
    /// <summary>Pul xaridorga qaytarildi.</summary>
    ResolvedForBuyer = 2,
    /// <summary>Pul sotuvchiga chiqarildi.</summary>
    ResolvedForSeller = 3,
    Rejected = 4
}

/// <summary>Foydalanuvchi roli.</summary>
public enum UserRole
{
    User = 0,
    Moderator = 1,
    Admin = 2
}

/// <summary>To'lov provayderi.</summary>
public enum PaymentMethod
{
    Click = 0,
    Payme = 1,
    Uzum = 2,
    Apelsin = 3
}
