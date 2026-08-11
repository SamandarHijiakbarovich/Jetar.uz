using System.Linq.Expressions;

namespace Jetar.Domain.Abstractions;

/// <summary>
/// So'rov mezonlarini (filtr, Include, saralash, sahifalash) bitta obyektga jamlaydi.
/// Repository shu spetsifikatsiyani <c>IQueryable</c>ga qo'llaydi — servis EF Core'ni
/// bevosita bilmaydi. (Specification pattern.)
/// </summary>
public interface ISpecification<T>
{
    /// <summary>WHERE sharti. Null bo'lsa filtr qo'llanmaydi.</summary>
    Expression<Func<T, bool>>? Criteria { get; }

    /// <summary>Eager-load qilinadigan navigatsiyalar (lambda orqali).</summary>
    IReadOnlyList<Expression<Func<T, object>>> Includes { get; }

    /// <summary>ThenInclude uchun matnli yo'llar (masalan "Transaction.Buyer").</summary>
    IReadOnlyList<string> IncludeStrings { get; }

    /// <summary>Saralash zanjiri: (kalit, kamayish tartibida-mi).</summary>
    IReadOnlyList<(Expression<Func<T, object>> KeySelector, bool Descending)> OrderBy { get; }

    int? Skip { get; }
    int? Take { get; }
    bool IsPagingEnabled { get; }

    /// <summary>Faqat o'qish uchun — o'zgarishlar kuzatilmaydi (tezroq).</summary>
    bool AsNoTracking { get; }

    /// <summary>Ko'p Include'da kartezian portlashini oldini olish uchun.</summary>
    bool AsSplitQuery { get; }
}
