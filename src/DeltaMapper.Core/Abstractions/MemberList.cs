namespace DeltaMapper;

/// <summary>
/// Controls which members AssertConfigurationIsValid() checks for a given type map.
/// </summary>
public enum MemberList
{
    /// <summary>All destination members must be mapped (default).</summary>
    Destination,

    /// <summary>All source members must be consumed by at least one mapping.</summary>
    Source,

    /// <summary>Skip validation for this map entirely.</summary>
    None
}
