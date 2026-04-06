using DeltaMapper.Configuration;

namespace DeltaMapper.EFCore;

/// <summary>
/// Extension methods for projecting IQueryable sources using DeltaMapper configuration.
/// Translates mapping profiles into expression trees that database providers can convert to SQL.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Projects the queryable source to <typeparamref name="TDst"/> using the mapping
    /// configuration. The projection is translated to an expression tree that database
    /// providers can convert to SQL.
    /// </summary>
    public static IQueryable<TDst> ProjectTo<TSrc, TDst>(
        this IQueryable<TSrc> source, MapperConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(config);

        var typeMap = config.GetTypeMap(typeof(TSrc), typeof(TDst))
            ?? throw new DeltaMapperException(
                $"No mapping configuration found for {typeof(TSrc).Name} → {typeof(TDst).Name}. " +
                $"Register a map with CreateMap<{typeof(TSrc).Name}, {typeof(TDst).Name}>() in a Profile.");

        ValidateUnsupportedFeatures(typeMap, typeof(TSrc), typeof(TDst));

        var projection = ProjectionBuilder.Build<TSrc, TDst>(typeMap, config);
        return source.Select(projection);
    }

    /// <summary>
    /// Projects the queryable source to <typeparamref name="TDst"/> by inferring the source
    /// element type from the IQueryable. Convenience overload for common usage patterns.
    /// </summary>
    public static IQueryable<TDst> ProjectTo<TDst>(
        this IQueryable source, MapperConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(config);

        var srcType = source.ElementType;
        var dstType = typeof(TDst);

        var typeMap = config.GetTypeMap(srcType, dstType)
            ?? throw new DeltaMapperException(
                $"No mapping configuration found for {srcType.Name} → {dstType.Name}. " +
                $"Register a map with CreateMap<{srcType.Name}, {dstType.Name}>() in a Profile.");

        ValidateUnsupportedFeatures(typeMap, srcType, dstType);

        // Build projection via reflection to call the generic Build<TSrc, TDst>()
        var buildMethod = typeof(ProjectionBuilder)
            .GetMethod("Build", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!
            .MakeGenericMethod(srcType, dstType);
        var projection = buildMethod.Invoke(null, [typeMap, config])!;

        // Call source.Select(projection) via reflection
        var selectMethod = typeof(Queryable).GetMethods()
            .First(m => m.Name == "Select" && m.GetParameters().Length == 2)
            .MakeGenericMethod(srcType, dstType);
        return (IQueryable<TDst>)selectMethod.Invoke(null, [source, projection])!;
    }

    private static void ValidateUnsupportedFeatures(TypeMapConfiguration typeMap, Type srcType, Type dstType)
    {
        if (typeMap.BeforeMapAction != null)
            throw new DeltaMapperException(
                $"ProjectTo does not support BeforeMap on {srcType.Name} → {dstType.Name}. " +
                $"Remove BeforeMap from the map configuration or use Map() instead.");

        if (typeMap.AfterMapAction != null)
            throw new DeltaMapperException(
                $"ProjectTo does not support AfterMap on {srcType.Name} → {dstType.Name}. " +
                $"Remove AfterMap from the map configuration or use Map() instead.");

        if (typeMap.CustomFactory != null)
            throw new DeltaMapperException(
                $"ProjectTo does not support ConstructUsing on {srcType.Name} → {dstType.Name}. " +
                $"Remove ConstructUsing from the map configuration or use Map() instead.");
    }
}
