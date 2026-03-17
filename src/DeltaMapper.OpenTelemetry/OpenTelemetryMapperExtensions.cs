using DeltaMapper.Configuration;

namespace DeltaMapper.OpenTelemetry;

/// <summary>
/// Extension methods for registering OpenTelemetry tracing middleware in DeltaMapper.
/// </summary>
public static class OpenTelemetryMapperExtensions
{
    /// <summary>
    /// Registers the <see cref="TracingMiddleware"/> in the mapping pipeline.
    /// This middleware emits <see cref="System.Diagnostics.Activity"/> spans for every
    /// mapping operation, enabling OpenTelemetry-compatible distributed tracing.
    /// </summary>
    /// <param name="builder">The mapper configuration builder.</param>
    /// <returns>The builder for fluent chaining.</returns>
    public static MapperConfigurationBuilder AddMapperTracing(this MapperConfigurationBuilder builder)
    {
        builder.Use<TracingMiddleware>();
        return builder;
    }
}
