namespace DeltaMapper.Abstractions;

/// <summary>
/// Defines the contract for object mapping operations.
/// </summary>
public interface IMapper
{
    /// <summary>
    /// Maps the source object to a new instance of TDestination. Source type is inferred at runtime.
    /// </summary>
    TDestination Map<TDestination>(object source);

    /// <summary>
    /// Maps from TSource to a new instance of TDestination.
    /// </summary>
    TDestination Map<TSource, TDestination>(TSource source);

    /// <summary>
    /// Maps from TSource onto an existing TDestination instance, updating its properties.
    /// </summary>
    TDestination Map<TSource, TDestination>(TSource source, TDestination destination);

    /// <summary>
    /// Maps each element in the source enumerable to TDestination, returning a read-only list.
    /// </summary>
    IReadOnlyList<TDestination> MapList<TSource, TDestination>(IEnumerable<TSource> source);

    /// <summary>
    /// Maps the source object to the specified destination type. Non-generic overload for dynamic scenarios.
    /// </summary>
    object Map(object source, Type sourceType, Type destinationType);
}
