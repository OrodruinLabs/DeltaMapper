namespace DeltaMapper.Benchmarks.Benchmarks;

using BenchmarkDotNet.Attributes;
using DeltaMapper.Abstractions;
using DeltaMapper.Benchmarks.Competitors;
using DeltaMapper.Benchmarks.Models;

using AmMapper = AutoMapper.IMapper;
using DmConfig = DeltaMapper.Configuration.MapperConfiguration;

[MemoryDiagnoser]
public class FlatObjectBenchmark
{
    private FlatSource _source = null!;

    private IMapper _deltaMapperRuntime = null!;
    private IMapper _deltaMapperSourceGen = null!;
    private AmMapper _autoMapper = null!;

    [GlobalSetup]
    public void Setup()
    {
        _source = new FlatSource
        {
            Id = 1,
            Name = "Alice",
            Email = "alice@example.com",
            Age = 30,
            IsActive = true,
        };

        // DeltaMapper runtime — explicit profile registration
        _deltaMapperRuntime = DmConfig
            .Create(cfg => cfg.AddProfile<FlatRuntimeProfile>())
            .CreateMapper();

        // DeltaMapper source-gen — GeneratedMapRegistry populated by [ModuleInitializer]; same runtime mapper
        _deltaMapperSourceGen = DmConfig
            .Create(_ => { })
            .CreateMapper();

        _autoMapper = AutoMapperSetup.CreateMapper();
    }

    [Benchmark(Baseline = true)]
    public FlatDest HandWritten() => HandWrittenMapper.MapFlat(_source);

    [Benchmark]
    public FlatDest Mapperly() => MapperlyMapper.MapFlat(_source);

    [Benchmark]
    public FlatDest AutoMapper() => _autoMapper.Map<FlatDest>(_source);

    [Benchmark]
    public FlatDest DeltaMapper_Runtime() => _deltaMapperRuntime.Map<FlatSource, FlatDest>(_source);

    [Benchmark]
    public FlatDest DeltaMapper_SourceGen() => _deltaMapperSourceGen.Map<FlatSource, FlatDest>(_source);

    [Benchmark]
    public FlatDest DeltaMapper_DirectCall() => FlatGenProfile.MapFlatSourceToFlatDest(_source);
}

/// <summary>Runtime-only profile for the flat benchmark (no source-gen attribute needed).</summary>
public class FlatRuntimeProfile : DeltaMapper.Configuration.Profile
{
    public FlatRuntimeProfile() => CreateMap<FlatSource, FlatDest>();
}
