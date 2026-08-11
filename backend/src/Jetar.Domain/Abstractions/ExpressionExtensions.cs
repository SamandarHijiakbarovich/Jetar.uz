using System.Linq.Expressions;

namespace Jetar.Domain.Abstractions;

/// <summary>Predikat lambda'larni bitta parametr ostida AND bilan birlashtiradi.</summary>
internal static class ExpressionExtensions
{
    public static Expression<Func<T, bool>> AndAlso<T>(
        this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        var param = Expression.Parameter(typeof(T), "x");
        var body = Expression.AndAlso(
            Rebind(left.Body, left.Parameters[0], param),
            Rebind(right.Body, right.Parameters[0], param));
        return Expression.Lambda<Func<T, bool>>(body, param);
    }

    private static Expression Rebind(Expression body, ParameterExpression from, ParameterExpression to)
        => new ParameterReplacer(from, to).Visit(body);

    private sealed class ParameterReplacer : ExpressionVisitor
    {
        private readonly ParameterExpression _from;
        private readonly ParameterExpression _to;
        public ParameterReplacer(ParameterExpression from, ParameterExpression to) { _from = from; _to = to; }
        protected override Expression VisitParameter(ParameterExpression node)
            => node == _from ? _to : base.VisitParameter(node);
    }
}
