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
    private readonly IReadOnlyList<ValidationSnapshot> _validationSnapshots;

    private MapperConfiguration(
        FrozenDictionary<(Type, Type), CompiledMap> registry,
        MappingPipeline pipeline,
        bool hasMiddleware,
        IReadOnlyList<ValidationSnapshot> validationSnapshots)
    {
        _registry = registry;
        _pipeline = pipeline;
        _hasMiddleware = hasMiddleware;
        _validationSnapshots = validationSnapshots;
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
        _validationSnapshots = [];
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
    /// Validates that all destination members (properties and constructor parameters) on every
    /// registered type map are mapped. Throws DeltaMapperException listing any unmapped members.
    /// </summary>
    public void AssertConfigurationIsValid()
    {
        var errors = new List<string>();
        foreach (var snap in _validationSnapshots)
        {
            // Check writable properties — skip properties covered by constructor params to avoid double-reporting
            var ctorParamSet = snap.UsesConstructorInjection
                ? new HashSet<string>(snap.ConstructorParameterNames, StringComparer.OrdinalIgnoreCase)
                : null;

            var destProps = snap.DestinationType
                .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .Where(p => p.CanWrite);

            foreach (var prop in destProps)
            {
                if (ctorParamSet?.Contains(prop.Name) == true)
                    continue; // Validated in constructor param check below

                if (!snap.MappedMembers.Contains(prop.Name))
                {
                    errors.Add($"Unmapped property '{prop.Name}' on destination type " +
                               $"'{snap.DestinationType.Name}' (source: '{snap.SourceType.Name}').");
                }
            }

            // Check constructor parameters only for types that actually use constructor injection
            if (snap.UsesConstructorInjection)
            {
                foreach (var paramName in snap.ConstructorParameterNames)
                {
                    if (!snap.MappedMembers.Contains(paramName))
                    {
                        errors.Add($"Unmapped constructor parameter '{paramName}' on destination type " +
                                   $"'{snap.DestinationType.Name}' (source: '{snap.SourceType.Name}').");
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

        // Deduplicate type maps by (SourceType, DestinationType), keeping the last registration.
        // This matches the compiled registry behavior where later maps overwrite earlier ones.
        var latestTypeMaps = new Dictionary<(Type, Type), TypeMapConfiguration>();
        foreach (var tm in typeMaps)
            latestTypeMaps[(tm.SourceType, tm.DestinationType)] = tm;

        // Create lightweight validation snapshots — avoids retaining full TypeMapConfiguration (delegates, resolvers)
        var snapshots = latestTypeMaps.Values.Select(tm => new ValidationSnapshot(
            tm.SourceType,
            tm.DestinationType,
            tm.MappedDestinationMembers.ToFrozenSet(StringComparer.OrdinalIgnoreCase),
            tm.UsesConstructorInjection,
            tm.ConstructorParameterNames.AsReadOnly())).ToList();
        return new MapperConfiguration(frozen, new MappingPipeline(middlewares), middlewares.Count > 0, snapshots);
    }

    /// <summary>
    /// Immutable snapshot of type map metadata for runtime validation.
    /// </summary>
    internal sealed record ValidationSnapshot(
        Type SourceType,
        Type DestinationType,
        FrozenSet<string> MappedMembers,
        bool UsesConstructorInjection,
        IReadOnlyList<string> ConstructorParameterNames);
}
