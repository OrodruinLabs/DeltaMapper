using DeltaMapper.Abstractions;
using DeltaMapper.Configuration;
using DeltaMapper.Exceptions;
using DeltaMapper.Runtime;
using DeltaMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace DeltaMapper.UnitTests;

public class NonGenericMapTests
{
    // NG-01: Map(object, Type, Type) returns a correctly mapped destination
    [Fact]
    public void Map_ObjectOverload_MapsCorrectly()
    {
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfile<NG01Profile>());
        var mapper = config.CreateMapper();

        var user = new User { Id = 1, FirstName = "Alice", LastName = "Liddell", Email = "alice@test.com", Age = 20 };

        var result = mapper.Map(user, typeof(User), typeof(UserDto));

        var dto = result.Should().BeOfType<UserDto>().Subject;
        dto.Id.Should().Be(1);
        dto.FirstName.Should().Be("Alice");
        dto.LastName.Should().Be("Liddell");
        dto.Email.Should().Be("alice@test.com");
        dto.Age.Should().Be(20);
    }

    private class NG01Profile : MappingProfile
    {
        public NG01Profile()
        {
            CreateMap<User, UserDto>();
        }
    }

    // NG-02: Map(object, Type, Type) throws DeltaMapperException for an unregistered type pair
    [Fact]
    public void Map_ObjectOverload_ThrowsForUnregisteredTypes()
    {
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfile<NG02Profile>());
        var mapper = config.CreateMapper();

        var user = new User { Id = 2, FirstName = "Bob", LastName = "Builder", Email = "bob@test.com", Age = 30 };

        Action act = () => mapper.Map(user, typeof(User), typeof(UserSummaryDto));

        act.Should().Throw<DeltaMapperException>();
    }

    private class NG02Profile : MappingProfile
    {
        public NG02Profile()
        {
            // Intentionally only registers User→UserDto, NOT User→UserSummaryDto
            CreateMap<User, UserDto>();
        }
    }

    // NG-03: Map<TDestination>(object) infers source type at runtime and maps correctly
    [Fact]
    public void Map_InferredSource_MapsCorrectly()
    {
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfile<NG03Profile>());
        var mapper = config.CreateMapper();

        // Source held as object — source type inferred at runtime from instance
        object userAsObject = new User { Id = 3, FirstName = "Carol", LastName = "White", Email = "carol@test.com", Age = 25 };

        var dto = mapper.Map<UserDto>(userAsObject);

        dto.Id.Should().Be(3);
        dto.FirstName.Should().Be("Carol");
        dto.LastName.Should().Be("White");
        dto.Email.Should().Be("carol@test.com");
        dto.Age.Should().Be(25);
    }

    private class NG03Profile : MappingProfile
    {
        public NG03Profile()
        {
            CreateMap<User, UserDto>();
        }
    }
}
