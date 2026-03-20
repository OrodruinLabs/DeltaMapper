using DeltaMapper.Abstractions;
using DeltaMapper.Configuration;
using DeltaMapper.Runtime;
using DeltaMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace DeltaMapper.UnitTests;

// Test profile
public class TestUserProfile : Profile
{
    public TestUserProfile()
    {
        CreateMap<User, UserDto>();
    }
}

public class TestUserSummaryProfile : Profile
{
    public TestUserSummaryProfile()
    {
        CreateMap<User, UserSummaryDto>()
            .ForMember(d => d.FullName, o => o.MapFrom(s => $"{s.FirstName} {s.LastName}"))
            .ForMember(d => d.Email, o => o.NullSubstitute("unknown@example.com"))
            .BeforeMap((src, dst) => { })
            .AfterMap((src, dst) => { })
            .ReverseMap();
    }
}

public class ProfileTests
{
    [Fact]
    public void CreateMap_RegistersTypeMapConfiguration()
    {
        var profile = new TestUserProfile();
        profile.TypeMaps.Should().HaveCount(1);
        profile.TypeMaps[0].SourceType.Should().Be(typeof(User));
        profile.TypeMaps[0].DestinationType.Should().Be(typeof(UserDto));
    }

    [Fact]
    public void ForMember_StoresMemberConfiguration()
    {
        var profile = new TestUserSummaryProfile();
        var typeMap = profile.TypeMaps[0];

        typeMap.MemberConfigurations.Should().HaveCount(2);
        typeMap.MemberConfigurations[0].DestinationMemberName.Should().Be("FullName");
        typeMap.MemberConfigurations[0].CustomResolver.Should().NotBeNull();
        typeMap.MemberConfigurations[1].DestinationMemberName.Should().Be("Email");
        typeMap.MemberConfigurations[1].HasNullSubstitute.Should().BeTrue();
    }

    [Fact]
    public void ForMember_Ignore_SetsIsIgnored()
    {
        Profile profile = new IgnoreTestProfile();
        var typeMap = profile.TypeMaps[0];

        typeMap.MemberConfigurations.Should().HaveCount(1);
        typeMap.MemberConfigurations[0].IsIgnored.Should().BeTrue();
    }

    [Fact]
    public void ReverseMap_SetsHasReverseMap()
    {
        var profile = new TestUserSummaryProfile();
        profile.TypeMaps[0].HasReverseMap.Should().BeTrue();
    }

    [Fact]
    public void BeforeMap_StoresAction()
    {
        var profile = new TestUserSummaryProfile();
        profile.TypeMaps[0].BeforeMapAction.Should().NotBeNull();
    }

    [Fact]
    public void AfterMap_StoresAction()
    {
        var profile = new TestUserSummaryProfile();
        profile.TypeMaps[0].AfterMapAction.Should().NotBeNull();
    }

    [Fact]
    public void MapFrom_CustomResolver_ProducesCorrectValue()
    {
        var profile = new TestUserSummaryProfile();
        var resolver = profile.TypeMaps[0].MemberConfigurations[0].CustomResolver;

        var user = new User { FirstName = "John", LastName = "Doe" };
        var result = resolver!(user);
        result.Should().Be("John Doe");
    }
}

public class IgnoreTestProfile : Profile
{
    public IgnoreTestProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(d => d.Age, o => o.Ignore());
    }
}
