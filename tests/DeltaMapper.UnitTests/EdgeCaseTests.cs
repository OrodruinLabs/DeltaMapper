using DeltaMapper;
using FluentAssertions;
using Xunit;

namespace DeltaMapper.UnitTests;

// ── Models ───────────────────────────────────────────────────────────

public class EC_Source
{
    public string? Name { get; set; }
    public List<string>? Tags { get; set; }
    public int Age { get; set; }
}

public class EC_Dest
{
    public string? Name { get; set; }
    public List<string>? Tags { get; set; }
    public int Age { get; set; }
}

file sealed class EdgeCaseProfile : Profile
{
    public EdgeCaseProfile()
    {
        CreateMap<EC_Source, EC_Dest>();
    }
}

// ── Tests ────────────────────────────────────────────────────────────

public class EdgeCaseTests
{
    private static IMapper CreateMapper()
    {
        return MapperConfiguration.Create(cfg => cfg.AddProfile<EdgeCaseProfile>())
            .CreateMapper();
    }

    [Fact]
    public void EC01_MapList_NullSource_Throws()
    {
        var mapper = CreateMapper();

        var act = () => mapper.Map<EC_Source, EC_Dest>((IEnumerable<EC_Source>)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void EC02_Map_EmptyCollection_Returns_EmptyList()
    {
        var mapper = CreateMapper();
        var source = new EC_Source { Name = "Test", Tags = new List<string>() };

        var dest = mapper.Map<EC_Source, EC_Dest>(source);

        dest.Tags.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void EC03_MapList_EmptyEnumerable_Returns_EmptyList()
    {
        var mapper = CreateMapper();

        var result = mapper.Map<EC_Source, EC_Dest>(Enumerable.Empty<EC_Source>());

        result.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void EC04_Map_AllPropertiesNull_Succeeds()
    {
        var mapper = CreateMapper();
        var source = new EC_Source();

        var dest = mapper.Map<EC_Source, EC_Dest>(source);

        dest.Name.Should().BeNull();
        dest.Tags.Should().BeNull();
        dest.Age.Should().Be(0);
    }

    [Fact]
    public void EC05_Patch_NullSource_Throws()
    {
        var mapper = CreateMapper();
        var dest = new EC_Dest { Name = "existing" };

        var act = () => mapper.Patch<EC_Source, EC_Dest>(null!, dest);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void EC06_Patch_NullDestination_Throws()
    {
        var mapper = CreateMapper();
        var source = new EC_Source { Name = "new" };

        var act = () => mapper.Patch<EC_Source, EC_Dest>(source, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void EC07_Build_NoTypeMaps_Exception_Has_No_InnerException()
    {
        var act = () => MapperConfiguration.Create(cfg => { }).CreateMapper();

        var ex = act.Should().Throw<DeltaMapperException>().Which;
        ex.InnerException.Should().BeNull();
    }
}
