using FluentAssertions;
using Xunit;

namespace DeltaMapper.UnitTests;

// ── Models ───────────────────────────────────────────────────────────

public class UCF_FlatDto
{
    public int Id { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public bool HasCustomer { get; set; }
}

public class UCF_Order
{
    public int Id { get; set; }
    public UCF_Customer? Customer { get; set; }
}

public class UCF_Customer
{
    public string? Name { get; set; }
    public string? Email { get; set; }
}

file sealed class UnflattenConditionalProfile : Profile
{
    public UnflattenConditionalProfile()
    {
        CreateMap<UCF_FlatDto, UCF_Order>()
            .ForMember(d => d.Customer, o => o.Condition(s => s.HasCustomer));
    }
}

file sealed class UnflattenRoundTripProfile : Profile
{
    public UnflattenRoundTripProfile()
    {
        CreateMap<UCF_Order, UCF_FlatDto>();
        CreateMap<UCF_FlatDto, UCF_Order>();
    }
}

// ── Tests ────────────────────────────────────────────────────────────

public class UnflattenCrossFeatureTests
{
    [Fact]
    public void UCF01_Unflatten_With_Condition_True()
    {
        var mapper = MapperConfiguration.Create(cfg =>
            cfg.AddProfile<UnflattenConditionalProfile>())
            .CreateMapper();

        var dto = new UCF_FlatDto
        {
            Id = 1,
            CustomerName = "Alice",
            CustomerEmail = "alice@test.com",
            HasCustomer = true
        };

        var order = mapper.Map<UCF_FlatDto, UCF_Order>(dto);

        order.Customer.Should().NotBeNull();
        order.Customer!.Name.Should().Be("Alice");
    }

    [Fact]
    public void UCF02_Unflatten_Condition_False_Convention_Still_Maps()
    {
        var mapper = MapperConfiguration.Create(cfg =>
            cfg.AddProfile<UnflattenConditionalProfile>())
            .CreateMapper();

        var dto = new UCF_FlatDto
        {
            Id = 2,
            CustomerName = "Bob",
            HasCustomer = false
        };

        var order = mapper.Map<UCF_FlatDto, UCF_Order>(dto);

        // The Condition on the Customer ForMember config is false, so the explicit
        // member assignment is skipped. However, convention-based unflattening still
        // maps CustomerName → Customer.Name via the prefix convention. The net result
        // is that Customer is populated (via convention), not suppressed entirely.
        // This documents the actual library behavior: Condition guards explicit
        // ForMember rules, not implicit unflattening conventions.
        order.Customer.Should().NotBeNull();
        order.Customer!.Name.Should().Be("Bob");
    }

    [Fact]
    public void UCF03_Flatten_Unflatten_RoundTrip_Preserves_Data()
    {
        var mapper = MapperConfiguration.Create(cfg =>
            cfg.AddProfile<UnflattenRoundTripProfile>())
            .CreateMapper();

        var original = new UCF_Order
        {
            Id = 3,
            Customer = new UCF_Customer { Name = "Carol", Email = "carol@test.com" }
        };

        var flat = mapper.Map<UCF_Order, UCF_FlatDto>(original);
        var restored = mapper.Map<UCF_FlatDto, UCF_Order>(flat);

        restored.Id.Should().Be(original.Id);
        restored.Customer.Should().NotBeNull();
        restored.Customer!.Name.Should().Be("Carol");
        restored.Customer.Email.Should().Be("carol@test.com");
    }
}
