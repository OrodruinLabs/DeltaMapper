using DeltaMapper.Runtime;

namespace DeltaMapper.Middleware;

/// <summary>
/// Chains registered middleware components around the core mapping delegate.
/// When no middleware is registered, the core delegate is invoked directly (zero overhead).
/// </summary>
internal sealed class MappingPipeline(IReadOnlyList<IMappingMiddleware> middlewares)
{

    /// <summary>
    /// Executes the mapping through the middleware chain, ending with the core delegate.
    /// </summary>
    internal object Execute(object source, Type destType, MapperContext ctx, Func<object> coreDelegate)
    {
        if (middlewares.Count == 0)
            return coreDelegate();

        // Build the chain from the inside out
        Func<object> next = coreDelegate;

        for (int i = middlewares.Count - 1; i >= 0; i--)
        {
            var middleware = middlewares[i];
            var currentNext = next;
            next = () => middleware.Map(source, destType, ctx, currentNext);
        }

        return next();
    }
}
