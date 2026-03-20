using System.Collections.Generic;

namespace DeltaMapper;

/// <summary>
/// Holds the mapped result alongside a list of property-level changes detected during the mapping.
/// </summary>
/// <typeparam name="T">The destination type produced by the mapping.</typeparam>
public sealed class MappingDiff<T>
{
    /// <summary>The mapped destination object.</summary>
    public T Result { get; init; } = default!;

    /// <summary>All property-level changes detected between source and destination.</summary>
    public IReadOnlyList<PropertyChange> Changes { get; init; } = [];

    /// <summary>Returns <see langword="true"/> when at least one property change was detected.</summary>
    public bool HasChanges => Changes.Count > 0;
}
