using System.Collections.Frozen;
using DeltaMapper.Configuration;
using DeltaMapper.Middleware;
using DeltaMapper.Runtime;

namespace DeltaMapper;

/// <summary>
/// Holds compiled mapping configuration. Created at startup, immutable after construction.
/// </summary>
public sealed class MapperConfiguration
{
    private readonly FrozenDictionary<(Type, Type), CompiledMap> _registry;
    private readonly MappingPipeline _pipeline;
    private readonly bool _hasMiddleware;
    private readonly IReadOnlyList<TypeMapConfiguration> _typeMaps;

    private MapperConfiguration(
        FrozenDictionary<(Type, Type), CompiledMap> registry,
        MappingPipeline pipeline,
        bool hasMiddleware,
        IReadOnlyList<TypeMapConfiguration> typeMaps)
    {
        _registry = registry;
        _pipeline = pipeline;
        _hasMiddleware = hasMiddleware;
        _typeMaps = typeMaps;
    }

    /// <summary>
    /// Internal parameterless constructor for creating an empty configuration.
    /// Used by MapperContext tests and middleware pipeline tests.
    /// </summary>
    internal MapperConfiguration()
    {
        _registry = new Dictionary<(Type, Type), CompiledMap>().ToFrozenDictionary();
        _pipeline = new MappingPipeline([]);
        _hasMiddleware = false;
        _typeMaps = [];
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

    internal bool HasMiddleware => _hasMiddleware;

    internal object Execute(object source, Type srcType, Type dstType, MapperContext ctx)
    {
        if (!_hasMiddleware)
            return ExecuteCore(source, srcType, dstType, ctx, null);
        return _pipeline.Execute(source, dstType, ctx, () => ExecuteCore(source, srcType, dstType, ctx, null));
    }

    internal object Execute(object source, Type srcType, Type dstType, MapperContext ctx, object? existingDest)
    {
        if (!_hasMiddleware)
            return ExecuteCore(source, srcType, dstType, ctx, existingDest);
        return _pipeline.Execute(source, dstType, ctx, () => ExecuteCore(source, srcType, dstType, ctx, existingDest));
    }

    private object ExecuteCore(object source, Type srcType, Type dstType, MapperContext ctx, object? existingDest)
    {
        // Check circular reference
        if (ctx.TryGetMapped(source, dstType, out var cached))
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
            ctx.Register(source, dstType, destination);
            generatedDelegate!(source, destination);
            return destination;
        }

        throw DeltaMapperException.ForMissingMapping(srcType, dstType);
    }

    internal bool HasMap(Type srcType, Type dstType) => _registry.ContainsKey((srcType, dstType));

    /// <summary>
    /// Validates that all destination members on every registered type map are mapped.
    /// Throws DeltaMapperException listing any unmapped members.
    /// </summary>
    public void AssertConfigurationIsValid()
    {
        var errors = new List<string>();
        foreach (var tm in _typeMaps)
        {
            // Check writable properties
            var destProps = tm.DestinationType
                .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .Where(p => p.CanWrite);

            foreach (var prop in destProps)
            {
                if (!tm.MappedDestinationMembers.Contains(prop.Name))
                {
                    errors.Add($"Unmapped property '{prop.Name}' on destination type " +
                               $"'{tm.DestinationType.Name}' (source: '{tm.SourceType.Name}').");
                }
            }

            // Check constructor parameters for types that use constructor injection
            var ctors = tm.DestinationType.GetConstructors(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var bestCtor = ctors.OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();
            if (bestCtor != null && bestCtor.GetParameters().Length > 0)
            {
                foreach (var param in bestCtor.GetParameters())
                {
                    if (!tm.MappedDestinationMembers.Contains(param.Name!))
                    {
                        errors.Add($"Unmapped constructor parameter '{param.Name}' on destination type " +
                                   $"'{tm.DestinationType.Name}' (source: '{tm.SourceType.Name}').");
                    }
                }
            }
        }

        if (errors.Count > 0)
        {
            throw new DeltaMapperException(
                $"Configuration validation failed with {errors.Count} unmapped " +
                $"{(errors.Count == 1 ? "member" : "members")}:\n" +
                string.Join("\n", errors));
        }
    }

    /// <summary>
    /// Called by MapperConfigurationBuilder.Build() to create the final immutable configuration.
    /// </summary>
    internal static MapperConfiguration CreateFromBuilder(
        Dictionary<(Type, Type), CompiledMap> maps,
        IReadOnlyList<IMappingMiddleware> middlewares,
        IReadOnlyList<TypeMapConfiguration> typeMaps)
    {
        var frozen = maps.ToFrozenDictionary();
        return new MapperConfiguration(frozen, new MappingPipeline(middlewares), middlewares.Count > 0, typeMaps);
    }
}
