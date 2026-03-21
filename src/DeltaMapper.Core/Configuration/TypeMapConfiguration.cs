namespace DeltaMapper.Configuration;

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
    public Func<object, object>? CustomFactory { get; set; }
    public bool HasReverseMap { get; set; }

    /// <summary>
    /// Destination member names resolved during compilation.
    /// Populated by MapperConfigurationBuilder at Build() time.
    /// </summary>
    public HashSet<string> MappedDestinationMembers { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Source property names consumed during compilation (convention match, flattening, constructor params).
    /// Populated by MapperConfigurationBuilder at Build() time for MemberList.Source validation.
    /// </summary>
    public HashSet<string> MappedSourceMembers { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Whether this type map uses constructor injection (records/init-only) vs property assignment.
    /// Set by MapperConfigurationBuilder during compilation.
    /// </summary>
    public bool UsesConstructorInjection { get; set; }

    /// <summary>
    /// Constructor parameter names selected during compilation.
    /// Only populated when UsesConstructorInjection is true.
    /// </summary>
    public List<string> ConstructorParameterNames { get; } = [];

    /// <summary>
    /// Which members to validate for this type map. Defaults to Destination.
    /// </summary>
    public MemberList MemberValidation { get; set; } = MemberList.Destination;
}
