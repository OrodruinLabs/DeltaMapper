using DeltaMapper.Abstractions;
using DeltaMapper.Configuration;
using FluentAssertions;
using Xunit;

namespace DeltaMapper.UnitTests;

// ── Test models ────────────────────────────────────────────────────

public enum SourceStatus { Active, Inactive, Pending }
public enum DestStatus { Active, Inactive, Pending }
public enum DifferentOrderStatus { Pending, Active, Inactive }

public class EnumSource
{
    public int Id { get; set; }
    public SourceStatus Status { get; set; }
}

public class EnumDest
{
    public int Id { get; set; }
    public DestStatus Status { get; set; }
}

public class EnumDiffOrderDest
{
    public int Id { get; set; }
    public DifferentOrderStatus Status { get; set; }
}

public class NullableEnumSource
{
    public int Id { get; set; }
    public SourceStatus? Status { get; set; }
}

public class NullableEnumDest
{
    public int Id { get; set; }
    public DestStatus? Status { get; set; }
}

// ── Profile ────────────────────────────────────────────────────────

public class EnumMappingProfile : MappingProfile
{
    public EnumMappingProfile()
    {
        CreateMap<EnumSource, EnumDest>();
        CreateMap<EnumSource, EnumDiffOrderDest>();
        CreateMap<NullableEnumSource, NullableEnumDest>();
    }
}

// ── Tests ──────────────────────────────────────────────────────────

public class EnumMappingTests
{
    private readonly IMapper _mapper;

    public EnumMappingTests()
    {
        _mapper = MapperConfiguration.Create(cfg => cfg.AddProfile<EnumMappingProfile>())
            .CreateMapper();
    }

    [Fact]
    public void Enum01_MapsMatchingEnumsByName()
    {
        var source = new EnumSource { Id = 1, Status = SourceStatus.Active };
        var dest = _mapper.Map<EnumSource, EnumDest>(source);

        dest.Id.Should().Be(1);
        dest.Status.Should().Be(DestStatus.Active);
    }

    [Fact]
    public void Enum02_MapsAllEnumValues()
    {
        foreach (var status in Enum.GetValues<SourceStatus>())
        {
            var source = new EnumSource { Id = 1, Status = status };
            var dest = _mapper.Map<EnumSource, EnumDest>(source);
            dest.Status.Should().Be(Enum.Parse<DestStatus>(status.ToString()));
        }
    }

    [Fact]
    public void Enum03_MapsDifferentEnumOrderByName()
    {
        var source = new EnumSource { Id = 1, Status = SourceStatus.Active };
        var dest = _mapper.Map<EnumSource, EnumDiffOrderDest>(source);

        // Active is at index 0 in SourceStatus but index 1 in DifferentOrderStatus
        // Mapping by name should still produce the correct value
        dest.Status.Should().Be(DifferentOrderStatus.Active);
    }

    [Fact]
    public void Enum04_MapsNullableEnumWithValue()
    {
        var source = new NullableEnumSource { Id = 1, Status = SourceStatus.Pending };
        var dest = _mapper.Map<NullableEnumSource, NullableEnumDest>(source);

        dest.Status.Should().Be(DestStatus.Pending);
    }

    [Fact]
    public void Enum05_MapsNullableEnumNull()
    {
        var source = new NullableEnumSource { Id = 1, Status = null };
        var dest = _mapper.Map<NullableEnumSource, NullableEnumDest>(source);

        dest.Status.Should().BeNull();
    }

    [Fact]
    public void Enum06_ThrowsOnMismatchedEnumName()
    {
        // MismatchedEnum has a value "Unknown" that doesn't exist in DestStatus
        var mapper = MapperConfiguration.Create(cfg =>
            cfg.AddProfile(new MismatchedEnumProfile()))
            .CreateMapper();

        var source = new MismatchedEnumSource { Id = 1, Status = MismatchedStatus.Unknown };

        var act = () => mapper.Map<MismatchedEnumSource, EnumDest>(source);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*No matching name found*");
    }

    [Fact]
    public void Enum07_ThrowsOnNullToNonNullableEnum()
    {
        var mapper = MapperConfiguration.Create(cfg =>
            cfg.AddProfile(new NullableToNonNullableEnumProfile()))
            .CreateMapper();

        var source = new NullableEnumSource { Id = 1, Status = null };

        var act = () => mapper.Map<NullableEnumSource, EnumDest>(source);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*non-nullable*");
    }
}

file class NullableToNonNullableEnumProfile : MappingProfile
{
    public NullableToNonNullableEnumProfile() => CreateMap<NullableEnumSource, EnumDest>();
}

public enum MismatchedStatus { Active, Unknown }
public class MismatchedEnumSource { public int Id { get; set; } public MismatchedStatus Status { get; set; } }

file class MismatchedEnumProfile : MappingProfile
{
    public MismatchedEnumProfile() => CreateMap<MismatchedEnumSource, EnumDest>();
}
