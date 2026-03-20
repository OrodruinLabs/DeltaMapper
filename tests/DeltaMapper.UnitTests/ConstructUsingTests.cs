using Xunit;

namespace DeltaMapper.UnitTests;

public class ConstructUsingTests
{
    // DDD entity with private ctor + static factory
    private class OrderSource
    {
        public string CustomerId { get; set; } = "";
        public decimal Total { get; set; }
        public string Currency { get; set; } = "USD";
    }

    private class Money
    {
        public decimal Amount { get; }
        public string Currency { get; }
        private Money(decimal amount, string currency)
        {
            Amount = amount;
            Currency = currency;
        }
        public static Money Create(decimal amount, string currency) => new(amount, currency);
    }

    private class Order
    {
        public string CustomerId { get; set; } = "";
        public Money Price { get; set; } = null!;
    }

    private class FactoryProfile : Profile
    {
        public FactoryProfile()
        {
            CreateMap<OrderSource, Money>()
                .ConstructUsing(src => Money.Create(src.Total, src.Currency));

            CreateMap<OrderSource, Order>()
                .ForMember(d => d.Price, o => o.MapFrom(s => s));
        }
    }

    [Fact]
    public void ConstructUsing_calls_factory_method()
    {
        var mapper = MapperConfiguration.Create(cfg => cfg.AddProfile<FactoryProfile>()).CreateMapper();
        var result = mapper.Map<OrderSource, Money>(new OrderSource { Total = 99.99m, Currency = "EUR" });

        Assert.Equal(99.99m, result.Amount);
        Assert.Equal("EUR", result.Currency);
    }

    [Fact]
    public void ConstructUsing_with_ForMember_applies_overrides_after_factory()
    {
        var mapper = MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile(new ConstructWithOverrideProfile());
        }).CreateMapper();

        var result = mapper.Map<SimpleSource, SimpleDest>(new SimpleSource { Name = "Original", Tag = "Override" });

        Assert.Equal("Override", result.Tag);       // explicit ForMember
        Assert.Equal("Original", result.Name);       // convention mapping runs after factory
    }

    private class SimpleSource { public string Name { get; set; } = ""; public string Tag { get; set; } = ""; }
    private class SimpleDest { public string Name { get; set; } = ""; public string Tag { get; set; } = ""; }

    private class ConstructWithOverrideProfile : Profile
    {
        public ConstructWithOverrideProfile()
        {
            CreateMap<SimpleSource, SimpleDest>()
                .ConstructUsing(src => new SimpleDest { Name = "constructed" })
                .ForMember(d => d.Tag, o => o.MapFrom(s => s.Tag));
        }
    }

    [Fact]
    public void ConstructUsing_factory_receives_source()
    {
        var received = false;
        var mapper = MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile(new LambdaProfile(() => received = true));
        }).CreateMapper();

        mapper.Map<SimpleSource, SimpleDest>(new SimpleSource { Name = "test" });
        Assert.True(received);
    }

    private class LambdaProfile : Profile
    {
        public LambdaProfile(Action onInvoke)
        {
            CreateMap<SimpleSource, SimpleDest>()
                .ConstructUsing(src => { onInvoke(); return new SimpleDest(); });
        }
    }

    [Fact]
    public void ConstructUsing_null_factory_result_throws()
    {
        var mapper = MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile(new NullFactoryProfile());
        }).CreateMapper();

        Assert.Throws<DeltaMapperException>(() =>
            mapper.Map<SimpleSource, SimpleDest>(new SimpleSource { Name = "test" }));
    }

    private class NullFactoryProfile : Profile
    {
        public NullFactoryProfile()
        {
            CreateMap<SimpleSource, SimpleDest>()
                .ConstructUsing(src => null!);
        }
    }
}
