using System.Reflection;
using DeltaMapper;
using FluentAssertions;
using Xunit;

namespace DeltaMapper.UnitTests;

// ── Test models (for scanning) ────────────────────────────────────

public sealed class ScanSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class ScanDest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class ScanSource2
{
    public string Code { get; set; } = string.Empty;
}

public sealed class ScanDest2
{
    public string Code { get; set; } = string.Empty;
}

// ── Test profiles ──────────────────────────────────────────────────

public sealed class ScanProfile1 : Profile
{
    public ScanProfile1()
    {
        CreateMap<ScanSource, ScanDest>();
    }
}

public sealed class ScanProfile2 : Profile
{
    public ScanProfile2()
    {
        CreateMap<ScanSource2, ScanDest2>();
    }
}

/// <summary>Abstract profile — should NOT be discovered by scanning.</summary>
public abstract class AbstractScanProfile : Profile
{
    protected AbstractScanProfile()
    {
        CreateMap<ScanSource, ScanDest>();
    }
}

/// <summary>Profile with no parameterless constructor — should NOT be discovered.</summary>
public sealed class NoParamlessCtor : Profile
{
    public NoParamlessCtor(string _)
    {
        CreateMap<ScanSource, ScanDest>();
    }
}

// ── Anchor type for AddProfilesFromAssemblyContaining ─────────────

/// <summary>Marker type used to locate this assembly via typeof.</summary>
public sealed class ScanAnchor { }

// ── Tests ──────────────────────────────────────────────────────────

public class AssemblyScanningTests
{
    private static readonly Assembly _thisAssembly = typeof(AssemblyScanningTests).Assembly;

    [Fact]
    public void AddProfilesFromAssembly_discovers_concrete_profiles()
    {
        var mapper = MapperConfiguration.Create(cfg =>
            cfg.AddProfilesFromAssembly(_thisAssembly)).CreateMapper();

        var result = mapper.Map<ScanSource, ScanDest>(new ScanSource { Id = 1, Name = "Alice" });

        result.Id.Should().Be(1);
        result.Name.Should().Be("Alice");
    }

    [Fact]
    public void AddProfilesFromAssembly_skips_abstract_profiles()
    {
        // If the abstract profile were instantiated this would throw.
        // Building without exception is the assertion.
        var act = () => MapperConfiguration.Create(cfg =>
            cfg.AddProfilesFromAssembly(_thisAssembly)).CreateMapper();

        act.Should().NotThrow();
    }

    [Fact]
    public void AddProfilesFromAssembly_skips_profiles_without_parameterless_constructor()
    {
        // NoParamlessCtor requires a string arg — scanning should silently skip it.
        var act = () => MapperConfiguration.Create(cfg =>
            cfg.AddProfilesFromAssembly(_thisAssembly)).CreateMapper();

        act.Should().NotThrow();
    }

    [Fact]
    public void AddProfilesFromAssembly_discovers_multiple_profiles_and_all_maps_work()
    {
        var mapper = MapperConfiguration.Create(cfg =>
            cfg.AddProfilesFromAssembly(_thisAssembly)).CreateMapper();

        var dest1 = mapper.Map<ScanSource, ScanDest>(new ScanSource { Id = 7, Name = "Bob" });
        var dest2 = mapper.Map<ScanSource2, ScanDest2>(new ScanSource2 { Code = "XYZ" });

        dest1.Id.Should().Be(7);
        dest1.Name.Should().Be("Bob");
        dest2.Code.Should().Be("XYZ");
    }

    [Fact]
    public void AddProfilesFromAssembly_combined_with_explicit_AddProfile_works()
    {
        // Adding the same profile explicitly and via scanning is allowed.
        var mapper = MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile<ScanProfile1>();
            cfg.AddProfilesFromAssembly(_thisAssembly);
        }).CreateMapper();

        var result = mapper.Map<ScanSource, ScanDest>(new ScanSource { Id = 42, Name = "Carol" });

        result.Id.Should().Be(42);
        result.Name.Should().Be("Carol");
    }

    [Fact]
    public void AddProfilesFromAssemblyContaining_convenience_overload_works()
    {
        // ScanAnchor is a type in this assembly — scanning should find ScanProfile1 and ScanProfile2.
        var mapper = MapperConfiguration.Create(cfg =>
            cfg.AddProfilesFromAssemblyContaining<ScanAnchor>()).CreateMapper();

        var result = mapper.Map<ScanSource, ScanDest>(new ScanSource { Id = 5, Name = "Dave" });

        result.Id.Should().Be(5);
        result.Name.Should().Be("Dave");
    }

    [Fact]
    public void AddProfilesFromAssembly_null_assembly_throws_ArgumentNullException()
    {
        var act = () => MapperConfiguration.Create(cfg =>
            cfg.AddProfilesFromAssembly(null!)).CreateMapper();

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddProfilesFromAssembly_handles_valid_assembly_gracefully()
    {
        // Verifies the ReflectionTypeLoadException catch path doesn't interfere
        // with normal assembly scanning on a valid assembly.
        var act = () => MapperConfiguration.Create(cfg =>
            cfg.AddProfilesFromAssembly(typeof(AssemblyScanningTests).Assembly));

        act.Should().NotThrow();
    }

    [Fact]
    public void AddProfilesFromAssembly_WithIncludeReferenced_FindsProfilesInReferencedAssembly()
    {
        // FixtureProfile lives in DeltaMapper.TestFixtures (a separate referenced assembly)
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfilesFromAssembly(typeof(AssemblyScanningTests).Assembly, includeReferencedAssemblies: true));
        var mapper = config.CreateMapper();

        // This map is ONLY registered in FixtureProfile (in the referenced assembly)
        var result = mapper.Map<DeltaMapper.TestFixtures.FixtureSource, DeltaMapper.TestFixtures.FixtureDest>(
            new DeltaMapper.TestFixtures.FixtureSource { Id = 42, Value = "cross-assembly" });

        result.Id.Should().Be(42);
        result.Value.Should().Be("cross-assembly");
    }

    [Fact]
    public void AddProfilesFromAssembly_DefaultFalse_DoesNotFindProfilesInReferencedAssembly()
    {
        // Without includeReferencedAssemblies, FixtureProfile should NOT be found
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfilesFromAssembly(typeof(AssemblyScanningTests).Assembly));
        var mapper = config.CreateMapper();

        // FixtureSource → FixtureDest map should NOT exist
        var act = () => mapper.Map<DeltaMapper.TestFixtures.FixtureSource, DeltaMapper.TestFixtures.FixtureDest>(
            new DeltaMapper.TestFixtures.FixtureSource { Id = 1, Value = "test" });

        act.Should().Throw<DeltaMapperException>();
    }

    [Fact]
    public void AddProfilesFromAssemblyContaining_WithIncludeReferenced_Works()
    {
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfilesFromAssemblyContaining<AssemblyScanningTests>(includeReferencedAssemblies: true));
        var mapper = config.CreateMapper();

        var result = mapper.Map<ScanSource, ScanDest>(new ScanSource { Id = 1, Name = "test" });
        result.Id.Should().Be(1);
    }

    [Fact]
    public void AddProfilesFromAssembly_WithIncludeReferenced_DeduplicatesProfiles()
    {
        var act = () => MapperConfiguration.Create(cfg =>
            cfg.AddProfilesFromAssembly(typeof(AssemblyScanningTests).Assembly, includeReferencedAssemblies: true));

        act.Should().NotThrow();
    }
}
