using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DeltaMapper.SourceGen.Tests.Helpers;

public static class GeneratorTestHelper
{
    /// <summary>
    /// Builds the set of MetadataReferences used for all test compilations.
    /// Includes all non-dynamic loaded assemblies plus DeltaMapper.Core explicitly,
    /// so generated code referencing DeltaMapper.Runtime resolves correctly.
    /// </summary>
    private static IReadOnlyList<MetadataReference> BuildReferences()
    {
        var refs = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>()
            .ToList();

        // Explicitly include DeltaMapper.Core so generated code can reference DeltaMapper.Runtime
        var coreLocation = typeof(Runtime.GeneratedMapRegistry).Assembly.Location;
        if (!string.IsNullOrWhiteSpace(coreLocation))
            refs.Add(MetadataReference.CreateFromFile(coreLocation));

        return refs;
    }

    public static GeneratorDriverRunResult RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            BuildReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new MapperGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
        return driver.GetRunResult();
    }

    /// <summary>
    /// Runs the generator and also returns the output compilation for diagnostic checking.
    /// </summary>
    public static (GeneratorDriverRunResult RunResult, Compilation OutputCompilation) RunGeneratorWithCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            BuildReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new MapperGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);
        return (driver.GetRunResult(), outputCompilation);
    }
}
