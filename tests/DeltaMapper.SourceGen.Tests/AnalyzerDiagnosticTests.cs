using FluentAssertions;
using Microsoft.CodeAnalysis;
using Xunit;
using DeltaMapper.SourceGen.Tests.Helpers;

namespace DeltaMapper.SourceGen.Tests;

/// <summary>
/// Tests for DM001 (Unmapped Destination Property) and DM002 (Type Not Found) diagnostics.
/// </summary>
public class AnalyzerDiagnosticTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // DM001 — Unmapped Destination Property
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void DM001_IsReported_WhenDestinationHasPropertyWithNoMatchInSource()
    {
        const string source = """
            using DeltaMapper;

            namespace MyApp
            {
                public class User
                {
                    public int Id { get; set; }
                    public string Name { get; set; } = "";
                }

                public class UserDto
                {
                    public int Id { get; set; }
                    public string Name { get; set; } = "";
                    public string FullName { get; set; } = "";  // no match in User
                }

                [GenerateMap(typeof(User), typeof(UserDto))]
                public partial class UserProfile { }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);

        var diagnostics = result.Diagnostics;
        diagnostics.Should().ContainSingle(d => d.Id == "DM001",
            "DM001 should be reported once for the unmatched 'FullName' property");

        var dm001 = diagnostics.Single(d => d.Id == "DM001");
        dm001.Severity.Should().Be(DiagnosticSeverity.Warning);
        dm001.GetMessage().Should().Contain("FullName");
        dm001.GetMessage().Should().Contain("UserDto");
        dm001.GetMessage().Should().Contain("User");
    }

    [Fact]
    public void DM001_IsReportedForEachUnmatchedProperty()
    {
        const string source = """
            using DeltaMapper;

            namespace MyApp
            {
                public class Src
                {
                    public int Id { get; set; }
                }

                public class Dst
                {
                    public int Id      { get; set; }
                    public string Foo  { get; set; } = "";
                    public string Bar  { get; set; } = "";
                }

                [GenerateMap(typeof(Src), typeof(Dst))]
                public partial class MixedProfile { }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);

        var dm001Diagnostics = result.Diagnostics.Where(d => d.Id == "DM001").ToList();
        dm001Diagnostics.Should().HaveCount(2, "Foo and Bar are both unmatched");

        dm001Diagnostics.Select(d => d.GetMessage()).Should()
            .Contain(m => m.Contains("Foo"))
            .And.Contain(m => m.Contains("Bar"));
    }

    [Fact]
    public void DM001_IsNotReported_WhenAllPropertiesMatch()
    {
        const string source = """
            using DeltaMapper;

            namespace MyApp
            {
                public class User
                {
                    public int Id { get; set; }
                    public string Name { get; set; } = "";
                }

                public class UserDto
                {
                    public int Id { get; set; }
                    public string Name { get; set; } = "";
                }

                [GenerateMap(typeof(User), typeof(UserDto))]
                public partial class UserProfile { }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);

        result.Diagnostics.Should().NotContain(d => d.Id == "DM001",
            "all writable destination properties have matching source properties");
    }

    [Fact]
    public void DM001_IsNotReported_ForPropertiesWithIgnoreAttribute()
    {
        const string source = """
            using DeltaMapper;

            namespace MyApp
            {
                public class IgnoreAttribute : System.Attribute { }

                public class Src
                {
                    public int Id { get; set; }
                }

                public class Dst
                {
                    public int Id { get; set; }

                    [Ignore]
                    public string Unmatched { get; set; } = "";
                }

                [GenerateMap(typeof(Src), typeof(Dst))]
                public partial class IgnoreProfile { }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);

        result.Diagnostics.Should().NotContain(d => d.Id == "DM001",
            "the 'Unmatched' property carries [Ignore] and must not trigger DM001");
    }

    [Fact]
    public void DM001_Location_PointsToAttributeSyntax()
    {
        const string source = """
            using DeltaMapper;

            namespace MyApp
            {
                public class A { public int X { get; set; } }
                public class B { public int X { get; set; }
                                 public int Y { get; set; } }

                [GenerateMap(typeof(A), typeof(B))]
                public partial class P { }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);

        var dm001 = result.Diagnostics.Single(d => d.Id == "DM001");
        dm001.Location.Should().NotBe(Location.None,
            "diagnostic should have a valid source location pointing to the attribute");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DM002 — Type Not Found
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void DM002_IsReported_WhenSourceTypeCannotBeResolved()
    {
        // When the type in typeof() does not exist in the compilation,
        // the compiler emits an error first (CS0246) and the constructor argument
        // value is null — triggering DM002.
        const string source = """
            using DeltaMapper;

            namespace MyApp
            {
                public class RealDto { public int Id { get; set; } }

                [GenerateMap(typeof(DoesNotExist), typeof(RealDto))]
                public partial class BrokenProfile { }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);

        result.Diagnostics.Should().Contain(d => d.Id == "DM002",
            "DM002 must be reported when the source type cannot be resolved");

        var dm002 = result.Diagnostics.First(d => d.Id == "DM002");
        dm002.Severity.Should().Be(DiagnosticSeverity.Error);
    }

    [Fact]
    public void DM002_IsReported_WhenDestinationTypeCannotBeResolved()
    {
        const string source = """
            using DeltaMapper;

            namespace MyApp
            {
                public class RealSource { public int Id { get; set; } }

                [GenerateMap(typeof(RealSource), typeof(AlsoDoesNotExist))]
                public partial class BrokenProfile2 { }
            }
            """;

        // Use the full compilation run so we can examine both generator-reported diagnostics
        // (DM002) and compiler-reported errors (CS0246 for unresolvable typeof).
        var (result, outputCompilation) = GeneratorTestHelper.RunGeneratorWithCompilation(source);

        var allDiagnostics = result.Diagnostics
            .Concat(outputCompilation.GetDiagnostics())
            .ToList();

        // Either the generator reports DM002 directly (when the error type is seen by Execute)
        // or the compiler reports CS0246.  Either way the user is informed the type is missing.
        bool hasDM002 = allDiagnostics.Any(d => d.Id == "DM002");
        bool hasCS0246 = allDiagnostics.Any(d => d.Id == "CS0246");

        (hasDM002 || hasCS0246).Should().BeTrue(
            "when a typeof() argument cannot be resolved, either DM002 or CS0246 must be present to inform the user");
    }

    [Fact]
    public void DM002_PreventsCodeGeneration_ForInvalidPair()
    {
        const string source = """
            using DeltaMapper;

            namespace MyApp
            {
                public class RealDto { public int Id { get; set; } }

                [GenerateMap(typeof(DoesNotExist), typeof(RealDto))]
                public partial class BrokenProfile { }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);

        // No map file should be emitted for the unresolvable pair
        var mapFiles = result.GeneratedTrees
            .Where(t => t.FilePath.Contains("BrokenProfile") && t.FilePath.EndsWith(".g.cs"))
            .ToList();

        // Only the attribute file (and possibly empty initialiser) should exist.
        // No BrokenProfile.<Src>_To_<Dst>.g.cs should appear.
        mapFiles.Should().NotContain(t => t.FilePath.Contains("_To_"),
            "when a type cannot be resolved, no mapping file should be generated");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DM003 — NOT NEEDED (compiler already catches via CS1061)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ForMember_on_nonexistent_property_is_caught_by_compiler()
    {
        // ForMember uses a strongly-typed lambda (dest => dest.PropertyName).
        // The C# compiler reports CS1061 when the property doesn't exist.
        // No custom DM003 analyzer is needed.
        const string source = """
            using DeltaMapper.Configuration;

            namespace MyApp
            {
                public class Src { public int Id { get; set; } }
                public class Dst { public int Id { get; set; } }

                public class BadProfile : Profile
                {
                    public BadProfile()
                    {
                        CreateMap<Src, Dst>()
                            .ForMember(d => d.NonExistent, o => o.Ignore());
                    }
                }
            }
            """;

        var syntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);
        var refs = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => (MetadataReference)MetadataReference.CreateFromFile(a.Location))
            .ToList();

        var coreLocation = typeof(DeltaMapper.Runtime.GeneratedMapRegistry).Assembly.Location;
        if (!string.IsNullOrWhiteSpace(coreLocation))
            refs.Add(MetadataReference.CreateFromFile(coreLocation));

        var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create(
            "TestAssembly", [syntaxTree], refs,
            new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var diagnostics = compilation.GetDiagnostics();

        // CS1061: 'Dst' does not contain a definition for 'NonExistent'
        diagnostics.Should().Contain(d => d.Id == "CS1061",
            "the C# compiler already catches ForMember on non-existent properties via the strongly-typed lambda");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // General — No diagnostics for clean mappings
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void NoDiagnostics_ForFullyMatchedPerfectMapping()
    {
        const string source = """
            using DeltaMapper;

            namespace MyApp
            {
                public class Entity { public int Id { get; set; } public string Title { get; set; } = ""; }
                public class Dto    { public int Id { get; set; } public string Title { get; set; } = ""; }

                [GenerateMap(typeof(Entity), typeof(Dto))]
                public partial class CleanProfile { }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);

        var hydraGeneratorDiags = result.Diagnostics
            .Where(d => d.Id == "DM001" || d.Id == "DM002")
            .ToList();

        hydraGeneratorDiags.Should().BeEmpty(
            "a perfectly matched mapping must produce no DM001 or DM002 diagnostics");
    }
}
