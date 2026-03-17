using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DeltaMapper.SourceGen.Tests.Helpers;

public static class GeneratorTestHelper
{
    public static GeneratorDriverRunResult RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        // Create references for compilation — include all loaded assemblies
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>()
            .ToList();

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references,
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

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>()
            .ToList();

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new MapperGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);
        return (driver.GetRunResult(), outputCompilation);
    }
}
