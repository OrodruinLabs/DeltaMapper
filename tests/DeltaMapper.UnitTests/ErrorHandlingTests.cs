using System;
using DeltaMapper.Exceptions;
using FluentAssertions;
using Xunit;

namespace DeltaMapper.UnitTests;

public class ErrorHandlingTests
{
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
