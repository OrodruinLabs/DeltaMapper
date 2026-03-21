namespace DeltaMapper.Benchmarks.Benchmarks;

using BenchmarkDotNet.Attributes;
using DeltaMapper;
using Competitors;
using Models;

using AmMapper = AutoMapper.IMapper;
using DmConfig = MapperConfiguration;

/// <summary>
/// Patch/update-in-place benchmark.
/// DeltaMapper's Patch&lt;TSource, TDest&gt; maps source onto an existing destination instance
/// and returns a diff of changed properties — this is unique to DeltaMapper.
/// Competitor columns perform a full re-map onto a pre-existing destination for a fair comparison.
/// </summary>
[MemoryDiagnoser]
public class PatchBenchmark
{
    private FlatSource _source = null!;
    private FlatDest _existing = null!;

    private IMapper _deltaMapperRuntime = null!;
    private IMapper _deltaMapperSourceGen = null!;
    private AmMapper _autoMapper = null!;

    [GlobalSetup]
    public void Setup()
    {
        _source = new FlatSource
        {
            Id = 1,
            Name = "Alice Updated",
            Email = "alice.new@example.com",
            Age = 31,
            IsActive = false,
        };

        _existing = new FlatDest
        {
            Id = 1,
            Name = "Alice",
            Email = "alice@example.com",
            Age = 30,
            IsActive = true,
        };

        _deltaMapperRuntime = DmConfig
            .Create(cfg => cfg.AddProfile<PatchRuntimeProfile>())
            .CreateMapper();

        // Source-gen path uses the same runtime mapper; [ModuleInitializer] registers the delegate
        _deltaMapperSourceGen = DmConfig
            .Create(_ => { })
            .CreateMapper();

        _autoMapper = AutoMapperSetup.CreateMapper();
    }

    /// <summary>DeltaMapper Patch — maps onto existing instance and returns property diff.</summary>
    [Benchmark]
    public MappingDiff<FlatDest> DeltaMapper_Patch_Runtime()
        => _deltaMapperRuntime.Patch<FlatSource, FlatDest>(_source, new FlatDest
        {
            Id = _existing.Id,
            Name = _existing.Name,
            Email = _existing.Email,
            Age = _existing.Age,
            IsActive = _existing.IsActive,
        });

    /// <summary>DeltaMapper source-gen Patch path.</summary>
    [Benchmark]
    public MappingDiff<FlatDest> DeltaMapper_Patch_SourceGen()
        => _deltaMapperSourceGen.Patch<FlatSource, FlatDest>(_source, new FlatDest
        {
            Id = _existing.Id,
            Name = _existing.Name,
            Email = _existing.Email,
            Age = _existing.Age,
            IsActive = _existing.IsActive,
        });

    /// <summary>AutoMapper Map onto existing destination — nearest equivalent to Patch.</summary>
    [Benchmark]
    public FlatDest AutoMapper_Map()
        => _autoMapper.Map(_source, new FlatDest
        {
            Id = _existing.Id,
            Name = _existing.Name,
            Email = _existing.Email,
            Age = _existing.Age,
            IsActive = _existing.IsActive,
        });

    /// <summary>Hand-written in-place update — direct property assignment baseline.</summary>
    [Benchmark(Baseline = true)]
    public FlatDest HandWritten_Overwrite()
    {
        var dest = new FlatDest
        {
            Id = _existing.Id,
            Name = _existing.Name,
            Email = _existing.Email,
            Age = _existing.Age,
            IsActive = _existing.IsActive,
        };
        dest.Id = _source.Id;
        dest.Name = _source.Name;
        dest.Email = _source.Email;
        dest.Age = _source.Age;
        dest.IsActive = _source.IsActive;
        return dest;
    }
}

/// <summary>Runtime-only profile for the patch benchmark.</summary>
public class PatchRuntimeProfile : Profile
{
    public PatchRuntimeProfile() => CreateMap<FlatSource, FlatDest>();
}
