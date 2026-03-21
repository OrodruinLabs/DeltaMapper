using Xunit;

namespace DeltaMapper.UnitTests;

public class AutoMapperParityTests
{
    // Domain entities with DDD patterns
    private class OrderSource
    {
        public Guid? TrackingId { get; set; }
        public string CustomerName { get; set; } = "";
        public List<LineItemSource> Items { get; set; } = [];
    }

    private class LineItemSource
    {
        public string ProductName { get; set; } = "";
        public decimal Price { get; set; }
        public string Currency { get; set; } = "USD";
    }

    private class OrderDto
    {
        public Guid TrackingId { get; set; }      // Feature 4: Guid? → Guid
        public string CustomerName { get; set; } = "";
        public List<LineItemDto> Items { get; set; } = [];
    }

    private class LineItemDto
    {
        public string ProductName { get; set; } = "";
        public MoneyDto Price { get; set; } = null!;   // Feature 3: nested MapFrom
    }

    private class MoneyDto
    {
        public decimal Amount { get; }
        public string Currency { get; }
        private MoneyDto(decimal amount, string currency) { Amount = amount; Currency = currency; }
        public static MoneyDto Create(decimal amount, string currency) => new(amount, currency);
    }

    // Feature 5: inherits from Profile (not MappingProfile)
    private class OrderProfile : Profile
    {
        public OrderProfile()
        {
            // Feature 3: ConstructUsing for MoneyDto
            CreateMap<LineItemSource, MoneyDto>()
                .ConstructUsing(s => MoneyDto.Create(s.Price, s.Currency));

            // Feature 3: nested MapFrom resolves LineItemSource → MoneyDto
            CreateMap<LineItemSource, LineItemDto>()
                .ForMember(d => d.Price, o => o.MapFrom(s => s));

            CreateMap<OrderSource, OrderDto>();
        }
    }

    [Fact]
    public void All_five_features_work_together()
    {
        var mapper = MapperConfiguration.Create(cfg => cfg.AddProfile<OrderProfile>()).CreateMapper();

        var sources = new List<OrderSource>
        {
            new()
            {
                TrackingId = null,  // Feature 4: should become Guid.Empty
                CustomerName = "Alice",
                Items =
                [
                    new() { ProductName = "Widget", Price = 9.99m, Currency = "EUR" },
                    new() { ProductName = "Gadget", Price = 19.99m, Currency = "USD" }
                ]
            },
            new()
            {
                TrackingId = Guid.NewGuid(),
                CustomerName = "Bob",
                Items = []
            }
        };

        // Feature 2: collection Map overload
        List<OrderDto> results = mapper.Map<OrderSource, OrderDto>(sources);

        Assert.Equal(2, results.Count);

        // First order
        Assert.Equal(Guid.Empty, results[0].TrackingId);       // Feature 4
        Assert.Equal("Alice", results[0].CustomerName);
        Assert.Equal(2, results[0].Items.Count);
        Assert.Equal(9.99m, results[0].Items[0].Price.Amount);  // Feature 3 + ConstructUsing
        Assert.Equal("EUR", results[0].Items[0].Price.Currency);
        Assert.Equal(19.99m, results[0].Items[1].Price.Amount);

        // Second order
        Assert.Equal(sources[1].TrackingId!.Value, results[1].TrackingId);
        Assert.Empty(results[1].Items);
    }
}
