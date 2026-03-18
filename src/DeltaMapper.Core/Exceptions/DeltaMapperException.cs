using System;

namespace DeltaMapper.Exceptions;

/// <summary>
/// Exception thrown when a mapping operation fails.
/// Always includes source type, destination type, and a resolution hint.
/// </summary>
public sealed class DeltaMapperException : Exception
{
    /// <summary>
    /// Creates a new DeltaMapperException with the specified message.
    /// </summary>
    public DeltaMapperException(string message) : base(message) { }

    /// <summary>
    /// Creates a new DeltaMapperException with the specified message and inner exception.
    /// </summary>
    public DeltaMapperException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    /// Creates an exception for when no mapping is registered between source and destination types.
    /// </summary>
    public static DeltaMapperException ForMissingMapping(Type sourceType, Type destinationType)
        => new($"No mapping registered from '{sourceType.Name}' to '{destinationType.Name}'. " +
               $"Register a mapping in a MappingProfile using CreateMap<{sourceType.Name}, {destinationType.Name}>().");
}
