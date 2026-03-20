namespace DeltaMapper.OpenTelemetry;

using System.Diagnostics;
using Middleware;
using Runtime;

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

        var sourceType = source.GetType();
        using var activity = Source.StartActivity($"Map {sourceType.Name} -> {destType.Name}");

        activity?.SetTag("mapper.source_type", sourceType.FullName);
        activity?.SetTag("mapper.dest_type", destType.FullName);

        try
        {
            return next();
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddEvent(new ActivityEvent("exception",
                tags: new ActivityTagsCollection
                {
                    { "exception.type", ex.GetType().FullName },
                    { "exception.message", ex.Message }
                }));
            throw;
        }
    }
}
