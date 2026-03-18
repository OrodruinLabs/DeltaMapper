namespace DeltaMapper.UnitTests.TestModels;

public class Address
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Zip { get; set; } = string.Empty;
}

public class AddressDto
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Zip { get; set; } = string.Empty;
}

public class Customer
{
    public string Name { get; set; } = string.Empty;
    public Address Address { get; set; } = null!;
}

public class CustomerDto
{
    public string Name { get; set; } = string.Empty;
    public AddressDto Address { get; set; } = null!;
}

public class Order
{
    public int Id { get; set; }
    public Customer Customer { get; set; } = null!;
}

public class OrderDto
{
    public int Id { get; set; }
    public CustomerDto Customer { get; set; } = null!;
}
