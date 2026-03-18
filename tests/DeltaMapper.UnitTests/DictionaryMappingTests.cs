using DeltaMapper.Abstractions;
using DeltaMapper.Configuration;
using FluentAssertions;
using Xunit;

namespace DeltaMapper.UnitTests;

// ── Test models ────────────────────────────────────────────────────

public class DictSource
{
    public int Id { get; set; }
    public Dictionary<string, int> Tags { get; set; } = new();
}

public class DictDest
{
    public int Id { get; set; }
    public Dictionary<string, int> Tags { get; set; } = new();
}

public class DictNullSource
{
    public int Id { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public class DictNullDest
{
    public int Id { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

// ── Profile ────────────────────────────────────────────────────────

public class DictionaryProfile : MappingProfile
{
    public DictionaryProfile()
    {
        CreateMap<DictSource, DictDest>();
        CreateMap<DictNullSource, DictNullDest>();
    }
}

// ── Tests ──────────────────────────────────────────────────────────

public class DictionaryMappingTests
{
    private readonly IMapper _mapper;

    public DictionaryMappingTests()
    {
        _mapper = MapperConfiguration.Create(cfg => cfg.AddProfile<DictionaryProfile>())
            .CreateMapper();
    }

    [Fact]
    public void Dict01_MapsDictionaryWithSameTypes()
    {
        var source = new DictSource
        {
            Id = 1,
            Tags = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 }
        };

        var dest = _mapper.Map<DictSource, DictDest>(source);

        dest.Id.Should().Be(1);
        dest.Tags.Should().BeEquivalentTo(source.Tags);
    }

    [Fact]
    public void Dict02_SameTypeDictionaryIsReferenceAssigned()
    {
        // Same Dictionary<string,int> type on both sides — direct assign (reference copy)
        var source = new DictSource
        {
            Id = 1,
            Tags = new Dictionary<string, int> { ["x"] = 42 }
        };

        var dest = _mapper.Map<DictSource, DictDest>(source);

        dest.Tags.Should().BeSameAs(source.Tags);
    }

    [Fact]
    public void Dict03_MapsEmptyDictionary()
    {
        var source = new DictSource { Id = 1, Tags = new() };

        var dest = _mapper.Map<DictSource, DictDest>(source);

        dest.Tags.Should().BeEmpty();
    }

    [Fact]
    public void Dict04_MapsNullDictionary()
    {
        var source = new DictNullSource { Id = 1, Metadata = null };

        var dest = _mapper.Map<DictNullSource, DictNullDest>(source);

        dest.Metadata.Should().BeNull();
    }

    [Fact]
    public void Dict05_MapsNullableDictionaryWithValue()
    {
        var source = new DictNullSource
        {
            Id = 1,
            Metadata = new Dictionary<string, string> { ["key"] = "val" }
        };

        var dest = _mapper.Map<DictNullSource, DictNullDest>(source);

        dest.Metadata.Should().ContainKey("key").WhoseValue.Should().Be("val");
    }

    [Fact]
    public void Dict06_MapsDictionaryWithMappedValueTypes()
    {
        var mapper = MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile(new DictComplexProfile());
        }).CreateMapper();

        var source = new DictComplexSource
        {
            Items = new Dictionary<string, DictChildSource>
            {
                ["a"] = new() { Name = "Alice" },
                ["b"] = new() { Name = "Bob" }
            }
        };

        var dest = mapper.Map<DictComplexSource, DictComplexDest>(source);

        dest.Items.Should().HaveCount(2);
        dest.Items["a"].Name.Should().Be("Alice");
        dest.Items["b"].Name.Should().Be("Bob");
    }

    [Fact]
    public void Dict07_MapsReadOnlyDictionarySource()
    {
        var mapper = MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile(new DictReadOnlyProfile());
        }).CreateMapper();

        var source = new DictReadOnlySource
        {
            Data = new Dictionary<string, int> { ["x"] = 1, ["y"] = 2 }
        };

        var dest = mapper.Map<DictReadOnlySource, DictReadOnlyDest>(source);

        dest.Data.Should().HaveCount(2);
        dest.Data["x"].Should().Be(1);
    }

    [Fact]
    public void Dict08_ClonesDictionaryWhenTypesMatch()
    {
        var mapper = MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile(new DictCloneProfile());
        }).CreateMapper();

        var source = new DictCloneSource
        {
            Tags = new Dictionary<string, int> { ["a"] = 1 }
        };

        var dest = mapper.Map<DictCloneSource, DictCloneDest>(source);

        dest.Tags.Should().BeEquivalentTo(source.Tags);
        dest.Tags.Should().NotBeSameAs(source.Tags); // cloned, not reference-shared
    }
}

// ── Complex value type models ─────────────────────────────────────

public class DictChildSource { public string Name { get; set; } = ""; }
public class DictChildDest { public string Name { get; set; } = ""; }

public class DictComplexSource { public Dictionary<string, DictChildSource> Items { get; set; } = new(); }
public class DictComplexDest { public Dictionary<string, DictChildDest> Items { get; set; } = new(); }

file class DictComplexProfile : MappingProfile
{
    public DictComplexProfile()
    {
        CreateMap<DictChildSource, DictChildDest>();
        CreateMap<DictComplexSource, DictComplexDest>();
    }
}

// ── IReadOnlyDictionary models ────────────────────────────────────

public class DictReadOnlySource { public IReadOnlyDictionary<string, int> Data { get; set; } = new Dictionary<string, int>(); }
public class DictReadOnlyDest { public Dictionary<string, int> Data { get; set; } = new(); }

file class DictReadOnlyProfile : MappingProfile
{
    public DictReadOnlyProfile() => CreateMap<DictReadOnlySource, DictReadOnlyDest>();
}

// ── Clone (same key/value types but different dict types) ─────────

public class DictCloneSource { public IDictionary<string, int> Tags { get; set; } = new Dictionary<string, int>(); }
public class DictCloneDest { public Dictionary<string, int> Tags { get; set; } = new(); }

file class DictCloneProfile : MappingProfile
{
    public DictCloneProfile() => CreateMap<DictCloneSource, DictCloneDest>();
}
