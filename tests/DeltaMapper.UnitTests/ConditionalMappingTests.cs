using DeltaMapper.Configuration;
using FluentAssertions;
using Xunit;

namespace DeltaMapper.UnitTests;

// ── Test models ────────────────────────────────────────────────────

public class CondSource
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public string? Email { get; set; }
}

public class CondDest
{
    public int Id { get; set; }
    public string Name { get; set; } = "default";
    public int Age { get; set; }
    public string? Email { get; set; } = "default@test.com";
}

// ── Tests ──────────────────────────────────────────────────────────

public class ConditionalMappingTests
{
    [Fact]
    public void Cond01_ConditionTrue_PropertyMapped()
    {
        var mapper = MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile(new InlineProfile<CondSource, CondDest>(map =>
                map.ForMember(d => d.Age, o => o.Condition(s => s.Age > 0))));
        }).CreateMapper();

        var src = new CondSource { Id = 1, Name = "Alice", Age = 30 };
        var dest = mapper.Map<CondSource, CondDest>(src);

        dest.Age.Should().Be(30);
    }

    [Fact]
    public void Cond02_ConditionFalse_PropertySkipped()
    {
        var mapper = MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile(new InlineProfile<CondSource, CondDest>(map =>
                map.ForMember(d => d.Age, o => o.Condition(s => s.Age > 0))));
        }).CreateMapper();

        var src = new CondSource { Id = 1, Name = "Bob", Age = 0 };
        var dest = mapper.Map<CondSource, CondDest>(src);

        // Age condition false → keeps destination default (0 for int)
        dest.Age.Should().Be(0);
        // Other properties still mapped
        dest.Name.Should().Be("Bob");
    }

    [Fact]
    public void Cond03_MultipleConditions()
    {
        var mapper = MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile(new InlineProfile<CondSource, CondDest>(map =>
            {
                map.ForMember(d => d.Age, o => o.Condition(s => s.Age > 0));
                map.ForMember(d => d.Email, o => o.Condition(s => s.Email != null));
            }));
        }).CreateMapper();

        var src = new CondSource { Id = 1, Name = "Carol", Age = 25, Email = null };
        var dest = mapper.Map<CondSource, CondDest>(src);

        dest.Age.Should().Be(25); // condition true
        dest.Email.Should().Be("default@test.com"); // condition false → keeps destination default
    }

    [Fact]
    public void Cond04_ConditionWithCustomResolver()
    {
        var mapper = MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile(new InlineProfile<CondSource, CondDest>(map =>
            {
                map.ForMember(d => d.Name, o =>
                {
                    o.MapFrom(s => s.Name.ToUpper());
                    o.Condition(s => s.Name.Length > 3);
                });
            }));
        }).CreateMapper();

        var short_name = new CondSource { Id = 1, Name = "Bo", Age = 1 };
        var long_name = new CondSource { Id = 2, Name = "Alice", Age = 1 };

        var dest1 = mapper.Map<CondSource, CondDest>(short_name);
        var dest2 = mapper.Map<CondSource, CondDest>(long_name);

        dest1.Name.Should().Be("default"); // condition false, keeps default
        dest2.Name.Should().Be("ALICE"); // condition true, resolver applied
    }
}

/// <summary>
/// Helper to create inline profiles without a separate class per test.
/// </summary>
file class InlineProfile<TSrc, TDst> : MappingProfile
{
    public InlineProfile(Action<Abstractions.IMappingExpression<TSrc, TDst>> configure)
    {
        var expr = CreateMap<TSrc, TDst>();
        configure(expr);
    }
}
