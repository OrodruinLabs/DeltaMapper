using DeltaMapper.SourceGen.AttributeSources;
using DeltaMapper.SourceGen.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace DeltaMapper.SourceGen
{
    /// <summary>
    /// Roslyn incremental source generator that emits compile-time mapping code
    /// for profiles decorated with <c>[GenerateMap]</c>.
    /// </summary>
    [Generator]
    public sealed class MapperGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // 1. Register the attribute source texts
            context.RegisterPostInitializationOutput(ctx =>
                ctx.AddSource(GenerateMapAttributeSource.HintName, GenerateMapAttributeSource.Source));
            context.RegisterPostInitializationOutput(ctx =>
                ctx.AddSource(IgnoreMemberAttributeSource.HintName, IgnoreMemberAttributeSource.Source));
            context.RegisterPostInitializationOutput(ctx =>
                ctx.AddSource(NullSubstituteAttributeSource.HintName, NullSubstituteAttributeSource.Source));
            context.RegisterPostInitializationOutput(ctx =>
                ctx.AddSource(MapMemberAttributeSource.HintName, MapMemberAttributeSource.Source));

            // 2. Create syntax provider that filters for classes with [GenerateMap]
            var classDeclarations = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    GenerateMapAttributeSource.AttributeName,
                    predicate: static (node, _) => node is ClassDeclarationSyntax,
                    transform: static (ctx, _) => GetMappingInfo(ctx))
                .Where(static m => m is not null);

            // 3. Register source output
            context.RegisterSourceOutput(classDeclarations,
                static (spc, info) => Execute(spc, info!));
        }

        private static MappingInfo? GetMappingInfo(GeneratorAttributeSyntaxContext context)
        {
            // Extract the class symbol
            var classSymbol = context.TargetSymbol as INamedTypeSymbol;
            if (classSymbol is null) return null;

            // Verify the class has the 'partial' modifier — generated methods require it
            var classDecl = context.TargetNode as ClassDeclarationSyntax;
            if (classDecl is null) return null;
            bool isPartial = false;
            foreach (var modifier in classDecl.Modifiers)
            {
                if (modifier.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword))
                {
                    isPartial = true;
                    break;
                }
            }
            if (!isPartial) return null;

            // Extract [GenerateMap(typeof(Src), typeof(Dst))] attributes —
            // capture raw AttributeData and the syntax location for diagnostics.
            var rawAttributes = new List<(AttributeData Data, Location Location)>();

            foreach (var attr in classSymbol.GetAttributes())
            {
                if (attr.AttributeClass?.ToDisplayString() != GenerateMapAttributeSource.AttributeName)
                    continue;

                // Resolve syntax location for the attribute node (best-effort)
                var attrLocation = attr.ApplicationSyntaxReference is not null
                    ? Location.Create(
                        attr.ApplicationSyntaxReference.SyntaxTree,
                        attr.ApplicationSyntaxReference.Span)
                    : context.TargetNode.GetLocation();

                rawAttributes.Add((attr, attrLocation));
            }

            if (rawAttributes.Count == 0) return null;

            // Parse companion attributes: [IgnoreMember], [NullSubstitute], [MapMember]
            var config = new AttributeConfig();

            foreach (var attr in classSymbol.GetAttributes())
            {
                var attrFullName = attr.AttributeClass?.ToDisplayString();

                // Resolve attribute location (best-effort)
                var attrLoc = attr.ApplicationSyntaxReference is not null
                    ? Location.Create(
                        attr.ApplicationSyntaxReference.SyntaxTree,
                        attr.ApplicationSyntaxReference.Span)
                    : context.TargetNode.GetLocation();

                var args = attr.ConstructorArguments;

                if (attrFullName == IgnoreMemberAttributeSource.AttributeName)
                {
                    // IgnoreMemberAttribute(Type sourceType, Type destinationType, string memberName)
                    if (args.Length == 3
                        && args[0].Value is INamedTypeSymbol ignoreSrc
                        && args[1].Value is INamedTypeSymbol ignoreDst
                        && args[2].Value is string ignoreMember)
                    {
                        config.Ignores.Add(new IgnoreMemberConfig(ignoreSrc, ignoreDst, ignoreMember, attrLoc));
                    }
                }
                else if (attrFullName == NullSubstituteAttributeSource.AttributeName)
                {
                    // NullSubstituteAttribute(Type sourceType, Type destinationType, string memberName, object value)
                    if (args.Length == 4
                        && args[0].Value is INamedTypeSymbol nullSrc
                        && args[1].Value is INamedTypeSymbol nullDst
                        && args[2].Value is string nullMember)
                    {
                        var nullValue = args[3].Value; // may be null, int, string, etc.
                        config.NullSubstitutes.Add(new NullSubstituteConfig(nullSrc, nullDst, nullMember, nullValue, attrLoc));
                    }
                }
                else if (attrFullName == MapMemberAttributeSource.AttributeName)
                {
                    // MapMemberAttribute(Type sourceType, Type destinationType, string destinationMember, string sourceMember)
                    if (args.Length == 4
                        && args[0].Value is INamedTypeSymbol mapSrc
                        && args[1].Value is INamedTypeSymbol mapDst
                        && args[2].Value is string dstMember
                        && args[3].Value is string srcMember)
                    {
                        config.MapMembers.Add(new MapMemberConfig(mapSrc, mapDst, dstMember, srcMember, attrLoc));
                    }
                }
            }

            return new MappingInfo(classSymbol, rawAttributes, config);
        }

        private static void Execute(SourceProductionContext context, MappingInfo info)
        {
            // Resolve types, report DM002 for unresolvable/error type arguments, and build
            // the valid-pairs list.  Each valid pair carries its attribute location so
            // DM001 can point back to the [GenerateMap] attribute site.
            var validMappings = new List<(INamedTypeSymbol Src, INamedTypeSymbol Dst, Location Location)>();

            foreach (var (attr, location) in info.RawAttributes)
            {
                var pair = MappingAnalyzer.ResolveAndValidateTypes(context, attr, location);
                if (pair is not null)
                    validMappings.Add((pair.Value.Src, pair.Value.Dst, location));
            }

            // Report DM001 for each valid (non-error) pair only.
            // Invalid pairs already have DM002 reported by ResolveAndValidateTypes.
            foreach (var (src, dst, location) in validMappings)
            {
                MappingAnalyzer.ReportUnmappedProperties(context, src, dst, location);
            }

            if (validMappings.Count == 0) return;

            // Emit one map-method file per (src, dst) pair.
            var pairs = new List<(INamedTypeSymbol Src, INamedTypeSymbol Dst)>(validMappings.Count);
            foreach (var (src, dst, _) in validMappings)
                pairs.Add((src, dst));

            foreach (var (src, dst) in pairs)
            {
                var source = EmitHelper.EmitMapMethod(info.ProfileClass, src, dst, pairs);
                var hintName = EmitHelper.BuildFileName(info.ProfileClass, src, dst);
                context.AddSource(hintName, source);

                var factorySource = EmitHelper.EmitFactoryMethod(info.ProfileClass, src, dst, pairs);
                var factoryHintName = EmitHelper.BuildFactoryFileName(info.ProfileClass, src, dst);
                context.AddSource(factoryHintName, factorySource);
            }

            // Emit one ModuleInitializer file per profile class (covers all pairs)
            var initSource = EmitHelper.EmitModuleInitializer(info.ProfileClass, pairs);
            var initHintName = EmitHelper.BuildModuleInitializerFileName(info.ProfileClass);
            context.AddSource(initHintName, initSource);
        }
    }

    internal sealed class MappingInfo
    {
        public INamedTypeSymbol ProfileClass { get; }
        public List<(AttributeData Data, Location Location)> RawAttributes { get; }
        public AttributeConfig Config { get; }

        public MappingInfo(
            INamedTypeSymbol profileClass,
            List<(AttributeData Data, Location Location)> rawAttributes,
            AttributeConfig config)
        {
            ProfileClass = profileClass;
            RawAttributes = rawAttributes;
            Config = config;
        }
    }
}
