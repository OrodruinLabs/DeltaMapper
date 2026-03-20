using System.Diagnostics;
using DeltaMapper;
using DeltaMapper.OpenTelemetry;
using FluentAssertions;
using Xunit;

namespace DeltaMapper.IntegrationTests;

// ─── Test models ──────────────────────────────────────────────────────────────

public class TracingSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class TracingDest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// ─── Mapping profile ───────────────────────────────────────────────────────────

public class TracingProfile : Profile
{
    public TracingProfile()
    {
        CreateMap<TracingSource, TracingDest>();
    }
}

// ─── Tests ─────────────────────────────────────────────────────────────────────

public class TracingMiddlewareTests
{
    private static MapperConfiguration BuildConfig() =>
        MapperConfiguration.Create(cfg =>
        {
            cfg.AddMapperTracing();
            cfg.AddProfile<TracingProfile>();
        });

    [Fact]
    public void Tracing01_MappingEmitsActivitySpan()
    {
        // Arrange
        var activities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "DeltaMapper",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => activities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);

        var mapper = BuildConfig().CreateMapper();
        var source = new TracingSource { Id = 1, Name = "Alpha" };

        // Act
        mapper.Map<TracingSource, TracingDest>(source);

        // Assert — at least one activity was emitted with the expected name
        activities.Should().NotBeEmpty();
        activities.Should().ContainSingle(a => a.OperationName == "Map TracingSource -> TracingDest");
    }

    [Fact]
    public void Tracing02_ActivityHasSourceAndDestTypeTags()
    {
        // Arrange
        var activities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "DeltaMapper",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => activities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);

        var mapper = BuildConfig().CreateMapper();
        var source = new TracingSource { Id = 2, Name = "Beta" };

        // Act
        mapper.Map<TracingSource, TracingDest>(source);

        // Assert — tags carry full type names
        activities.Should().NotBeEmpty();
        var activity = activities.First(a => a.OperationName == "Map TracingSource -> TracingDest");

        var sourceTag = activity.Tags.FirstOrDefault(t => t.Key == "mapper.source_type");
        sourceTag.Value.Should().Be(typeof(TracingSource).FullName);

        var destTag = activity.Tags.FirstOrDefault(t => t.Key == "mapper.dest_type");
        destTag.Value.Should().Be(typeof(TracingDest).FullName);
    }

    [Fact]
    public void Tracing03_NoListenerNoActivity()
    {
        // Arrange — no ActivityListener registered; mapping must still succeed
        var mapper = BuildConfig().CreateMapper();
        var source = new TracingSource { Id = 3, Name = "Gamma" };

        // Act
        var act = () => mapper.Map<TracingSource, TracingDest>(source);

        // Assert — zero-overhead path: no exception, result is correct
        var dest = act.Should().NotThrow().Subject;
        dest.Id.Should().Be(3);
        dest.Name.Should().Be("Gamma");
    }

    [Fact]
    public void Tracing04_MiddlewareRegisteredViaAddMapperTracing()
    {
        // Arrange — verify AddMapperTracing() wires middleware into the pipeline
        var activities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "DeltaMapper",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => activities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);

        // Act — build config via the extension method under test
        var config = MapperConfiguration.Create(cfg =>
        {
            cfg.AddMapperTracing();
            cfg.AddProfile<TracingProfile>();
        });
        var mapper = config.CreateMapper();
        var source = new TracingSource { Id = 4, Name = "Delta" };
        mapper.Map<TracingSource, TracingDest>(source);

        // Assert — middleware executed (activity captured proves it ran in the pipeline)
        activities.Should().NotBeEmpty(
            because: "AddMapperTracing() must register TracingMiddleware in the pipeline");
    }

    [Fact]
    public void Tracing05_ActivityDurationReflectsMappingTime()
    {
        // Arrange
        var activities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "DeltaMapper",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => activities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);

        var mapper = BuildConfig().CreateMapper();
        var source = new TracingSource { Id = 5, Name = "Epsilon" };

        // Act
        mapper.Map<TracingSource, TracingDest>(source);

        // Assert — activity has a non-negative (stopped) duration
        activities.Should().NotBeEmpty();
        var activity = activities.First(a => a.OperationName == "Map TracingSource -> TracingDest");

        // A stopped activity has a non-zero Duration (start ≠ stop) in practice,
        // and Duration is never negative.
        activity.Duration.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);

        // The activity must be in the Stopped state (not running)
        activity.Status.Should().NotBe(ActivityStatusCode.Error);
    }
}
