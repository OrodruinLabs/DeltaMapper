using DeltaMapper.Abstractions;
using DeltaMapper.Configuration;
using DeltaMapper.Runtime;
using DeltaMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace DeltaMapper.UnitTests;

public class MappingHooksTests
{
    // H-01: BeforeMap executes before property assignment and its mutations are visible
    [Fact]
    public void BeforeMap_ExecutesBeforePropertyAssignment()
    {
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfile<H01Profile>());
        var mapper = config.CreateMapper();

        // The profile's BeforeMap will uppercase FirstName on the source before properties are copied
        var user = new User { Id = 1, FirstName = "john", LastName = "Doe", Email = "john@test.com", Age = 25 };
        var dto = mapper.Map<User, UserDto>(user);

        // Source was mutated in BeforeMap, so the mapped value should be upper-cased
        dto.FirstName.Should().Be("JOHN");
    }

    private class H01Profile : MappingProfile
    {
        public H01Profile()
        {
            CreateMap<User, UserDto>()
                .BeforeMap((src, dst) => src.FirstName = src.FirstName.ToUpperInvariant());
        }
    }

    // H-02: AfterMap executes after property assignment and can modify the destination
    [Fact]
    public void AfterMap_ExecutesAfterPropertyAssignment()
    {
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfile<H02Profile>());
        var mapper = config.CreateMapper();

        var user = new User { Id = 2, FirstName = "Jane", LastName = "Doe", Email = "jane@test.com", Age = 28 };
        var dto = mapper.Map<User, UserDto>(user);

        // AfterMap adds " (mapped)" suffix to LastName
        dto.LastName.Should().Be("Doe (mapped)");
    }

    private class H02Profile : MappingProfile
    {
        public H02Profile()
        {
            CreateMap<User, UserDto>()
                .AfterMap((src, dst) => dst.LastName = dst.LastName + " (mapped)");
        }
    }

    // H-03: BeforeMap and AfterMap both execute, in the correct order
    [Fact]
    public void BeforeMap_AndAfterMap_BothExecute_InOrder()
    {
        var executionOrder = new List<string>();

        var profile = new H03Profile(executionOrder);
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile(profile));
        var mapper = config.CreateMapper();

        var user = new User { Id = 3, FirstName = "Frank", LastName = "Castle", Email = "frank@test.com", Age = 40 };
        mapper.Map<User, UserDto>(user);

        executionOrder.Should().HaveCount(2);
        executionOrder[0].Should().Be("before");
        executionOrder[1].Should().Be("after");
    }

    private class H03Profile : MappingProfile
    {
        public H03Profile(List<string> log)
        {
            CreateMap<User, UserDto>()
                .BeforeMap((src, dst) => log.Add("before"))
                .AfterMap((src, dst) => log.Add("after"));
        }
    }

    // H-04: BeforeMap receives both source and destination with correct types/values
    [Fact]
    public void BeforeMap_ReceivesBothSourceAndDestination()
    {
        User? capturedSrc = null;
        UserDto? capturedDst = null;

        var profile = new H04Profile((src, dst) =>
        {
            capturedSrc = src;
            capturedDst = dst;
        });

        var config = MapperConfiguration.Create(cfg => cfg.AddProfile(profile));
        var mapper = config.CreateMapper();

        var user = new User { Id = 4, FirstName = "Gwen", LastName = "Stacy", Email = "gwen@test.com", Age = 21 };
        mapper.Map<User, UserDto>(user);

        capturedSrc.Should().NotBeNull();
        capturedSrc.Should().BeOfType<User>();
        capturedSrc!.Id.Should().Be(4);
        capturedSrc.FirstName.Should().Be("Gwen");

        capturedDst.Should().NotBeNull();
        capturedDst.Should().BeOfType<UserDto>();
    }

    private class H04Profile : MappingProfile
    {
        public H04Profile(Action<User, UserDto> hook)
        {
            CreateMap<User, UserDto>()
                .BeforeMap(hook);
        }
    }
}
