using System.Collections.Frozen;
using DeltaMapper.Exceptions;
using DeltaMapper.Middleware;

namespace DeltaMapper;

/// <summary>
/// Holds compiled mapping configuration. Created at startup, immutable after construction.
/// </summary>
public sealed class MapperConfiguration
{
    private readonly FrozenDictionary<(Type, Type), CompiledMap> _registry;
    private readonly MappingPipeline _pipeline;

    private MapperConfiguration(
        FrozenDictionary<(Type, Type), CompiledMap> registry,
        MappingPipeline pipeline)
    {
        _registry = registry;
        _pipeline = pipeline;
    }

    /// <summary>
    /// Internal parameterless constructor for creating an empty configuration.
    /// Used by MapperContext tests and middleware pipeline tests.
    /// </summary>
    internal MapperConfiguration()
    {
        _registry = new Dictionary<(Type, Type), CompiledMap>().ToFrozenDictionary();
        _pipeline = new MappingPipeline([]);
    }

    /// <summary>
    /// Creates a MapperConfiguration by scanning profiles and compiling all mappings.
    /// </summary>
    public static MapperConfiguration Create(Action<MapperConfigurationBuilder> configure)
    {
        var builder = new MapperConfigurationBuilder();
        configure(builder);
        return builder.Build();
    }

    /// <summary>
    /// Creates an IMapper instance from this configuration.
    /// </summary>
    public IMapper CreateMapper() => new Mapper(this);

    internal object Execute(object source, Type srcType, Type dstType, MapperContext ctx)
    {
        return _pipeline.Execute(source, dstType, ctx, () => ExecuteCore(source, srcType, dstType, ctx, null));
    }

    internal object Execute(object source, Type srcType, Type dstType, MapperContext ctx, object? existingDest)
    {
        return _pipeline.Execute(source, dstType, ctx, () => ExecuteCore(source, srcType, dstType, ctx, existingDest));
    }

    private object ExecuteCore(object source, Type srcType, Type dstType, MapperContext ctx, object? existingDest)
    {
        // Check circular reference
        if (ctx.TryGetMapped(source, out var cached))
            return cached!;

        if (!_registry.TryGetValue((srcType, dstType), out var compiledMap))
            throw DeltaMapperException.ForMissingMapping(srcType, dstType);

        return compiledMap.Execute(source, existingDest, ctx);
    }

    internal bool HasMap(Type srcType, Type dstType) => _registry.ContainsKey((srcType, dstType));

    /// <summary>
    /// Called by MapperConfigurationBuilder.Build() to create the final immutable configuration.
    /// </summary>
    internal static MapperConfiguration CreateFromBuilder(
        Dictionary<(Type, Type), CompiledMap> maps,
        IReadOnlyList<IMappingMiddleware> middlewares)
    {
        var frozen = maps.ToFrozenDictionary();
        return new MapperConfiguration(frozen, new MappingPipeline(middlewares));
    }
}
