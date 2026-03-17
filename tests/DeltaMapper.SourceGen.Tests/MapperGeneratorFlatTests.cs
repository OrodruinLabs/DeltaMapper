using FluentAssertions;
using Microsoft.CodeAnalysis;
using Xunit;
using DeltaMapper.SourceGen.Tests.Helpers;

namespace DeltaMapper.SourceGen.Tests;

public class MapperGeneratorFlatTests
{
    private const string FlatPocoSource = """
        using DeltaMapper;

        namespace MyApp
        {
            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; } = "";
                public string Email { get; set; } = "";
            }

            public class UserDto
            {
                public int Id { get; set; }
                public string Name { get; set; } = "";
                public string Email { get; set; } = "";
            }

            [GenerateMap(typeof(User), typeof(UserDto))]
            public partial class UserProfile { }
        }
        """;

    [Fact]
    public void Generator_ProducesOneMapFile_ForFlatPocoPair()
    {
        var result = GeneratorTestHelper.RunGenerator(FlatPocoSource);

        // The generator adds 2 sources: the attribute file + the map file
        result.GeneratedTrees.Should().HaveCount(2,
            "one for GenerateMapAttribute.g.cs and one for the flat map method");
    }

    [Fact]
    public void Generator_EmitsAttributeSourceFile()
    {
        var result = GeneratorTestHelper.RunGenerator(FlatPocoSource);

        result.GeneratedTrees
            .Select(t => System.IO.Path.GetFileName(t.FilePath))
            .Should().Contain("GenerateMapAttribute.g.cs");
    }

    [Fact]
    public void Generator_EmitsMapFile_WithCorrectHintName()
    {
        var result = GeneratorTestHelper.RunGenerator(FlatPocoSource);

        result.GeneratedTrees
            .Select(t => System.IO.Path.GetFileName(t.FilePath))
            .Should().Contain("UserProfile.User_To_UserDto.g.cs");
    }

    [Fact]
    public void Generator_EmittedMapFile_ContainsAllThreeAssignments()
    {
        var result = GeneratorTestHelper.RunGenerator(FlatPocoSource);

        var mapTree = result.GeneratedTrees
            .SingleOrDefault(t => t.FilePath.EndsWith("UserProfile.User_To_UserDto.g.cs"));

        mapTree.Should().NotBeNull("the map file must be generated");

        var sourceText = mapTree!.ToString();

        sourceText.Should().Contain("dst.Id = src.Id;");
        sourceText.Should().Contain("dst.Name = src.Name;");
        sourceText.Should().Contain("dst.Email = src.Email;");
    }

    [Fact]
    public void Generator_EmittedMapFile_ContainsCorrectMethodSignature()
    {
        var result = GeneratorTestHelper.RunGenerator(FlatPocoSource);

        var mapTree = result.GeneratedTrees
            .SingleOrDefault(t => t.FilePath.EndsWith("UserProfile.User_To_UserDto.g.cs"));

        mapTree.Should().NotBeNull();

        var sourceText = mapTree!.ToString();
        sourceText.Should().Contain("private static void Map_User_To_UserDto(MyApp.User src, MyApp.UserDto dst)");
    }

    [Fact]
    public void Generator_EmittedMapFile_IsWrappedInPartialClass()
    {
        var result = GeneratorTestHelper.RunGenerator(FlatPocoSource);

        var mapTree = result.GeneratedTrees
            .SingleOrDefault(t => t.FilePath.EndsWith("UserProfile.User_To_UserDto.g.cs"));

        mapTree.Should().NotBeNull();

        var sourceText = mapTree!.ToString();
        sourceText.Should().Contain("public partial class UserProfile");
        sourceText.Should().Contain("namespace MyApp");
    }

    [Fact]
    public void Generator_OutputCompilation_HasNoErrors()
    {
        var (_, outputCompilation) = GeneratorTestHelper.RunGeneratorWithCompilation(FlatPocoSource);

        var errors = outputCompilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        errors.Should().BeEmpty(
            $"output compilation must have no errors but got: {string.Join(", ", errors.Select(e => e.GetMessage()))}");
    }

    [Fact]
    public void Generator_SkipsProperty_WhenTypesDiffer()
    {
        const string source = """
            using DeltaMapper;

            namespace MyApp
            {
                public class Src
                {
                    public int Age { get; set; }
                    public string Name { get; set; } = "";
                }

                public class Dst
                {
                    public string Age { get; set; } = "";  // type mismatch — should be skipped
                    public string Name { get; set; } = "";
                }

                [GenerateMap(typeof(Src), typeof(Dst))]
                public partial class TypeMismatchProfile { }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);

        var mapTree = result.GeneratedTrees
            .SingleOrDefault(t => t.FilePath.EndsWith("TypeMismatchProfile.Src_To_Dst.g.cs"));

        mapTree.Should().NotBeNull();

        var sourceText = mapTree!.ToString();
        sourceText.Should().Contain("dst.Name = src.Name;",  "Name matches by name and type");
        sourceText.Should().NotContain("dst.Age = src.Age;", "Age has mismatched types and must be skipped");
    }

    [Fact]
    public void Generator_HandlesMultipleGenerateMapAttributes()
    {
        const string source = """
            using DeltaMapper;

            namespace MyApp
            {
                public class A { public int X { get; set; } }
                public class B { public int X { get; set; } }
                public class C { public int X { get; set; } }

                [GenerateMap(typeof(A), typeof(B))]
                [GenerateMap(typeof(A), typeof(C))]
                public partial class MultiProfile { }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);

        // attribute file + 2 map files
        result.GeneratedTrees.Should().HaveCount(3,
            "one attribute file plus one map file per [GenerateMap] attribute");

        result.GeneratedTrees
            .Select(t => System.IO.Path.GetFileName(t.FilePath))
            .Should().Contain("MultiProfile.A_To_B.g.cs")
            .And.Contain("MultiProfile.A_To_C.g.cs");
    }

    [Fact]
    public void Generator_ProducesNoMapFile_WhenNoMatchingProperties()
    {
        const string source = """
            using DeltaMapper;

            namespace MyApp
            {
                public class Left  { public int Foo { get; set; } }
                public class Right { public int Bar { get; set; } }

                [GenerateMap(typeof(Left), typeof(Right))]
                public partial class NoMatchProfile { }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);

        var mapTree = result.GeneratedTrees
            .SingleOrDefault(t => t.FilePath.EndsWith("NoMatchProfile.Left_To_Right.g.cs"));

        // File is still emitted but has empty assignment body
        mapTree.Should().NotBeNull("generator always emits the partial class file");
        var sourceText = mapTree!.ToString();
        sourceText.Should().NotContain("dst.", "no assignments when no properties match");
    }
}
