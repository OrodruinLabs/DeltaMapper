using DeltaMapper.Configuration;
using DeltaMapper.Middleware;
using DeltaMapper.Runtime;
using FluentAssertions;
using Xunit;

namespace DeltaMapper.UnitTests;

public class MiddlewarePipelineTests
{
    [Fact]
    public void Execute_NoMiddleware_CallsCoreDirectly()
    {
        var pipeline = new MappingPipeline(Array.Empty<IMappingMiddleware>());
        var coreCalled = false;
        var config = new MapperConfiguration();
        var ctx = new MapperContext(config);

        var result = pipeline.Execute("source", typeof(string), ctx, () =>
        {
            coreCalled = true;
            return "result";
        });

        coreCalled.Should().BeTrue();
        result.Should().Be("result");
    }

    [Fact]
    public void Execute_SingleMiddleware_WrapsCore()
    {
        var order = new List<string>();
        var middleware = new TestMiddleware("M1", order);
        var pipeline = new MappingPipeline(new[] { middleware });
        var config = new MapperConfiguration();
        var ctx = new MapperContext(config);

        var result = pipeline.Execute("source", typeof(string), ctx, () =>
        {
            order.Add("core");
            return "result";
        });

        order.Should().Equal("M1-before", "core", "M1-after");
        result.Should().Be("result");
    }

    [Fact]
    public void Execute_MultipleMiddleware_ExecuteInOrder()
    {
        var order = new List<string>();
        var middlewares = new IMappingMiddleware[]
        {
            new TestMiddleware("M1", order),
            new TestMiddleware("M2", order)
        };
        var pipeline = new MappingPipeline(middlewares);
        var config = new MapperConfiguration();
        var ctx = new MapperContext(config);

        var result = pipeline.Execute("source", typeof(string), ctx, () =>
        {
            order.Add("core");
            return "result";
        });

        order.Should().Equal("M1-before", "M2-before", "core", "M2-after", "M1-after");
    }

    private class TestMiddleware : IMappingMiddleware
    {
        private readonly string _name;
        private readonly List<string> _order;

        public TestMiddleware(string name, List<string> order)
        {
            _name = name;
            _order = order;
        }

        public object Map(object source, Type destType, MapperContext ctx, Func<object> next)
        {
            _order.Add($"{_name}-before");
            var result = next();
            _order.Add($"{_name}-after");
            return result;
        }
    }
}
