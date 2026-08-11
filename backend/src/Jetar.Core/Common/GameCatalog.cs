using Jetar.Core.Enums;

namespace Jetar.Core.Common;

/// <summary>O'yin turi uchun ko'rsatiladigan nom, emoji va brend rangi — dizayndagi kartochkalar bilan bir xil.</summary>
public record GameInfo(GameType Type, string Slug, string Name, string Glyph, string Color);

/// <summary>E'lon turi uchun ko'rsatiladigan nom, slug va emoji.</summary>
public record ListingTypeInfo(ListingType Type, string Slug, string Name, string Glyph, string Description);

public static class ListingTypeCatalog
{
    public static readonly IReadOnlyList<ListingTypeInfo> All = new List<ListingTypeInfo>
    {
        new(ListingType.Account,  "akkaunt", "Akkauntlar",     "🎮", "To'liq o'yin akkauntlari"),
        new(ListingType.Currency, "valyuta", "O'yin valyutasi", "💎", "UC, Diamond, GP, Coins"),
        new(ListingType.Item,     "buyum",   "Skin va buyum",   "🎁", "Skinlar, bundle va qurollar"),
        new(ListingType.Service,  "xizmat",  "Xizmatlar",       "🚀", "Rank ko'tarish va mashg'ulot")
    };

    public static ListingTypeInfo Get(ListingType type) => All.First(t => t.Type == type);

    public static ListingType? FromSlug(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        var hit = All.FirstOrDefault(t => string.Equals(t.Slug, slug, StringComparison.OrdinalIgnoreCase));
        return hit?.Type;
    }
}

public static class GameCatalog
{
    public static readonly IReadOnlyList<GameInfo> All = new List<GameInfo>
    {
        new(GameType.EFootball,  "efootball",   "eFootball",   "⚽",         "#3B82F6"),
        new(GameType.PubgMobile, "pubg-mobile", "PUBG Mobile", "🎯",   "#F59E0B"),
        new(GameType.FreeFire,   "free-fire",   "Free Fire",   "🔥",   "#EF4444"),
        new(GameType.CsGo,       "csgo",        "CS:GO",       "💀",   "#FF6B35"),
        new(GameType.Dota2,      "dota-2",      "Dota 2",      "🗡️", "#7C3AED"),
        new(GameType.Valorant,   "valorant",    "Valorant",    "🎯",   "#DC2626"),
        new(GameType.CodMobile,  "cod-mobile",  "COD Mobile",  "⚔️",   "#0EA5E9")
    };

    public static GameInfo Get(GameType type) => All.First(g => g.Type == type);

    public static GameType? FromSlug(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        var hit = All.FirstOrDefault(g => string.Equals(g.Slug, slug, StringComparison.OrdinalIgnoreCase));
        return hit?.Type;
    }
}
