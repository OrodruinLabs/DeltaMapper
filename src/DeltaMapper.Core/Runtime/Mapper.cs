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
    private readonly MapperConfiguration _config;
    private readonly bool _fastPathEnabled;

    internal Mapper(MapperConfiguration config)
    {
        _config = config;
        _fastPathEnabled = !config.HasMiddleware;
    }

    /// <inheritdoc />
    public TDestination Map<TDestination>(object source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var ctx = new MapperContext(_config);
        return (TDestination)_config.Execute(source, source.GetType(), typeof(TDestination), ctx);
    }

    /// <inheritdoc />
    public TDestination Map<TSource, TDestination>(TSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        // Fast path: source-gen factory, no middleware, no compiled profile override.
        // Bypasses MapperContext creation, pipeline, and Activator entirely.
        if (_fastPathEnabled
            && !_config.HasMap(typeof(TSource), typeof(TDestination))
            && GeneratedMapRegistry.TryGetFactory<TSource, TDestination>(out var factory))
            return factory!(source);

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
    public IReadOnlyList<TDestination> MapList<TSource, TDestination>(IEnumerable<TSource> source)
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
}
