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
    }
}
