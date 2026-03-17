using DeltaMapper.Configuration;

namespace DeltaMapper.Runtime;

/// <summary>
/// Tracks per-call mapping state including circular reference detection.
/// Created fresh for each top-level Map() call.
/// </summary>
public sealed class MapperContext
{
    internal MapperConfiguration Config { get; }
    private readonly Dictionary<object, object> _visited = new(ReferenceEqualityComparer.Instance);

    internal MapperContext(MapperConfiguration config) => Config = config;

    /// <summary>
    /// Attempts to retrieve a previously mapped destination for the given source object.
    /// Uses reference equality (identity comparison).
    /// </summary>
    internal bool TryGetMapped(object source, out object? mapped)
        => _visited.TryGetValue(source, out mapped);

    /// <summary>
    /// Registers a source-to-destination mapping for circular reference detection.
    /// </summary>
    internal void Register(object source, object dest)
        => _visited[source] = dest;
}
