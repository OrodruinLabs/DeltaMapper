namespace DeltaMapper;

/// <summary>
/// Stores all configuration for a single source-to-destination type pair.
/// </summary>
internal sealed class TypeMapConfiguration
{
    public Type SourceType { get; init; } = null!;
    public Type DestinationType { get; init; } = null!;
    public List<MemberConfiguration> MemberConfigurations { get; } = [];
    public Action<object, object>? BeforeMapAction { get; set; }
    public Action<object, object>? AfterMapAction { get; set; }
    public bool HasReverseMap { get; set; }
}
