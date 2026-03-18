using FluentAssertions;
using Microsoft.CodeAnalysis;
using Xunit;
using DeltaMapper.SourceGen.Tests.Helpers;

namespace DeltaMapper.SourceGen.Tests;

/// <summary>
/// Tests for nested types, collection mapping (List&lt;T&gt;/T[]), and [Ignore] support.
/// </summary>
public class MapperGeneratorAdvancedTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Nested type tests
    // ─────────────────────────────────────────────────────────────────────────

    private const string NestedTypeSource = """
        using DeltaMapper;
        using System.Collections.Generic;

        namespace MyApp
        {
            public class Address
            {
                public string Street { get; set; } = "";
                public string City   { get; set; } = "";
            }

            public class AddressDto
            {
                public string Street { get; set; } = "";
                public string City   { get; set; } = "";
            }

            public class Order
            {
                public int     Id      { get; set; }
                public Address Address { get; set; } = new();
            }

            public class OrderDto
            {
                public int        Id      { get; set; }
                public AddressDto Address { get; set; } = new();
            }

            // Both pairs declared on the same profile so knownPairs resolves the nested type.
            [GenerateMap(typeof(Address), typeof(AddressDto))]
            [GenerateMap(typeof(Order),   typeof(OrderDto))]
            public partial class OrderProfile { }
        }
        """;

    [Fact]
    public void Generator_NestedType_EmitsRecursiveMapCall()
    {
        var result = GeneratorTestHelper.RunGenerator(NestedTypeSource);

        var mapTree = result.GeneratedTrees
            .SingleOrDefault(t => t.FilePath.EndsWith("OrderProfile.Order_To_OrderDto.g.cs"));

        mapTree.Should().NotBeNull("Order_To_OrderDto file must be generated");

        var sourceText = mapTree!.ToString();
        sourceText.Should().Contain("Map_Address_To_AddressDto",
            "nested Address property should delegate to the Address->AddressDto map method");
        sourceText.Should().Contain("new MyApp.AddressDto()",
            "nested destination must be instantiated before the recursive call");
    }

    [Fact]
    public void Generator_NestedType_FlatPropertiesStillMapped()
    {
        var result = GeneratorTestHelper.RunGenerator(NestedTypeSource);

        var mapTree = result.GeneratedTrees
            .SingleOrDefault(t => t.FilePath.EndsWith("OrderProfile.Order_To_OrderDto.g.cs"));

        mapTree.Should().NotBeNull();
        var sourceText = mapTree!.ToString();

        sourceText.Should().Contain("dst.Id = src.Id;",
            "flat scalar property Id must still be mapped");
    }

    [Fact]
    public void Generator_NestedType_OutputCompilation_HasNoErrors()
    {
        var (_, outputCompilation) = GeneratorTestHelper.RunGeneratorWithCompilation(NestedTypeSource);

        var errors = outputCompilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        errors.Should().BeEmpty(
            $"output compilation must have no errors but got: {string.Join(", ", errors.Select(e => e.GetMessage()))}");
    }

    [Fact]
    public void Generator_NestedType_SkippedWhenNoPairExists()
    {
        // Address only — no GenerateMap for the nested pair, so Address property is skipped.
        const string source = """
            using DeltaMapper;

            namespace MyApp
            {
                public class Address    { public string Street { get; set; } = ""; }
                public class AddressDto { public string Street { get; set; } = ""; }

                public class Order    { public int Id { get; set; } public Address Address { get; set; } = new(); }
                public class OrderDto { public int Id { get; set; } public AddressDto Address { get; set; } = new(); }

                // Only Order->OrderDto declared; no Address->AddressDto in knownPairs
                [GenerateMap(typeof(Order), typeof(OrderDto))]
                public partial class OrderOnlyProfile { }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);

        var mapTree = result.GeneratedTrees
            .SingleOrDefault(t => t.FilePath.EndsWith("OrderOnlyProfile.Order_To_OrderDto.g.cs"));

        mapTree.Should().NotBeNull();
        var sourceText = mapTree!.ToString();

        // Address must NOT be emitted because no pair exists for it
        sourceText.Should().NotContain("dst.Address",
            "Address property must be skipped when no [GenerateMap] pair covers Address->AddressDto");
        // But the flat scalar must still appear
        sourceText.Should().Contain("dst.Id = src.Id;");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // List<T> collection tests
    // ─────────────────────────────────────────────────────────────────────────

    private const string ListCollectionSource = """
        using DeltaMapper;
        using System.Collections.Generic;

        namespace MyApp
        {
            public class Item    { public string Name { get; set; } = ""; }
            public class ItemDto { public string Name { get; set; } = ""; }

            public class Cart
            {
                public int         Id    { get; set; }
                public List<Item>  Items { get; set; } = new();
            }

            public class CartDto
            {
                public int            Id    { get; set; }
                public List<ItemDto>  Items { get; set; } = new();
            }

            [GenerateMap(typeof(Item),  typeof(ItemDto))]
            [GenerateMap(typeof(Cart),  typeof(CartDto))]
            public partial class CartProfile { }
        }
        """;

    [Fact]
    public void Generator_ListProperty_EmitsSelectToList()
    {
        var result = GeneratorTestHelper.RunGenerator(ListCollectionSource);

        var mapTree = result.GeneratedTrees
            .SingleOrDefault(t => t.FilePath.EndsWith("CartProfile.Cart_To_CartDto.g.cs"));

        mapTree.Should().NotBeNull("Cart_To_CartDto file must be generated");

        var sourceText = mapTree!.ToString();
        sourceText.Should().Contain(".Select(",  "collection mapping must use LINQ Select");
        sourceText.Should().Contain(".ToList()", "List<T> destination must use ToList()");
        sourceText.Should().Contain("Map_Item_To_ItemDto",
            "collection element map method must be called");
    }

    [Fact]
    public void Generator_ListProperty_OutputCompilation_HasNoErrors()
    {
        var (_, outputCompilation) = GeneratorTestHelper.RunGeneratorWithCompilation(ListCollectionSource);

        var errors = outputCompilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        errors.Should().BeEmpty(
            $"output compilation must have no errors but got: {string.Join(", ", errors.Select(e => e.GetMessage()))}");
    }

    [Fact]
    public void Generator_PrimitiveList_EmitsDirectToList()
    {
        const string source = """
            using DeltaMapper;
            using System.Collections.Generic;

            namespace MyApp
            {
                public class Src { public List<string> Tags { get; set; } = new(); }
                public class Dst { public List<string> Tags { get; set; } = new(); }

                [GenerateMap(typeof(Src), typeof(Dst))]
                public partial class PrimitiveListProfile { }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);

        var mapTree = result.GeneratedTrees
            .SingleOrDefault(t => t.FilePath.EndsWith("PrimitiveListProfile.Src_To_Dst.g.cs"));

        mapTree.Should().NotBeNull();
        var sourceText = mapTree!.ToString();

        sourceText.Should().Contain("dst.Tags = src.Tags?.ToList();",
            "primitive/string List<T> must use direct .ToList() without Select");
        sourceText.Should().NotContain(".Select(",
            "no Select needed for same-element-type list");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Array collection tests
    // ─────────────────────────────────────────────────────────────────────────

    private const string ArrayCollectionSource = """
        using DeltaMapper;

        namespace MyApp
        {
            public class Tag    { public string Value { get; set; } = ""; }
            public class TagDto { public string Value { get; set; } = ""; }

            public class Document
            {
                public string  Title { get; set; } = "";
                public Tag[]   Tags  { get; set; } = System.Array.Empty<Tag>();
            }

            public class DocumentDto
            {
                public string    Title { get; set; } = "";
                public TagDto[]  Tags  { get; set; } = System.Array.Empty<TagDto>();
            }

            [GenerateMap(typeof(Tag),      typeof(TagDto))]
            [GenerateMap(typeof(Document), typeof(DocumentDto))]
            public partial class DocumentProfile { }
        }
        """;

    [Fact]
    public void Generator_ArrayProperty_EmitsSelectToArray()
    {
        var result = GeneratorTestHelper.RunGenerator(ArrayCollectionSource);

        var mapTree = result.GeneratedTrees
            .SingleOrDefault(t => t.FilePath.EndsWith("DocumentProfile.Document_To_DocumentDto.g.cs"));

        mapTree.Should().NotBeNull("Document_To_DocumentDto file must be generated");

        var sourceText = mapTree!.ToString();
        sourceText.Should().Contain(".Select(",   "array mapping must use LINQ Select");
        sourceText.Should().Contain(".ToArray()", "array destination must use ToArray()");
        sourceText.Should().Contain("Map_Tag_To_TagDto",
            "element map method must be called for arrays");
    }

    [Fact]
    public void Generator_ArrayProperty_OutputCompilation_HasNoErrors()
    {
        var (_, outputCompilation) = GeneratorTestHelper.RunGeneratorWithCompilation(ArrayCollectionSource);

        var errors = outputCompilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        errors.Should().BeEmpty(
            $"output compilation must have no errors but got: {string.Join(", ", errors.Select(e => e.GetMessage()))}");
    }

    [Fact]
    public void Generator_PrimitiveArray_EmitsDirectToArray()
    {
        const string source = """
            using DeltaMapper;

            namespace MyApp
            {
                public class Src { public string[] Codes { get; set; } = System.Array.Empty<string>(); }
                public class Dst { public string[] Codes { get; set; } = System.Array.Empty<string>(); }

                [GenerateMap(typeof(Src), typeof(Dst))]
                public partial class PrimitiveArrayProfile { }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);

        var mapTree = result.GeneratedTrees
            .SingleOrDefault(t => t.FilePath.EndsWith("PrimitiveArrayProfile.Src_To_Dst.g.cs"));

        mapTree.Should().NotBeNull();
        var sourceText = mapTree!.ToString();

        sourceText.Should().Contain("dst.Codes = src.Codes?.ToArray();",
            "primitive/string array must use direct .ToArray() without Select");
        sourceText.Should().NotContain(".Select(",
            "no Select needed for same-element-type array");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Ignore attribute tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Generator_IgnoredDestinationProperty_IsSkipped()
    {
        const string source = """
            using DeltaMapper;

            namespace MyApp
            {
                // Declare a custom Ignore attribute in the same compilation
                public sealed class IgnoreAttribute : System.Attribute { }

                public class UserSrc
                {
                    public int    Id       { get; set; }
                    public string Name     { get; set; } = "";
                    public string Password { get; set; } = "";
                }

                public class UserDst
                {
                    public int    Id       { get; set; }
                    public string Name     { get; set; } = "";

                    [Ignore]
                    public string Password { get; set; } = "";  // must be skipped
                }

                [GenerateMap(typeof(UserSrc), typeof(UserDst))]
                public partial class UserIgnoreProfile { }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);

        var mapTree = result.GeneratedTrees
            .SingleOrDefault(t => t.FilePath.EndsWith("UserIgnoreProfile.UserSrc_To_UserDst.g.cs"));

        mapTree.Should().NotBeNull("UserSrc_To_UserDst file must be generated");

        var sourceText = mapTree!.ToString();
        sourceText.Should().Contain("dst.Id = src.Id;",     "Id must be mapped normally");
        sourceText.Should().Contain("dst.Name = src.Name;", "Name must be mapped normally");
        sourceText.Should().NotContain("dst.Password",      "Password must be skipped due to [Ignore]");
    }

    [Fact]
    public void Generator_IgnoredProperty_OutputCompilation_HasNoErrors()
    {
        const string source = """
            using DeltaMapper;

            namespace MyApp
            {
                public sealed class IgnoreAttribute : System.Attribute { }

                public class Src { public int X { get; set; } public string Secret { get; set; } = ""; }
                public class Dst
                {
                    public int X { get; set; }
                    [Ignore] public string Secret { get; set; } = "";
                }

                [GenerateMap(typeof(Src), typeof(Dst))]
                public partial class IgnoreCompileProfile { }
            }
            """;

        var (_, outputCompilation) = GeneratorTestHelper.RunGeneratorWithCompilation(source);

        var errors = outputCompilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        errors.Should().BeEmpty(
            $"output compilation must have no errors but got: {string.Join(", ", errors.Select(e => e.GetMessage()))}");
    }

    [Fact]
    public void Generator_DeltaMapperIgnoreAttribute_IsSkipped()
    {
        const string source = """
            using DeltaMapper;

            namespace MyApp
            {
                public sealed class DeltaMapperIgnoreAttribute : System.Attribute { }

                public class Src { public int A { get; set; } public int B { get; set; } }
                public class Dst
                {
                    public int A { get; set; }
                    [DeltaMapperIgnore] public int B { get; set; }
                }

                [GenerateMap(typeof(Src), typeof(Dst))]
                public partial class AltIgnoreProfile { }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);

        var mapTree = result.GeneratedTrees
            .SingleOrDefault(t => t.FilePath.EndsWith("AltIgnoreProfile.Src_To_Dst.g.cs"));

        mapTree.Should().NotBeNull();
        var sourceText = mapTree!.ToString();
        sourceText.Should().Contain("dst.A = src.A;", "A must be mapped");
        sourceText.Should().NotContain("dst.B",        "B must be skipped due to [DeltaMapperIgnore]");
    }
}
