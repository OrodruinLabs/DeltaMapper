using DeltaMapper.Configuration;
using DeltaMapper.Diff;
using DeltaMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace DeltaMapper.UnitTests;

public class PatchNestedTests
{
    // PN-01: Patch on a Warehouse/WarehouseDto where only Address.City changes
    //        should emit a single change with dot-notation path "Address.City"
    [Fact]
    public void Patch_NestedObjectChange_UsesDotNotation()
    {
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfile<PN01Profile>());
        var mapper = config.CreateMapper();

        var source = new Warehouse
        {
            Name = "West Hub",
            Address = new Address { Street = "1 Oak Ave", City = "Austin", Zip = "73301" }
        };
        var destination = new WarehouseDto
        {
            Name = "West Hub",
            Address = new AddressDto { Street = "1 Oak Ave", City = "Dallas", Zip = "73301" }
        };

        var diff = mapper.Patch<Warehouse, WarehouseDto>(source, destination);

        diff.HasChanges.Should().BeTrue();
        diff.Changes.Should().HaveCount(1);

        var change = diff.Changes[0];
        change.PropertyName.Should().Be("Address.City");
        change.From.Should().Be("Dallas");
        change.To.Should().Be("Austin");
        change.Kind.Should().Be(ChangeKind.Modified);
    }

    private class PN01Profile : MappingProfile
    {
        public PN01Profile()
        {
            CreateMap<Address, AddressDto>();
            CreateMap<Warehouse, WarehouseDto>();
        }
    }

    // PN-02: Patch on a Warehouse/WarehouseDto with no changes returns empty Changes list
    [Fact]
    public void Patch_NestedObjectNoChanges_ReturnsEmpty()
    {
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfile<PN02Profile>());
        var mapper = config.CreateMapper();

        var source = new Warehouse
        {
            Name = "East Hub",
            Address = new Address { Street = "5 Pine Rd", City = "Denver", Zip = "80203" }
        };
        var destination = new WarehouseDto
        {
            Name = "East Hub",
            Address = new AddressDto { Street = "5 Pine Rd", City = "Denver", Zip = "80203" }
        };

        var diff = mapper.Patch<Warehouse, WarehouseDto>(source, destination);

        diff.HasChanges.Should().BeFalse();
        diff.Changes.Should().BeEmpty();
        diff.Result.Should().BeSameAs(destination);
    }

    private class PN02Profile : MappingProfile
    {
        public PN02Profile()
        {
            CreateMap<Address, AddressDto>();
            CreateMap<Warehouse, WarehouseDto>();
        }
    }

    // PN-03: Patch where destination Address is null and source Address is set
    //        should emit a single Modified change for the whole "Address" property
    //        (null-to-value case — not recursed into)
    [Fact]
    public void Patch_NestedNullToValue_EmitsModifiedChange()
    {
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfile<PN03Profile>());
        var mapper = config.CreateMapper();

        var source = new Warehouse
        {
            Name = "South Hub",
            Address = new Address { Street = "9 Elm St", City = "Miami", Zip = "33101" }
        };
        var destination = new WarehouseDto
        {
            Name = "South Hub",
            Address = null!
        };

        var diff = mapper.Patch<Warehouse, WarehouseDto>(source, destination);

        diff.HasChanges.Should().BeTrue();
        diff.Changes.Should().ContainSingle(c =>
            c.PropertyName == "Address"
            && c.From == null
            && c.Kind == ChangeKind.Modified);
        diff.Changes[0].To.Should().NotBeNull();
    }

    private class PN03Profile : MappingProfile
    {
        public PN03Profile()
        {
            CreateMap<Address, AddressDto>();
            CreateMap<Warehouse, WarehouseDto>();
        }
    }
}
