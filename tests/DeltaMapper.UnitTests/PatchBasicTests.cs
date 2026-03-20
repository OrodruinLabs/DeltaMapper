using DeltaMapper.Configuration;
using DeltaMapper.Diff;
using DeltaMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace DeltaMapper.UnitTests;

public class PatchBasicTests
{
    // PB-01: Patch with a single changed property returns exactly one PropertyChange with correct From/To/Kind
    [Fact]
    public void Patch_SingleChangedProperty_ReturnsOneChange()
    {
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfile<PB01Profile>());
        var mapper = config.CreateMapper();

        var source = new Product { Name = "Widget Pro", Price = 9.99m, Stock = 5 };
        var destination = new ProductDto { Name = "Widget", Price = 9.99m, Stock = 5 };

        var diff = mapper.Patch<Product, ProductDto>(source, destination);

        diff.HasChanges.Should().BeTrue();
        diff.Changes.Should().HaveCount(1);

        var change = diff.Changes[0];
        change.PropertyName.Should().Be("Name");
        change.From.Should().Be("Widget");
        change.To.Should().Be("Widget Pro");
        change.Kind.Should().Be(ChangeKind.Modified);
    }

    private class PB01Profile : Profile
    {
        public PB01Profile()
        {
            CreateMap<Product, ProductDto>();
        }
    }

    // PB-02: Patch with no property changes returns empty Changes list and HasChanges == false
    [Fact]
    public void Patch_NoChanges_ReturnsEmptyAndHasChangesFalse()
    {
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfile<PB02Profile>());
        var mapper = config.CreateMapper();

        var source = new Product { Name = "Widget", Price = 9.99m, Stock = 5 };
        var destination = new ProductDto { Name = "Widget", Price = 9.99m, Stock = 5 };

        var diff = mapper.Patch<Product, ProductDto>(source, destination);

        diff.HasChanges.Should().BeFalse();
        diff.Changes.Should().BeEmpty();
        diff.Result.Should().BeSameAs(destination);
    }

    private class PB02Profile : Profile
    {
        public PB02Profile()
        {
            CreateMap<Product, ProductDto>();
        }
    }

    // PB-03: Patch with multiple changed properties returns a change for each modified property
    [Fact]
    public void Patch_MultipleChanges_ReturnsAllChanges()
    {
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfile<PB03Profile>());
        var mapper = config.CreateMapper();

        var source = new Product { Name = "Widget Pro", Price = 19.99m, Stock = 100 };
        var destination = new ProductDto { Name = "Widget", Price = 9.99m, Stock = 5 };

        var diff = mapper.Patch<Product, ProductDto>(source, destination);

        diff.HasChanges.Should().BeTrue();
        diff.Changes.Should().HaveCount(3);

        diff.Changes.Should().ContainSingle(c => c.PropertyName == "Name"
            && Equals(c.From, "Widget") && Equals(c.To, "Widget Pro") && c.Kind == ChangeKind.Modified);

        diff.Changes.Should().ContainSingle(c => c.PropertyName == "Price"
            && Equals(c.From, 9.99m) && Equals(c.To, 19.99m) && c.Kind == ChangeKind.Modified);

        diff.Changes.Should().ContainSingle(c => c.PropertyName == "Stock"
            && Equals(c.From, 5) && Equals(c.To, 100) && c.Kind == ChangeKind.Modified);
    }

    private class PB03Profile : Profile
    {
        public PB03Profile()
        {
            CreateMap<Product, ProductDto>();
        }
    }
}
