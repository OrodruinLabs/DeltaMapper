using System.Collections.Concurrent;

namespace DeltaMapper.Runtime;

/// <summary>
/// Thread-safe registry for source-generated mapping delegates.
/// [ModuleInitializer] in generated code calls Register() before Main() runs.
/// MapperConfiguration checks TryGet() before falling back to expression-compiled delegates.
/// </summary>
public static class GeneratedMapRegistry
{
    private static readonly ConcurrentDictionary<(Type, Type), Delegate> _registry = new();

    // Boxed Action<object,object> wrappers keyed the same way — avoids DynamicInvoke at call time.
    private static readonly ConcurrentDictionary<(Type, Type), Action<object, object>> _boxedRegistry = new();

    // Factory delegates: create AND map in one call — used by the fast path in Mapper.Map<>.
    private static readonly ConcurrentDictionary<(Type, Type), Delegate> _factoryRegistry = new();

    /// <summary>
    /// Registers a source-generated mapping delegate for the given type pair.
    /// Called by [ModuleInitializer] in generated assemblies.
    /// </summary>
    public static void Register<TSource, TDestination>(Action<TSource, TDestination> mapAction)
    {
        ArgumentNullException.ThrowIfNull(mapAction);

        var key = (typeof(TSource), typeof(TDestination));
        _registry[key] = mapAction;
        _boxedRegistry[key] = (src, dst) => mapAction((TSource)src, (TDestination)dst);
    }

    /// <summary>
    /// Attempts to retrieve a registered mapping delegate for the given type pair.
    /// </summary>
    public static bool TryGet<TSource, TDestination>(out Action<TSource, TDestination>? mapAction)
    {
        if (_registry.TryGetValue((typeof(TSource), typeof(TDestination)), out var del))
        {
            mapAction = (Action<TSource, TDestination>)del;
            return true;
        }
        mapAction = null;
        return false;
    }

    /// <summary>
    /// Registers a source-generated factory delegate for the given type pair.
    /// The factory creates a new destination instance AND maps all properties in one call.
    /// Called by [ModuleInitializer] in generated assemblies.
    /// </summary>
    public static void RegisterFactory<TSource, TDestination>(Func<TSource, TDestination> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factoryRegistry[(typeof(TSource), typeof(TDestination))] = factory;
    }

    /// <summary>
    /// Attempts to retrieve a registered factory delegate for the given type pair.
    /// </summary>
    public static bool TryGetFactory<TSource, TDestination>(out Func<TSource, TDestination>? factory)
    {
        if (_factoryRegistry.TryGetValue((typeof(TSource), typeof(TDestination)), out var del))
        {
            factory = (Func<TSource, TDestination>)del;
            return true;
        }
        factory = null;
        return false;
    }

    /// <summary>
    /// Non-generic overload for runtime type lookups.
    /// Returns a boxed <see cref="Action{Object,Object}"/> wrapper to avoid DynamicInvoke.
    /// </summary>
    public static bool TryGet(Type sourceType, Type destType, out Action<object, object>? mapAction)
    {
        ArgumentNullException.ThrowIfNull(sourceType);
        ArgumentNullException.ThrowIfNull(destType);

        return _boxedRegistry.TryGetValue((sourceType, destType), out mapAction);
    }

    /// <summary>
    /// Checks whether a factory delegate is registered for the given type pair.
    /// Used by smart collection detection in Mapper.Map&lt;TDest&gt;(object).
    /// </summary>
    public static bool HasFactory(Type srcType, Type dstType)
    {
        ArgumentNullException.ThrowIfNull(srcType);
        ArgumentNullException.ThrowIfNull(dstType);
        return _factoryRegistry.ContainsKey((srcType, dstType));
    }

    /// <summary>
    /// Removes all registered delegates. Intended for test isolation only.
    /// </summary>
    internal static void Clear()
    {
        _registry.Clear();
        _boxedRegistry.Clear();
        _factoryRegistry.Clear();
    }
}
