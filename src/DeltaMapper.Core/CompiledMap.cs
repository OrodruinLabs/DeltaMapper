namespace DeltaMapper;

/// <summary>
/// Holds the compiled mapping delegate and hooks for a single type pair.
/// </summary>
internal sealed class CompiledMap(Func<object, object?, MapperContext, object> mapFunc)
{
    internal object Execute(object source, object? existingDest, MapperContext ctx)
        => mapFunc(source, existingDest, ctx);
}
