using DeltaMapper.Runtime;

namespace DeltaMapper.Middleware;

/// <summary>
/// Defines a middleware component that can intercept and transform mapping operations.
/// </summary>
public interface IMappingMiddleware
{
    /// <summary>
    /// Intercepts a mapping operation. Call next() to continue the pipeline.
    /// </summary>
    /// <param name="source">The source object being mapped.</param>
    /// <param name="destType">The destination type.</param>
    /// <param name="ctx">The per-call mapping context.</param>
    /// <param name="next">Delegate to invoke the next middleware or the core mapping delegate.</param>
    /// <returns>The mapped destination object.</returns>
    object Map(object source, Type destType, MapperContext ctx, Func<object> next);
}
