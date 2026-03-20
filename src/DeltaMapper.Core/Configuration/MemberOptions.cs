using System.Linq.Expressions;

namespace DeltaMapper.Configuration;

internal sealed class MemberOptions<TSrc> : IMemberOptions<TSrc>
{
    public Func<object, object?>? Resolver { get; private set; }
    public Type? ResolverReturnType { get; private set; }
    public bool IsIgnored { get; private set; }
    public object? NullSubstituteValue { get; private set; }
    public bool HasNullSubstitute { get; private set; }
    public Func<object, bool>? ConditionPredicate { get; private set; }

    public void MapFrom<TResult>(Expression<Func<TSrc, TResult>> resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        var compiled = resolver.Compile();
        Resolver = src => compiled((TSrc)src);
        ResolverReturnType = typeof(TResult);
    }

    public void Ignore()
    {
        if (ConditionPredicate is not null)
            throw new InvalidOperationException(
                "Cannot combine Ignore() with Condition() on the same member. " +
                "Use Condition() alone to conditionally skip mapping.");
        IsIgnored = true;
    }

    public void NullSubstitute(object value)
    {
        NullSubstituteValue = value;
        HasNullSubstitute = true;
    }

    public void Condition(Expression<Func<TSrc, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        if (IsIgnored)
            throw new InvalidOperationException(
                "Cannot combine Condition() with Ignore() on the same member. " +
                "Use Condition() alone to conditionally skip mapping.");
        var compiled = predicate.Compile();
        ConditionPredicate = src => compiled((TSrc)src);
    }
}
