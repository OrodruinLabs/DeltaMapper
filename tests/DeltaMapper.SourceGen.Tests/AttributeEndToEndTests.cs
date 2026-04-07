using FluentAssertions;
using Microsoft.CodeAnalysis;
using Xunit;
using DeltaMapper.SourceGen.Tests.Helpers;

namespace DeltaMapper.SourceGen.Tests;

/// <summary>
/// End-to-end tests verifying that IgnoreMember, NullSubstitute, and MapMember
/// attributes work together correctly across combined profiles, multi-pair scoping,
/// and edge-case scenarios. Compilation must remain error-free in all cases.
/// </summary>
public class AttributeEndToEndTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Combined usage: all three attributes on one profile — compilation check
    // ─────────────────────────────────────────────────────────────────────────

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
    public void CombinedAttributes_OutputCompilation_HasNoErrors()
    {
        var (_, outputCompilation) = GeneratorTestHelper.RunGeneratorWithCompilation(CombinedSource);

        var errors = outputCompilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        errors.Should().BeEmpty(
            $"combined-attribute profile must compile with no errors but got: {string.Join(", ", errors.Select(e => e.GetMessage()))}");
    }

    [Fact]
    public void CombinedAttributes_AllBehaviorsApplied_InFactoryMethod()
    {
        var result = GeneratorTestHelper.RunGenerator(CombinedSource);

        var factoryFile = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("CombinedProfile.Source_To_Dest.Factory.g.cs"));

        factoryFile.Should().NotBeNull("the factory method file must be generated");

        var text = factoryFile!.GetText().ToString();

        text.Should().NotContain("Password",
            "IgnoreMember should suppress Password in the factory method too");

        text.Should().Contain("src.Note ?? \"none\"",
            "NullSubstitute should emit ?? with quoted string in the factory method");

        text.Should().Contain("src.OriginalName",
            "MapMember should use OriginalName as source expression in the factory method");

        text.Should().Contain("AliasName",
            "AliasName should appear as destination in the factory method");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Multi-pair scoping: attributes must only affect the targeted (src, dst) pair
    // ─────────────────────────────────────────────────────────────────────────

    private const string MultiPairSource = """
        using DeltaMapper;

        namespace MyApp
        {
            public class Alpha
            {
                public int Id { get; set; }
                public string Secret { get; set; } = "";
                public string Name { get; set; } = "";
            }

            public class AlphaDto
            {
                public int Id { get; set; }
                public string Secret { get; set; } = "";
                public string Name { get; set; } = "";
            }

            public class Beta
            {
                public int Id { get; set; }
                public string Secret { get; set; } = "";
                public string Name { get; set; } = "";
            }

            public class BetaDto
            {
                public int Id { get; set; }
                public string Secret { get; set; } = "";
                public string Name { get; set; } = "";
            }

            // IgnoreMember targets Alpha->AlphaDto only; Beta->BetaDto should be unaffected.
            [GenerateMap(typeof(Alpha), typeof(AlphaDto))]
            [GenerateMap(typeof(Beta), typeof(BetaDto))]
            [IgnoreMember(typeof(Alpha), typeof(AlphaDto), "Secret")]
            public partial class MultiPairProfile { }
        }
        """;

    [Fact]
    public void MultiPairProfile_IgnoreMember_SuppressesOnlyTargetedPair()
    {
        var result = GeneratorTestHelper.RunGenerator(MultiPairSource);

        var alphaFile = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("MultiPairProfile.Alpha_To_AlphaDto.g.cs"));

        alphaFile.Should().NotBeNull("Alpha_To_AlphaDto file must be generated");
        var alphaText = alphaFile!.GetText().ToString();

        alphaText.Should().NotContain("Secret",
            "IgnoreMember targeting Alpha->AlphaDto must suppress Secret in that pair");
        alphaText.Should().Contain("dst.Name = src.Name;",
            "Name is not ignored and must still be mapped in the Alpha pair");
    }

    [Fact]
    public void MultiPairProfile_IgnoreMember_DoesNotAffectUntargetedPair()
    {
        var result = GeneratorTestHelper.RunGenerator(MultiPairSource);

        var betaFile = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("MultiPairProfile.Beta_To_BetaDto.g.cs"));

        betaFile.Should().NotBeNull("Beta_To_BetaDto file must be generated");
        var betaText = betaFile!.GetText().ToString();

        betaText.Should().Contain("Secret",
            "IgnoreMember scoped to Alpha->AlphaDto must NOT suppress Secret in the Beta pair");
        betaText.Should().Contain("dst.Name = src.Name;",
            "Name must be mapped normally in the untargeted Beta pair");
    }

    [Fact]
    public void MultiPairProfile_OutputCompilation_HasNoErrors()
    {
        var (_, outputCompilation) = GeneratorTestHelper.RunGeneratorWithCompilation(MultiPairSource);

        var errors = outputCompilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        errors.Should().BeEmpty(
            $"multi-pair profile must compile with no errors but got: {string.Join(", ", errors.Select(e => e.GetMessage()))}");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Edge case: NullSubstitute scoping across two pairs
    // ─────────────────────────────────────────────────────────────────────────

    private const string NullSubstituteMultiPairSource = """
        using DeltaMapper;

        namespace MyApp
        {
            public class OrderA
            {
                public string? Remark { get; set; }
                public string? Tag { get; set; }
            }

            public class OrderADto
            {
                public string? Remark { get; set; }
                public string? Tag { get; set; }
            }

            public class OrderB
            {
                public string? Remark { get; set; }
            }

            public class OrderBDto
            {
                public string? Remark { get; set; }
            }

            // NullSubstitute targets Remark only in OrderA->OrderADto
            [GenerateMap(typeof(OrderA), typeof(OrderADto))]
            [GenerateMap(typeof(OrderB), typeof(OrderBDto))]
            [NullSubstitute(typeof(OrderA), typeof(OrderADto), "Remark", "n/a")]
            public partial class OrderProfile { }
        }
        """;

    [Fact]
    public void NullSubstitute_MultiPair_AppliesOnlyToTargetedPair()
    {
        var result = GeneratorTestHelper.RunGenerator(NullSubstituteMultiPairSource);

        var orderAFile = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("OrderProfile.OrderA_To_OrderADto.g.cs"));

        orderAFile.Should().NotBeNull("OrderA_To_OrderADto must be generated");
        var aText = orderAFile!.GetText().ToString();
        aText.Should().Contain("src.Remark ?? \"n/a\"",
            "NullSubstitute targeting OrderA->OrderADto must apply to Remark in that pair");

        var orderBFile = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("OrderProfile.OrderB_To_OrderBDto.g.cs"));

        orderBFile.Should().NotBeNull("OrderB_To_OrderBDto must be generated");
        var bText = orderBFile!.GetText().ToString();
        bText.Should().NotContain("?? \"n/a\"",
            "NullSubstitute targeting OrderA->OrderADto must NOT emit fallback in the OrderB pair");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Edge case: IgnoreMember on a convention-matched property
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void IgnoreMember_ConventionMatchedProperty_AssignmentIsSuppressed()
    {
        const string source = """
            using DeltaMapper;

            namespace MyApp
            {
                public class Employee
                {
                    public int Id { get; set; }
                    public string Name { get; set; } = "";
                    public string Department { get; set; } = "";
                }

                public class EmployeeDto
                {
                    public int Id { get; set; }
                    public string Name { get; set; } = "";
                    public string Department { get; set; } = "";
                }

                // Department would normally be convention-matched (same name, same type)
                [GenerateMap(typeof(Employee), typeof(EmployeeDto))]
                [IgnoreMember(typeof(Employee), typeof(EmployeeDto), "Department")]
                public partial class EmployeeProfile { }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);

        var mapFile = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("EmployeeProfile.Employee_To_EmployeeDto.g.cs"));

        mapFile.Should().NotBeNull();
        var text = mapFile!.GetText().ToString();

        text.Should().Contain("dst.Id = src.Id;", "Id must still be mapped");
        text.Should().Contain("dst.Name = src.Name;", "Name must still be mapped");
        text.Should().NotContain("Department",
            "Department is convention-matched but IgnoreMember overrides that and suppresses the assignment");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Edge case: NullSubstitute with bool type
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void NullSubstitute_BoolType_EmitsLowerCaseLiteral()
    {
        const string source = """
            using DeltaMapper;

            namespace MyApp
            {
                public class Config
                {
                    public bool? IsEnabled { get; set; }
                }

                public class ConfigDto
                {
                    public bool? IsEnabled { get; set; }
                }

                [GenerateMap(typeof(Config), typeof(ConfigDto))]
                [NullSubstitute(typeof(Config), typeof(ConfigDto), "IsEnabled", false)]
                public partial class ConfigProfile { }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);

        var mapFile = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("ConfigProfile.Config_To_ConfigDto.g.cs"));

        mapFile.Should().NotBeNull();
        var text = mapFile!.GetText().ToString();

        text.Should().Contain("src.IsEnabled ?? false",
            "NullSubstitute with bool false should emit lowercase 'false' keyword literal");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Edge case: multiple IgnoreMember attributes of the same kind on one class
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void MultipleIgnoreMember_SuppressesAllTargetedProperties()
    {
        const string source = """
            using DeltaMapper;

            namespace MyApp
            {
                public class Payload
                {
                    public int Id { get; set; }
                    public string Token { get; set; } = "";
                    public string Hash { get; set; } = "";
                    public string Value { get; set; } = "";
                }

                public class PayloadDto
                {
                    public int Id { get; set; }
                    public string Token { get; set; } = "";
                    public string Hash { get; set; } = "";
                    public string Value { get; set; } = "";
                }

                [GenerateMap(typeof(Payload), typeof(PayloadDto))]
                [IgnoreMember(typeof(Payload), typeof(PayloadDto), "Token")]
                [IgnoreMember(typeof(Payload), typeof(PayloadDto), "Hash")]
                public partial class PayloadProfile { }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);

        var mapFile = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("PayloadProfile.Payload_To_PayloadDto.g.cs"));

        mapFile.Should().NotBeNull();
        var text = mapFile!.GetText().ToString();

        text.Should().Contain("dst.Id = src.Id;", "Id must be mapped");
        text.Should().Contain("dst.Value = src.Value;", "Value must be mapped");
        text.Should().NotContain("Token", "first IgnoreMember must suppress Token");
        text.Should().NotContain("Hash", "second IgnoreMember must suppress Hash");
    }

    [Fact]
    public void MultipleIgnoreMember_OutputCompilation_HasNoErrors()
    {
        const string source = """
            using DeltaMapper;

            namespace MyApp
            {
                public class Payload
                {
                    public int Id { get; set; }
                    public string Token { get; set; } = "";
                    public string Hash { get; set; } = "";
                    public string Value { get; set; } = "";
                }

                public class PayloadDto
                {
                    public int Id { get; set; }
                    public string Token { get; set; } = "";
                    public string Hash { get; set; } = "";
                    public string Value { get; set; } = "";
                }

                [GenerateMap(typeof(Payload), typeof(PayloadDto))]
                [IgnoreMember(typeof(Payload), typeof(PayloadDto), "Token")]
                [IgnoreMember(typeof(Payload), typeof(PayloadDto), "Hash")]
                public partial class PayloadProfile { }
            }
            """;

        var (_, outputCompilation) = GeneratorTestHelper.RunGeneratorWithCompilation(source);

        var errors = outputCompilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        errors.Should().BeEmpty(
            $"multiple IgnoreMember profile must compile with no errors but got: {string.Join(", ", errors.Select(e => e.GetMessage()))}");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Edge case: MapMember between properties with different names but same type
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void MapMember_DifferentNamesSameType_RemapsCorrectly()
    {
        const string source = """
            using DeltaMapper;

            namespace MyApp
            {
                public class Invoice
                {
                    public string ClientName { get; set; } = "";
                    public decimal TotalAmount { get; set; }
                }

                public class InvoiceDto
                {
                    public string RecipientName { get; set; } = "";
                    public decimal TotalAmount { get; set; }
                }

                // ClientName and RecipientName have the same type (string) but different names
                [GenerateMap(typeof(Invoice), typeof(InvoiceDto))]
                [MapMember(typeof(Invoice), typeof(InvoiceDto), "RecipientName", "ClientName")]
                public partial class InvoiceProfile { }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);

        var mapFile = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("InvoiceProfile.Invoice_To_InvoiceDto.g.cs"));

        mapFile.Should().NotBeNull();
        var text = mapFile!.GetText().ToString();

        text.Should().Contain("dst.RecipientName = src.ClientName;",
            "MapMember should remap ClientName (source) to RecipientName (destination)");
        text.Should().Contain("dst.TotalAmount = src.TotalAmount;",
            "Convention-matched TotalAmount must still be emitted");
    }

    [Fact]
    public void MapMember_DifferentNamesSameType_OutputCompilation_HasNoErrors()
    {
        const string source = """
            using DeltaMapper;

            namespace MyApp
            {
                public class Invoice
                {
                    public string ClientName { get; set; } = "";
                    public decimal TotalAmount { get; set; }
                }

                public class InvoiceDto
                {
                    public string RecipientName { get; set; } = "";
                    public decimal TotalAmount { get; set; }
                }

                [GenerateMap(typeof(Invoice), typeof(InvoiceDto))]
                [MapMember(typeof(Invoice), typeof(InvoiceDto), "RecipientName", "ClientName")]
                public partial class InvoiceProfile { }
            }
            """;

        var (_, outputCompilation) = GeneratorTestHelper.RunGeneratorWithCompilation(source);

        var errors = outputCompilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        errors.Should().BeEmpty(
            $"MapMember profile must compile with no errors but got: {string.Join(", ", errors.Select(e => e.GetMessage()))}");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Edge case: multiple MapMember attributes (rename two properties)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void MultipleMapMember_BothRemappingsApplied()
    {
        const string source = """
            using DeltaMapper;

            namespace MyApp
            {
                public class Report
                {
                    public string AuthorName { get; set; } = "";
                    public string SummaryText { get; set; } = "";
                    public int Version { get; set; }
                }

                public class ReportDto
                {
                    public string WrittenBy { get; set; } = "";
                    public string Abstract { get; set; } = "";
                    public int Version { get; set; }
                }

                [GenerateMap(typeof(Report), typeof(ReportDto))]
                [MapMember(typeof(Report), typeof(ReportDto), "WrittenBy", "AuthorName")]
                [MapMember(typeof(Report), typeof(ReportDto), "Abstract", "SummaryText")]
                public partial class ReportProfile { }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);

        var mapFile = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("ReportProfile.Report_To_ReportDto.g.cs"));

        mapFile.Should().NotBeNull();
        var text = mapFile!.GetText().ToString();

        text.Should().Contain("dst.WrittenBy = src.AuthorName;",
            "first MapMember must remap AuthorName -> WrittenBy");
        text.Should().Contain("dst.Abstract = src.SummaryText;",
            "second MapMember must remap SummaryText -> Abstract");
        text.Should().Contain("dst.Version = src.Version;",
            "convention-matched Version must still be emitted");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Full regression: all existing tests types still compile cleanly
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Regression guard: a profile using all three attribute types together
    /// must produce an output compilation with zero errors, verifying no
    /// regressions were introduced in the emit pipeline by FEAT-017.
    /// </summary>
    [Fact]
    public void Regression_AllThreeAttributeTypes_ProduceCleanCompilation()
    {
        const string source = """
            using DeltaMapper;

            namespace Regression
            {
                public class Src
                {
                    public int Id { get; set; }
                    public string? Label { get; set; }
                    public string SourceProp { get; set; } = "";
                    public string Excluded { get; set; } = "";
                }

                public class Dst
                {
                    public int Id { get; set; }
                    public string? Label { get; set; }
                    public string TargetProp { get; set; } = "";
                    public string Excluded { get; set; } = "";
                }

                [GenerateMap(typeof(Src), typeof(Dst))]
                [IgnoreMember(typeof(Src), typeof(Dst), "Excluded")]
                [NullSubstitute(typeof(Src), typeof(Dst), "Label", "default")]
                [MapMember(typeof(Src), typeof(Dst), "TargetProp", "SourceProp")]
                public partial class RegressionProfile { }
            }
            """;

        var (runResult, outputCompilation) = GeneratorTestHelper.RunGeneratorWithCompilation(source);

        // No generator errors
        runResult.Diagnostics.Should().NotContain(
            d => d.Severity == DiagnosticSeverity.Error,
            "the generator itself must not emit errors");

        // Output compilation is clean
        var errors = outputCompilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        errors.Should().BeEmpty(
            $"regression profile output must compile cleanly but got: {string.Join(", ", errors.Select(e => e.GetMessage()))}");

        // Verify all three attribute behaviors are present
        var mapFile = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("RegressionProfile.Src_To_Dst.g.cs"));

        mapFile.Should().NotBeNull("the map method file must be generated");
        var text = mapFile!.GetText().ToString();

        text.Should().NotContain("Excluded", "IgnoreMember must suppress Excluded");
        text.Should().Contain("src.Label ?? \"default\"", "NullSubstitute must apply ?? fallback");
        text.Should().Contain("src.SourceProp", "MapMember must reference SourceProp as source");
        text.Should().Contain("TargetProp", "MapMember must map into TargetProp");
    }
}
