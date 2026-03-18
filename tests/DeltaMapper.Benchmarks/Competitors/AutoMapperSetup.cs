namespace DeltaMapper.Benchmarks.Competitors;

using AutoMapper;
using DeltaMapper.Benchmarks.Models;

public static class AutoMapperSetup
{
    public static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<FlatSource, FlatDest>();
            cfg.CreateMap<NestedSource, NestedDest>();
            cfg.CreateMap<AddressSource, AddressDest>();
            cfg.CreateMap<CollectionSource, CollectionDest>();
            cfg.CreateMap<ItemSource, ItemDest>();
        });
        return config.CreateMapper();
    }
}
