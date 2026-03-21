using FluentAssertions;
using Microsoft.CodeAnalysis;
using Xunit;
using DeltaMapper.SourceGen.Tests.Helpers;

namespace DeltaMapper.SourceGen.Tests;

public class ModuleInitializerEmitTests
{
    private const string FlatPocoSource = """
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

    [Fact]
    public void Generator_EmitsModuleInitializerFile_ForSingleMapping()
    {
        var result = GeneratorTestHelper.RunGenerator(FlatPocoSource);

        result.GeneratedTrees
            .Select(t => Path.GetFileName(t.FilePath))
            .Should().Contain("UserProfile.ModuleInit.g.cs",
                "the generator must emit a ModuleInitializer file for each profile class");
    }

    [Fact]
    public void Generator_ModuleInitializerFile_ContainsModuleInitializerAttribute()
    {
        var result = GeneratorTestHelper.RunGenerator(FlatPocoSource);

        var initTree = result.GeneratedTrees
            .SingleOrDefault(t => t.FilePath.EndsWith("UserProfile.ModuleInit.g.cs"));

        initTree.Should().NotBeNull("the ModuleInitializer file must be generated");

        var sourceText = initTree!.ToString();
        sourceText.Should().Contain("[System.Runtime.CompilerServices.ModuleInitializer]",
            "the generated file must apply the ModuleInitializer attribute");
    }

    [Fact]
    public void Generator_ModuleInitializerFile_ContainsRegisterCall()
    {
        var result = GeneratorTestHelper.RunGenerator(FlatPocoSource);

        var initTree = result.GeneratedTrees
            .SingleOrDefault(t => t.FilePath.EndsWith("UserProfile.ModuleInit.g.cs"));

        initTree.Should().NotBeNull();

        var sourceText = initTree!.ToString();
        sourceText.Should().Contain("DeltaMapper.Runtime.GeneratedMapRegistry.Register<MyApp.User, MyApp.UserDto>",
            "the initializer must call GeneratedMapRegistry.Register with the correct type arguments");
    }

    [Fact]
    public void Generator_ModuleInitializerFile_DelegateWrapsMapMethod()
    {
        var result = GeneratorTestHelper.RunGenerator(FlatPocoSource);

        var initTree = result.GeneratedTrees
            .SingleOrDefault(t => t.FilePath.EndsWith("UserProfile.ModuleInit.g.cs"));

        initTree.Should().NotBeNull();

        var sourceText = initTree!.ToString();
        sourceText.Should().Contain("static (src, dst) => Map_User_To_UserDto(src, dst)",
            "the registration delegate must be a static lambda wrapping the generated map method");
    }

    [Fact]
    public void Generator_ModuleInitializerFile_MethodIsInternalStatic()
    {
        var result = GeneratorTestHelper.RunGenerator(FlatPocoSource);

        var initTree = result.GeneratedTrees
            .SingleOrDefault(t => t.FilePath.EndsWith("UserProfile.ModuleInit.g.cs"));

        initTree.Should().NotBeNull();

        var sourceText = initTree!.ToString();
        sourceText.Should().Contain("internal static void RegisterGeneratedMaps_UserProfile()",
            "ModuleInitializer method must be internal static void and parameterless");
    }

    [Fact]
    public void Generator_ModuleInitializerFile_IsWrappedInPartialClass()
    {
        var result = GeneratorTestHelper.RunGenerator(FlatPocoSource);

        var initTree = result.GeneratedTrees
            .SingleOrDefault(t => t.FilePath.EndsWith("UserProfile.ModuleInit.g.cs"));

        initTree.Should().NotBeNull();

        var sourceText = initTree!.ToString();
        sourceText.Should().Contain("public partial class UserProfile",
            "the initializer method must live inside the partial profile class");
        sourceText.Should().Contain("namespace MyApp",
            "the partial class must be in the correct namespace");
    }

    [Fact]
    public void Generator_ModuleInitializerFile_MultipleAttributes_RegistersAllMappings()
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

        var initTree = result.GeneratedTrees
            .SingleOrDefault(t => t.FilePath.EndsWith("MultiProfile.ModuleInit.g.cs"));

        initTree.Should().NotBeNull("one ModuleInitializer file must be emitted for MultiProfile");

        var sourceText = initTree!.ToString();

        sourceText.Should().Contain("DeltaMapper.Runtime.GeneratedMapRegistry.Register<MyApp.A, MyApp.B>",
            "must register the A -> B mapping");
        sourceText.Should().Contain("DeltaMapper.Runtime.GeneratedMapRegistry.Register<MyApp.A, MyApp.C>",
            "must register the A -> C mapping");

        // Only one ModuleInitializer method for the profile class
        var initMethodCount = System.Text.RegularExpressions.Regex
            .Matches(sourceText, "ModuleInitializer")
            .Count;
        initMethodCount.Should().Be(1,
            "only one [ModuleInitializer] attribute should appear per profile class file");
    }

    [Fact]
    public void Generator_OutputCompilation_WithModuleInitializer_HasNoErrors()
    {
        var (_, outputCompilation) = GeneratorTestHelper.RunGeneratorWithCompilation(FlatPocoSource);

        var errors = outputCompilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        errors.Should().BeEmpty(
            $"output compilation including the ModuleInitializer file must have no errors but got: " +
            $"{string.Join(", ", errors.Select(e => e.GetMessage()))}");
    }
}
