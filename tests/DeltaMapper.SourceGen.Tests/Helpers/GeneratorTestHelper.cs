using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DeltaMapper.SourceGen.Tests.Helpers;

public static class GeneratorTestHelper
{
    public static GeneratorDriverRunResult RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        // Create references for compilation
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

        // TODO: Replace with actual generator type once TASK-025 creates it
        // var generator = new DeltaMapperSourceGenerator();
        // GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        // driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);
        // return driver.GetRunResult();

        throw new NotImplementedException("Generator not yet implemented — will be wired in TASK-025");
    }
}
