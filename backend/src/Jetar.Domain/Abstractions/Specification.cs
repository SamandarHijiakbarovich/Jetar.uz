using System.Linq.Expressions;

namespace Jetar.Domain.Abstractions;

/// <summary>
/// <see cref="ISpecification{T}"/> uchun bazaviy sinf. Aniq spetsifikatsiyalar
/// shundan meros oladi va konstruktorda quyidagi <c>Apply*</c> metodlaridan foydalanadi.
/// </summary>
public abstract class Specification<T> : ISpecification<T>
{
    protected Specification(Expression<Func<T, bool>>? criteria = null) => Criteria = criteria;

    public Expression<Func<T, bool>>? Criteria { get; private set; }

    private readonly List<Expression<Func<T, object>>> _includes = new();
    public IReadOnlyList<Expression<Func<T, object>>> Includes => _includes;

    private readonly List<string> _includeStrings = new();
    public IReadOnlyList<string> IncludeStrings => _includeStrings;

    private readonly List<(Expression<Func<T, object>>, bool)> _orderBy = new();
    public IReadOnlyList<(Expression<Func<T, object>> KeySelector, bool Descending)> OrderBy => _orderBy;

    public int? Skip { get; private set; }
    public int? Take { get; private set; }
    public bool IsPagingEnabled { get; private set; }
    public bool AsNoTracking { get; private set; }
    public bool AsSplitQuery { get; private set; }

    /// <summary>Filtr shartini qo'shadi. Bir necha marta chaqirilsa shartlar AND bilan birlashadi.</summary>
    protected void Where(Expression<Func<T, bool>> criteria)
        => Criteria = Criteria is null ? criteria : Criteria.AndAlso(criteria);

    protected void AddInclude(Expression<Func<T, object>> include) => _includes.Add(include);
    protected void AddInclude(string includeString) => _includeStrings.Add(includeString);

    protected void AddOrderBy(Expression<Func<T, object>> keySelector) => _orderBy.Add((keySelector, false));
    protected void AddOrderByDescending(Expression<Func<T, object>> keySelector) => _orderBy.Add((keySelector, true));

    /// <summary>page 1'dan boshlanadi.</summary>
    protected void ApplyPaging(int page, int pageSize)
    {
        Skip = Math.Max(0, (page - 1) * pageSize);
        Take = pageSize;
        IsPagingEnabled = true;
    }

    protected void ApplyNoTracking() => AsNoTracking = true;
    protected void ApplySplitQuery() => AsSplitQuery = true;
}
