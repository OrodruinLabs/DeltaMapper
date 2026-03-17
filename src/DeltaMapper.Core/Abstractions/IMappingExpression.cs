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
