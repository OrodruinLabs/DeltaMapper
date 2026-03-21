namespace DeltaMapper;

/// <summary>
/// Represents a single property-level change observed during a mapping diff operation.
/// </summary>
/// <param name="PropertyName">The name of the property that changed.</param>
/// <param name="From">The destination value before mapping.</param>
/// <param name="To">The destination value after mapping.</param>
/// <param name="Kind">The kind of change that was detected.</param>
public sealed record PropertyChange(
    string PropertyName,
    object? From,
    object? To,
    ChangeKind Kind);
