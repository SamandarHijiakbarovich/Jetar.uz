using System.Linq.Expressions;

namespace Jetar.Domain.Abstractions;

/// <summary>Faqat o'qish amallari (Repository pattern, o'qish tomoni).</summary>
public interface IReadRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Spetsifikatsiyaga mos birinchi element (Include/tartib bilan).</summary>
    Task<T?> FirstOrDefaultAsync(ISpecification<T> spec, CancellationToken ct = default);

    /// <summary>Oddiy predikat bo'yicha birinchi element.</summary>
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec, CancellationToken ct = default);
    Task<IReadOnlyList<T>> ListAllAsync(CancellationToken ct = default);

    Task<int> CountAsync(ISpecification<T> spec, CancellationToken ct = default);
    Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);

    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
}

/// <summary>O'qish + yozish amallari. Saqlash <see cref="IUnitOfWork.SaveChangesAsync"/> orqali bo'ladi.</summary>
public interface IRepository<T> : IReadRepository<T> where T : class
{
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);
}
