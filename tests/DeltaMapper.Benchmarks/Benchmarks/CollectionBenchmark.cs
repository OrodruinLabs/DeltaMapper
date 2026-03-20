namespace DeltaMapper.Benchmarks.Benchmarks;

using BenchmarkDotNet.Attributes;
using DeltaMapper;
using DeltaMapper.Benchmarks.Competitors;
using DeltaMapper.Benchmarks.Models;

using AmMapper = AutoMapper.IMapper;
using DmConfig = DeltaMapper.MapperConfiguration;

[MemoryDiagnoser]
public class CollectionBenchmark
{
    private CollectionSource _source = null!;

    private IMapper _deltaMapperRuntime = null!;
    private IMapper _deltaMapperSourceGen = null!;
    private AmMapper _autoMapper = null!;

    [GlobalSetup]
    public void Setup()
    {
        _source = new CollectionSource
        {
            Id = 1,
            Items = Enumerable.Range(1, 10)
                .Select(i => new ItemSource { Id = i, Label = $"Item {i}" })
                .ToList(),
        };

        _deltaMapperRuntime = DmConfig
            .Create(cfg => cfg.AddProfile<CollectionRuntimeProfile>())
            .CreateMapper();

        // Source-gen path: [ModuleInitializer] has already populated GeneratedMapRegistry
        _deltaMapperSourceGen = DmConfig
            .Create(_ => { })
            .CreateMapper();

        _autoMapper = AutoMapperSetup.CreateMapper();
    }

    [Benchmark(Baseline = true)]
    public CollectionDest HandWritten() => HandWrittenMapper.MapCollection(_source);

    [Benchmark]
    public CollectionDest Mapperly() => MapperlyMapper.MapCollection(_source);

    [Benchmark]
    public CollectionDest AutoMapper() => _autoMapper.Map<CollectionDest>(_source);

    [Benchmark]
    public CollectionDest DeltaMapper_Runtime() => _deltaMapperRuntime.Map<CollectionSource, CollectionDest>(_source);

    [Benchmark]
    public CollectionDest DeltaMapper_SourceGen() => _deltaMapperSourceGen.Map<CollectionSource, CollectionDest>(_source);
}

/// <summary>Runtime-only profile for the collection benchmark.</summary>
public class CollectionRuntimeProfile : Profile
{
    public CollectionRuntimeProfile()
    {
        CreateMap<ItemSource, ItemDest>();
        CreateMap<CollectionSource, CollectionDest>();
    }
}
