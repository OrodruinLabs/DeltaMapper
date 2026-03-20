using DeltaMapper.Abstractions;
using DeltaMapper.Configuration;
using FluentAssertions;
using Xunit;

namespace DeltaMapper.UnitTests;

// ── Test models ────────────────────────────────────────────────────

public class FlatOrder
{
    public int Id { get; set; }
    public FlatCustomer? Customer { get; set; }
    public FlatAddress? Address { get; set; }
}

public class FlatCustomer
{
    public string? Name { get; set; }
    public FlatAddress? Address { get; set; }
}

public class FlatAddress
{
    public string? City { get; set; }
    public string? Zip { get; set; }
}

// Single-level flattening destination (Order.Customer.Name → CustomerName)
public class OrderFlatDto
{
    public int Id { get; set; }
    public string? CustomerName { get; set; }
    public string? AddressCity { get; set; }
}

// Multi-level flattening destination (Order.Customer.Address.Zip → CustomerAddressZip)
public class OrderDeepFlatDto
{
    public int Id { get; set; }
    public string? CustomerAddressZip { get; set; }
}

// Value-type flattened property (tests null intermediate → value type destination)
public class FlatCustomerWithAge
{
    public string? Name { get; set; }
    public int Age { get; set; }
}

public class OrderWithAgeCustomer
{
    public int Id { get; set; }
    public FlatCustomerWithAge? Customer { get; set; }
}

public class OrderValueTypeFlatDto
{
    public int Id { get; set; }
    public int CustomerAge { get; set; }
}

// Mixed: some properties flat (convention), some flattened
public class OrderMixedDto
{
    public int Id { get; set; }
    public string? CustomerName { get; set; }   // flattened
    public FlatAddress? Address { get; set; }   // convention (direct)
}

// Incompatible flattened type (Order.Customer.Name is string, CustomerName is int)
public class OrderIncompatibleDto
{
    public int Id { get; set; }
    public int CustomerName { get; set; } // incompatible: source Customer.Name is string
}

// ── Tests ──────────────────────────────────────────────────────────

public class FlatteningTests
{
    [Fact]
    public void Flat01_BasicFlattening_CustomerName()
    {
        // Order.Customer.Name → CustomerName
        var mapper = MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile(new FlatInlineProfile<FlatOrder, OrderFlatDto>());
        }).CreateMapper();

        var src = new FlatOrder
        {
            Id = 1,
            Customer = new FlatCustomer { Name = "Alice" }
        };

        var dst = mapper.Map<FlatOrder, OrderFlatDto>(src);

        dst.Id.Should().Be(1);
        dst.CustomerName.Should().Be("Alice");
    }

    [Fact]
    public void Flat02_BasicFlattening_AddressCity()
    {
        // Order.Address.City → AddressCity
        var mapper = MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile(new FlatInlineProfile<FlatOrder, OrderFlatDto>());
        }).CreateMapper();

        var src = new FlatOrder
        {
            Id = 2,
            Address = new FlatAddress { City = "New York" }
        };

        var dst = mapper.Map<FlatOrder, OrderFlatDto>(src);

        dst.Id.Should().Be(2);
        dst.AddressCity.Should().Be("New York");
    }

    [Fact]
    public void Flat03_MultiLevel_CustomerAddressZip()
    {
        // Order.Customer.Address.Zip → CustomerAddressZip
        var mapper = MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile(new FlatInlineProfile<FlatOrder, OrderDeepFlatDto>());
        }).CreateMapper();

        var src = new FlatOrder
        {
            Id = 3,
            Customer = new FlatCustomer
            {
                Name = "Bob",
                Address = new FlatAddress { Zip = "10001" }
            }
        };

        var dst = mapper.Map<FlatOrder, OrderDeepFlatDto>(src);

        dst.Id.Should().Be(3);
        dst.CustomerAddressZip.Should().Be("10001");
    }

    [Fact]
    public void Flat04_NoMatch_PropertySkipped()
    {
        // Destination property with no matching source chain is silently skipped
        var mapper = MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile(new FlatInlineProfile<FlatOrder, OrderFlatDto>());
        }).CreateMapper();

        // No Customer or Address on source → flattened properties should remain null (default)
        var src = new FlatOrder { Id = 4 };

        var dst = mapper.Map<FlatOrder, OrderFlatDto>(src);

        dst.Id.Should().Be(4);
        dst.CustomerName.Should().BeNull();
        dst.AddressCity.Should().BeNull();
    }

    [Fact]
    public void Flat05_NullIntermediate_ReturnsNull()
    {
        // Customer is null → CustomerName should be null (not throw)
        var mapper = MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile(new FlatInlineProfile<FlatOrder, OrderFlatDto>());
        }).CreateMapper();

        var src = new FlatOrder { Id = 5, Customer = null };

        var dst = mapper.Map<FlatOrder, OrderFlatDto>(src);

        dst.Id.Should().Be(5);
        dst.CustomerName.Should().BeNull();
    }

    [Fact]
    public void Flat06_Mixed_FlatAndFlattenedProperties()
    {
        // Id + Address map by convention; CustomerName is flattened
        var mapper = MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile(new FlatInlineProfile<FlatOrder, OrderMixedDto>());
        }).CreateMapper();

        var address = new FlatAddress { City = "Boston", Zip = "02101" };
        var src = new FlatOrder
        {
            Id = 6,
            Customer = new FlatCustomer { Name = "Carol" },
            Address = address
        };

        var dst = mapper.Map<FlatOrder, OrderMixedDto>(src);

        dst.Id.Should().Be(6);
        dst.CustomerName.Should().Be("Carol");       // flattened
        dst.Address.Should().BeSameAs(address);      // direct convention mapping
    }
    [Fact]
    public void Flat07_NullIntermediate_ValueTypeDestination_DefaultsToZero()
    {
        // Customer is null → CustomerAge (int) should remain default (0), not throw NullReferenceException
        var mapper = MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile(new FlatInlineProfile<OrderWithAgeCustomer, OrderValueTypeFlatDto>());
        }).CreateMapper();

        var src = new OrderWithAgeCustomer { Id = 7, Customer = null };

        var dst = mapper.Map<OrderWithAgeCustomer, OrderValueTypeFlatDto>(src);

        dst.Id.Should().Be(7);
        dst.CustomerAge.Should().Be(0); // default(int), not crash
    }

    [Fact]
    public void Flat08_IncompatibleFlattenedType_PropertySkipped()
    {
        // Order.Customer.Name is string, destination CustomerName is int
        // Flattening should skip incompatible property gracefully
        var mapper = MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile(new FlatInlineProfile<FlatOrder, OrderIncompatibleDto>());
        }).CreateMapper();

        var src = new FlatOrder
        {
            Id = 8,
            Customer = new FlatCustomer { Name = "Alice" }
        };

        var dst = mapper.Map<FlatOrder, OrderIncompatibleDto>(src);

        dst.Id.Should().Be(8);
        dst.CustomerName.Should().Be(0); // default(int), incompatible type skipped
    }
}

// ── Helpers ────────────────────────────────────────────────────────

/// <summary>
/// Inline profile that creates a convention-only map (no ForMember customisation).
/// Used by flattening tests to verify automatic discovery.
/// </summary>
file sealed class FlatInlineProfile<TSrc, TDst> : Profile
{
    public FlatInlineProfile()
    {
        CreateMap<TSrc, TDst>();
    }
}
