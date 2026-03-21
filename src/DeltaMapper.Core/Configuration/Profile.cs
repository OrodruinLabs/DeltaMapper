using DeltaMapper.Configuration;

namespace DeltaMapper;

/// <summary>
/// Base class for mapping configuration profiles. Users subclass this and call CreateMap in the constructor.
/// </summary>
public abstract class Profile
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

    /// <summary>
    /// Creates a mapping configuration from TSrc to TDst with the specified validation mode.
    /// </summary>
    protected IMappingExpression<TSrc, TDst> CreateMap<TSrc, TDst>(MemberList memberList)
    {
        var expression = new MappingExpression<TSrc, TDst>(memberList);
        TypeMaps.Add(expression.TypeMapConfig);
        return expression;
    }
}
