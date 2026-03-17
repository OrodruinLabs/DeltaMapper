namespace DeltaMapper;

/// <summary>
/// Runtime mapper that executes compiled mapping delegates.
/// Created via MapperConfiguration.CreateMapper().
/// </summary>
public sealed class Mapper(MapperConfiguration config) : IMapper
{

    /// <inheritdoc />
    public TDestination Map<TDestination>(object source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var ctx = new MapperContext(config);
        return (TDestination)config.Execute(source, source.GetType(), typeof(TDestination), ctx);
    }

    /// <inheritdoc />
    public TDestination Map<TSource, TDestination>(TSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var ctx = new MapperContext(config);
        return (TDestination)config.Execute(source, typeof(TSource), typeof(TDestination), ctx);
    }

    /// <inheritdoc />
    public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        var ctx = new MapperContext(config);
        return (TDestination)config.Execute(source, typeof(TSource), typeof(TDestination), ctx, destination);
    }

    /// <inheritdoc />
    public IReadOnlyList<TDestination> MapList<TSource, TDestination>(IEnumerable<TSource> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var ctx = new MapperContext(config);
        var sourceList = source as ICollection<TSource> ?? source.ToList();
        var result = new List<TDestination>(sourceList.Count);
        foreach (var item in sourceList)
        {
            if (item is null)
                throw new ArgumentNullException(nameof(source), "Source enumerable contains a null element.");
            result.Add((TDestination)config.Execute(item, typeof(TSource), typeof(TDestination), ctx));
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

        var ctx = new MapperContext(config);
        return config.Execute(source, sourceType, destinationType, ctx);
    }
}
