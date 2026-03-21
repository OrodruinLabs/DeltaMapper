using FluentAssertions;
using Xunit;

namespace DeltaMapper.UnitTests;

/// <summary>
/// Tests for the MemberList parameter on CreateMap — controls which members
/// AssertConfigurationIsValid() enforces for a given type map.
/// </summary>
public class MemberListValidationTests
{
    // ── Shared test models ────────────────────────────────────────────────────

    private class FullSource { public int Id { get; set; } public string Name { get; set; } = string.Empty; public string Extra { get; set; } = string.Empty; }
    private class SmallDest { public int Id { get; set; } public string Name { get; set; } = string.Empty; }
    private class FullDest { public int Id { get; set; } public string Name { get; set; } = string.Empty; public string Extra { get; set; } = string.Empty; }

    // For flattening test
    private class SourceWithNested { public int Id { get; set; } public NestedName Customer { get; set; } = new(); }
    private class NestedName { public string Name { get; set; } = string.Empty; }
    private class FlatDest { public int Id { get; set; } public string CustomerName { get; set; } = string.Empty; }

    // For constructor test
    private record RecordDest(int Id, string Name);

    // ── MLV-01 ───────────────────────────────────────────────────────────────
    // MemberList.None — unmapped destination properties pass

    private class MLV01_Profile : Profile
    {
        public MLV01_Profile() => CreateMap<SmallDest, FullDest>(MemberList.None);
    }

    [Fact]
    public void MemberListNone_UnmappedDestinationProperty_DoesNotThrow()
    {
        // SmallDest → FullDest: FullDest.Extra has no matching source property.
        // With MemberList.None validation is skipped entirely, so no throw.
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<MLV01_Profile>());

        var act = () => config.AssertConfigurationIsValid();

        act.Should().NotThrow();
    }

    // ── MLV-02 ───────────────────────────────────────────────────────────────
    // MemberList.None — unconsumed source property passes

    private class MLV02_Profile : Profile
    {
        public MLV02_Profile() => CreateMap<FullSource, SmallDest>(MemberList.None);
    }

    [Fact]
    public void MemberListNone_UnconsumedSourceProperty_DoesNotThrow()
    {
        // FullSource.Extra is never written to SmallDest.
        // With MemberList.None validation is skipped entirely, so no throw.
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<MLV02_Profile>());

        var act = () => config.AssertConfigurationIsValid();

        act.Should().NotThrow();
    }

    // ── MLV-03 ───────────────────────────────────────────────────────────────
    // MemberList.Source — throws when source property is unconsumed

    private class MLV03_Profile : Profile
    {
        public MLV03_Profile() => CreateMap<FullSource, SmallDest>(MemberList.Source);
    }

    [Fact]
    public void MemberListSource_UnconsumedSourceProperty_ThrowsWithPropertyName()
    {
        // FullSource.Extra has no matching SmallDest property — must be flagged.
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<MLV03_Profile>());

        var act = () => config.AssertConfigurationIsValid();

        act.Should().Throw<DeltaMapperException>()
            .WithMessage("*Extra*");
    }

    // ── MLV-04 ───────────────────────────────────────────────────────────────
    // MemberList.Source — passes when all source properties consumed by convention

    private class MLV04_Profile : Profile
    {
        public MLV04_Profile() => CreateMap<FullSource, FullDest>(MemberList.Source);
    }

    [Fact]
    public void MemberListSource_AllSourcePropertiesConsumed_DoesNotThrow()
    {
        // FullSource and FullDest have identical property names — all consumed.
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<MLV04_Profile>());

        var act = () => config.AssertConfigurationIsValid();

        act.Should().NotThrow();
    }

    // ── MLV-05 ───────────────────────────────────────────────────────────────
    // MemberList.Source — passes when source property consumed via flattening

    private class MLV05_Profile : Profile
    {
        public MLV05_Profile() => CreateMap<SourceWithNested, FlatDest>(MemberList.Source);
    }

    [Fact]
    public void MemberListSource_SourcePropertyConsumedViaFlattening_DoesNotThrow()
    {
        // SourceWithNested.Customer is consumed via FlatDest.CustomerName (flattening).
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<MLV05_Profile>());

        var act = () => config.AssertConfigurationIsValid();

        act.Should().NotThrow();
    }

    // ── MLV-06 ───────────────────────────────────────────────────────────────
    // MemberList.Source — passes when source properties consumed via constructor

    private class MLV06_Profile : Profile
    {
        public MLV06_Profile() => CreateMap<SmallDest, RecordDest>(MemberList.Source);
    }

    [Fact]
    public void MemberListSource_SourcePropertiesConsumedViaConstructor_DoesNotThrow()
    {
        // SmallDest has Id and Name; RecordDest(int Id, string Name) consumes both via ctor.
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<MLV06_Profile>());

        var act = () => config.AssertConfigurationIsValid();

        act.Should().NotThrow();
    }

    // ── MLV-07 ───────────────────────────────────────────────────────────────
    // Default CreateMap (no MemberList arg) uses Destination validation

    private class MLV07_Profile : Profile
    {
        public MLV07_Profile() => CreateMap<SmallDest, FullDest>();
    }

    [Fact]
    public void DefaultCreateMap_UnmappedDestinationProperty_ThrowsWithPropertyName()
    {
        // No MemberList arg defaults to Destination. FullDest.Extra has no source — must throw.
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<MLV07_Profile>());

        var act = () => config.AssertConfigurationIsValid();

        act.Should().Throw<DeltaMapperException>()
            .WithMessage("*Extra*");
    }

    // ── MLV-08 ───────────────────────────────────────────────────────────────
    // Mixed MemberList modes in same configuration — only failing map throws

    private class MLV08_Profile : Profile
    {
        public MLV08_Profile()
        {
            // This map uses None — no validation, should not contribute to throw.
            CreateMap<FullSource, SmallDest>(MemberList.None);

            // This map uses Destination (default) — FullDest.Extra has no source → throws.
            CreateMap<SmallDest, FullDest>();
        }
    }

    [Fact]
    public void MixedMemberListModes_OnlyDestinationMapThrows()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<MLV08_Profile>());

        var act = () => config.AssertConfigurationIsValid();

        act.Should().Throw<DeltaMapperException>()
            .WithMessage("*Extra*");
    }

    // ── MLV-09 ───────────────────────────────────────────────────────────────
    // MemberList.Source — property consumed via MapFrom is not flagged

    private class MapFromSource { public int Id { get; set; } public string FirstName { get; set; } = string.Empty; public string LastName { get; set; } = string.Empty; }
    private class MapFromDest { public int Id { get; set; } public string FullName { get; set; } = string.Empty; }

    private class MLV09_Profile : Profile
    {
        public MLV09_Profile()
        {
            CreateMap<MapFromSource, MapFromDest>(MemberList.Source)
                .ForMember(d => d.FullName, o => o.MapFrom(s => s.FirstName + " " + s.LastName));
        }
    }

    [Fact]
    public void Source_Validation_Passes_When_Properties_Used_Via_MapFrom()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<MLV09_Profile>());

        var act = () => config.AssertConfigurationIsValid();

        act.Should().NotThrow();
    }
}
