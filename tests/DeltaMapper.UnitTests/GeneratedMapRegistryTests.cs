using DeltaMapper.Runtime;
using DeltaMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace DeltaMapper.UnitTests;

[Collection("GeneratedMapRegistry")]

/// <summary>
/// Tests for GeneratedMapRegistry — the runtime bridge for source-generated delegates.
/// Each test clears the registry to avoid cross-test pollution.
/// </summary>
public class GeneratedMapRegistryTests : IDisposable
{
    public GeneratedMapRegistryTests()
    {
        // Clear before each test to ensure clean state
        GeneratedMapRegistry.Clear();
    }

    public void Dispose()
    {
        // Clear after each test to prevent state leakage to parallel test classes
        GeneratedMapRegistry.Clear();
    }

    // ── GR-01 ─────────────────────────────────────────────────────────────────

    [Fact]
    public void GR01_Register_ThenTryGetGeneric_ReturnsRegisteredDelegate()
    {
        Action<User, UserDto> expectedDelegate = (src, dst) =>
        {
            dst.Id = src.Id;
            dst.FirstName = src.FirstName;
        };

        GeneratedMapRegistry.Register(expectedDelegate);

        var found = GeneratedMapRegistry.TryGet<User, UserDto>(out var retrieved);

        found.Should().BeTrue();
        retrieved.Should().BeSameAs(expectedDelegate);
    }

    // ── GR-02 ─────────────────────────────────────────────────────────────────

    [Fact]
    public void GR02_TryGet_WhenNotRegistered_ReturnsFalseAndNullOut()
    {
        var found = GeneratedMapRegistry.TryGet<User, UserSummaryDto>(out var retrieved);

        found.Should().BeFalse();
        retrieved.Should().BeNull();
    }

    // ── GR-03 ─────────────────────────────────────────────────────────────────

    [Fact]
    public void GR03_TryGetNonGeneric_WhenRegistered_ReturnsInvocableAction()
    {
        var invoked = false;
        Action<User, UserDto> expectedDelegate = (src, dst) => { dst.Id = src.Id; invoked = true; };
        GeneratedMapRegistry.Register(expectedDelegate);

        var found = GeneratedMapRegistry.TryGet(typeof(User), typeof(UserDto), out var retrieved);

        found.Should().BeTrue();
        retrieved.Should().NotBeNull();

        // Verify the boxed wrapper correctly delegates to the original action
        var user = new User { Id = 42, FirstName = "Test" };
        var dto = new UserDto();
        retrieved!(user, dto);
        dto.Id.Should().Be(42);
        invoked.Should().BeTrue();
    }

    // GR-04: Integration test (GeneratedMapRegistry + MapperConfiguration) is in DeltaMapper.SourceGen.Tests
    // to avoid static registry pollution across parallel xUnit test classes.

    // ── GR-05 ─────────────────────────────────────────────────────────────────

    [Fact]
    public void GR05_Register_OverwritesPreviousDelegate()
    {
        var firstCallCount = 0;
        var secondCallCount = 0;

        Action<User, UserDto> first = (src, dst) => firstCallCount++;
        Action<User, UserDto> second = (src, dst) => secondCallCount++;

        GeneratedMapRegistry.Register(first);
        GeneratedMapRegistry.Register(second); // overwrite

        GeneratedMapRegistry.TryGet<User, UserDto>(out var retrieved);
        retrieved.Should().BeSameAs(second, "the second registration should overwrite the first");
    }

    // ── GR-06 ─────────────────────────────────────────────────────────────────

    [Fact]
    public void GR06_RegisterFactory_ThenTryGetFactory_ReturnsRegisteredDelegate()
    {
        Func<User, UserDto> factory = src => new UserDto { Id = src.Id, FirstName = src.FirstName };

        GeneratedMapRegistry.RegisterFactory(factory);

        var found = GeneratedMapRegistry.TryGetFactory<User, UserDto>(out var retrieved);

        found.Should().BeTrue();
        retrieved.Should().BeSameAs(factory);
    }

    // ── GR-07 ─────────────────────────────────────────────────────────────────

    [Fact]
    public void GR07_TryGetFactory_WhenNotRegistered_ReturnsFalse()
    {
        var found = GeneratedMapRegistry.TryGetFactory<User, UserSummaryDto>(out var retrieved);

        found.Should().BeFalse();
        retrieved.Should().BeNull();
    }

    // ── GR-08 ─────────────────────────────────────────────────────────────────

    [Fact]
    public void GR08_FastPath_UsesFactory_WhenNoMiddlewareOrProfile()
    {
        // Register a factory — simulating what [ModuleInitializer] does
        Func<User, UserDto> factory = src => new UserDto { Id = src.Id, FirstName = src.FirstName };
        GeneratedMapRegistry.RegisterFactory(factory);

        // Create config with no profiles (empty) and no middleware
        var config = DeltaMapper.Configuration.MapperConfiguration.Create(_ => { });
        var mapper = config.CreateMapper();

        // Map should use the fast path (factory)
        var user = new User { Id = 99, FirstName = "FastPath" };
        var result = mapper.Map<User, UserDto>(user);

        result.Id.Should().Be(99);
        result.FirstName.Should().Be("FastPath");
    }

    // ── GR-09 ─────────────────────────────────────────────────────────────────

    [Fact]
    public void GR09_FastPath_NotUsed_WhenCompiledMapExists()
    {
        // Register a factory that returns wrong data
        Func<User, UserDto> factory = src => new UserDto { Id = -1, FirstName = "WRONG" };
        GeneratedMapRegistry.RegisterFactory(factory);

        // Create config WITH a profile — compiled map should take precedence
        var config = DeltaMapper.Configuration.MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile(new TestUserProfile());
        });
        var mapper = config.CreateMapper();

        var user = new User { Id = 42, FirstName = "Correct" };
        var result = mapper.Map<User, UserDto>(user);

        // Should use compiled map (profile), NOT the factory
        result.Id.Should().Be(42);
        result.FirstName.Should().Be("Correct");
    }
}

file class TestUserProfile : DeltaMapper.Configuration.MappingProfile
{
    public TestUserProfile() => CreateMap<User, UserDto>();
}
