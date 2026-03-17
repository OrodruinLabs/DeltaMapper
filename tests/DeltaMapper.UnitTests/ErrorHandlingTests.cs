using System;
using DeltaMapper.Abstractions;
using DeltaMapper.Configuration;
using DeltaMapper.Exceptions;
using DeltaMapper.Runtime;
using DeltaMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace DeltaMapper.UnitTests;

public class ErrorHandlingTests
{
    // E-01: Mapping an unregistered type pair throws DeltaMapperException
    [Fact]
    public void Map_NoMappingRegistered_ThrowsDeltaMapperException()
    {
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfile<E01Profile>());
        var mapper = config.CreateMapper();

        var user = new User { Id = 1, FirstName = "Test", LastName = "User", Email = "test@test.com", Age = 25 };

        Action act = () => mapper.Map<User, UserSummaryDto>(user);

        act.Should().Throw<DeltaMapperException>();
    }

    private class E01Profile : MappingProfile
    {
        public E01Profile()
        {
            // Only registers User→UserDto, NOT User→UserSummaryDto
            CreateMap<User, UserDto>();
        }
    }

    // E-02: Exception message contains both type names
    [Fact]
    public void Map_NoMappingRegistered_ExceptionContainsTypeNames()
    {
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfile<E02Profile>());
        var mapper = config.CreateMapper();

        var user = new User { Id = 2, FirstName = "Test", LastName = "User", Email = "test@test.com", Age = 25 };

        var exception = Assert.Throws<DeltaMapperException>(() => mapper.Map<User, UserSummaryDto>(user));

        exception.Message.Should().Contain(nameof(User));
        exception.Message.Should().Contain(nameof(UserSummaryDto));
    }

    private class E02Profile : MappingProfile
    {
        public E02Profile()
        {
            CreateMap<User, UserDto>();
        }
    }

    // E-03: Exception message contains the resolution hint
    [Fact]
    public void Map_NoMappingRegistered_ExceptionContainsResolutionHint()
    {
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfile<E03Profile>());
        var mapper = config.CreateMapper();

        var user = new User { Id = 3, FirstName = "Test", LastName = "User", Email = "test@test.com", Age = 25 };

        var exception = Assert.Throws<DeltaMapperException>(() => mapper.Map<User, UserSummaryDto>(user));

        exception.Message.Should().Contain("Register a mapping");
    }

    private class E03Profile : MappingProfile
    {
        public E03Profile()
        {
            CreateMap<User, UserDto>();
        }
    }

    // E-04: Passing null source throws ArgumentNullException
    [Fact]
    public void Map_NullSource_ThrowsArgumentNullException()
    {
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfile<E04Profile>());
        var mapper = config.CreateMapper();

        Action act = () => mapper.Map<User, UserDto>(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    private class E04Profile : MappingProfile
    {
        public E04Profile()
        {
            CreateMap<User, UserDto>();
        }
    }


    // E-05
    [Fact]
    public void DeltaMapperException_HasCorrectMessage()
    {
        const string expectedMessage = "Something went wrong during mapping.";

        var exception = new DeltaMapperException(expectedMessage);

        exception.Message.Should().Be(expectedMessage);
    }

    [Fact]
    public void ForMissingMapping_ContainsSourceTypeName()
    {
        var exception = DeltaMapperException.ForMissingMapping(typeof(string), typeof(int));

        exception.Message.Should().Contain(nameof(String));
    }

    [Fact]
    public void ForMissingMapping_ContainsDestinationTypeName()
    {
        var exception = DeltaMapperException.ForMissingMapping(typeof(string), typeof(int));

        exception.Message.Should().Contain(nameof(Int32));
    }

    [Fact]
    public void ForMissingMapping_ContainsResolutionHint()
    {
        var exception = DeltaMapperException.ForMissingMapping(typeof(string), typeof(int));

        exception.Message.Should().Contain("Register a mapping");
    }

    [Fact]
    public void ForMissingMapping_InnerExceptionPreserved()
    {
        var inner = new InvalidOperationException("inner cause");
        const string message = "Outer message.";

        var exception = new DeltaMapperException(message, inner);

        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeSameAs(inner);
    }
}
