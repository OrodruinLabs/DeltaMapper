using Microsoft.CodeAnalysis;

namespace DeltaMapper.SourceGen.Diagnostics
{
    internal static class DiagnosticDescriptors
    {
        // DM001 — Unmapped Destination Property (Warning)
        public static readonly DiagnosticDescriptor UnmappedDestinationProperty = new DiagnosticDescriptor(
            id: "DM001",
            title: "Unmapped destination property",
            messageFormat: "Destination property '{0}' on '{1}' has no matching source property on '{2}'. Add a manual mapping or mark it with [Ignore].",
            category: "DeltaMapper",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "A writable destination property has no matching readable source property by name. " +
                         "The property will not be populated by the generated mapping.");

        // DM002 — Type Not Found (Error)
        public static readonly DiagnosticDescriptor TypeNotFound = new DiagnosticDescriptor(
            id: "DM002",
            title: "Type not found",
            messageFormat: "Type argument {0} in [GenerateMap] could not be resolved to a named type. Verify the type exists and is accessible.",
            category: "DeltaMapper",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "A typeof() expression in [GenerateMap] could not be resolved to an INamedTypeSymbol. " +
                         "No mapping code will be generated for this attribute.");

        // DM003 — IgnoreMember Property Not Found (Warning)
        public static readonly DiagnosticDescriptor IgnoreMemberPropertyNotFound = new DiagnosticDescriptor(
            id: "DM003",
            title: "IgnoreMember property not found",
            messageFormat: "[IgnoreMember] references property '{0}' which does not exist on destination type '{1}'",
            category: "DeltaMapper",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "A property name supplied to [IgnoreMember] could not be matched to any writable property on " +
                         "the destination type. The attribute has no effect and is likely a typo.");

        // DM004 — MapMember Type Incompatible (Warning)
        public static readonly DiagnosticDescriptor MapMemberTypeIncompatible = new DiagnosticDescriptor(
            id: "DM004",
            title: "MapMember type incompatible",
            messageFormat: "[MapMember] source property '{0}' on '{1}' has type '{2}' which is not compatible with destination property '{3}' on '{4}' (type '{5}')",
            category: "DeltaMapper",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "The source and destination properties named in [MapMember] have types that cannot be assigned " +
                         "to each other without an explicit conversion. The generated mapping may not compile.");
    }
}
