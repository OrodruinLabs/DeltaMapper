using DeltaMapper;
using DeltaMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace DeltaMapper.UnitTests;

public class ConventionMappingTests
{
    // ── C-01 ─────────────────────────────────────────────────────────────────
    private class C01_UserToUserDtoProfile : MappingProfile
    {
        public C01_UserToUserDtoProfile()
        {
            CreateMap<User, UserDto>();
        }
    }

    [Fact]
    public void Map_SameNameSameType_MapsAllProperties()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<C01_UserToUserDtoProfile>());
        var mapper = config.CreateMapper();

        var source = new User
        {
            Id = 42,
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane@example.com",
            Age = 28
        };

        var result = mapper.Map<User, UserDto>(source);

        result.Id.Should().Be(42);
        result.FirstName.Should().Be("Jane");
        result.LastName.Should().Be("Smith");
        result.Email.Should().Be("jane@example.com");
        result.Age.Should().Be(28);
    }

    // ── C-02 ─────────────────────────────────────────────────────────────────
    private class C02_UserToUserLowerCaseProfile : MappingProfile
    {
        public C02_UserToUserLowerCaseProfile()
        {
            CreateMap<User, UserLowerCase>();
        }
    }

    [Fact]
    public void Map_CaseInsensitiveNames_MapsCorrectly()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<C02_UserToUserLowerCaseProfile>());
        var mapper = config.CreateMapper();

        var source = new User
        {
            Id = 7,
            FirstName = "Alice",
            LastName = "Jones"
        };

        var result = mapper.Map<User, UserLowerCase>(source);

        result.id.Should().Be(7);
        result.firstName.Should().Be("Alice");
        result.lastName.Should().Be("Jones");
    }

    // ── C-03 ─────────────────────────────────────────────────────────────────
    private class C03_UserToUserWithLongProfile : MappingProfile
    {
        public C03_UserToUserWithLongProfile()
        {
            CreateMap<User, UserWithLong>();
        }
    }

    [Fact]
    public void Map_AssignableTypes_MapsWithImplicitConversion()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<C03_UserToUserWithLongProfile>());
        var mapper = config.CreateMapper();

        var source = new User
        {
            Id = 99,
            FirstName = "Bob"
        };

        var result = mapper.Map<User, UserWithLong>(source);

        result.Id.Should().Be(99L);
        result.FirstName.Should().Be("Bob");
    }

    // ── C-04 ─────────────────────────────────────────────────────────────────
    private class C04_UserToUserSummaryDtoProfile : MappingProfile
    {
        public C04_UserToUserSummaryDtoProfile()
        {
            // No ForMember config — FullName has no matching source property
            CreateMap<User, UserSummaryDto>();
        }
    }

    [Fact]
    public void Map_UnmappedDestinationProperty_RemainsDefault()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<C04_UserToUserSummaryDtoProfile>());
        var mapper = config.CreateMapper();

        var source = new User
        {
            Id = 1,
            FirstName = "Carlos",
            LastName = "Diaz",
            Email = "carlos@example.com"
        };

        var result = mapper.Map<User, UserSummaryDto>(source);

        result.Id.Should().Be(1);
        result.Email.Should().Be("carlos@example.com");
        // FullName has no source match — stays at default empty string
        result.FullName.Should().Be(string.Empty);
    }

    // ── C-05 ─────────────────────────────────────────────────────────────────
    private class C05_UserToUserDtoProfile : MappingProfile
    {
        public C05_UserToUserDtoProfile()
        {
            CreateMap<User, UserDto>();
        }
    }

    [Fact]
    public void Map_NullSourceProperty_MapsNull()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<C05_UserToUserDtoProfile>());
        var mapper = config.CreateMapper();

        var source = new User
        {
            Id = 5,
            FirstName = "Dana",
            LastName = "Lee",
            Email = null!,
            Age = 22
        };

        var result = mapper.Map<User, UserDto>(source);

        result.Id.Should().Be(5);
        result.Email.Should().BeNull();
    }
}
