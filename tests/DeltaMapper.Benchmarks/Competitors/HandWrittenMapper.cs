namespace DeltaMapper.Benchmarks.Competitors;

using DeltaMapper.Benchmarks.Models;

/// <summary>
/// Baseline hand-written mapper — direct property assignment with no abstraction overhead.
/// </summary>
public static class HandWrittenMapper
{
    public static FlatDest MapFlat(FlatSource source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        Email = source.Email,
        Age = source.Age,
        IsActive = source.IsActive,
    };

    public static NestedDest MapNested(NestedSource source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        Address = MapAddress(source.Address),
    };

    public static AddressDest MapAddress(AddressSource source) => new()
    {
        Street = source.Street,
        City = source.City,
        Zip = source.Zip,
    };

    public static CollectionDest MapCollection(CollectionSource source) => new()
    {
        Id = source.Id,
        Items = source.Items.Select(i => new ItemDest { Id = i.Id, Label = i.Label }).ToList(),
    };

    public static ItemDest MapItem(ItemSource source) => new()
    {
        Id = source.Id,
        Label = source.Label,
    };
}
