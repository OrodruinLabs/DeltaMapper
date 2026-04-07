using FluentAssertions;
using Xunit;
using DeltaMapper.SourceGen.Tests.Helpers;

namespace DeltaMapper.SourceGen.Tests;

/// <summary>
/// Tests that verify [IgnoreMember], [NullSubstitute], and [MapMember] attributes
/// produce the correct emitted code in the generated source.
/// </summary>
public class AttributeEmitTests
{
    // ── IgnoreMember ──────────────────────────────────────────────────────────

    private const string IgnoreMemberSource = """
        using DeltaMapper;

        namespace MyApp
        {
            public class Order
            {
                public int Id { get; set; }
                public string Name { get; set; } = "";
                public string Secret { get; set; } = "";
            }

            public class OrderDto
            {
                public int Id { get; set; }
                public string Name { get; set; } = "";
                public string Secret { get; set; } = "";
            }

            [GenerateMap(typeof(Order), typeof(OrderDto))]
            [IgnoreMember(typeof(Order), typeof(OrderDto), "Secret")]
            public partial class OrderProfile { }
        }
        """;

    [Fact]
    public void IgnoreMember_SuppressesAssignment_InMapMethod()
    {
        var result = GeneratorTestHelper.RunGenerator(IgnoreMemberSource);

        var mapFile = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("OrderProfile.Order_To_OrderDto.g.cs"));

        mapFile.Should().NotBeNull("the map method file must be generated");

        var text = mapFile!.GetText().ToString();
        text.Should().Contain("dst.Id = src.Id;");
        text.Should().Contain("dst.Name = src.Name;");
        text.Should().NotContain("Secret", "IgnoreMember should suppress the Secret assignment");
    }

    [Fact]
    public void IgnoreMember_SuppressesAssignment_InFactoryMethod()
    {
        var result = GeneratorTestHelper.RunGenerator(IgnoreMemberSource);

        var factoryFile = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("OrderProfile.Order_To_OrderDto.Factory.g.cs"));

        factoryFile.Should().NotBeNull("the factory method file must be generated");

        var text = factoryFile!.GetText().ToString();
        text.Should().NotContain("Secret", "IgnoreMember should suppress the Secret assignment in factory");
    }

    // ── NullSubstitute ────────────────────────────────────────────────────────

    private const string NullSubstituteStringSource = """
        using DeltaMapper;

        namespace MyApp
        {
            public class Product
            {
                public string? Description { get; set; }
            }

            public class ProductDto
            {
                public string? Description { get; set; }
            }

            [GenerateMap(typeof(Product), typeof(ProductDto))]
            [NullSubstitute(typeof(Product), typeof(ProductDto), "Description", "N/A")]
            public partial class ProductProfile { }
        }
        """;

    [Fact]
    public void NullSubstitute_EmitsNullCoalescing_WithQuotedStringDefault()
    {
        var result = GeneratorTestHelper.RunGenerator(NullSubstituteStringSource);

        var mapFile = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("ProductProfile.Product_To_ProductDto.g.cs"));

        mapFile.Should().NotBeNull("the map method file must be generated");

        var text = mapFile!.GetText().ToString();
        text.Should().Contain("src.Description ?? \"N/A\"",
            "NullSubstitute should emit ?? with a quoted string literal");
    }

    [Fact]
    public void NullSubstitute_EmitsNullCoalescing_InFactoryMethod()
    {
        var result = GeneratorTestHelper.RunGenerator(NullSubstituteStringSource);

        var factoryFile = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("ProductProfile.Product_To_ProductDto.Factory.g.cs"));

        factoryFile.Should().NotBeNull("the factory method file must be generated");

        var text = factoryFile!.GetText().ToString();
        text.Should().Contain("src.Description ?? \"N/A\"",
            "NullSubstitute should emit ?? in factory method too");
    }

    private const string NullSubstituteIntSource = """
        using DeltaMapper;

        namespace MyApp
        {
            public class Item
            {
                public int? Quantity { get; set; }
            }

            public class ItemDto
            {
                public int? Quantity { get; set; }
            }

            [GenerateMap(typeof(Item), typeof(ItemDto))]
            [NullSubstitute(typeof(Item), typeof(ItemDto), "Quantity", 0)]
            public partial class ItemProfile { }
        }
        """;

    [Fact]
    public void NullSubstitute_EmitsNullCoalescing_WithIntDefault()
    {
        var result = GeneratorTestHelper.RunGenerator(NullSubstituteIntSource);

        var mapFile = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("ItemProfile.Item_To_ItemDto.g.cs"));

        mapFile.Should().NotBeNull();

        var text = mapFile!.GetText().ToString();
        text.Should().Contain("src.Quantity ?? 0",
            "NullSubstitute with int value should emit unquoted integer literal");
    }

    // ── MapMember ─────────────────────────────────────────────────────────────

    private const string MapMemberSource = """
        using DeltaMapper;

        namespace MyApp
        {
            public class Customer
            {
                public string FullName { get; set; } = "";
                public string Email { get; set; } = "";
            }

            public class CustomerDto
            {
                public string DisplayName { get; set; } = "";
                public string Email { get; set; } = "";
            }

            [GenerateMap(typeof(Customer), typeof(CustomerDto))]
            [MapMember(typeof(Customer), typeof(CustomerDto), "DisplayName", "FullName")]
            public partial class CustomerProfile { }
        }
        """;

    [Fact]
    public void MapMember_EmitsRemappedSourceProperty_InMapMethod()
    {
        var result = GeneratorTestHelper.RunGenerator(MapMemberSource);

        var mapFile = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("CustomerProfile.Customer_To_CustomerDto.g.cs"));

        mapFile.Should().NotBeNull("the map method file must be generated");

        var text = mapFile!.GetText().ToString();
        text.Should().Contain("dst.DisplayName = src.FullName;",
            "MapMember should remap FullName -> DisplayName");
        text.Should().Contain("dst.Email = src.Email;",
            "Convention-matched Email should still be mapped");
    }

    [Fact]
    public void MapMember_EmitsRemappedSourceProperty_InFactoryMethod()
    {
        var result = GeneratorTestHelper.RunGenerator(MapMemberSource);

        var factoryFile = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("CustomerProfile.Customer_To_CustomerDto.Factory.g.cs"));

        factoryFile.Should().NotBeNull("the factory method file must be generated");

        var text = factoryFile!.GetText().ToString();
        text.Should().Contain("src.FullName",
            "MapMember should remap FullName as source in factory method");
        text.Should().Contain("DisplayName",
            "DisplayName should appear as destination property in factory method");
    }

    // ── Combined: all three attributes on one profile ─────────────────────────

    private const string CombinedSource = """
        using DeltaMapper;

        namespace MyApp
        {
            public class Source
            {
                public int Id { get; set; }
                public string? Note { get; set; }
                public string OriginalName { get; set; } = "";
                public string Password { get; set; } = "";
            }

            public class Dest
            {
                public int Id { get; set; }
                public string? Note { get; set; }
                public string AliasName { get; set; } = "";
                public string Password { get; set; } = "";
            }

            [GenerateMap(typeof(Source), typeof(Dest))]
            [IgnoreMember(typeof(Source), typeof(Dest), "Password")]
            [NullSubstitute(typeof(Source), typeof(Dest), "Note", "none")]
            [MapMember(typeof(Source), typeof(Dest), "AliasName", "OriginalName")]
            public partial class CombinedProfile { }
        }
        """;

    [Fact]
    public void CombinedAttributes_AllBehaviorsApplied_InMapMethod()
    {
        var result = GeneratorTestHelper.RunGenerator(CombinedSource);

        var mapFile = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("CombinedProfile.Source_To_Dest.g.cs"));

        mapFile.Should().NotBeNull();

        var text = mapFile!.GetText().ToString();

        // IgnoreMember: Password should be suppressed
        text.Should().NotContain("Password",
            "IgnoreMember should suppress the Password assignment");

        // NullSubstitute: Note emits with ?? fallback
        text.Should().Contain("src.Note ?? \"none\"",
            "NullSubstitute should emit ?? with quoted string");

        // MapMember: AliasName maps from OriginalName
        text.Should().Contain("src.OriginalName",
            "MapMember should use OriginalName as source");
        text.Should().Contain("AliasName",
            "MapMember destination AliasName should appear");
    }

    // ── FormatConstantLiteral unit tests ──────────────────────────────────────

    [Theory]
    [InlineData("hello", "\"hello\"")]
    [InlineData("say \"hi\"", "\"say \\\"hi\\\"\"")]
    [InlineData("back\\slash", "\"back\\\\slash\"")]
    public void FormatConstantLiteral_String_WrapsInDoubleQuotes(string input, string expected)
    {
        EmitHelper.FormatConstantLiteral(input).Should().Be(expected);
    }

    [Fact]
    public void FormatConstantLiteral_Null_ReturnsNullKeyword()
    {
        EmitHelper.FormatConstantLiteral(null).Should().Be("null");
    }

    [Fact]
    public void FormatConstantLiteral_Int_ReturnsPlainNumber()
    {
        EmitHelper.FormatConstantLiteral(42).Should().Be("42");
    }

    [Fact]
    public void FormatConstantLiteral_Bool_ReturnsLowerCaseKeyword()
    {
        EmitHelper.FormatConstantLiteral(true).Should().Be("true");
        EmitHelper.FormatConstantLiteral(false).Should().Be("false");
    }

    [Fact]
    public void FormatConstantLiteral_Long_AppendsSuffix()
    {
        EmitHelper.FormatConstantLiteral(100L).Should().Be("100L");
    }

    [Fact]
    public void FormatConstantLiteral_Float_AppendsFSuffix()
    {
        EmitHelper.FormatConstantLiteral(1.5f).Should().Be("1.5f");
    }
}
