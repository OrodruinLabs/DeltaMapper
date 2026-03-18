using System.Linq.Expressions;
using DeltaMapper.Abstractions;

namespace DeltaMapper.Configuration;

internal sealed class MemberOptions<TSrc> : IMemberOptions<TSrc>
{
    public Func<object, object?>? Resolver { get; private set; }
    public bool IsIgnored { get; private set; }
    public object? NullSubstituteValue { get; private set; }
    public bool HasNullSubstitute { get; private set; }
    public Func<object, bool>? ConditionPredicate { get; private set; }

    public void MapFrom<TResult>(Expression<Func<TSrc, TResult>> resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        var compiled = resolver.Compile();
        Resolver = src => compiled((TSrc)src);
    }

    public void Ignore() => IsIgnored = true;

    public void NullSubstitute(object value)
    {
        NullSubstituteValue = value;
        HasNullSubstitute = true;
    }

    public void Condition(Expression<Func<TSrc, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        var compiled = predicate.Compile();
        ConditionPredicate = src => compiled((TSrc)src);
    }
}
