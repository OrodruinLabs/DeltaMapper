using DeltaMapper.Abstractions;
using DeltaMapper.Configuration;
using DeltaMapper.Exceptions;
using DeltaMapper.Runtime;
using DeltaMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace DeltaMapper.UnitTests;

public class UserMappingProfile : MappingProfile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserDto>();
    }
}

public class UserSummaryMappingProfile : MappingProfile
{
    public UserSummaryMappingProfile()
    {
        CreateMap<User, UserSummaryDto>()
            .ForMember(d => d.FullName, o => o.MapFrom(s => $"{s.FirstName} {s.LastName}"));
    }
}

public class DuplicateUserProfile : MappingProfile
{
    public DuplicateUserProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(d => d.FirstName, o => o.MapFrom(s => s.FirstName.ToUpperInvariant()));
    }
}

public class MapperConfigurationTests
{
    [Fact]
    public void MC01_Create_WithProfile_CompilesMappings()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<UserMappingProfile>());
        var mapper = config.CreateMapper();

        var user = new User { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@test.com", Age = 30 };
        var dto = mapper.Map<User, UserDto>(user);

        dto.Id.Should().Be(1);
        dto.FirstName.Should().Be("John");
        dto.LastName.Should().Be("Doe");
        dto.Email.Should().Be("john@test.com");
        dto.Age.Should().Be(30);
    }

    [Fact]
    public void MC02_Create_WithMultipleProfiles_AllRegistered()
    {
        var config = MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile<UserMappingProfile>();
            cfg.AddProfile<UserSummaryMappingProfile>();
        });
        var mapper = config.CreateMapper();

        var user = new User { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@test.com", Age = 30 };

        var dto = mapper.Map<User, UserDto>(user);
        dto.Id.Should().Be(1);

        var summary = mapper.Map<User, UserSummaryDto>(user);
        summary.FullName.Should().Be("John Doe");
    }

    [Fact]
    public void MC03_Create_DuplicateMapping_LastWins()
    {
        // Two profiles both register User -> UserDto, the second one should win
        var config = MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile<UserMappingProfile>();
            cfg.AddProfile(new DuplicateUserProfile());
        });

        // Last registration wins — DuplicateUserProfile uppercases FirstName
        var mapper = config.CreateMapper();
        var user = new User { Id = 1, FirstName = "John", LastName = "Doe", Email = "test@test.com", Age = 30 };
        var dto = mapper.Map<User, UserDto>(user);
        dto.FirstName.Should().Be("JOHN", "the second profile (which uppercases) should win");
    }

    [Fact]
    public void MC04_CreateMapper_ReturnsWorkingMapper()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<UserMappingProfile>());
        var mapper = config.CreateMapper();

        mapper.Should().NotBeNull();
        mapper.Should().BeAssignableTo<IMapper>();
    }

    [Fact]
    public void MC05_Create_UsesFrozenDictionary_Internally()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<UserMappingProfile>());

        // Use reflection to verify FrozenDictionary is used
        var registryField = typeof(MapperConfiguration)
            .GetField("_registry", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        registryField.Should().NotBeNull();

        var registryValue = registryField!.GetValue(config);
        registryValue.Should().NotBeNull();
        registryValue!.GetType().Name.Should().Contain("FrozenDictionary");
    }

    [Fact]
    public void Build_WithNoProfiles_ThrowsDeltaMapperException()
    {
        var act = () => MapperConfiguration.Create(cfg => { });

        act.Should().Throw<DeltaMapperException>()
            .WithMessage("*no type maps*");
    }

    [Fact]
    public void Build_WithProfileButNoMaps_ThrowsDeltaMapperException()
    {
        var act = () => MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile(new EmptyTestProfile());
        });

        act.Should().Throw<DeltaMapperException>()
            .WithMessage("*no type maps*");
    }
}

file sealed class EmptyTestProfile : MappingProfile
{
    public EmptyTestProfile() { }
}
