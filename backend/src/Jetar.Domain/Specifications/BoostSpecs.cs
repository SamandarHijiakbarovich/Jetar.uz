using Jetar.Domain.Abstractions;
using Jetar.Domain.Entities;
using Jetar.Domain.Enums;

namespace Jetar.Domain.Specifications;

/// <summary>Ko'rib chiqilishi kutilayotgan boost so'rovlari — e'lon va sotuvchi bilan, eng eskisidan.</summary>
public sealed class PendingBoostsSpec : Specification<BoostRequest>
{
    public PendingBoostsSpec()
    {
        Where(b => b.Status == BoostStatus.Pending);
        AddInclude(b => b.Listing!);
        AddInclude(b => b.User!);
        ApplyNoTracking();
        AddOrderBy(b => b.CreatedAt);
    }
}

/// <summary>Bitta boost so'rovi — e'lon va sotuvchi bilan (kuzatiladi, holatini o'zgartirish uchun).</summary>
public sealed class BoostWithDetailsSpec : Specification<BoostRequest>
{
    public BoostWithDetailsSpec(Guid id) : base(b => b.Id == id)
    {
        AddInclude(b => b.Listing!);
        AddInclude(b => b.User!);
    }
}

/// <summary>Sotuvchining o'z boost so'rovlari — eng yangisidan.</summary>
public sealed class MyBoostsSpec : Specification<BoostRequest>
{
    public MyBoostsSpec(Guid userId)
    {
        Where(b => b.UserId == userId);
        AddInclude(b => b.Listing!);
        ApplyNoTracking();
        AddOrderByDescending(b => b.CreatedAt);
    }
}
