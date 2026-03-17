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

    /// <summary>
    /// Registers a source-generated mapping delegate for the given type pair.
    /// Called by [ModuleInitializer] in generated assemblies.
    /// </summary>
    public static void Register<TSource, TDestination>(Action<TSource, TDestination> mapAction)
    {
        _registry[(typeof(TSource), typeof(TDestination))] = mapAction;
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
    /// </summary>
    public static bool TryGet(Type sourceType, Type destType, out Delegate? mapAction)
    {
        return _registry.TryGetValue((sourceType, destType), out mapAction);
    }

    /// <summary>
    /// Removes all registered delegates. Intended for test isolation only.
    /// </summary>
    internal static void Clear() => _registry.Clear();
}
