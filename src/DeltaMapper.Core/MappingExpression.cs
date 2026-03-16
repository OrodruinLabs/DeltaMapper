using System.Linq.Expressions;

namespace DeltaMapper;

/// <summary>
/// Fluent interface for configuring a mapping between TSrc and TDst.
/// </summary>
public interface IMappingExpression<TSrc, TDst>
{
    /// <summary>
    /// Configure a specific destination member.
    /// </summary>
    IMappingExpression<TSrc, TDst> ForMember<TMember>(
        Expression<Func<TDst, TMember>> destinationMember,
        Action<IMemberOptions<TSrc>> options);

    /// <summary>
    /// Execute an action before mapping properties.
    /// </summary>
    IMappingExpression<TSrc, TDst> BeforeMap(Action<TSrc, TDst> beforeAction);

    /// <summary>
    /// Execute an action after mapping properties.
    /// </summary>
    IMappingExpression<TSrc, TDst> AfterMap(Action<TSrc, TDst> afterAction);

    /// <summary>
    /// Automatically register the reverse mapping (TDst to TSrc) using convention matching.
    /// </summary>
    IMappingExpression<TSrc, TDst> ReverseMap();
}

/// <summary>
/// Options for configuring how a specific destination member is mapped.
/// </summary>
public interface IMemberOptions<TSrc>
{
    /// <summary>
    /// Map this member using a custom expression.
    /// </summary>
    void MapFrom<TResult>(Expression<Func<TSrc, TResult>> resolver);

    /// <summary>
    /// Ignore this destination member (do not map it).
    /// </summary>
    void Ignore();

    /// <summary>
    /// Use the specified value when the source value is null.
    /// </summary>
    void NullSubstitute(object value);
}

internal sealed class MappingExpression<TSrc, TDst> : IMappingExpression<TSrc, TDst>
{
    internal TypeMapConfiguration TypeMapConfig { get; } = new()
    {
        SourceType = typeof(TSrc),
        DestinationType = typeof(TDst)
    };

    public IMappingExpression<TSrc, TDst> ForMember<TMember>(
        Expression<Func<TDst, TMember>> destinationMember,
        Action<IMemberOptions<TSrc>> options)
    {
        var memberName = GetMemberName(destinationMember);
        var memberOptions = new MemberOptions<TSrc>();
        options(memberOptions);

        TypeMapConfig.MemberConfigurations.Add(new MemberConfiguration
        {
            DestinationMemberName = memberName,
            CustomResolver = memberOptions.Resolver,
            IsIgnored = memberOptions.IsIgnored,
            NullSubstituteValue = memberOptions.NullSubstituteValue,
            HasNullSubstitute = memberOptions.HasNullSubstitute
        });

        return this;
    }

    public IMappingExpression<TSrc, TDst> BeforeMap(Action<TSrc, TDst> beforeAction)
    {
        TypeMapConfig.BeforeMapAction = (src, dst) => beforeAction((TSrc)src, (TDst)dst);
        return this;
    }

    public IMappingExpression<TSrc, TDst> AfterMap(Action<TSrc, TDst> afterAction)
    {
        TypeMapConfig.AfterMapAction = (src, dst) => afterAction((TSrc)src, (TDst)dst);
        return this;
    }

    public IMappingExpression<TSrc, TDst> ReverseMap()
    {
        TypeMapConfig.HasReverseMap = true;
        return this;
    }

    private static string GetMemberName<TMember>(Expression<Func<TDst, TMember>> expression)
    {
        if (expression.Body is MemberExpression memberExpression)
            return memberExpression.Member.Name;
        throw new ArgumentException("Expression must be a member access expression (e.g., d => d.PropertyName).");
    }
}

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
