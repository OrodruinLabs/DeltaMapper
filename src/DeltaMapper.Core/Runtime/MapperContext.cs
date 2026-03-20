using System.Runtime.CompilerServices;
using DeltaMapper;

namespace DeltaMapper.Runtime;

/// <summary>
/// Tracks per-call mapping state including circular reference detection.
/// Created fresh for each top-level Map() call.
/// </summary>
public sealed class MapperContext
{
    internal MapperConfiguration Config { get; }
    private Dictionary<(object, Type), object>? _visited;

    internal MapperContext(MapperConfiguration config) => Config = config;

    /// <summary>
    /// When true, the current mapping source is an EF Core proxy entity.
    /// Collection navigation properties should be skipped to prevent lazy loading.
    /// </summary>
    internal bool IsProxyMapping { get; set; }

    /// <summary>
    /// Attempts to retrieve a previously mapped destination for the given source object
    /// and destination type. Uses reference equality for source identity comparison.
    /// The destination type is included in the key so the same source object can be
    /// mapped to multiple destination types without cache collisions.
    /// </summary>
    internal bool TryGetMapped(object source, Type destType, out object? mapped)
    {
        if (_visited is null) { mapped = null; return false; }
        return _visited.TryGetValue((source, destType), out mapped);
    }

    /// <summary>
    /// Registers a source-to-destination mapping for circular reference detection.
    /// </summary>
    internal void Register(object source, Type destType, object dest)
    {
        _visited ??= new Dictionary<(object, Type), object>(SourceTypeKeyComparer.Instance);
        _visited[(source, destType)] = dest;
    }

    /// <summary>
    /// Equality comparer for (object, Type) keys that uses reference equality for the source object.
    /// </summary>
    private sealed class SourceTypeKeyComparer : IEqualityComparer<(object, Type)>
    {
        internal static readonly SourceTypeKeyComparer Instance = new();
        public bool Equals((object, Type) x, (object, Type) y)
            => ReferenceEquals(x.Item1, y.Item1) && x.Item2 == y.Item2;
        public int GetHashCode((object, Type) obj)
            => HashCode.Combine(RuntimeHelpers.GetHashCode(obj.Item1), obj.Item2);
    }
}
