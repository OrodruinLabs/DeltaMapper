using DeltaMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace DeltaMapper.UnitTests;

public class ExistingDestinationTests
{
    // ED-01: Map onto existing destination updates all mapped properties
    [Fact]
    public void Map_WithExistingDestination_UpdatesProperties()
    {
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfile<ED01Profile>());
        var mapper = config.CreateMapper();

        var source = new User { Id = 10, FirstName = "Diana", LastName = "Prince", Email = "diana@test.com", Age = 35 };
        var existingDst = new UserDto { Id = 99, FirstName = "Old", LastName = "Values", Email = "old@test.com", Age = 0 };

        mapper.Map(source, existingDst);

        existingDst.Id.Should().Be(10);
        existingDst.FirstName.Should().Be("Diana");
        existingDst.LastName.Should().Be("Prince");
        existingDst.Email.Should().Be("diana@test.com");
        existingDst.Age.Should().Be(35);
    }

    private class ED01Profile : Profile
    {
        public ED01Profile()
        {
            CreateMap<User, UserDto>();
        }
    }

    // ED-02: Unmapped properties (no ForMember, no convention match) are preserved on the existing destination
    [Fact]
    public void Map_WithExistingDestination_PreservesUnmappedProperties()
    {
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfile<ED02Profile>());
        var mapper = config.CreateMapper();

        var source = new User { Id = 11, FirstName = "Bruce", LastName = "Wayne", Email = "bruce@test.com", Age = 40 };
        // FullName has no matching source property and no ForMember — should remain unchanged
        var existingDst = new UserSummaryDto { Id = 0, FullName = "Preserved Value", Email = "old@test.com" };

        mapper.Map(source, existingDst);

        // Convention-mapped properties should be updated
        existingDst.Id.Should().Be(11);
        existingDst.Email.Should().Be("bruce@test.com");
        // FullName has no source — existing value must be preserved
        existingDst.FullName.Should().Be("Preserved Value");
    }

    private class ED02Profile : Profile
    {
        public ED02Profile()
        {
            // No ForMember for FullName — convention mapping will not find a match
            CreateMap<User, UserSummaryDto>();
        }
    }

    // ED-03: ForMember override applies correctly when mapping onto an existing destination
    [Fact]
    public void Map_WithExistingDestination_AppliesForMemberOverrides()
    {
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfile<ED03Profile>());
        var mapper = config.CreateMapper();

        var source = new User { Id = 12, FirstName = "Clark", LastName = "Kent", Email = "clark@test.com", Age = 33 };
        var existingDst = new UserSummaryDto { Id = 0, FullName = "Old Name", Email = "old@test.com" };

        mapper.Map(source, existingDst);

        existingDst.Id.Should().Be(12);
        existingDst.Email.Should().Be("clark@test.com");
        existingDst.FullName.Should().Be("Clark Kent");
    }

    private class ED03Profile : Profile
    {
        public ED03Profile()
        {
            CreateMap<User, UserSummaryDto>()
                .ForMember(d => d.FullName, o => o.MapFrom(s => $"{s.FirstName} {s.LastName}"));
        }
    }
}
