using DeltaMapper.Abstractions;
using DeltaMapper.Configuration;
using FluentAssertions;
using Xunit;

namespace DeltaMapper.UnitTests;

// ── Test models ────────────────────────────────────────────────────

// Flat source: CustomerName, CustomerEmail, Id
public sealed class FlatOrderSource
{
    public int Id { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? AddressCity { get; set; }
}

// Nested destination: Customer.Name, Customer.Email
public sealed class NestedOrderDestination
{
    public int Id { get; set; }
    public NestedCustomer? Customer { get; set; }
}

public sealed class NestedCustomer
{
    public string? Name { get; set; }
    public string? Email { get; set; }
}

// Mixed: some direct props, some unflattened
public sealed class MixedOrderDestination
{
    public int Id { get; set; }
    public string? CustomerName { get; set; }   // direct (flat source has CustomerName)
    public NestedAddress? Address { get; set; } // unflattened from AddressCity
}

public sealed class NestedAddress
{
    public string? City { get; set; }
}

// For round-trip test: nested → flat → nested
public sealed class RoundTripOrder
{
    public int Id { get; set; }
    public RoundTripCustomer? Customer { get; set; }
}

public sealed class RoundTripCustomer
{
    public string? Name { get; set; }
}

public sealed class RoundTripFlatDto
{
    public int Id { get; set; }
    public string? CustomerName { get; set; }
}

// ── Tests ──────────────────────────────────────────────────────────

public sealed class UnflatteningTests
{
    [Fact]
    public void Unflatten01_BasicUnflattening_CustomerName()
    {
        // CustomerName (flat) → Customer.Name (nested)
        var mapper = MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile(new UnflatInlineProfile<FlatOrderSource, NestedOrderDestination>());
        }).CreateMapper();

        var src = new FlatOrderSource { Id = 1, CustomerName = "Alice" };
        var dst = mapper.Map<FlatOrderSource, NestedOrderDestination>(src);

        dst.Id.Should().Be(1);
        dst.Customer.Should().NotBeNull();
        dst.Customer!.Name.Should().Be("Alice");
    }

    [Fact]
    public void Unflatten02_MultipleProps_CustomerNameAndEmail()
    {
        // CustomerName + CustomerEmail → Customer.Name + Customer.Email
        var mapper = MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile(new UnflatInlineProfile<FlatOrderSource, NestedOrderDestination>());
        }).CreateMapper();

        var src = new FlatOrderSource
        {
            Id = 2,
            CustomerName = "Bob",
            CustomerEmail = "bob@example.com"
        };

        var dst = mapper.Map<FlatOrderSource, NestedOrderDestination>(src);

        dst.Id.Should().Be(2);
        dst.Customer.Should().NotBeNull();
        dst.Customer!.Name.Should().Be("Bob");
        dst.Customer!.Email.Should().Be("bob@example.com");
    }

    [Fact]
    public void Unflatten03_Mixed_DirectAndUnflattened()
    {
        // Id + CustomerName map directly; Address.City is unflattened from AddressCity
        var mapper = MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile(new UnflatInlineProfile<FlatOrderSource, MixedOrderDestination>());
        }).CreateMapper();

        var src = new FlatOrderSource
        {
            Id = 3,
            CustomerName = "Carol",
            AddressCity = "Boston"
        };

        var dst = mapper.Map<FlatOrderSource, MixedOrderDestination>(src);

        dst.Id.Should().Be(3);
        dst.CustomerName.Should().Be("Carol");  // direct mapping
        dst.Address.Should().NotBeNull();
        dst.Address!.City.Should().Be("Boston"); // unflattened
    }

    [Fact]
    public void Unflatten04_SourceHasMatchingPropsButNullValues_CreatesNestedWithNulls()
    {
        // FlatOrderSource declares CustomerName/CustomerEmail (compile-time match exists),
        // so unflattening activates and creates a Customer object — even though values are null.
        var mapper = MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile(new UnflatInlineProfile<FlatOrderSource, NestedOrderDestination>());
        }).CreateMapper();

        var src = new FlatOrderSource { Id = 4 }; // CustomerName/CustomerEmail are null (default)

        var dst = mapper.Map<FlatOrderSource, NestedOrderDestination>(src);

        dst.Id.Should().Be(4);
        dst.Customer.Should().NotBeNull();
        dst.Customer!.Name.Should().BeNull();
        dst.Customer!.Email.Should().BeNull();
    }

    [Fact]
    public void Unflatten05_RoundTrip_FlattenThenUnflatten()
    {
        // Nested → flat (flattening), then flat → nested (unflattening)
        var mapper = MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile(new UnflatInlineProfile<RoundTripOrder, RoundTripFlatDto>());
            cfg.AddProfile(new UnflatInlineProfile<RoundTripFlatDto, RoundTripOrder>());
        }).CreateMapper();

        var original = new RoundTripOrder
        {
            Id = 5,
            Customer = new RoundTripCustomer { Name = "Dave" }
        };

        // Flatten: RoundTripOrder → RoundTripFlatDto
        var flat = mapper.Map<RoundTripOrder, RoundTripFlatDto>(original);
        flat.Id.Should().Be(5);
        flat.CustomerName.Should().Be("Dave");

        // Unflatten: RoundTripFlatDto → RoundTripOrder
        var restored = mapper.Map<RoundTripFlatDto, RoundTripOrder>(flat);
        restored.Id.Should().Be(5);
        restored.Customer.Should().NotBeNull();
        restored.Customer!.Name.Should().Be("Dave");
    }
}

// ── Helpers ────────────────────────────────────────────────────────

file sealed class UnflatInlineProfile<TSrc, TDst> : Profile
{
    public UnflatInlineProfile()
    {
        CreateMap<TSrc, TDst>();
    }
}
