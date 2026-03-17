using DeltaMapper;

namespace DeltaMapper.Benchmarks.Models;

// ── Flat models ──────────────────────────────────────────────────────────────

public class FlatSource
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public int Age { get; set; }
    public bool IsActive { get; set; }
}

public class FlatDest
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public int Age { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Source-generator profiles for benchmark paths.</summary>
[GenerateMap(typeof(FlatSource), typeof(FlatDest))]
public partial class FlatGenProfile { }

[GenerateMap(typeof(AddressSource), typeof(AddressDest))]
public partial class AddressGenProfile { }

[GenerateMap(typeof(NestedSource), typeof(NestedDest))]
public partial class NestedGenProfile { }

[GenerateMap(typeof(ItemSource), typeof(ItemDest))]
public partial class ItemGenProfile { }

[GenerateMap(typeof(CollectionSource), typeof(CollectionDest))]
public partial class CollectionGenProfile { }

// ── Nested models ────────────────────────────────────────────────────────────

public class AddressSource
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string Zip { get; set; } = "";
}

public class AddressDest
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string Zip { get; set; } = "";
}

public class NestedSource
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public AddressSource Address { get; set; } = new();
}

public class NestedDest
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public AddressDest Address { get; set; } = new();
}

// ── Collection models ────────────────────────────────────────────────────────

public class ItemSource
{
    public int Id { get; set; }
    public string Label { get; set; } = "";
}

public class ItemDest
{
    public int Id { get; set; }
    public string Label { get; set; } = "";
}

public class CollectionSource
{
    public int Id { get; set; }
    public List<ItemSource> Items { get; set; } = [];
}

public class CollectionDest
{
    public int Id { get; set; }
    public List<ItemDest> Items { get; set; } = [];
}
