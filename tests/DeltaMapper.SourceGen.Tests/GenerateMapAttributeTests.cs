using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using FluentAssertions;
using Xunit;

namespace DeltaMapper.SourceGen.Tests;

public class GenerateMapAttributeTests
{
    [Fact]
    public void AttributeSource_CompilesWithoutErrors()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(GenerateMapAttributeSource.Source);
        var references = new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) };
        var compilation = CSharpCompilation.Create("TestAssembly", [syntaxTree], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var diagnostics = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error);
        diagnostics.Should().BeEmpty();
    }
}
