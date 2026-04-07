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

        // The generator adds 7 sources: 4 attribute files (GenerateMap, IgnoreMember, NullSubstitute, MapMember) +
        // the map method file + the factory method file + the ModuleInitializer file
        result.GeneratedTrees.Should().HaveCount(7,
            "four attribute files (GenerateMap, IgnoreMember, NullSubstitute, MapMember), one for the flat map method, one for the factory method, and one for the ModuleInitializer");
    }

    [Fact]
    public void Generator_EmitsAttributeSourceFile()
    {
        var result = GeneratorTestHelper.RunGenerator(FlatPocoSource);

        result.GeneratedTrees
            .Select(t => Path.GetFileName(t.FilePath))
            .Should().Contain("GenerateMapAttribute.g.cs");
    }

    [Fact]
    public void Generator_EmitsMapFile_WithCorrectHintName()
    {
        var result = GeneratorTestHelper.RunGenerator(FlatPocoSource);

        result.GeneratedTrees
            .Select(t => Path.GetFileName(t.FilePath))
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
        sourceText.Should().Contain("dst.Name = src.Name;", "Name matches by name and type");
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

        // 4 attribute files + 2 map files + 2 factory files + 1 ModuleInitializer file (one per profile class)
        result.GeneratedTrees.Should().HaveCount(9,
            "four attribute files (GenerateMap, IgnoreMember, NullSubstitute, MapMember) plus one map file per [GenerateMap] attribute plus one factory file per [GenerateMap] attribute plus one ModuleInitializer file per profile");

        result.GeneratedTrees
            .Select(t => Path.GetFileName(t.FilePath))
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

    [Fact]
    public void Generator_FactoryMethod_UsesObjectInitializerForFlatTypes()
    {
        var result = GeneratorTestHelper.RunGenerator(FlatPocoSource);

        var factoryTree = result.GeneratedTrees
            .SingleOrDefault(t => t.FilePath.EndsWith("UserProfile.User_To_UserDto.Factory.g.cs"));

        factoryTree.Should().NotBeNull("factory file must be generated");

        var sourceText = factoryTree!.ToString();

        // Should use object initializer pattern (=> new()) not two-step (var dst = new)
        sourceText.Should().Contain("=> new()", "flat types should use object initializer");
        sourceText.Should().Contain("Id = src.Id,");
        sourceText.Should().Contain("Name = src.Name,");
        sourceText.Should().Contain("Email = src.Email,");
        sourceText.Should().NotContain("var dst = new", "flat types should not use two-step pattern");
    }

    [Fact]
    public void Generator_FactoryMethod_EmitsPublicStaticMapMethod()
    {
        var result = GeneratorTestHelper.RunGenerator(FlatPocoSource);

        var factoryTree = result.GeneratedTrees
            .SingleOrDefault(t => t.FilePath.EndsWith("UserProfile.User_To_UserDto.Factory.g.cs"));

        factoryTree.Should().NotBeNull();

        var sourceText = factoryTree!.ToString();

        sourceText.Should().Contain("public static MyApp.UserDto MapUserToUserDto(MyApp.User src)",
            "should generate public static Map method for direct zero-overhead calls");
        sourceText.Should().Contain("ArgumentNullException.ThrowIfNull(src)",
            "public method should validate input");
    }
}
