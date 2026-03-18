using DeltaMapper.Abstractions;
using DeltaMapper.Configuration;
using DeltaMapper.Runtime;
using DeltaMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace DeltaMapper.UnitTests;

public class ReverseMapTests
{
    // R-01: ReverseMap registers the inverse mapping so Map<UserDto, User> works
    [Fact]
    public void ReverseMap_RegistersInverseMapping()
    {
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfile<R01Profile>());
        var mapper = config.CreateMapper();

        var dto = new UserDto { Id = 1, FirstName = "Alice", LastName = "Walker", Email = "alice@test.com", Age = 32 };

        var act = () => mapper.Map<UserDto, User>(dto);
        act.Should().NotThrow();

        var user = mapper.Map<UserDto, User>(dto);
        user.Should().NotBeNull();
    }

    private class R01Profile : MappingProfile
    {
        public R01Profile()
        {
            CreateMap<User, UserDto>().ReverseMap();
        }
    }

    // R-02: Convention properties map correctly in both forward and reverse directions
    [Fact]
    public void ReverseMap_ConventionPropertiesMapBothDirections()
    {
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfile<R02Profile>());
        var mapper = config.CreateMapper();

        // Forward: User -> UserDto
        var user = new User { Id = 2, FirstName = "Bob", LastName = "Ross", Email = "bob@test.com", Age = 55 };
        var dto = mapper.Map<User, UserDto>(user);

        dto.Id.Should().Be(2);
        dto.FirstName.Should().Be("Bob");
        dto.LastName.Should().Be("Ross");
        dto.Email.Should().Be("bob@test.com");
        dto.Age.Should().Be(55);

        // Reverse: UserDto -> User
        var roundTripped = mapper.Map<UserDto, User>(dto);

        roundTripped.Id.Should().Be(2);
        roundTripped.FirstName.Should().Be("Bob");
        roundTripped.LastName.Should().Be("Ross");
        roundTripped.Email.Should().Be("bob@test.com");
        roundTripped.Age.Should().Be(55);
    }

    private class R02Profile : MappingProfile
    {
        public R02Profile()
        {
            CreateMap<User, UserDto>().ReverseMap();
        }
    }

    // R-03: A custom ForMember resolver on the forward map is NOT applied in reverse
    [Fact]
    public void ReverseMap_CustomResolverNotAppliedInReverse()
    {
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfile<R03Profile>());
        var mapper = config.CreateMapper();

        // Forward: User -> UserSummaryDto — FullName is set via custom resolver
        var user = new User { Id = 3, FirstName = "Clara", LastName = "Oswald", Email = "clara@test.com", Age = 26 };
        var summary = mapper.Map<User, UserSummaryDto>(user);
        summary.FullName.Should().Be("Clara Oswald");

        // Reverse: UserSummaryDto -> User — FullName has no match in User, so User.FirstName and User.LastName
        // remain at default since convention finds no "FullName" property on User.
        var roundTripped = mapper.Map<UserSummaryDto, User>(summary);

        // Convention only — User has no "FullName" property so it stays default
        roundTripped.FirstName.Should().Be(string.Empty);
        roundTripped.LastName.Should().Be(string.Empty);

        // Properties that exist in both (Id, Email) are still mapped by convention
        roundTripped.Id.Should().Be(3);
        roundTripped.Email.Should().Be("clara@test.com");
    }

    private class R03Profile : MappingProfile
    {
        public R03Profile()
        {
            CreateMap<User, UserSummaryDto>()
                .ForMember(d => d.FullName, o => o.MapFrom(s => $"{s.FirstName} {s.LastName}"))
                .ReverseMap();
        }
    }
}
