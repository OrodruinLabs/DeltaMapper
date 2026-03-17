using System.Collections.Frozen;
using DeltaMapper.Abstractions;
using DeltaMapper.Exceptions;
using DeltaMapper.Middleware;
using DeltaMapper.Runtime;

namespace DeltaMapper.Configuration;

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

        // Compiled maps (with hooks/middleware) take precedence over source-generated delegates
        // so that BeforeMap/AfterMap and other pipeline behaviours are honoured when explicitly configured.
        if (_registry.TryGetValue((srcType, dstType), out var compiledMap))
            return compiledMap.Execute(source, existingDest, ctx);

        // Fall back to source-generated delegates registered via GeneratedMapRegistry
        if (GeneratedMapRegistry.TryGet(srcType, dstType, out var generatedDelegate))
        {
            var destination = existingDest ?? Activator.CreateInstance(dstType)
                ?? throw new InvalidOperationException($"Cannot create an instance of '{dstType.FullName}'.");
            ctx.Register(source, destination);
            generatedDelegate!(source, destination);
            return destination;
        }

        throw DeltaMapperException.ForMissingMapping(srcType, dstType);
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
