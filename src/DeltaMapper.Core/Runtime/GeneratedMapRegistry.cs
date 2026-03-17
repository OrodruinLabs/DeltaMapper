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
    /// Removes all registered delegates. Intended for test isolation only.
    /// </summary>
    internal static void Clear()
    {
        _registry.Clear();
        _boxedRegistry.Clear();
    }
}
