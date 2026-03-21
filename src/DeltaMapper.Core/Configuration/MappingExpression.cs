using System.Linq.Expressions;

namespace DeltaMapper.Configuration;

internal sealed class MappingExpression<TSrc, TDst> : IMappingExpression<TSrc, TDst>
{
    internal TypeMapConfiguration TypeMapConfig { get; }

    internal MappingExpression() : this(MemberList.Destination) { }

    internal MappingExpression(MemberList memberList)
    {
        TypeMapConfig = new TypeMapConfiguration
        {
            SourceType = typeof(TSrc),
            DestinationType = typeof(TDst),
            MemberValidation = memberList
        };
    }

    public IMappingExpression<TSrc, TDst> ForMember<TMember>(
        Expression<Func<TDst, TMember>> destinationMember,
        Action<IMemberOptions<TSrc>> options)
    {
        ArgumentNullException.ThrowIfNull(destinationMember);
        ArgumentNullException.ThrowIfNull(options);
        var memberName = GetMemberName(destinationMember);
        var memberOptions = new MemberOptions<TSrc>();
        options(memberOptions);

        TypeMapConfig.MemberConfigurations.Add(new MemberConfiguration
        {
            DestinationMemberName = memberName,
            CustomResolver = memberOptions.Resolver,
            ResolverReturnType = memberOptions.ResolverReturnType,
            IsIgnored = memberOptions.IsIgnored,
            NullSubstituteValue = memberOptions.NullSubstituteValue,
            HasNullSubstitute = memberOptions.HasNullSubstitute,
            ConditionPredicate = memberOptions.ConditionPredicate
        });

        return this;
    }

    public IMappingExpression<TSrc, TDst> BeforeMap(Action<TSrc, TDst> beforeAction)
    {
        ArgumentNullException.ThrowIfNull(beforeAction);
        TypeMapConfig.BeforeMapAction = (src, dst) => beforeAction((TSrc)src, (TDst)dst);
        return this;
    }

    public IMappingExpression<TSrc, TDst> AfterMap(Action<TSrc, TDst> afterAction)
    {
        ArgumentNullException.ThrowIfNull(afterAction);
        TypeMapConfig.AfterMapAction = (src, dst) => afterAction((TSrc)src, (TDst)dst);
        return this;
    }

    public IMappingExpression<TSrc, TDst> ConstructUsing(Func<TSrc, TDst> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        TypeMapConfig.CustomFactory = src => factory((TSrc)src)!;
        return this;
    }

    public IMappingExpression<TSrc, TDst> ReverseMap()
    {
        TypeMapConfig.HasReverseMap = true;
        return this;
    }

    private static string GetMemberName<TMember>(Expression<Func<TDst, TMember>> expression)
    {
        var body = expression.Body;
        // Handle UnaryExpression (Convert) wrapping value-type member access
        if (body is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
            body = unary.Operand;
        if (body is MemberExpression memberExpression)
            return memberExpression.Member.Name;
        throw new ArgumentException("Expression must be a member access expression (e.g., d => d.PropertyName).");
    }
}
