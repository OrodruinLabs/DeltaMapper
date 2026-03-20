using System.Text.Json;
using DeltaMapper.Configuration;
using DeltaMapper.Diff;
using DeltaMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace DeltaMapper.UnitTests;

public class PatchEdgeCaseTests
{
    // PE-01: NullSubstitute — null source property is replaced, diff captures the substituted value
    [Fact]
    public void Patch_NullSubstitute_CapturesSubstitutedValue()
    {
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfile<NullSubProfile>());
        var mapper = config.CreateMapper();

        var source = new ProductWithNullable { Name = "Widget", Price = 9.99m, Nickname = null };
        var destination = new ProductWithNullableDto { Name = "Widget", Price = 9.99m, Nickname = "OldNick" };

        var diff = mapper.Patch<ProductWithNullable, ProductWithNullableDto>(source, destination);

        diff.HasChanges.Should().BeTrue();
        diff.Changes.Should().ContainSingle(c =>
            c.PropertyName == "Nickname"
            && Equals(c.From, "OldNick")
            && Equals(c.To, "N/A")
            && c.Kind == ChangeKind.Modified);
    }

    private class NullSubProfile : Profile
    {
        public NullSubProfile()
        {
            CreateMap<ProductWithNullable, ProductWithNullableDto>()
                .ForMember(d => d.Nickname, o => o.NullSubstitute("N/A"));
        }
    }

    // PE-02: MappingDiff<T> serializes to JSON and back with System.Text.Json
    [Fact]
    public void MappingDiff_SerializesToJson_RoundTrips()
    {
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfile<ProductProfile>());
        var mapper = config.CreateMapper();

        var source = new Product { Name = "Gadget", Price = 19.99m, Stock = 50 };
        var destination = new ProductDto { Name = "Widget", Price = 9.99m, Stock = 50 };

        var diff = mapper.Patch<Product, ProductDto>(source, destination);

        var json = JsonSerializer.Serialize(diff);
        json.Should().NotBeNullOrWhiteSpace();

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.TryGetProperty("Result", out _).Should().BeTrue();
        root.TryGetProperty("Changes", out var changesElement).Should().BeTrue();
        changesElement.GetArrayLength().Should().Be(2); // Name + Price changed
        root.TryGetProperty("HasChanges", out var hasChanges).Should().BeTrue();
        hasChanges.GetBoolean().Should().BeTrue();
    }

    private class ProductProfile : Profile
    {
        public ProductProfile()
        {
            CreateMap<Product, ProductDto>();
        }
    }

    // PE-03: All properties ignored — Patch returns empty changes
    [Fact]
    public void Patch_AllPropertiesIgnored_ReturnsNoChanges()
    {
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfile<AllIgnoredProfile>());
        var mapper = config.CreateMapper();

        var source = new Product { Name = "Gadget", Price = 19.99m, Stock = 200 };
        var destination = new ProductDto { Name = "Widget", Price = 9.99m, Stock = 50 };

        var diff = mapper.Patch<Product, ProductDto>(source, destination);

        diff.HasChanges.Should().BeFalse();
        diff.Changes.Should().BeEmpty();
        // Destination should be unchanged
        diff.Result.Name.Should().Be("Widget");
        diff.Result.Price.Should().Be(9.99m);
        diff.Result.Stock.Should().Be(50);
    }

    private class AllIgnoredProfile : Profile
    {
        public AllIgnoredProfile()
        {
            CreateMap<Product, ProductDto>()
                .ForMember(d => d.Name, o => o.Ignore())
                .ForMember(d => d.Price, o => o.Ignore())
                .ForMember(d => d.Stock, o => o.Ignore());
        }
    }
}
