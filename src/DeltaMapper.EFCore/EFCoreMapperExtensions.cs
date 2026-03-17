using DeltaMapper.Configuration;

namespace DeltaMapper.EFCore;

/// <summary>
/// Extension methods for registering EF Core proxy-aware middleware in DeltaMapper.
/// </summary>
public static class EFCoreMapperExtensions
{
    /// <summary>
    /// Registers the <see cref="EFCoreProxyMiddleware"/> in the mapping pipeline.
    /// This middleware detects EF Core proxy entities and skips unloaded navigation
    /// properties to prevent lazy loading triggers during mapping.
    /// </summary>
    /// <param name="builder">The mapper configuration builder.</param>
    /// <returns>The builder for fluent chaining.</returns>
    public static MapperConfigurationBuilder AddEFCoreSupport(this MapperConfigurationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Use<EFCoreProxyMiddleware>();
        return builder;
    }
}
