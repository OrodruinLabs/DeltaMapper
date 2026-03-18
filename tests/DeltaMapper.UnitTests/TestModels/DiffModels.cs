namespace DeltaMapper.UnitTests.TestModels;

// Flat product pair — for basic diff/patch tests
public class Product
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
}

public class ProductDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
}

// Warehouse with nested Address — for nested-object diff tests
public class Warehouse
{
    public string Name { get; set; } = string.Empty;
    public Address Address { get; set; } = null!;
}

public class WarehouseDto
{
    public string Name { get; set; } = string.Empty;
    public AddressDto Address { get; set; } = null!;
}

// Collection element pair — for collection diff tests
public class Player
{
    public string Name { get; set; } = string.Empty;
    public int Score { get; set; }
}

public class PlayerDto
{
    public string Name { get; set; } = string.Empty;
    public int Score { get; set; }
}

// Team with collection of players — for collection diff tests
public class Team
{
    public string Name { get; set; } = string.Empty;
    public List<Player> Players { get; set; } = [];
}

public class TeamDto
{
    public string Name { get; set; } = string.Empty;
    public List<PlayerDto> Players { get; set; } = [];
}

// Product with nullable property — for NullSubstitute tests
public class ProductWithNullable
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Nickname { get; set; }
}

public class ProductWithNullableDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Nickname { get; set; }
}
