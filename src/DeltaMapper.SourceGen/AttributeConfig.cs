using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace DeltaMapper.SourceGen
{
    /// <summary>
    /// Aggregates all companion attribute configs parsed from a profile class.
    /// </summary>
    internal sealed class AttributeConfig
    {
        public List<IgnoreMemberConfig> Ignores { get; } = new List<IgnoreMemberConfig>();
        public List<NullSubstituteConfig> NullSubstitutes { get; } = new List<NullSubstituteConfig>();
        public List<MapMemberConfig> MapMembers { get; } = new List<MapMemberConfig>();
    }

    /// <summary>
    /// Parsed data from a single <c>[IgnoreMember(sourceType, destinationType, memberName)]</c> attribute.
    /// </summary>
    internal sealed class IgnoreMemberConfig
    {
        public INamedTypeSymbol SourceType { get; }
        public INamedTypeSymbol DestinationType { get; }
        public string MemberName { get; }
        public Location Location { get; }

        public IgnoreMemberConfig(
            INamedTypeSymbol sourceType,
            INamedTypeSymbol destinationType,
            string memberName,
            Location location)
        {
            SourceType = sourceType;
            DestinationType = destinationType;
            MemberName = memberName;
            Location = location;
        }
    }

    /// <summary>
    /// Parsed data from a single <c>[NullSubstitute(sourceType, destinationType, memberName, value)]</c> attribute.
    /// </summary>
    internal sealed class NullSubstituteConfig
    {
        public INamedTypeSymbol SourceType { get; }
        public INamedTypeSymbol DestinationType { get; }
        public string MemberName { get; }
        public object? Value { get; }
        public Location Location { get; }

        public NullSubstituteConfig(
            INamedTypeSymbol sourceType,
            INamedTypeSymbol destinationType,
            string memberName,
            object? value,
            Location location)
        {
            SourceType = sourceType;
            DestinationType = destinationType;
            MemberName = memberName;
            Value = value;
            Location = location;
        }
    }

    /// <summary>
    /// Parsed data from a single <c>[MapMember(sourceType, destinationType, destinationMember, sourceMember)]</c> attribute.
    /// </summary>
    internal sealed class MapMemberConfig
    {
        public INamedTypeSymbol SourceType { get; }
        public INamedTypeSymbol DestinationType { get; }
        public string DestinationMember { get; }
        public string SourceMember { get; }
        public Location Location { get; }

        public MapMemberConfig(
            INamedTypeSymbol sourceType,
            INamedTypeSymbol destinationType,
            string destinationMember,
            string sourceMember,
            Location location)
        {
            SourceType = sourceType;
            DestinationType = destinationType;
            DestinationMember = destinationMember;
            SourceMember = sourceMember;
            Location = location;
        }
    }
}
