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

public class EnumProfile : Profile
{
    public EnumProfile()
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
        _mapper = MapperConfiguration.Create(cfg => cfg.AddProfile<EnumProfile>())
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

        act.Should().Throw<DeltaMapperException>()
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

        act.Should().Throw<DeltaMapperException>()
            .WithMessage("*non-nullable*");
    }
    [Fact]
    public void Enum08_MapsFlagsEnumCompositeByName()
    {
        var mapper = MapperConfiguration.Create(cfg =>
            cfg.AddProfile(new FlagsEnumProfile()))
            .CreateMapper();

        var source = new FlagsEnumSource { Permissions = SourcePermissions.Read | SourcePermissions.Write };
        var dest = mapper.Map<FlagsEnumSource, FlagsEnumDest>(source);

        dest.Permissions.Should().Be(DestPermissions.Read | DestPermissions.Write);
    }

    [Fact]
    public void Enum09_MapsFlagsEnumSingleValue()
    {
        var mapper = MapperConfiguration.Create(cfg =>
            cfg.AddProfile(new FlagsEnumProfile()))
            .CreateMapper();

        var source = new FlagsEnumSource { Permissions = SourcePermissions.Execute };
        var dest = mapper.Map<FlagsEnumSource, FlagsEnumDest>(source);

        dest.Permissions.Should().Be(DestPermissions.Execute);
    }

    [Fact]
    public void Enum10_MapsFlagsEnumAliasToConstituents()
    {
        // Source has ReadWrite alias (Read|Write), dest does not — should decompose to constituents
        var mapper = MapperConfiguration.Create(cfg =>
            cfg.AddProfile(new FlagsEnumProfile()))
            .CreateMapper();

        var source = new FlagsEnumSource { Permissions = SourcePermissions.ReadWrite };
        var dest = mapper.Map<FlagsEnumSource, FlagsEnumDest>(source);

        dest.Permissions.Should().Be(DestPermissions.Read | DestPermissions.Write);
    }

    [Fact]
    public void Enum11_MapsEnumViaConstructorPath()
    {
        var mapper = MapperConfiguration.Create(cfg =>
            cfg.AddProfile(new RecordEnumProfile()))
            .CreateMapper();

        var source = new EnumSource { Id = 1, Status = SourceStatus.Pending };
        var dest = mapper.Map<EnumSource, EnumRecordDest>(source);

        dest.Id.Should().Be(1);
        dest.Status.Should().Be(DestStatus.Pending);
    }

    [Fact]
    public void Enum12_MapsNullableEnumViaConstructorPath()
    {
        var mapper = MapperConfiguration.Create(cfg =>
            cfg.AddProfile(new NullableRecordEnumProfile()))
            .CreateMapper();

        var source = new NullableEnumSource { Id = 1, Status = SourceStatus.Active };
        var dest = mapper.Map<NullableEnumSource, NullableEnumRecordDest>(source);

        dest.Status.Should().Be(DestStatus.Active);
    }

    [Fact]
    public void Enum13_ThrowsNullToNonNullableEnumViaConstructorPath()
    {
        var mapper = MapperConfiguration.Create(cfg =>
            cfg.AddProfile(new NullableToNonNullRecordEnumProfile()))
            .CreateMapper();

        var source = new NullableEnumSource { Id = 1, Status = null };

        var act = () => mapper.Map<NullableEnumSource, EnumRecordDest>(source);

        act.Should().Throw<DeltaMapperException>()
            .WithMessage("*non-nullable*");
    }

    [Fact]
    public void Enum14_MapsNonNullableToNullableSameEnum()
    {
        var mapper = MapperConfiguration.Create(cfg =>
            cfg.AddProfile(new SameEnumNonNullToNullableProfile()))
            .CreateMapper();

        var source = new SameEnumNonNullableSource { Status = SourceStatus.Active };
        var dest = mapper.Map<SameEnumNonNullableSource, SameEnumNullableDest>(source);

        dest.Status.Should().Be(SourceStatus.Active);
    }

    [Fact]
    public void Enum15_MapsNullableToNonNullableSameEnumWithValue()
    {
        var mapper = MapperConfiguration.Create(cfg =>
            cfg.AddProfile(new SameEnumNullableToNonNullProfile()))
            .CreateMapper();

        var source = new SameEnumNullableSource { Status = SourceStatus.Inactive };
        var dest = mapper.Map<SameEnumNullableSource, SameEnumNonNullableDest>(source);

        dest.Status.Should().Be(SourceStatus.Inactive);
    }

    [Fact]
    public void Enum16_ThrowsNullableToNonNullableSameEnumNull()
    {
        var mapper = MapperConfiguration.Create(cfg =>
            cfg.AddProfile(new SameEnumNullableToNonNullProfile()))
            .CreateMapper();

        var source = new SameEnumNullableSource { Status = null };

        var act = () => mapper.Map<SameEnumNullableSource, SameEnumNonNullableDest>(source);

        act.Should().Throw<DeltaMapperException>()
            .WithMessage("*non-nullable*");
    }
}

// ── Same-enum nullable pair models ────────────────────────────────

public class SameEnumNonNullableSource { public SourceStatus Status { get; set; } }
public class SameEnumNullableDest { public SourceStatus? Status { get; set; } }
public class SameEnumNullableSource { public SourceStatus? Status { get; set; } }
public class SameEnumNonNullableDest { public SourceStatus Status { get; set; } }

file class SameEnumNonNullToNullableProfile : Profile
{
    public SameEnumNonNullToNullableProfile() => CreateMap<SameEnumNonNullableSource, SameEnumNullableDest>();
}

file class SameEnumNullableToNonNullProfile : Profile
{
    public SameEnumNullableToNonNullProfile() => CreateMap<SameEnumNullableSource, SameEnumNonNullableDest>();
}

file class NullableToNonNullableEnumProfile : Profile
{
    public NullableToNonNullableEnumProfile() => CreateMap<NullableEnumSource, EnumDest>();
}

// ── [Flags] enum models ───────────────────────────────────────────

[Flags] public enum SourcePermissions { None = 0, Read = 1, Write = 2, Execute = 4, ReadWrite = Read | Write }
[Flags] public enum DestPermissions { None = 0, Read = 1, Write = 2, Execute = 4 }

public class FlagsEnumSource { public SourcePermissions Permissions { get; set; } }
public class FlagsEnumDest { public DestPermissions Permissions { get; set; } }

file class FlagsEnumProfile : Profile
{
    public FlagsEnumProfile() => CreateMap<FlagsEnumSource, FlagsEnumDest>();
}

// ── Record/init-only enum models ──────────────────────────────────

public record EnumRecordDest(int Id, DestStatus Status);
public record NullableEnumRecordDest(int Id, DestStatus? Status);

file class RecordEnumProfile : Profile
{
    public RecordEnumProfile() => CreateMap<EnumSource, EnumRecordDest>();
}

file class NullableRecordEnumProfile : Profile
{
    public NullableRecordEnumProfile() => CreateMap<NullableEnumSource, NullableEnumRecordDest>();
}

file class NullableToNonNullRecordEnumProfile : Profile
{
    public NullableToNonNullRecordEnumProfile() => CreateMap<NullableEnumSource, EnumRecordDest>();
}

// ── Mismatch models ──────────────────────────────────────────────

public enum MismatchedStatus { Active, Unknown }
public class MismatchedEnumSource { public int Id { get; set; } public MismatchedStatus Status { get; set; } }

file class MismatchedEnumProfile : Profile
{
    public MismatchedEnumProfile() => CreateMap<MismatchedEnumSource, EnumDest>();
}
