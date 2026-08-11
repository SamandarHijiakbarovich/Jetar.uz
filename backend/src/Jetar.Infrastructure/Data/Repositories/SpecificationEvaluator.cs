using Jetar.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Jetar.Infrastructure.Data.Repositories;

/// <summary>
/// <see cref="ISpecification{T}"/>ni EF Core <c>IQueryable</c>ga qo'llaydi:
/// filtr, Include, saralash, sahifalash va tracking rejimini bosqichma-bosqich biriktiradi.
/// </summary>
internal static class SpecificationEvaluator
{
    /// <param name="forCounting">
    /// true bo'lsa faqat filtr qo'llanadi — Include, saralash va sahifalash o'tkazib yuboriladi
    /// (COUNT so'rovi uchun ular keraksiz va noto'g'ri natija beradi).
    /// </param>
    public static IQueryable<T> Apply<T>(IQueryable<T> query, ISpecification<T> spec, bool forCounting = false) where T : class
    {
        if (spec.Criteria is not null)
            query = query.Where(spec.Criteria);

        if (forCounting)
            return query;

        query = spec.Includes.Aggregate(query, (current, include) => current.Include(include));
        query = spec.IncludeStrings.Aggregate(query, (current, include) => current.Include(include));

        if (spec.OrderBy.Count > 0)
        {
            IOrderedQueryable<T>? ordered = null;
            foreach (var (keySelector, descending) in spec.OrderBy)
            {
                ordered = ordered is null
                    ? (descending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector))
                    : (descending ? ordered.ThenByDescending(keySelector) : ordered.ThenBy(keySelector));
            }
            query = ordered!;
        }

        if (spec.IsPagingEnabled)
        {
            if (spec.Skip is int skip) query = query.Skip(skip);
            if (spec.Take is int take) query = query.Take(take);
        }

        if (spec.AsSplitQuery) query = query.AsSplitQuery();
        if (spec.AsNoTracking) query = query.AsNoTracking();

        return query;
    }
}
