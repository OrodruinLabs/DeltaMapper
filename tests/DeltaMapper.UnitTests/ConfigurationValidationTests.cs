using FluentAssertions;
using Xunit;

namespace DeltaMapper.UnitTests;

/// <summary>
/// Tests for MapperConfiguration.AssertConfigurationIsValid().
/// </summary>
public class ConfigurationValidationTests
{
    // ── CV-01 ────────────────────────────────────────────────────────────────
    // All properties mapped by convention → passes

    private class CV01_Source
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class CV01_Dest
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class CV01_Profile : Profile
    {
        public CV01_Profile() => CreateMap<CV01_Source, CV01_Dest>();
    }

    [Fact]
    public void AssertConfigurationIsValid_AllConventionMapped_DoesNotThrow()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<CV01_Profile>());

        var act = () => config.AssertConfigurationIsValid();

        act.Should().NotThrow();
    }

    // ── CV-02 ────────────────────────────────────────────────────────────────
    // Unmapped destination property → throws with property name in message

    private class CV02_Source
    {
        public int Id { get; set; }
    }

    private class CV02_Dest
    {
        public int Id { get; set; }
        public string UnmappedField { get; set; } = string.Empty;
    }

    private class CV02_Profile : Profile
    {
        public CV02_Profile() => CreateMap<CV02_Source, CV02_Dest>();
    }

    [Fact]
    public void AssertConfigurationIsValid_UnmappedProperty_ThrowsWithPropertyName()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<CV02_Profile>());

        var act = () => config.AssertConfigurationIsValid();

        act.Should().Throw<DeltaMapperException>()
            .WithMessage("*UnmappedField*");
    }

    // ── CV-03 ────────────────────────────────────────────────────────────────
    // Ignored property → passes

    private class CV03_Source
    {
        public int Id { get; set; }
    }

    private class CV03_Dest
    {
        public int Id { get; set; }
        public string IgnoredProp { get; set; } = string.Empty;
    }

    private class CV03_Profile : Profile
    {
        public CV03_Profile()
        {
            CreateMap<CV03_Source, CV03_Dest>()
                .ForMember(d => d.IgnoredProp, o => o.Ignore());
        }
    }

    [Fact]
    public void AssertConfigurationIsValid_IgnoredProperty_DoesNotThrow()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<CV03_Profile>());

        var act = () => config.AssertConfigurationIsValid();

        act.Should().NotThrow();
    }

    // ── CV-04 ────────────────────────────────────────────────────────────────
    // MapFrom property → passes

    private class CV04_Source
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }

    private class CV04_Dest
    {
        public string FullName { get; set; } = string.Empty;
    }

    private class CV04_Profile : Profile
    {
        public CV04_Profile()
        {
            CreateMap<CV04_Source, CV04_Dest>()
                .ForMember(d => d.FullName, o => o.MapFrom(s => $"{s.FirstName} {s.LastName}"));
        }
    }

    [Fact]
    public void AssertConfigurationIsValid_MapFromProperty_DoesNotThrow()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<CV04_Profile>());

        var act = () => config.AssertConfigurationIsValid();

        act.Should().NotThrow();
    }

    // ── CV-05 ────────────────────────────────────────────────────────────────
    // Record with all constructor params matching → passes

    private record CV05_Source(string FirstName, string LastName, int Age);
    private record CV05_Dest(string FirstName, string LastName, int Age);

    private class CV05_Profile : Profile
    {
        public CV05_Profile() => CreateMap<CV05_Source, CV05_Dest>();
    }

    [Fact]
    public void AssertConfigurationIsValid_RecordAllParamsMatched_DoesNotThrow()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<CV05_Profile>());

        var act = () => config.AssertConfigurationIsValid();

        act.Should().NotThrow();
    }

    // ── CV-06 ────────────────────────────────────────────────────────────────
    // Record where a non-constructor init property has no source match → throws

    private record CV06_Source(string Name);

    // Record with matching ctor param + an extra init property that has no source match
    private record CV06_Dest(string Name)
    {
        public string ExtraField { get; init; } = string.Empty;
    }

    private class CV06_Profile : Profile
    {
        public CV06_Profile() => CreateMap<CV06_Source, CV06_Dest>();
    }

    [Fact]
    public void AssertConfigurationIsValid_RecordWithUnmappedExtraInitProp_Throws()
    {
        // CV06_Dest.ExtraField is an init property not covered by constructor and has no
        // matching source property — AssertConfigurationIsValid should flag it.
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<CV06_Profile>());

        var act = () => config.AssertConfigurationIsValid();

        act.Should().Throw<DeltaMapperException>()
            .WithMessage("*ExtraField*");
    }

    // ── CV-07 ────────────────────────────────────────────────────────────────
    // Error message lists count of unmapped members

    private class CV07_Source
    {
        public int Id { get; set; }
    }

    private class CV07_Dest
    {
        public int Id { get; set; }
        public string Field1 { get; set; } = string.Empty;
        public string Field2 { get; set; } = string.Empty;
    }

    private class CV07_Profile : Profile
    {
        public CV07_Profile() => CreateMap<CV07_Source, CV07_Dest>();
    }

    [Fact]
    public void AssertConfigurationIsValid_MultipleUnmapped_MessageListsCount()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<CV07_Profile>());

        var act = () => config.AssertConfigurationIsValid();

        act.Should().Throw<DeltaMapperException>()
            .WithMessage("*2 unmapped members*");
    }

    // ── CV-08 ────────────────────────────────────────────────────────────────
    // Condition-only member (ForMember with Condition but no MapFrom) → passes
    // if source convention matches

    private class CV08_Source
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class CV08_Dest
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class CV08_Profile : Profile
    {
        public CV08_Profile()
        {
            CreateMap<CV08_Source, CV08_Dest>()
                .ForMember(d => d.Name, o => o.Condition(s => ((CV08_Source)s).Id > 0));
        }
    }

    [Fact]
    public void AssertConfigurationIsValid_ConditionWithConventionMatch_DoesNotThrow()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<CV08_Profile>());

        var act = () => config.AssertConfigurationIsValid();

        act.Should().NotThrow();
    }

    // ── CV-09 ────────────────────────────────────────────────────────────────
    // Flattened property → passes

    private class CV09_Customer
    {
        public string Name { get; set; } = string.Empty;
    }

    private class CV09_Source
    {
        public CV09_Customer Customer { get; set; } = new();
    }

    private class CV09_Dest
    {
        public string CustomerName { get; set; } = string.Empty;
    }

    private class CV09_Profile : Profile
    {
        public CV09_Profile() => CreateMap<CV09_Source, CV09_Dest>();
    }

    [Fact]
    public void AssertConfigurationIsValid_FlattenedProperty_DoesNotThrow()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<CV09_Profile>());

        var act = () => config.AssertConfigurationIsValid();

        act.Should().NotThrow();
    }

    // ── CV-10 ────────────────────────────────────────────────────────────────
    // Single unmapped member → singular message wording

    private class CV10_Source
    {
        public int Id { get; set; }
    }

    private class CV10_Dest
    {
        public int Id { get; set; }
        public string OnlyOne { get; set; } = string.Empty;
    }

    private class CV10_Profile : Profile
    {
        public CV10_Profile() => CreateMap<CV10_Source, CV10_Dest>();
    }

    [Fact]
    public void AssertConfigurationIsValid_SingleUnmapped_MessageUsesSingular()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<CV10_Profile>());

        var act = () => config.AssertConfigurationIsValid();

        act.Should().Throw<DeltaMapperException>()
            .WithMessage("*1 unmapped member:*")
            .And.Message.Should().Contain("OnlyOne");
    }
}
