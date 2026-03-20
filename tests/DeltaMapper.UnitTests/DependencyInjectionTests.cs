using DeltaMapper.Abstractions;
using DeltaMapper.Configuration;
using DeltaMapper.Extensions;
using DeltaMapper.Runtime;
using DeltaMapper.UnitTests.TestModels;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DeltaMapper.UnitTests;

public class DependencyInjectionTests
{
    [Fact]
    public void DI01_AddDeltaMapper_RegistersIMapper()
    {
        var services = new ServiceCollection();
        services.AddDeltaMapper(cfg => cfg.AddProfile<SimpleUserProfile>());
        var provider = services.BuildServiceProvider();

        var mapper = provider.GetService<IMapper>();
        mapper.Should().NotBeNull();
    }

    [Fact]
    public void DI02_AddDeltaMapper_RegistersMapperConfiguration()
    {
        var services = new ServiceCollection();
        services.AddDeltaMapper(cfg => cfg.AddProfile<SimpleUserProfile>());
        var provider = services.BuildServiceProvider();

        var config = provider.GetService<MapperConfiguration>();
        config.Should().NotBeNull();
    }

    [Fact]
    public void DI03_AddDeltaMapper_MapperIsSingleton()
    {
        var services = new ServiceCollection();
        services.AddDeltaMapper(cfg => cfg.AddProfile<SimpleUserProfile>());
        var provider = services.BuildServiceProvider();

        var mapper1 = provider.GetRequiredService<IMapper>();
        var mapper2 = provider.GetRequiredService<IMapper>();
        mapper1.Should().BeSameAs(mapper2);
    }

    [Fact]
    public void DI04_AddDeltaMapper_MappingWorks()
    {
        var services = new ServiceCollection();
        services.AddDeltaMapper(cfg => cfg.AddProfile<SimpleUserProfile>());
        var provider = services.BuildServiceProvider();

        var mapper = provider.GetRequiredService<IMapper>();
        var user = new User { Id = 1, FirstName = "Jane", LastName = "Doe", Email = "jane@test.com", Age = 25 };
        var dto = mapper.Map<User, UserDto>(user);

        dto.Id.Should().Be(1);
        dto.FirstName.Should().Be("Jane");
    }

    [Fact]
    public void DI05_AddDeltaMapper_MultipleProfiles()
    {
        var services = new ServiceCollection();
        services.AddDeltaMapper(cfg =>
        {
            cfg.AddProfile<SimpleUserProfile>();
            cfg.AddProfile<SimpleAddressProfile>();
        });
        var provider = services.BuildServiceProvider();

        var mapper = provider.GetRequiredService<IMapper>();

        var user = new User { Id = 1, FirstName = "John" };
        mapper.Map<User, UserDto>(user).Id.Should().Be(1);

        var addr = new Address { Street = "123 Main", City = "Springfield", Zip = "12345" };
        mapper.Map<Address, AddressDto>(addr).City.Should().Be("Springfield");
    }

    private class SimpleUserProfile : Profile
    {
        public SimpleUserProfile() { CreateMap<User, UserDto>(); }
    }

    private class SimpleAddressProfile : Profile
    {
        public SimpleAddressProfile() { CreateMap<Address, AddressDto>(); }
    }
}
