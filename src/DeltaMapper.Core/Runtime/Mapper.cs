using System.Collections.Concurrent;
using System.Reflection;
using DeltaMapper.Abstractions;
using DeltaMapper.Configuration;
using DeltaMapper.Diff;

namespace DeltaMapper.Runtime;

/// <summary>
/// Runtime mapper that executes compiled mapping delegates.
/// Created via MapperConfiguration.CreateMapper().
/// </summary>
public sealed class Mapper : IMapper
{
    private static readonly object NoFactory = new();
    private readonly MapperConfiguration _config;
    private readonly bool _fastPathEnabled;
    private readonly ConcurrentDictionary<(Type, Type), object> _factoryCache = new();

    internal Mapper(MapperConfiguration config)
    {
        _config = config;
        _fastPathEnabled = !config.HasMiddleware;
    }

    /// <inheritdoc />
    public TDestination Map<TDestination>(object source)
    {
        ArgumentNullException.ThrowIfNull(source);

        // Try exact map first (e.g., a registered List<S> → List<D> map takes priority)
        var srcType = source.GetType();
        var dstType = typeof(TDestination);
        if (_config.HasMap(srcType, dstType))
        {
            var ctx = new MapperContext(_config);
            return (TDestination)_config.Execute(source, srcType, dstType, ctx);
        }

        // Then try smart collection detection
        if (TryMapCollection<TDestination>(source, out var collectionResult))
            return collectionResult;

        var ctx2 = new MapperContext(_config);
        return (TDestination)_config.Execute(source, srcType, dstType, ctx2);
    }

    /// <inheritdoc />
    public TDestination Map<TSource, TDestination>(TSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        // Fast path: cached routing decision — ONE dictionary lookup after first call.
        if (_fastPathEnabled)
        {
            var key = (typeof(TSource), typeof(TDestination));
            if (!_factoryCache.TryGetValue(key, out var cached))
            {
                cached = !_config.HasMap(key.Item1, key.Item2)
                    && GeneratedMapRegistry.TryGetFactory<TSource, TDestination>(out var f)
                    ? (object)f! : NoFactory;
                _factoryCache[key] = cached;
            }

            if (cached is Func<TSource, TDestination> factory)
                return factory(source);
        }

        var ctx = new MapperContext(_config);
        return (TDestination)_config.Execute(source, typeof(TSource), typeof(TDestination), ctx);
    }

    /// <inheritdoc />
    public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        var ctx = new MapperContext(_config);
        return (TDestination)_config.Execute(source, typeof(TSource), typeof(TDestination), ctx, destination);
    }

    /// <inheritdoc />
    public List<TDestination> Map<TSource, TDestination>(IEnumerable<TSource> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var ctx = new MapperContext(_config);
        var sourceList = source as ICollection<TSource> ?? source.ToList();
        var result = new List<TDestination>(sourceList.Count);
        foreach (var item in sourceList)
        {
            if (item is null)
                throw new ArgumentNullException(nameof(source), "Source enumerable contains a null element.");
            result.Add((TDestination)_config.Execute(item, typeof(TSource), typeof(TDestination), ctx));
        }
        return result;
    }

    /// <inheritdoc />
    public object Map(object source, Type sourceType, Type destinationType)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(sourceType);
        ArgumentNullException.ThrowIfNull(destinationType);

        if (!sourceType.IsInstanceOfType(source))
            throw new ArgumentException(
                $"Source object of type '{source.GetType().Name}' is not assignable to sourceType '{sourceType.Name}'.",
                nameof(source));

        var ctx = new MapperContext(_config);
        return _config.Execute(source, sourceType, destinationType, ctx);
    }

    /// <inheritdoc />
    public MappingDiff<TDestination> Patch<TSource, TDestination>(TSource source, TDestination destination)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);

        var props = typeof(TDestination).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToArray();

        var before = new Dictionary<string, object?>(props.Length);
        foreach (var prop in props)
            before[prop.Name] = prop.GetValue(destination);

        Map<TSource, TDestination>(source, destination);

        var after = new Dictionary<string, object?>(props.Length);
        foreach (var prop in props)
            after[prop.Name] = prop.GetValue(destination);

        var changes = DiffEngine.Compare(before, after);
        return new MappingDiff<TDestination> { Result = destination, Changes = changes };
    }

    private bool TryMapCollection<TDestination>(object source, out TDestination result)
    {
        result = default!;
        var dstType = typeof(TDestination);

        // Skip string (implements IEnumerable<char>) — both source and destination
        if (dstType == typeof(string) || source is string)
            return false;

        var dstElementType = GetEnumerableElementType(dstType);
        if (dstElementType == null)
            return false;

        var srcElementType = GetEnumerableElementType(source.GetType());
        if (srcElementType == null)
            return false;

        if (srcElementType == dstElementType)
            return false;

        if (!_config.HasMap(srcElementType, dstElementType)
            && !GeneratedMapRegistry.HasMapping(srcElementType, dstElementType))
            return false;

        // Only support collection types we can actually build
        if (!CanBuildCollectionType(dstType, dstElementType))
            return false;

        var ctx = new MapperContext(_config);
        var items = new List<object>();
        foreach (var item in (System.Collections.IEnumerable)source)
        {
            if (item is null)
                throw new ArgumentNullException(nameof(source), "Source enumerable contains a null element.");
            items.Add(_config.Execute(item, srcElementType, dstElementType, ctx));
        }

        result = (TDestination)BuildCollection(dstType, dstElementType, items);
        return true;
    }

    private static bool CanBuildCollectionType(Type dstType, Type elementType)
    {
        // Single-dimensional arrays only
        if (dstType.IsArray && dstType.GetArrayRank() == 1)
            return true;

        // Check if List<T> is assignable to the destination type
        // This covers: List<T>, IEnumerable<T>, IReadOnlyList<T>, ICollection<T>, IList<T>
        var listType = typeof(List<>).MakeGenericType(elementType);
        if (dstType.IsAssignableFrom(listType))
            return true;

        return false;
    }

    private static Type? GetEnumerableElementType(Type type)
    {
        if (type.IsArray)
            return type.GetElementType();

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            return type.GetGenericArguments()[0];

        var enumerableInterface = type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        return enumerableInterface?.GetGenericArguments()[0];
    }

    private static object BuildCollection(Type dstType, Type elementType, List<object> items)
    {
        var listType = typeof(List<>).MakeGenericType(elementType);
        var list = (System.Collections.IList)Activator.CreateInstance(listType, items.Count)!;
        foreach (var item in items)
            list.Add(item);

        if (dstType.IsArray)
        {
            var array = Array.CreateInstance(elementType, items.Count);
            list.CopyTo(array, 0);
            return array;
        }

        return list;
    }
}
