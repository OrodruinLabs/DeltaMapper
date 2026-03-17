namespace DeltaMapper.Diff;

/// <summary>
/// Represents a single property-level change observed during a mapping diff operation.
/// </summary>
/// <param name="PropertyName">The name of the property that changed.</param>
/// <param name="From">The value before mapping (source side).</param>
/// <param name="To">The value after mapping (destination side).</param>
/// <param name="Kind">The kind of change that was detected.</param>
public sealed record PropertyChange(
    string PropertyName,
    object? From,
    object? To,
    ChangeKind Kind);
