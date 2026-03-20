namespace DeltaMapper.Benchmarks.Competitors;

using Models;
using Riok.Mapperly.Abstractions;

[Mapper]
public static partial class MapperlyMapper
{
    public static partial FlatDest MapFlat(FlatSource source);
    public static partial NestedDest MapNested(NestedSource source);
    public static partial CollectionDest MapCollection(CollectionSource source);
}
