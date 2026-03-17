using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DeltaMapper.SourceGen.Diagnostics
{
    /// <summary>
    /// Analyzes a [GenerateMap] attribute and reports DM001/DM002 diagnostics
    /// via the supplied <see cref="SourceProductionContext"/>.
    /// </summary>
    internal static class MappingAnalyzer
    {
        // Attribute short names that mark a destination property as ignored
        private const string IgnoreAttributeShortName  = "Ignore";
        private const string IgnoreAttributeAltName    = "DeltaMapperIgnore";

        /// <summary>
        /// Called during the Execute phase.  Reports DM001 for every unmatched,
        /// non-ignored writable destination property.
        /// </summary>
        public static void ReportUnmappedProperties(
            SourceProductionContext context,
            INamedTypeSymbol src,
            INamedTypeSymbol dst,
            Location attributeLocation)
        {
            var srcReadableNames = GetReadablePropertyNames(src);

            foreach (var dstProp in GetWritableProperties(dst))
            {
                if (IsIgnored(dstProp))
                    continue;

                bool hasMatch = srcReadableNames.Any(n =>
                    string.Equals(n, dstProp.Name, System.StringComparison.OrdinalIgnoreCase));

                if (!hasMatch)
                {
                    var diagnostic = Diagnostic.Create(
                        DiagnosticDescriptors.UnmappedDestinationProperty,
                        attributeLocation,
                        dstProp.Name,
                        dst.Name,
                        src.Name);

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        /// <summary>
        /// Reports DM002 when a constructor argument in [GenerateMap] could not be resolved or
        /// resolved to an error type (unresolvable typeof expression).
        /// Returns the resolved (src, dst) pair, or null if either type is invalid.
        /// </summary>
        public static (INamedTypeSymbol Src, INamedTypeSymbol Dst)? ResolveAndValidateTypes(
            SourceProductionContext context,
            AttributeData attr,
            Location attributeLocation)
        {
            if (attr.ConstructorArguments.Length != 2)
                return null;

            var srcType = attr.ConstructorArguments[0].Value as INamedTypeSymbol;
            var dstType = attr.ConstructorArguments[1].Value as INamedTypeSymbol;

            bool srcInvalid = srcType is null || IsErrorType(srcType);
            bool dstInvalid = dstType is null || IsErrorType(dstType);

            if (srcInvalid)
            {
                var diag = Diagnostic.Create(
                    DiagnosticDescriptors.TypeNotFound,
                    attributeLocation,
                    "source (first argument)");
                context.ReportDiagnostic(diag);
            }

            if (dstInvalid)
            {
                var diag = Diagnostic.Create(
                    DiagnosticDescriptors.TypeNotFound,
                    attributeLocation,
                    "destination (second argument)");
                context.ReportDiagnostic(diag);
            }

            if (srcInvalid || dstInvalid)
                return null;

            return (srcType!, dstType!);
        }

        private static bool IsErrorType(INamedTypeSymbol type)
        {
            return type.TypeKind == TypeKind.Error;
        }

        // ── helpers ──────────────────────────────────────────────────────────

        private static List<IPropertySymbol> GetWritableProperties(INamedTypeSymbol type)
        {
            return type.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => !p.IsStatic && !p.IsIndexer && p.SetMethod is not null)
                .ToList();
        }

        private static HashSet<string> GetReadablePropertyNames(INamedTypeSymbol type)
        {
            var result = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var p in type.GetMembers().OfType<IPropertySymbol>())
            {
                if (!p.IsStatic && !p.IsIndexer && p.GetMethod is not null)
                    result.Add(p.Name);
            }
            return result;
        }

        private static bool IsIgnored(IPropertySymbol prop)
        {
            return prop.GetAttributes().Any(a =>
            {
                var name     = a.AttributeClass?.Name ?? string.Empty;
                var fullName = a.AttributeClass?.ToDisplayString() ?? string.Empty;

                return name     == IgnoreAttributeShortName                         ||
                       name     == IgnoreAttributeAltName                           ||
                       name     == IgnoreAttributeShortName + "Attribute"           ||
                       name     == IgnoreAttributeAltName   + "Attribute"           ||
                       fullName == "DeltaMapper.Ignore"                             ||
                       fullName == "DeltaMapper.DeltaMapperIgnore"                  ||
                       fullName == "DeltaMapper.IgnoreAttribute"                    ||
                       fullName == "DeltaMapper.DeltaMapperIgnoreAttribute";
            });
        }
    }
}
