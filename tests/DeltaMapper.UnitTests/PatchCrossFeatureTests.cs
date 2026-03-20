using System.Globalization;
using DeltaMapper;
using FluentAssertions;
using Xunit;

namespace DeltaMapper.UnitTests;

// ── Models ───────────────────────────────────────────────────────────

public class PCF_Source
{
    public string? DateStr { get; set; }
    public string? Name { get; set; }
    public bool ShouldMapName { get; set; }
}

public class PCF_Dest
{
    public DateTime DateStr { get; set; }
    public string? Name { get; set; }
}

file sealed class PatchCrossProfile : Profile
{
    public PatchCrossProfile()
    {
        CreateMap<PCF_Source, PCF_Dest>()
            .ForMember(d => d.Name, o => o.Condition(s => s.ShouldMapName));
    }
}

// ── Tests ────────────────────────────────────────────────────────────

public class PatchCrossFeatureTests
{
    [Fact]
    public void PCF01_Patch_TypeConverter_Detects_Change()
    {
        var mapper = MapperConfiguration.Create(cfg =>
        {
            cfg.CreateTypeConverter<string, DateTime>(s =>
                DateTime.Parse(s, CultureInfo.InvariantCulture));
            cfg.AddProfile<PatchCrossProfile>();
        }).CreateMapper();

        var source = new PCF_Source { DateStr = "2026-06-15", Name = "Test", ShouldMapName = true };
        var dest = new PCF_Dest { DateStr = new DateTime(2026, 1, 1), Name = "Old" };

        var diff = mapper.Patch<PCF_Source, PCF_Dest>(source, dest);

        diff.HasChanges.Should().BeTrue();
        diff.Changes.Should().Contain(c => c.PropertyName == "DateStr");
    }

    [Fact]
    public void PCF02_Patch_Condition_False_No_Change_Detected()
    {
        var mapper = MapperConfiguration.Create(cfg =>
        {
            cfg.CreateTypeConverter<string, DateTime>(s =>
                DateTime.Parse(s, CultureInfo.InvariantCulture));
            cfg.AddProfile<PatchCrossProfile>();
        }).CreateMapper();

        var source = new PCF_Source { DateStr = "2026-01-01", Name = "NewName", ShouldMapName = false };
        var dest = new PCF_Dest { DateStr = new DateTime(2026, 1, 1), Name = "Old" };

        var diff = mapper.Patch<PCF_Source, PCF_Dest>(source, dest);

        diff.Changes.Should().NotContain(c => c.PropertyName == "Name");
        dest.Name.Should().Be("Old");
    }

    [Fact]
    public void PCF03_Patch_Condition_True_With_Converter_Both_Changes_Detected()
    {
        var mapper = MapperConfiguration.Create(cfg =>
        {
            cfg.CreateTypeConverter<string, DateTime>(s =>
                DateTime.Parse(s, CultureInfo.InvariantCulture));
            cfg.AddProfile<PatchCrossProfile>();
        }).CreateMapper();

        var source = new PCF_Source { DateStr = "2026-06-15", Name = "NewName", ShouldMapName = true };
        var dest = new PCF_Dest { DateStr = new DateTime(2026, 1, 1), Name = "Old" };

        var diff = mapper.Patch<PCF_Source, PCF_Dest>(source, dest);

        diff.HasChanges.Should().BeTrue();
        diff.Changes.Should().Contain(c => c.PropertyName == "Name" && (string)c.To! == "NewName");
        diff.Changes.Should().Contain(c => c.PropertyName == "DateStr");
    }
}
