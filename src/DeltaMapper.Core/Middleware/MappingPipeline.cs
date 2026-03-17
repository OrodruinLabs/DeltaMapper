namespace DeltaMapper.Middleware;

/// <summary>
/// Chains registered middleware components around the core mapping delegate.
/// When no middleware is registered, the core delegate is invoked directly (zero overhead).
/// </summary>
internal sealed class MappingPipeline
{
    private readonly IReadOnlyList<IMappingMiddleware> _middlewares;

    internal MappingPipeline(IReadOnlyList<IMappingMiddleware> middlewares)
    {
        _middlewares = middlewares;
    }

    /// <summary>
    /// Executes the mapping through the middleware chain, ending with the core delegate.
    /// </summary>
    internal object Execute(object source, Type destType, MapperContext ctx, Func<object> coreDelegate)
    {
        if (_middlewares.Count == 0)
            return coreDelegate();

        // Build the chain from the inside out
        Func<object> next = coreDelegate;

        for (int i = _middlewares.Count - 1; i >= 0; i--)
        {
            var middleware = _middlewares[i];
            var currentNext = next;
            next = () => middleware.Map(source, destType, ctx, currentNext);
        }

        return next();
    }
}
