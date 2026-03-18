using DeltaMapper.Configuration;
using DeltaMapper.Runtime;
using FluentAssertions;
using Xunit;

namespace DeltaMapper.UnitTests;

public class MapperContextTests
{
    private readonly MapperContext _ctx;

    public MapperContextTests()
    {
        var config = new MapperConfiguration();
        _ctx = new MapperContext(config);
    }

    [Fact]
    public void TryGetMapped_UnvisitedObject_ReturnsFalse()
    {
        var source = new object();
        _ctx.TryGetMapped(source, out _).Should().BeFalse();
    }

    [Fact]
    public void TryGetMapped_AfterRegister_ReturnsTrue()
    {
        var source = new object();
        var dest = new object();
        _ctx.Register(source, dest);

        _ctx.TryGetMapped(source, out var mapped).Should().BeTrue();
        mapped.Should().BeSameAs(dest);
    }

    [Fact]
    public void TryGetMapped_DifferentReference_ReturnsFalse()
    {
        var source1 = new object();
        var source2 = new object();
        var dest = new object();
        _ctx.Register(source1, dest);

        _ctx.TryGetMapped(source2, out _).Should().BeFalse();
    }

    [Fact]
    public void Register_SameSourceTwice_UpdatesMapping()
    {
        var source = new object();
        var dest1 = new object();
        var dest2 = new object();

        _ctx.Register(source, dest1);
        _ctx.Register(source, dest2);

        _ctx.TryGetMapped(source, out var mapped).Should().BeTrue();
        mapped.Should().BeSameAs(dest2);
    }
}
