using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace DeltaMapper.SourceGen.Diagnostics
{
    /// <summary>
    /// Analyzes a [GenerateMap] attribute and reports DM001/DM002/DM003/DM004 diagnostics
    /// via the supplied <see cref="SourceProductionContext"/>.
    /// </summary>
    internal static class MappingAnalyzer
    {
        // Attribute short names that mark a destination property as ignored
        private const string IgnoreAttributeShortName = "Ignore";
        private const string IgnoreAttributeAltName = "DeltaMapperIgnore";

        /// <summary>
        /// Called during the Execute phase.  Reports DM001 for every unmatched,
        /// non-ignored writable destination property.
        /// A property is considered matched when a readable source property has the same
        /// name (case-insensitive) AND a compatible (symbol-equal) type.
        /// Properties covered by <see cref="AttributeConfig"/> (via [IgnoreMember] or [MapMember])
        /// are suppressed from DM001.
        /// </summary>
        public static void ReportUnmappedProperties(
            SourceProductionContext context,
            INamedTypeSymbol src,
            INamedTypeSymbol dst,
            Location attributeLocation,
            AttributeConfig config)
        {
            var srcReadableProps = GetReadableProperties(src);

            // Build suppressed set: properties covered by [IgnoreMember] or [MapMember]
            var suppressedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var ignore in config.Ignores)
            {
                if (SymbolEqualityComparer.Default.Equals(ignore.SourceType, src) &&
                    SymbolEqualityComparer.Default.Equals(ignore.DestinationType, dst))
                {
                    suppressedNames.Add(ignore.MemberName);
                }
            }

            foreach (var mapMember in config.MapMembers)
            {
                if (SymbolEqualityComparer.Default.Equals(mapMember.SourceType, src) &&
                    SymbolEqualityComparer.Default.Equals(mapMember.DestinationType, dst))
                {
                    suppressedNames.Add(mapMember.DestinationMember);
                }
            }

            foreach (var dstProp in GetWritableProperties(dst))
            {
                if (IsIgnored(dstProp))
                    continue;

                if (suppressedNames.Contains(dstProp.Name))
                    continue;

                bool hasMatch = srcReadableProps.Any(srcProp =>
                    string.Equals(srcProp.Name, dstProp.Name, StringComparison.OrdinalIgnoreCase) &&
                    SymbolEqualityComparer.Default.Equals(srcProp.Type, dstProp.Type));

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
        /// Reports DM003 for each [IgnoreMember] or [NullSubstitute] entry whose memberName
        /// does not exist as a writable property on the destination type.
        /// </summary>
        public static void ReportInvalidIgnoreMembers(
            SourceProductionContext context,
            INamedTypeSymbol src,
            INamedTypeSymbol dst,
            AttributeConfig config)
        {
            var dstPropNames = new HashSet<string>(
                GetWritableProperties(dst).Select(p => p.Name),
                StringComparer.OrdinalIgnoreCase);

            foreach (var ignore in config.Ignores)
            {
                if (!SymbolEqualityComparer.Default.Equals(ignore.SourceType, src) ||
                    !SymbolEqualityComparer.Default.Equals(ignore.DestinationType, dst))
                {
                    continue;
                }

                if (!dstPropNames.Contains(ignore.MemberName))
                {
                    var diagnostic = Diagnostic.Create(
                        DiagnosticDescriptors.IgnoreMemberPropertyNotFound,
                        ignore.Location,
                        ignore.MemberName,
                        dst.Name);

                    context.ReportDiagnostic(diagnostic);
                }
            }

            foreach (var nullSub in config.NullSubstitutes)
            {
                if (!SymbolEqualityComparer.Default.Equals(nullSub.SourceType, src) ||
                    !SymbolEqualityComparer.Default.Equals(nullSub.DestinationType, dst))
                {
                    continue;
                }

                if (!dstPropNames.Contains(nullSub.MemberName))
                {
                    var diagnostic = Diagnostic.Create(
                        DiagnosticDescriptors.IgnoreMemberPropertyNotFound,
                        nullSub.Location,
                        nullSub.MemberName,
                        dst.Name);

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        /// <summary>
        /// Reports DM004 for each [MapMember] entry where the source property type is not
        /// symbol-equal to the destination property type.
        /// </summary>
        public static void ReportIncompatibleMapMembers(
            SourceProductionContext context,
            INamedTypeSymbol src,
            INamedTypeSymbol dst,
            AttributeConfig config)
        {
            var srcProps = GetReadableProperties(src);
            var dstProps = GetWritableProperties(dst);

            foreach (var mapMember in config.MapMembers)
            {
                if (!SymbolEqualityComparer.Default.Equals(mapMember.SourceType, src) ||
                    !SymbolEqualityComparer.Default.Equals(mapMember.DestinationType, dst))
                {
                    continue;
                }

                var srcProp = srcProps.FirstOrDefault(p =>
                    string.Equals(p.Name, mapMember.SourceMember, StringComparison.OrdinalIgnoreCase));

                var dstProp = dstProps.FirstOrDefault(p =>
                    string.Equals(p.Name, mapMember.DestinationMember, StringComparison.OrdinalIgnoreCase));

                // Only check type compatibility when both properties actually exist
                if (srcProp is null || dstProp is null)
                    continue;

                if (!SymbolEqualityComparer.Default.Equals(srcProp.Type, dstProp.Type))
                {
                    var diagnostic = Diagnostic.Create(
                        DiagnosticDescriptors.MapMemberTypeIncompatible,
                        mapMember.Location,
                        srcProp.Name,
                        src.Name,
                        srcProp.Type.ToDisplayString(),
                        dstProp.Name,
                        dst.Name,
                        dstProp.Type.ToDisplayString());

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

        private static List<IPropertySymbol> GetReadableProperties(INamedTypeSymbol type)
        {
            return type.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => !p.IsStatic && !p.IsIndexer && p.GetMethod is not null)
                .ToList();
        }

        private static bool IsIgnored(IPropertySymbol prop)
        {
            return prop.GetAttributes().Any(a =>
            {
                var name = a.AttributeClass?.Name ?? string.Empty;
                var fullName = a.AttributeClass?.ToDisplayString() ?? string.Empty;

                return name == IgnoreAttributeShortName ||
                       name == IgnoreAttributeAltName ||
                       name == IgnoreAttributeShortName + "Attribute" ||
                       name == IgnoreAttributeAltName + "Attribute" ||
                       fullName == "DeltaMapper.Ignore" ||
                       fullName == "DeltaMapper.DeltaMapperIgnore" ||
                       fullName == "DeltaMapper.IgnoreAttribute" ||
                       fullName == "DeltaMapper.DeltaMapperIgnoreAttribute";
            });
        }
    }
}
