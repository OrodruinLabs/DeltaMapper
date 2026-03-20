using DeltaMapper;
using Xunit;

namespace DeltaMapper.UnitTests;

public class NestedMapFromTests
{
    private class OrderEntity
    {
        public int Id { get; set; }
        public CustomerEntity Customer { get; set; } = null!;
    }

    private class CustomerEntity
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
    }

    private class OrderDto
    {
        public int Id { get; set; }
        public CustomerDto Customer { get; set; } = null!;
    }

    private class CustomerDto
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
    }

    private class ProfileWithNestedMapFrom : Profile
    {
        public ProfileWithNestedMapFrom()
        {
            CreateMap<CustomerEntity, CustomerDto>();
            CreateMap<OrderEntity, OrderDto>()
                .ForMember(d => d.Customer, o => o.MapFrom(s => s.Customer));
        }
    }

    [Fact]
    public void MapFrom_with_registered_nested_map_resolves_via_type_map()
    {
        var mapper = MapperConfiguration.Create(cfg => cfg.AddProfile<ProfileWithNestedMapFrom>()).CreateMapper();
        var order = new OrderEntity
        {
            Id = 1,
            Customer = new CustomerEntity { Name = "Alice", Email = "alice@test.com" }
        };

        var result = mapper.Map<OrderEntity, OrderDto>(order);

        Assert.Equal(1, result.Id);
        Assert.NotNull(result.Customer);
        Assert.Equal("Alice", result.Customer.Name);
        Assert.Equal("alice@test.com", result.Customer.Email);
    }

    [Fact]
    public void MapFrom_returning_destination_type_does_not_double_map()
    {
        // When MapFrom returns the destination type directly, no recursive mapping
        var mapper = MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile(new InlineProfile());
        }).CreateMapper();

        var result = mapper.Map<OrderEntity, OrderDto>(new OrderEntity
        {
            Id = 2,
            Customer = new CustomerEntity { Name = "Bob", Email = "bob@test.com" }
        });

        Assert.Equal("INLINE", result.Customer.Name);
    }

    private class InlineProfile : Profile
    {
        public InlineProfile()
        {
            CreateMap<OrderEntity, OrderDto>()
                .ForMember(d => d.Customer, o => o.MapFrom(s =>
                    new CustomerDto { Name = "INLINE", Email = s.Customer.Email }));
        }
    }

    [Fact]
    public void MapFrom_with_null_nested_source_returns_null()
    {
        var mapper = MapperConfiguration.Create(cfg => cfg.AddProfile<ProfileWithNestedMapFrom>()).CreateMapper();
        var order = new OrderEntity { Id = 3, Customer = null! };

        var result = mapper.Map<OrderEntity, OrderDto>(order);

        Assert.Null(result.Customer);
    }
}
