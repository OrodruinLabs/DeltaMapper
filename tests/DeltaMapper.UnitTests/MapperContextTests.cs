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
        _ctx.TryGetMapped(source, typeof(object), out _).Should().BeFalse();
    }

    [Fact]
    public void TryGetMapped_AfterRegister_ReturnsTrue()
    {
        var source = new object();
        var dest = new object();
        _ctx.Register(source, typeof(object), dest);

        _ctx.TryGetMapped(source, typeof(object), out var mapped).Should().BeTrue();
        mapped.Should().BeSameAs(dest);
    }

    [Fact]
    public void TryGetMapped_DifferentReference_ReturnsFalse()
    {
        var source1 = new object();
        var source2 = new object();
        var dest = new object();
        _ctx.Register(source1, typeof(object), dest);

        _ctx.TryGetMapped(source2, typeof(object), out _).Should().BeFalse();
    }

    [Fact]
    public void Register_SameSourceTwice_UpdatesMapping()
    {
        var source = new object();
        var dest1 = new object();
        var dest2 = new object();

        _ctx.Register(source, typeof(object), dest1);
        _ctx.Register(source, typeof(object), dest2);

        _ctx.TryGetMapped(source, typeof(object), out var mapped).Should().BeTrue();
        mapped.Should().BeSameAs(dest2);
    }

    [Fact]
    public void TryGetMapped_SameSource_DifferentDestType_ReturnsFalse()
    {
        var source = new object();
        var dest = "hello";
        _ctx.Register(source, typeof(string), dest);

        _ctx.TryGetMapped(source, typeof(int), out _).Should().BeFalse();
    }

    [Fact]
    public void Register_SameSource_DifferentDestTypes_BothRetrievable()
    {
        var source = new object();
        var dest1 = "hello";
        var dest2 = 42;

        _ctx.Register(source, typeof(string), dest1);
        _ctx.Register(source, typeof(int), dest2);

        _ctx.TryGetMapped(source, typeof(string), out var mapped1).Should().BeTrue();
        mapped1.Should().Be("hello");

        _ctx.TryGetMapped(source, typeof(int), out var mapped2).Should().BeTrue();
        mapped2.Should().Be(42);
    }
}
