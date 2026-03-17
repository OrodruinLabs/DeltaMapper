using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DeltaMapper.SourceGen
{
    [Generator]
    public sealed class MapperGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // 1. Register the attribute source text
            context.RegisterPostInitializationOutput(ctx =>
                ctx.AddSource(GenerateMapAttributeSource.HintName, GenerateMapAttributeSource.Source));

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

            // Extract [GenerateMap(typeof(Src), typeof(Dst))] attributes
            var mappings = new List<(INamedTypeSymbol Src, INamedTypeSymbol Dst)>();

            foreach (var attr in classSymbol.GetAttributes())
            {
                if (attr.AttributeClass?.ToDisplayString() != GenerateMapAttributeSource.AttributeName)
                    continue;

                if (attr.ConstructorArguments.Length != 2) continue;

                var srcType = attr.ConstructorArguments[0].Value as INamedTypeSymbol;
                var dstType = attr.ConstructorArguments[1].Value as INamedTypeSymbol;

                if (srcType is not null && dstType is not null)
                    mappings.Add((srcType, dstType));
            }

            if (mappings.Count == 0) return null;

            return new MappingInfo(classSymbol, mappings);
        }

        private static void Execute(SourceProductionContext context, MappingInfo info)
        {
            foreach (var (src, dst) in info.Mappings)
            {
                var source = EmitHelper.EmitMapMethod(info.ProfileClass, src, dst);
                var hintName = EmitHelper.BuildFileName(info.ProfileClass, src, dst);
                context.AddSource(hintName, source);
            }
        }
    }

    internal sealed class MappingInfo
    {
        public INamedTypeSymbol ProfileClass { get; }
        public List<(INamedTypeSymbol Src, INamedTypeSymbol Dst)> Mappings { get; }

        public MappingInfo(INamedTypeSymbol profileClass, List<(INamedTypeSymbol, INamedTypeSymbol)> mappings)
        {
            ProfileClass = profileClass;
            Mappings = mappings;
        }
    }
}
