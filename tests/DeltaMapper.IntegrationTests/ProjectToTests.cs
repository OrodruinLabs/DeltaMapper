using DeltaMapper.EFCore;
using FluentAssertions;
using Xunit;

namespace DeltaMapper.IntegrationTests;

public class ProjectToTests
{
    // ── Test models ──

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public int Age { get; set; }
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public int Age { get; set; }
    }

    public class UserProfile : Profile
    {
        public UserProfile()
        {
            CreateMap<User, UserDto>();
        }
    }

    // ── Tests ──

    [Fact]
    public void ProjectTo_maps_flat_convention_matched_properties()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<UserProfile>());
        var users = new List<User>
        {
            new() { Id = 1, Name = "Alice", Email = "a@test.com", Age = 30 },
            new() { Id = 2, Name = "Bob", Email = "b@test.com", Age = 25 }
        }.AsQueryable();

        var result = users.ProjectTo<User, UserDto>(config).ToList();

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(1);
        result[0].Name.Should().Be("Alice");
        result[0].Email.Should().Be("a@test.com");
        result[0].Age.Should().Be(30);
        result[1].Name.Should().Be("Bob");
    }
}
