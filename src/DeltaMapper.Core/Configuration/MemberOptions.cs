using System.Linq.Expressions;

namespace DeltaMapper;

internal sealed class MemberOptions<TSrc> : IMemberOptions<TSrc>
{
    public Func<object, object?>? Resolver { get; private set; }
    public bool IsIgnored { get; private set; }
    public object? NullSubstituteValue { get; private set; }
    public bool HasNullSubstitute { get; private set; }

    public void MapFrom<TResult>(Expression<Func<TSrc, TResult>> resolver)
    {
        var compiled = resolver.Compile();
        Resolver = src => compiled((TSrc)src);
    }

    public void Ignore() => IsIgnored = true;

    public void NullSubstitute(object value)
    {
        NullSubstituteValue = value;
        HasNullSubstitute = true;
    }
}
