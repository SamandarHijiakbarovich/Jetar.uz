using System.Linq.Expressions;
using Jetar.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Jetar.Infrastructure.Data.Repositories;

/// <summary>
/// <see cref="IRepository{T}"/>ning EF Core ustidagi umumiy implementatsiyasi.
/// Barcha repository'lar bitta (scoped) <see cref="AppDbContext"/> nusxasini bo'lishadi,
/// shuning uchun ular ustidagi o'zgarishlar yagona <c>SaveChangesAsync</c>da saqlanadi.
/// </summary>
public class EfRepository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext Db;
    protected DbSet<T> Set => Db.Set<T>();

    public EfRepository(AppDbContext db) => Db = db;

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await Set.FindAsync(new object?[] { id }, ct);

    public virtual Task<T?> FirstOrDefaultAsync(ISpecification<T> spec, CancellationToken ct = default)
        => SpecificationEvaluator.Apply(Set.AsQueryable(), spec).FirstOrDefaultAsync(ct);

    public virtual Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => Set.FirstOrDefaultAsync(predicate, ct);

    public virtual async Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec, CancellationToken ct = default)
        => await SpecificationEvaluator.Apply(Set.AsQueryable(), spec).ToListAsync(ct);

    public virtual async Task<IReadOnlyList<T>> ListAllAsync(CancellationToken ct = default)
        => await Set.AsNoTracking().ToListAsync(ct);

    public virtual Task<int> CountAsync(ISpecification<T> spec, CancellationToken ct = default)
        => SpecificationEvaluator.Apply(Set.AsQueryable(), spec, forCounting: true).CountAsync(ct);

    public virtual Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => Set.CountAsync(predicate, ct);

    public virtual Task<int> CountAsync(CancellationToken ct = default)
        => Set.CountAsync(ct);

    public virtual Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => Set.AnyAsync(predicate, ct);

    public virtual async Task AddAsync(T entity, CancellationToken ct = default)
        => await Set.AddAsync(entity, ct);

    public virtual void Update(T entity) => Set.Update(entity);

    public virtual void Remove(T entity) => Set.Remove(entity);

    public virtual void RemoveRange(IEnumerable<T> entities) => Set.RemoveRange(entities);
}
