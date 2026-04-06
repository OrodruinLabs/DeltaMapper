using System.Linq.Expressions;
using System.Reflection;
using DeltaMapper.Configuration;

namespace DeltaMapper.EFCore;

/// <summary>
/// Builds Expression{Func{TSrc, TDst}} projection trees from TypeMapConfiguration.
/// The resulting expression can be passed to IQueryable.Select() for SQL translation.
/// </summary>
internal static class ProjectionBuilder
{
    /// <summary>
    /// Builds a projection expression for the given type pair using the provided configuration.
    /// </summary>
    internal static Expression<Func<TSrc, TDst>> Build<TSrc, TDst>(
        TypeMapConfiguration typeMap, MapperConfiguration config)
    {
        var srcType = typeof(TSrc);
        var dstType = typeof(TDst);
        var srcParam = Expression.Parameter(srcType, "src");

        var bindings = BuildMemberBindings(srcParam, srcType, dstType, typeMap, config);

        var body = Expression.MemberInit(Expression.New(dstType), bindings);
        return Expression.Lambda<Func<TSrc, TDst>>(body, srcParam);
    }

    private static List<MemberBinding> BuildMemberBindings(
        Expression srcExpr,
        Type srcType,
        Type dstType,
        TypeMapConfiguration typeMap,
        MapperConfiguration config)
    {
        var srcProps = srcType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var dstProps = dstType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var bindings = new List<MemberBinding>();

        foreach (var dstProp in dstProps)
        {
            if (!dstProp.CanWrite) continue;

            var memberConfig = typeMap.MemberConfigurations
                .FirstOrDefault(mc => mc.DestinationMemberName.Equals(
                    dstProp.Name, StringComparison.OrdinalIgnoreCase));

            // Ignored members — skip
            if (memberConfig is { IsIgnored: true })
                continue;

            // Custom MapFrom expression
            if (memberConfig?.SourceExpression is LambdaExpression sourceExpr)
            {
                // Rewrite the parameter to use our srcParam
                var rewritten = new ParameterRewriter(sourceExpr.Parameters[0], srcExpr)
                    .Visit(sourceExpr.Body);
                bindings.Add(Expression.Bind(dstProp, rewritten));
                continue;
            }

            // NullSubstitute
            if (memberConfig is { HasNullSubstitute: true })
            {
                var srcProp = FindSourceProperty(srcProps, dstProp.Name);
                if (srcProp != null)
                {
                    var access = Expression.Property(srcExpr, srcProp);
                    var substitute = Expression.Constant(memberConfig.NullSubstituteValue, dstProp.PropertyType);
                    var coalesce = Expression.Coalesce(access, substitute);
                    bindings.Add(Expression.Bind(dstProp, coalesce));
                }
                else
                {
                    var substitute = Expression.Constant(memberConfig.NullSubstituteValue, dstProp.PropertyType);
                    bindings.Add(Expression.Bind(dstProp, substitute));
                }
                continue;
            }

            // Unsupported features — throw clear errors
            if (memberConfig?.ConditionPredicate != null)
                throw new DeltaMapperException(
                    $"ProjectTo does not support Condition() on member '{dstProp.Name}'. " +
                    $"Remove the condition from the {srcType.Name} → {dstType.Name} map or use Map() instead.");

            // Convention matching — same name
            var conventionSrcProp = FindSourceProperty(srcProps, dstProp.Name);
            if (conventionSrcProp != null)
            {
                var access = Expression.Property(srcExpr, conventionSrcProp);

                // Nested complex type — build sub-projection
                if (IsComplexType(conventionSrcProp.PropertyType) && IsComplexType(dstProp.PropertyType))
                {
                    var nestedTypeMap = config.GetTypeMap(conventionSrcProp.PropertyType, dstProp.PropertyType);
                    if (nestedTypeMap != null)
                    {
                        var nestedBindings = BuildMemberBindings(
                            access, conventionSrcProp.PropertyType, dstProp.PropertyType, nestedTypeMap, config);
                        var nestedInit = Expression.MemberInit(Expression.New(dstProp.PropertyType), nestedBindings);
                        bindings.Add(Expression.Bind(dstProp, nestedInit));
                        continue;
                    }
                }

                // Collection navigation — build Select() sub-projection
                if (IsCollectionNavigation(conventionSrcProp.PropertyType, dstProp.PropertyType,
                    out var srcElem, out var dstElem))
                {
                    var nestedTypeMap = config.GetTypeMap(srcElem!, dstElem!);
                    if (nestedTypeMap != null)
                    {
                        var elemParam = Expression.Parameter(srcElem!, "e");
                        var elemBindings = BuildMemberBindings(
                            elemParam, srcElem!, dstElem!, nestedTypeMap, config);
                        var elemInit = Expression.MemberInit(Expression.New(dstElem!), elemBindings);
                        var selectLambda = Expression.Lambda(elemInit, elemParam);

                        // .Select(e => new DstElem { ... })
                        var selectMethod = typeof(Enumerable).GetMethods()
                            .First(m => m.Name == "Select" && m.GetParameters().Length == 2)
                            .MakeGenericMethod(srcElem!, dstElem!);
                        var selectCall = Expression.Call(selectMethod, access, selectLambda);

                        // .ToList()
                        var toListMethod = typeof(Enumerable).GetMethod("ToList")!
                            .MakeGenericMethod(dstElem!);
                        var toListCall = Expression.Call(toListMethod, selectCall);

                        bindings.Add(Expression.Bind(dstProp, toListCall));
                        continue;
                    }
                }

                // Direct assignment (same type or implicitly convertible)
                if (conventionSrcProp.PropertyType == dstProp.PropertyType)
                {
                    bindings.Add(Expression.Bind(dstProp, access));
                }
                else
                {
                    bindings.Add(Expression.Bind(dstProp, Expression.Convert(access, dstProp.PropertyType)));
                }
                continue;
            }

            // Flattening — CustomerName → src.Customer.Name
            var flattenedExpr = TryBuildFlattenedExpression(srcType, srcExpr, dstProp.Name);
            if (flattenedExpr != null)
            {
                var expr = flattenedExpr.Type == dstProp.PropertyType
                    ? flattenedExpr
                    : Expression.Convert(flattenedExpr, dstProp.PropertyType);
                bindings.Add(Expression.Bind(dstProp, expr));
            }
        }

        return bindings;
    }

    private static Expression? TryBuildFlattenedExpression(
        Type srcType, Expression srcExpr, string dstPropertyName)
    {
        return TryBuildChain(srcType, srcExpr, dstPropertyName);
    }

    private static Expression? TryBuildChain(Type currentType, Expression currentExpr, string remaining)
    {
        var props = currentType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        for (int len = remaining.Length; len >= 1; len--)
        {
            var prefix = remaining[..len];
            var suffix = remaining[len..];

            var matched = props.FirstOrDefault(p =>
                p.Name.Equals(prefix, StringComparison.OrdinalIgnoreCase));
            if (matched == null) continue;

            var propAccess = Expression.Property(currentExpr, matched);

            if (suffix.Length == 0)
                return propAccess;

            if (!IsTraversableType(matched.PropertyType)) continue;

            var inner = TryBuildChain(matched.PropertyType, propAccess, suffix);
            if (inner != null) return inner;
        }

        return null;
    }

    private static PropertyInfo? FindSourceProperty(PropertyInfo[] srcProps, string name)
    {
        return srcProps.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsComplexType(Type type)
    {
        return !type.IsPrimitive
            && !type.IsEnum
            && type != typeof(string)
            && type != typeof(decimal)
            && type != typeof(DateTime)
            && type != typeof(DateTimeOffset)
            && type != typeof(Guid)
            && !IsCollectionType(type);
    }

    private static bool IsCollectionType(Type type)
    {
        if (type == typeof(string)) return false;
        return type.GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
    }

    private static bool IsCollectionNavigation(Type srcType, Type dstType,
        out Type? srcElem, out Type? dstElem)
    {
        srcElem = null;
        dstElem = null;

        var srcEnumerable = srcType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        var dstEnumerable = dstType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (srcEnumerable == null || dstEnumerable == null) return false;

        srcElem = srcEnumerable.GetGenericArguments()[0];
        dstElem = dstEnumerable.GetGenericArguments()[0];

        return srcElem != dstElem && IsComplexType(srcElem) && IsComplexType(dstElem);
    }

    private static bool IsTraversableType(Type type)
    {
        return !type.IsPrimitive
            && !type.IsEnum
            && type != typeof(string)
            && type != typeof(decimal);
    }

    /// <summary>
    /// Rewrites all references to one parameter to use a different expression.
    /// Used to rebind MapFrom lambda parameters to the projection's source parameter.
    /// </summary>
    private sealed class ParameterRewriter(ParameterExpression oldParam, Expression newExpr)
        : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
            => node == oldParam ? newExpr : base.VisitParameter(node);
    }
}
