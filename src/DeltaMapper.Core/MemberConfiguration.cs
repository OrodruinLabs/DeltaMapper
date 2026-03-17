namespace DeltaMapper;

/// <summary>
/// Stores configuration for a single destination member override.
/// </summary>
internal sealed class MemberConfiguration
{
    public string DestinationMemberName { get; init; } = null!;
    public Func<object, object?>? CustomResolver { get; set; }
    public bool IsIgnored { get; set; }
    public object? NullSubstituteValue { get; set; }
    public bool HasNullSubstitute { get; set; }
}
