using System.Text.Json;
using Jetar.Application.Contracts;
using Jetar.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace Jetar.Infrastructure.Services;

/// <summary>
/// Tasdiqlanmagan ro'yxatni keshda saqlaydi (Redis mavjud bo'lsa Redis, aks holda xotira).
/// TTL tugagach o'zi o'chadi — migratsiya/jadval kerak emas.
/// </summary>
public class DistributedCacheVerificationStore : IVerificationCodeStore
{
    private readonly IDistributedCache _cache;

    public DistributedCacheVerificationStore(IDistributedCache cache) => _cache = cache;

    private static string Key(string email) => $"emailverify:{email.Trim().ToLowerInvariant()}";

    public Task SaveAsync(string email, PendingRegistration data, TimeSpan ttl, CancellationToken ct = default)
        => _cache.SetStringAsync(
            Key(email),
            JsonSerializer.Serialize(data),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
            ct);

    public async Task<PendingRegistration?> GetAsync(string email, CancellationToken ct = default)
    {
        var json = await _cache.GetStringAsync(Key(email), ct);
        return string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<PendingRegistration>(json);
    }

    public Task RemoveAsync(string email, CancellationToken ct = default)
        => _cache.RemoveAsync(Key(email), ct);
}
