using DeltaMapper.Abstractions;
using DeltaMapper.Configuration;
using DeltaMapper.Exceptions;
using DeltaMapper.Runtime;
using DeltaMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace DeltaMapper.UnitTests;

public class NestedMappingTests
{
    // ── N-01 ─────────────────────────────────────────────────────────────────
    private class N01_OrderProfile : Profile
    {
        public N01_OrderProfile()
        {
            CreateMap<Address, AddressDto>();
            CreateMap<Customer, CustomerDto>();
            CreateMap<Order, OrderDto>();
        }
    }

    [Fact]
    public void Map_NestedObject_MapsRecursively()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<N01_OrderProfile>());
        var mapper = config.CreateMapper();

        var source = new Order
        {
            Id = 1,
            Customer = new Customer
            {
                Name = "Alice",
                Address = new Address
                {
                    Street = "123 Main St",
                    City = "Springfield",
                    Zip = "62701"
                }
            }
        };

        var result = mapper.Map<Order, OrderDto>(source);

        result.Id.Should().Be(1);
        result.Customer.Should().NotBeNull();
        result.Customer.Name.Should().Be("Alice");
        result.Customer.Address.Should().NotBeNull();
        result.Customer.Address.City.Should().Be("Springfield");
    }

    // ── N-02 ─────────────────────────────────────────────────────────────────
    private class N02_CustomerNullAddressProfile : Profile
    {
        public N02_CustomerNullAddressProfile()
        {
            CreateMap<Address, AddressDto>();
            CreateMap<Customer, CustomerDto>();
        }
    }

    [Fact]
    public void Map_NestedObjectIsNull_MapsToNull()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<N02_CustomerNullAddressProfile>());
        var mapper = config.CreateMapper();

        var source = new Customer
        {
            Name = "Bob",
            Address = null!
        };

        var result = mapper.Map<Customer, CustomerDto>(source);

        result.Name.Should().Be("Bob");
        result.Address.Should().BeNull();
    }

    // ── N-03 ─────────────────────────────────────────────────────────────────
    private class N03_DeepNestingProfile : Profile
    {
        public N03_DeepNestingProfile()
        {
            CreateMap<Address, AddressDto>();
            CreateMap<Customer, CustomerDto>();
            CreateMap<Order, OrderDto>();
        }
    }

    [Fact]
    public void Map_DeepNesting_ThreeLevels_MapsCorrectly()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<N03_DeepNestingProfile>());
        var mapper = config.CreateMapper();

        var source = new Order
        {
            Id = 42,
            Customer = new Customer
            {
                Name = "Carol",
                Address = new Address
                {
                    Street = "456 Elm Ave",
                    City = "Shelbyville",
                    Zip = "62565"
                }
            }
        };

        var result = mapper.Map<Order, OrderDto>(source);

        result.Id.Should().Be(42);
        result.Customer.Should().NotBeNull();
        result.Customer.Name.Should().Be("Carol");
        result.Customer.Address.Should().NotBeNull();
        result.Customer.Address.Street.Should().Be("456 Elm Ave");
        result.Customer.Address.City.Should().Be("Shelbyville");
        result.Customer.Address.Zip.Should().Be("62565");
    }

    // ── N-04 ─────────────────────────────────────────────────────────────────
    private class N04_MissingAddressProfile : Profile
    {
        public N04_MissingAddressProfile()
        {
            // Intentionally omit Address -> AddressDto mapping
            CreateMap<Customer, CustomerDto>();
        }
    }

    [Fact]
    public void Map_NestedWithNoRegisteredMapping_ThrowsDeltaMapperException()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<N04_MissingAddressProfile>());
        var mapper = config.CreateMapper();

        var source = new Customer
        {
            Name = "Dave",
            Address = new Address
            {
                Street = "789 Oak Blvd",
                City = "Capital City",
                Zip = "00001"
            }
        };

        var act = () => mapper.Map<Customer, CustomerDto>(source);

        act.Should().Throw<DeltaMapperException>();
    }
}
