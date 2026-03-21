namespace DeltaMapper;

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
    /// Maps each element in the source enumerable to TDestination, returning a list.
    /// </summary>
    List<TDestination> Map<TSource, TDestination>(IEnumerable<TSource> source);

    /// <summary>
    /// Maps the source object to the specified destination type. Non-generic overload for dynamic scenarios.
    /// </summary>
    object Map(object source, Type sourceType, Type destinationType);

    /// <summary>
    /// Maps the source object onto an existing destination instance. Both types are inferred at runtime.
    /// For record/init-only destinations, a new instance may be returned instead of updating in place.
    /// </summary>
    object Map(object source, object destination);

    /// <summary>
    /// Maps the source object onto an existing TDestination instance. Source type is inferred at runtime.
    /// For record/init-only destinations, a new instance may be returned instead of updating in place.
    /// </summary>
    TDestination Map<TDestination>(object source, TDestination destination);

    /// <summary>
    /// Maps from TSource onto the existing TDestination instance and returns a diff of changed properties.
    /// </summary>
    MappingDiff<TDestination> Patch<TSource, TDestination>(TSource source, TDestination destination);
}
