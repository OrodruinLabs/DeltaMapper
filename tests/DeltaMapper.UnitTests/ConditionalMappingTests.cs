using DeltaMapper.Abstractions;
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
                    o.MapFrom(s => s.Name.ToUpperInvariant());
                    o.Condition(s => s.Name.Length > 3);
                });
            }));
        }).CreateMapper();

        var shortName = new CondSource { Id = 1, Name = "Bo", Age = 1 };
        var longName = new CondSource { Id = 2, Name = "Alice", Age = 1 };

        var dest1 = mapper.Map<CondSource, CondDest>(shortName);
        var dest2 = mapper.Map<CondSource, CondDest>(longName);

        dest1.Name.Should().Be("default"); // condition false, keeps default
        dest2.Name.Should().Be("ALICE"); // condition true, resolver applied
    }

    [Fact]
    public void Cond05_ConditionFalse_ExistingDestPreserved()
    {
        var mapper = MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile(new InlineProfile<CondSource, CondDest>(map =>
                map.ForMember(d => d.Age, o => o.Condition(s => s.Age > 0))));
        }).CreateMapper();

        var src = new CondSource { Id = 1, Name = "Bob", Age = 0 };
        var existing = new CondDest { Id = 99, Name = "Original", Age = 42, Email = "keep@me.com" };
        var dest = mapper.Map(src, existing);

        dest.Age.Should().Be(42); // condition false → existing value preserved
        dest.Name.Should().Be("Bob"); // no condition → mapped
        dest.Should().BeSameAs(existing);
    }

    [Fact]
    public void Cond06_NullSubstituteWithConditionTrue()
    {
        var mapper = MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile(new InlineProfile<CondSource, CondDest>(map =>
                map.ForMember(d => d.Email, o =>
                {
                    o.NullSubstitute("fallback@test.com");
                    o.Condition(s => s.Email != null || s.Age > 0);
                })));
        }).CreateMapper();

        var src = new CondSource { Id = 1, Name = "Alice", Age = 5, Email = null };
        var dest = mapper.Map<CondSource, CondDest>(src);

        dest.Email.Should().Be("fallback@test.com"); // condition true, null substitute applied
    }

    [Fact]
    public void Cond07_NullSubstituteWithConditionFalse()
    {
        var mapper = MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile(new InlineProfile<CondSource, CondDest>(map =>
                map.ForMember(d => d.Email, o =>
                {
                    o.NullSubstitute("fallback@test.com");
                    o.Condition(s => s.Age > 0);
                })));
        }).CreateMapper();

        var src = new CondSource { Id = 1, Name = "Bob", Age = 0, Email = null };
        var dest = mapper.Map<CondSource, CondDest>(src);

        dest.Email.Should().Be("default@test.com"); // condition false → keeps destination default
    }

    [Fact]
    public void Cond08_ConditionFalse_CtorParam_UsesDefault()
    {
        var mapper = MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile(new InlineProfile<CondSource, CondRecordDest>(map =>
                map.ForMember(d => d.Age, o => o.Condition(s => s.Age > 0))));
        }).CreateMapper();

        var src = new CondSource { Id = 1, Name = "Bob", Age = 0 };
        var dest = mapper.Map<CondSource, CondRecordDest>(src);

        dest.Age.Should().Be(0); // condition false → ctor gets default(int)
        dest.Name.Should().Be("Bob"); // no condition → mapped
    }

    [Fact]
    public void Cond09_ConditionTrue_CtorParam_Mapped()
    {
        var mapper = MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile(new InlineProfile<CondSource, CondRecordDest>(map =>
                map.ForMember(d => d.Age, o => o.Condition(s => s.Age > 0))));
        }).CreateMapper();

        var src = new CondSource { Id = 1, Name = "Alice", Age = 30 };
        var dest = mapper.Map<CondSource, CondRecordDest>(src);

        dest.Age.Should().Be(30); // condition true → ctor gets source value
    }

    [Fact]
    public void Cond10_ConditionWithCustomResolver_CtorParam()
    {
        var mapper = MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile(new InlineProfile<CondSource, CondRecordDest>(map =>
                map.ForMember(d => d.Name, o =>
                {
                    o.MapFrom(s => s.Name.ToUpperInvariant());
                    o.Condition(s => s.Name.Length > 3);
                })));
        }).CreateMapper();

        var shortSrc = new CondSource { Id = 1, Name = "Bo", Age = 1 };
        var longSrc = new CondSource { Id = 2, Name = "Alice", Age = 1 };

        var dest1 = mapper.Map<CondSource, CondRecordDest>(shortSrc);
        var dest2 = mapper.Map<CondSource, CondRecordDest>(longSrc);

        dest1.Name.Should().BeNull(); // condition false → ctor gets null default
        dest2.Name.Should().Be("ALICE"); // condition true → resolver applied
    }

    [Fact]
    public void Cond11_Ignore_Then_Condition_Throws()
    {
        var act = () => MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile(new InlineProfile<CondSource, CondDest>(map =>
                map.ForMember(d => d.Name, o =>
                {
                    o.Ignore();
                    o.Condition(s => s.Name.Length > 3);
                })));
        });

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot combine*Condition*Ignore*");
    }

    [Fact]
    public void Cond12_Condition_Then_Ignore_Throws()
    {
        var act = () => MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile(new InlineProfile<CondSource, CondDest>(map =>
                map.ForMember(d => d.Name, o =>
                {
                    o.Condition(s => s.Name.Length > 3);
                    o.Ignore();
                })));
        });

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot combine*Ignore*Condition*");
    }
}

// ── Record model for ctor-bound tests ─────────────────────────────

public record CondRecordDest(int Id, string Name, int Age, string? Email);

/// <summary>
/// Helper to create inline profiles without a separate class per test.
/// </summary>
file class InlineProfile<TSrc, TDst> : Profile
{
    public InlineProfile(Action<IMappingExpression<TSrc, TDst>> configure)
    {
        var expr = CreateMap<TSrc, TDst>();
        configure(expr);
    }
}
