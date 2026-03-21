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

    private class NG01Profile : Profile
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

    private class NG02Profile : Profile
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

    private class NG03Profile : Profile
    {
        public NG03Profile()
        {
            CreateMap<User, UserDto>();
        }
    }

    // NG-04: Map(object, object) maps onto an existing destination instance (both types inferred at runtime)
    [Fact]
    public void Map_ObjectObjectOverload_MapsOntoExistingDestination()
    {
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfile<NG04Profile>());
        var mapper = config.CreateMapper();

        object user = new User { Id = 4, FirstName = "Dave", LastName = "Smith", Email = "dave@test.com", Age = 35 };
        object dest = new UserDto { Id = 99 };

        var result = mapper.Map(user, dest);

        var dto = result.Should().BeOfType<UserDto>().Subject;
        dto.Id.Should().Be(4);
        dto.FirstName.Should().Be("Dave");
        dto.LastName.Should().Be("Smith");
        dto.Email.Should().Be("dave@test.com");
        dto.Age.Should().Be(35);
    }

    // NG-04b: Map(object, object) throws on null source
    [Fact]
    public void Map_ObjectObjectOverload_ThrowsOnNullSource()
    {
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfile<NG04Profile>());
        var mapper = config.CreateMapper();

        object dest = new UserDto();
        Action act = () => mapper.Map(null!, dest);

        act.Should().Throw<ArgumentNullException>();
    }

    // NG-04c: Map(object, object) throws on null destination
    [Fact]
    public void Map_ObjectObjectOverload_ThrowsOnNullDestination()
    {
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfile<NG04Profile>());
        var mapper = config.CreateMapper();

        object src = new User { Id = 5, FirstName = "Eve" };
        Action act = () => mapper.Map(src, (object)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    private class NG04Profile : Profile
    {
        public NG04Profile()
        {
            CreateMap<User, UserDto>();
        }
    }

    // NG-05: Map<TDestination>(object, TDestination) maps source (held as object) onto existing instance
    [Fact]
    public void Map_GenericDestObjectOverload_MapsOntoExistingDestination()
    {
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfile<NG05Profile>());
        var mapper = config.CreateMapper();

        object userAsObject = new User { Id = 6, FirstName = "Frank", LastName = "Ocean", Email = "frank@test.com", Age = 28 };
        var dest = new UserDto { Id = 99 };

        var result = mapper.Map(userAsObject, dest);

        result.Should().BeSameAs(dest);
        result.Id.Should().Be(6);
        result.FirstName.Should().Be("Frank");
        result.LastName.Should().Be("Ocean");
        result.Email.Should().Be("frank@test.com");
        result.Age.Should().Be(28);
    }

    // NG-05b: Map<TDestination>(object, TDestination) throws on null source
    [Fact]
    public void Map_GenericDestObjectOverload_ThrowsOnNullSource()
    {
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfile<NG05Profile>());
        var mapper = config.CreateMapper();

        var dest = new UserDto();
        Action act = () => mapper.Map<UserDto>(null!, dest);

        act.Should().Throw<ArgumentNullException>();
    }

    // NG-05c: Map<TDestination>(object, TDestination) throws on null destination
    [Fact]
    public void Map_GenericDestObjectOverload_ThrowsOnNullDestination()
    {
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfile<NG05Profile>());
        var mapper = config.CreateMapper();

        object src = new User { Id = 7, FirstName = "Grace" };
        Action act = () => mapper.Map<UserDto>(src, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    private class NG05Profile : Profile
    {
        public NG05Profile()
        {
            CreateMap<User, UserDto>();
        }
    }
}
