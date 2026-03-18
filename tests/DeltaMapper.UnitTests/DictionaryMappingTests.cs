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
    private readonly Abstractions.IMapper _mapper;

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
}
