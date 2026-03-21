namespace DeltaMapper;

/// <summary>
/// Describes the kind of change that occurred on a mapped property.
/// </summary>
public enum ChangeKind
{
    /// <summary>The property value differs between the before and after snapshots.</summary>
    Modified,

    /// <summary>The element was not present before mapping but exists after (collection growth).</summary>
    Added,

    /// <summary>The element was present before mapping but absent after (collection shrink).</summary>
    Removed,
}
