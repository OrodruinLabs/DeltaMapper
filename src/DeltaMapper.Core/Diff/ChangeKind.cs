namespace DeltaMapper.Diff;

/// <summary>
/// Describes the kind of change that occurred on a mapped property.
/// </summary>
public enum ChangeKind
{
    /// <summary>The property value was present on both source and destination but differs.</summary>
    Modified,

    /// <summary>The property value was absent on the source but present on the destination.</summary>
    Added,

    /// <summary>The property value was present on the source but absent on the destination.</summary>
    Removed,
}
