using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Jetar.Infrastructure.Data;

/// <summary>
/// jsonb ustunlar uchun konverterlar. Postgres'da haqiqiy jsonb, InMemory'da oddiy obyekt bo'lib qoladi.
/// </summary>
internal static class JsonConverters
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static readonly ValueConverter<List<string>, string> StringList =
        new(v => JsonSerializer.Serialize(v ?? new List<string>(), Options),
            v => string.IsNullOrWhiteSpace(v)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(v, Options) ?? new List<string>());

    public static readonly ValueComparer<List<string>> StringListComparer =
        new((a, b) => a != null && b != null && a.SequenceEqual(b),
            v => v == null ? 0 : v.Aggregate(0, (acc, s) => HashCode.Combine(acc, s.GetHashCode())),
            v => v == null ? new List<string>() : v.ToList());

    public static readonly ValueConverter<Dictionary<string, string>, string> StringMap =
        new(v => JsonSerializer.Serialize(v ?? new Dictionary<string, string>(), Options),
            v => string.IsNullOrWhiteSpace(v)
                ? new Dictionary<string, string>()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(v, Options) ?? new Dictionary<string, string>());

    public static readonly ValueComparer<Dictionary<string, string>> StringMapComparer =
        new((a, b) => a != null && b != null && a.Count == b.Count && !a.Except(b).Any(),
            v => v == null ? 0 : v.Aggregate(0, (acc, kv) => HashCode.Combine(acc, kv.Key.GetHashCode(), kv.Value.GetHashCode())),
            v => v == null ? new Dictionary<string, string>() : new Dictionary<string, string>(v));
}
