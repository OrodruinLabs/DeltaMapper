using System.Linq.Expressions;

namespace DeltaMapper;

/// <summary>
/// Base class for mapping configuration profiles. Users subclass this and call CreateMap in the constructor.
/// </summary>
public abstract class MappingProfile
{
    internal List<TypeMapConfiguration> TypeMaps { get; } = [];

    /// <summary>
    /// Creates a mapping configuration from TSrc to TDst.
    /// </summary>
    protected IMappingExpression<TSrc, TDst> CreateMap<TSrc, TDst>()
    {
        var expression = new MappingExpression<TSrc, TDst>();
        TypeMaps.Add(expression.TypeMapConfig);
        return expression;
    }
}

/// <summary>
/// Stores all configuration for a single source-to-destination type pair.
/// </summary>
internal sealed class TypeMapConfiguration
{
    public Type SourceType { get; init; } = null!;
    public Type DestinationType { get; init; } = null!;
    public List<MemberConfiguration> MemberConfigurations { get; } = [];
    public Action<object, object>? BeforeMapAction { get; set; }
    public Action<object, object>? AfterMapAction { get; set; }
    public bool HasReverseMap { get; set; }
}

/// <summary>
/// Stores configuration for a single destination member override.
/// </summary>
internal sealed class MemberConfiguration
{
    public string DestinationMemberName { get; init; } = null!;
    public Func<object, object?>? CustomResolver { get; set; }
    public bool IsIgnored { get; set; }
    public object? NullSubstituteValue { get; set; }
    public bool HasNullSubstitute { get; set; }
}
