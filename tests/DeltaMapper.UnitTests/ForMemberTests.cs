using DeltaMapper;
using DeltaMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace DeltaMapper.UnitTests;

public class ForMemberTests
{
    // FM-01: MapFrom applies a custom resolver to compute FullName
    [Fact]
    public void ForMember_MapFrom_AppliesCustomResolver()
    {
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfile<FM01Profile>());
        var mapper = config.CreateMapper();

        var user = new User { Id = 1, FirstName = "Jane", LastName = "Smith", Email = "jane@test.com", Age = 28 };
        var dto = mapper.Map<User, UserSummaryDto>(user);

        dto.FullName.Should().Be("Jane Smith");
    }

    private class FM01Profile : MappingProfile
    {
        public FM01Profile()
        {
            CreateMap<User, UserSummaryDto>()
                .ForMember(d => d.FullName, o => o.MapFrom(s => $"{s.FirstName} {s.LastName}"));
        }
    }

    // FM-02: MapFrom with a complex expression (derived value)
    [Fact]
    public void ForMember_MapFrom_WithComplexExpression()
    {
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfile<FM02Profile>());
        var mapper = config.CreateMapper();

        // Email length: "john@test.com" = 13 characters — mapped into FullName via complex expression
        var user = new User { Id = 2, FirstName = "John", LastName = "Doe", Email = "john@test.com", Age = 35 };
        var dto = mapper.Map<User, UserSummaryDto>(user);

        // FullName derived from email character count
        dto.FullName.Should().Be("Email length: 13");
    }

    private class FM02Profile : MappingProfile
    {
        public FM02Profile()
        {
            CreateMap<User, UserSummaryDto>()
                .ForMember(d => d.FullName, o => o.MapFrom(s => $"Email length: {s.Email.Length}"));
        }
    }

    // FM-03: Ignore skips the property, leaving it at default
    [Fact]
    public void ForMember_Ignore_SkipsProperty()
    {
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfile<FM03Profile>());
        var mapper = config.CreateMapper();

        var user = new User { Id = 3, FirstName = "Alice", LastName = "Wonder", Email = "alice@test.com", Age = 22 };
        var dto = mapper.Map<User, UserDto>(user);

        dto.Email.Should().Be(string.Empty);
    }

    private class FM03Profile : MappingProfile
    {
        public FM03Profile()
        {
            CreateMap<User, UserDto>()
                .ForMember(d => d.Email, o => o.Ignore());
        }
    }

    // FM-04: Ignoring one property does not affect others
    [Fact]
    public void ForMember_Ignore_DoesNotAffectOtherProperties()
    {
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfile<FM04Profile>());
        var mapper = config.CreateMapper();

        var user = new User { Id = 4, FirstName = "Bob", LastName = "Builder", Email = "bob@test.com", Age = 40 };
        var dto = mapper.Map<User, UserDto>(user);

        dto.Id.Should().Be(4);
        dto.FirstName.Should().Be("Bob");
        dto.LastName.Should().Be("Builder");
        dto.Age.Should().Be(40);
        dto.Email.Should().Be(string.Empty); // ignored
    }

    private class FM04Profile : MappingProfile
    {
        public FM04Profile()
        {
            CreateMap<User, UserDto>()
                .ForMember(d => d.Email, o => o.Ignore());
        }
    }

    // FM-05: NullSubstitute uses the substitute value when source is null
    [Fact]
    public void ForMember_NullSubstitute_WhenSourceIsNull_UsesSubstitute()
    {
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfile<FM05Profile>());
        var mapper = config.CreateMapper();

        var user = new User { Id = 5, FirstName = "Carol", LastName = "White", Email = null!, Age = 30 };
        var dto = mapper.Map<User, UserSummaryDto>(user);

        dto.Email.Should().Be("N/A");
    }

    private class FM05Profile : MappingProfile
    {
        public FM05Profile()
        {
            CreateMap<User, UserSummaryDto>()
                .ForMember(d => d.FullName, o => o.MapFrom(s => $"{s.FirstName} {s.LastName}"))
                .ForMember(d => d.Email, o => o.NullSubstitute("N/A"));
        }
    }

    // FM-06: NullSubstitute does not override a present source value
    [Fact]
    public void ForMember_NullSubstitute_WhenSourceHasValue_UsesSourceValue()
    {
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfile<FM06Profile>());
        var mapper = config.CreateMapper();

        var user = new User { Id = 6, FirstName = "Dave", LastName = "Brown", Email = "dave@test.com", Age = 45 };
        var dto = mapper.Map<User, UserSummaryDto>(user);

        dto.Email.Should().Be("dave@test.com");
    }

    private class FM06Profile : MappingProfile
    {
        public FM06Profile()
        {
            CreateMap<User, UserSummaryDto>()
                .ForMember(d => d.FullName, o => o.MapFrom(s => $"{s.FirstName} {s.LastName}"))
                .ForMember(d => d.Email, o => o.NullSubstitute("N/A"));
        }
    }

    // FM-07: Multiple ForMember overrides on the same mapping are all respected
    [Fact]
    public void ForMember_MultipleOverrides_AllApplied()
    {
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfile<FM07Profile>());
        var mapper = config.CreateMapper();

        var user = new User { Id = 7, FirstName = "Eve", LastName = "Adams", Email = null!, Age = 33 };
        var dto = mapper.Map<User, UserSummaryDto>(user);

        dto.Id.Should().Be(7);
        dto.FullName.Should().Be("Eve Adams");
        dto.Email.Should().Be("no-email");
    }

    private class FM07Profile : MappingProfile
    {
        public FM07Profile()
        {
            CreateMap<User, UserSummaryDto>()
                .ForMember(d => d.FullName, o => o.MapFrom(s => $"{s.FirstName} {s.LastName}"))
                .ForMember(d => d.Email, o => o.NullSubstitute("no-email"));
        }
    }
}
