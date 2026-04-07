using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using FluentAssertions;
using Xunit;
using DeltaMapper.SourceGen.AttributeSources;

namespace DeltaMapper.SourceGen.Tests;

public class GenerateMapAttributeTests
{
    private static MetadataReference[] CommonReferences =>
        new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) };

    [Fact]
    public void AttributeSource_CompilesWithoutErrors()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(GenerateMapAttributeSource.Source);
        var compilation = CSharpCompilation.Create("TestAssembly", [syntaxTree], CommonReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var diagnostics = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void IgnoreMemberAttributeSource_CompilesWithoutErrors()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(IgnoreMemberAttributeSource.Source);
        var compilation = CSharpCompilation.Create("TestAssembly", [syntaxTree], CommonReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var diagnostics = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void NullSubstituteAttributeSource_CompilesWithoutErrors()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(NullSubstituteAttributeSource.Source);
        var compilation = CSharpCompilation.Create("TestAssembly", [syntaxTree], CommonReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var diagnostics = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void MapMemberAttributeSource_CompilesWithoutErrors()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(MapMemberAttributeSource.Source);
        var compilation = CSharpCompilation.Create("TestAssembly", [syntaxTree], CommonReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var diagnostics = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error);
        diagnostics.Should().BeEmpty();
    }
}
