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
