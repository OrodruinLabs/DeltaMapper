using System.Globalization;
using DeltaMapper.Abstractions;
using DeltaMapper.Configuration;
using FluentAssertions;
using Xunit;

namespace DeltaMapper.UnitTests;

// ── Models ───────────────────────────────────────────────────────────

public class CF_Order
{
    public int Id { get; set; }
    public CF_Customer? Customer { get; set; }
    public string? OrderDate { get; set; }
    public bool IsPriority { get; set; }
}

public class CF_Customer
{
    public string? Name { get; set; }
    public string? Email { get; set; }
}

public class CF_OrderFlatDto
{
    public int Id { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public DateTime OrderDate { get; set; }
    public string? PriorityLabel { get; set; }
}

// ── Profile ──────────────────────────────────────────────────────────

file sealed class CrossFeatureProfile : Profile
{
    public CrossFeatureProfile()
    {
        CreateMap<CF_Order, CF_OrderFlatDto>()
            .ForMember(d => d.PriorityLabel, o =>
            {
                o.Condition(s => s.IsPriority);
                o.MapFrom(s => "PRIORITY");
            });
    }
}

// ── Tests ────────────────────────────────────────────────────────────

public class CrossFeatureTests
{
    [Fact]
    public void CF01_Flattening_Plus_TypeConverter()
    {
        var mapper = MapperConfiguration.Create(cfg =>
        {
            cfg.CreateTypeConverter<string, DateTime>(s =>
                DateTime.Parse(s, CultureInfo.InvariantCulture));
            cfg.AddProfile<CrossFeatureProfile>();
        }).CreateMapper();

        var order = new CF_Order
        {
            Id = 1,
            Customer = new CF_Customer { Name = "Alice", Email = "a@b.com" },
            OrderDate = "2026-01-15"
        };

        var dto = mapper.Map<CF_Order, CF_OrderFlatDto>(order);

        dto.CustomerName.Should().Be("Alice");
        dto.CustomerEmail.Should().Be("a@b.com");
        dto.OrderDate.Should().Be(new DateTime(2026, 1, 15));
    }

    [Fact]
    public void CF02_Flattening_Plus_Condition_True()
    {
        var mapper = MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile<CrossFeatureProfile>();
        }).CreateMapper();

        var order = new CF_Order
        {
            Id = 2,
            Customer = new CF_Customer { Name = "Bob" },
            IsPriority = true
        };

        var dto = mapper.Map<CF_Order, CF_OrderFlatDto>(order);

        dto.CustomerName.Should().Be("Bob");
        dto.PriorityLabel.Should().Be("PRIORITY");
    }

    [Fact]
    public void CF03_Flattening_Plus_Condition_False()
    {
        var mapper = MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile<CrossFeatureProfile>();
        }).CreateMapper();

        var order = new CF_Order
        {
            Id = 3,
            Customer = new CF_Customer { Name = "Carol" },
            IsPriority = false
        };

        var dto = mapper.Map<CF_Order, CF_OrderFlatDto>(order);

        dto.CustomerName.Should().Be("Carol");
        dto.PriorityLabel.Should().BeNull();
    }

    [Fact]
    public void CF04_Flattening_NullIntermediate_Plus_TypeConverter()
    {
        var mapper = MapperConfiguration.Create(cfg =>
        {
            cfg.CreateTypeConverter<string, DateTime>(s =>
                DateTime.Parse(s, CultureInfo.InvariantCulture));
            cfg.AddProfile<CrossFeatureProfile>();
        }).CreateMapper();

        var order = new CF_Order { Id = 4, Customer = null, OrderDate = "2026-06-01" };

        var dto = mapper.Map<CF_Order, CF_OrderFlatDto>(order);

        dto.CustomerName.Should().BeNull();
        dto.CustomerEmail.Should().BeNull();
        dto.OrderDate.Should().Be(new DateTime(2026, 6, 1));
    }

    [Fact]
    public void CF05_TypeConverter_Plus_Condition_Both_Active()
    {
        var mapper = MapperConfiguration.Create(cfg =>
        {
            cfg.CreateTypeConverter<string, DateTime>(s =>
                DateTime.Parse(s, CultureInfo.InvariantCulture));
            cfg.AddProfile<CrossFeatureProfile>();
        }).CreateMapper();

        var order = new CF_Order
        {
            Id = 5,
            Customer = new CF_Customer { Name = "Dave" },
            OrderDate = "2026-12-25",
            IsPriority = true
        };

        var dto = mapper.Map<CF_Order, CF_OrderFlatDto>(order);

        dto.CustomerName.Should().Be("Dave");
        dto.OrderDate.Should().Be(new DateTime(2026, 12, 25));
        dto.PriorityLabel.Should().Be("PRIORITY");
    }
}
