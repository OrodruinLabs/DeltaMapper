namespace DeltaMapper.EFCore;

using DeltaMapper.Middleware;
using DeltaMapper.Runtime;

/// <summary>
/// Middleware that detects EF Core Castle.Core proxy entities and skips all collection-typed
/// properties during mapping to prevent lazy loading triggers. The proxy flag is scoped
/// per source object, so nested non-proxy objects retain normal collection mapping.
/// </summary>
internal sealed class EFCoreProxyMiddleware : IMappingMiddleware
{
    /// <inheritdoc />
    public object Map(object source, Type destType, MapperContext ctx, Func<object> next)
    {
        // Set the proxy flag based on the current source object so that
        // nested non-proxy objects correctly re-enable collection mapping.
        var wasProxy = ctx.IsProxyMapping;
        ctx.IsProxyMapping = IsProxy(source);
        try
        {
            return next();
        }
        finally
        {
            ctx.IsProxyMapping = wasProxy;
        }
    }

    /// <summary>
    /// Detects if the given object is an EF Core Castle.Core dynamic proxy.
    /// </summary>
    private static bool IsProxy(object entity)
    {
        var type = entity.GetType();
        var baseType = type.BaseType;

        // EF Core proxies inherit from the entity type and live in a dynamic assembly
        if (baseType is null || baseType == typeof(object))
            return false;

        // Castle.Core proxies have type names like "EntityTypeProxy" in DynamicProxyGenAssembly2
        return type != baseType
            && type.Assembly.GetName().Name is "DynamicProxyGenAssembly2" or "Castle.Core";
    }
}
