using System.Linq.Expressions;

namespace Usm.Shared.Patterns.Specification.Internal;

internal static class ExpressionComposer
{
    public static Expression<Func<T, bool>> And<T>(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
        => Combine(left, right, Expression.AndAlso);

    public static Expression<Func<T, bool>> Or<T>(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
        => Combine(left, right, Expression.OrElse);

    public static Expression<Func<T, bool>> Not<T>(Expression<Func<T, bool>> source)
    {
        var parameter = source.Parameters[0];
        return Expression.Lambda<Func<T, bool>>(Expression.Not(source.Body), parameter);
    }

    private static Expression<Func<T, bool>> Combine<T>(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right,
        Func<Expression, Expression, Expression> merge)
    {
        var parameter = left.Parameters[0];
        var rewrittenRight = new ParameterReplacer(right.Parameters[0], parameter).Visit(right.Body);
        return Expression.Lambda<Func<T, bool>>(merge(left.Body, rewrittenRight!), parameter);
    }
}

internal sealed class ParameterReplacer : ExpressionVisitor
{
    private readonly ParameterExpression _source;
    private readonly ParameterExpression _target;

    public ParameterReplacer(ParameterExpression source, ParameterExpression target)
    {
        _source = source;
        _target = target;
    }

    protected override Expression VisitParameter(ParameterExpression node)
        => node == _source ? _target : base.VisitParameter(node);
}
