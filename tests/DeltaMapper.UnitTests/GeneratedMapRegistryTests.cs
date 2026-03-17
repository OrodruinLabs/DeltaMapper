using DeltaMapper.Configuration;
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
    public void GR03_TryGetNonGeneric_WhenRegistered_ReturnsDelegate()
    {
        Action<User, UserDto> expectedDelegate = (src, dst) => { dst.Id = src.Id; };
        GeneratedMapRegistry.Register(expectedDelegate);

        var found = GeneratedMapRegistry.TryGet(typeof(User), typeof(UserDto), out var retrieved);

        found.Should().BeTrue();
        retrieved.Should().BeSameAs(expectedDelegate);
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
}
