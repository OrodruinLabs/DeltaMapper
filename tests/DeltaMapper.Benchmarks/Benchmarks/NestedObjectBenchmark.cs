namespace DeltaMapper.Benchmarks.Benchmarks;

using BenchmarkDotNet.Attributes;
using DeltaMapper.Abstractions;
using DeltaMapper.Benchmarks.Competitors;
using DeltaMapper.Benchmarks.Models;

using AmMapper = AutoMapper.IMapper;
using DmConfig = DeltaMapper.Configuration.MapperConfiguration;

[MemoryDiagnoser]
public class NestedObjectBenchmark
{
    private NestedSource _source = null!;

    private IMapper _deltaMapperRuntime = null!;
    private IMapper _deltaMapperSourceGen = null!;
    private AmMapper _autoMapper = null!;

    [GlobalSetup]
    public void Setup()
    {
        _source = new NestedSource
        {
            Id = 1,
            Name = "Bob",
            Address = new AddressSource
            {
                Street = "123 Main St",
                City = "Springfield",
                Zip = "12345",
            },
        };

        _deltaMapperRuntime = DmConfig
            .Create(cfg => cfg.AddProfile<NestedRuntimeProfile>())
            .CreateMapper();

        // Source-gen path: [ModuleInitializer] has already populated GeneratedMapRegistry
        _deltaMapperSourceGen = DmConfig
            .Create(_ => { })
            .CreateMapper();

        _autoMapper = AutoMapperSetup.CreateMapper();
    }

    [Benchmark(Baseline = true)]
    public NestedDest HandWritten() => HandWrittenMapper.MapNested(_source);

    [Benchmark]
    public NestedDest Mapperly() => MapperlyMapper.MapNested(_source);

    [Benchmark]
    public NestedDest AutoMapper() => _autoMapper.Map<NestedDest>(_source);

    [Benchmark]
    public NestedDest DeltaMapper_Runtime() => _deltaMapperRuntime.Map<NestedSource, NestedDest>(_source);

    [Benchmark]
    public NestedDest DeltaMapper_SourceGen() => _deltaMapperSourceGen.Map<NestedSource, NestedDest>(_source);
}

/// <summary>Runtime-only profile for the nested benchmark.</summary>
public class NestedRuntimeProfile : DeltaMapper.Configuration.Profile
{
    public NestedRuntimeProfile()
    {
        CreateMap<AddressSource, AddressDest>();
        CreateMap<NestedSource, NestedDest>();
    }
}
