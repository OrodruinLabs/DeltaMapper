namespace DeltaMapper.OpenTelemetry;

using System.Diagnostics;
using DeltaMapper.Middleware;
using DeltaMapper.Runtime;

/// <summary>
/// Middleware that emits <see cref="Activity"/> spans for every mapping operation,
/// enabling OpenTelemetry-compatible distributed tracing.
/// </summary>
internal sealed class TracingMiddleware : IMappingMiddleware
{
    private static readonly ActivitySource Source = new("DeltaMapper");

    /// <inheritdoc />
    public object Map(object source, Type destType, MapperContext ctx, Func<object> next)
    {
        if (!Source.HasListeners())
            return next();

        using var activity = Source.StartActivity($"Map {source.GetType().Name} -> {destType.Name}");

        var result = next();

        activity?.SetTag("mapper.source_type", source.GetType().FullName);
        activity?.SetTag("mapper.dest_type", destType.FullName);

        return result;
    }
}
