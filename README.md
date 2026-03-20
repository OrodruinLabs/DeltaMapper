<p align="center">
  <img src="icon.png" alt="DeltaMapper" width="200" />
  <br />
  <h1 align="center">DeltaMapper</h1>
  <p align="center"><em>Fast, diff-aware .NET object mapper. MIT licensed. Minimal dependencies.</em></p>
  <p align="center">
    <a href="https://www.nuget.org/packages/DeltaMapper"><img src="https://img.shields.io/nuget/v/DeltaMapper.svg" alt="NuGet" /></a>
    <a href="https://www.nuget.org/packages/DeltaMapper"><img src="https://img.shields.io/nuget/dt/DeltaMapper.svg" alt="Downloads" /></a>
    <a href="https://github.com/OrodruinLabs/DeltaMapper/actions/workflows/ci.yml"><img src="https://github.com/OrodruinLabs/DeltaMapper/actions/workflows/ci.yml/badge.svg" alt="Build" /></a>
    <a href="LICENSE"><img src="https://img.shields.io/badge/License-MIT-blue.svg" alt="License: MIT" /></a>
    <img src="https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-512BD4" alt=".NET 8 | 9 | 10" />
  </p>
</p>

---

## Why DeltaMapper?

- **Near-zero overhead** — source-generated direct calls run at 7 ns, same as hand-written code
- **`MappingDiff<T>`** — map _and_ get a structured change set in one call
- **Source generator** — `[GenerateMap]` emits assignment code at build time, zero reflection
- **Full IMapper pipeline** — DI, middleware, hooks, EF Core proxy detection, OpenTelemetry tracing

## Install

```bash
dotnet add package DeltaMapper                    # core runtime
dotnet add package DeltaMapper.SourceGen          # optional: compile-time codegen
dotnet add package DeltaMapper.EFCore             # optional: EF Core proxy awareness
dotnet add package DeltaMapper.OpenTelemetry      # optional: Activity spans
```

Requires .NET 8+ (currently ships net8.0, net9.0, and net10.0 assets).

## Quick Start

```csharp
// 1. Define a profile
public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(d => d.FullName, o => o.MapFrom(s => $"{s.First} {s.Last}"))
            .ReverseMap();
    }
}

// 2. Build & map
var mapper = MapperConfiguration.Create(cfg => cfg.AddProfile<UserProfile>())
    .CreateMapper();

var dto = mapper.Map<User, UserDto>(user);
```

## Built-in Change Tracking

```csharp
var diff = mapper.Patch(updateDto, existingUser);

if (diff.HasChanges)
    await auditLog.RecordAsync(userId, diff.Changes);
```

`diff.Changes` is `IReadOnlyList<PropertyChange>` — each entry has `PropertyName`, `From`, `To`, `ChangeKind`. Nested paths use dot-notation (`"Address.City"`).

## Flattening and Unflattening

DeltaMapper automatically flattens nested objects to flat DTOs and unflattens flat DTOs back to nested objects — no configuration needed.

```csharp
// Source model
public class Order
{
    public int Id { get; set; }
    public Customer? Customer { get; set; }
}
public class Customer { public string? Name { get; set; } }

// Flat DTO — convention maps Order.Customer.Name → CustomerName
public class OrderFlatDto
{
    public int Id { get; set; }
    public string? CustomerName { get; set; }
}

// Flattening: nested → flat
var flat = mapper.Map<Order, OrderFlatDto>(order);
// flat.CustomerName == order.Customer.Name

// Unflattening: flat → nested (reverse map or separate CreateMap)
var restored = mapper.Map<OrderFlatDto, Order>(flat);
// restored.Customer.Name == flat.CustomerName
```

Null intermediate objects in the flattened chain return null without throwing. Multi-level chains (`Customer.Address.Zip` → `CustomerAddressZip`) are also resolved automatically.

## Assembly Scanning

Register all profiles in an assembly in one call instead of listing them individually.

```csharp
// Scan by assembly reference
var mapper = MapperConfiguration.Create(cfg =>
    cfg.AddProfilesFromAssembly(typeof(UserProfile).Assembly))
    .CreateMapper();

// Or scan by any type in the target assembly
var mapper = MapperConfiguration.Create(cfg =>
    cfg.AddProfilesFromAssemblyContaining<UserProfile>())
    .CreateMapper();
```

Abstract profiles and profiles without a parameterless constructor are silently skipped. Assembly scanning and explicit `AddProfile<T>()` calls can be combined in the same configuration.

## Type Converters

Register a global converter for a type pair once and it applies automatically across all maps.

```csharp
var mapper = MapperConfiguration.Create(cfg =>
{
    cfg.CreateTypeConverter<string, DateTime>(s => DateTime.Parse(s));
    cfg.CreateTypeConverter<int, string>(i => i.ToString("D6"));
    cfg.AddProfilesFromAssemblyContaining<UserProfile>();
})
.CreateMapper();

// Any map with a string → DateTime property pair now uses the converter
var dto = mapper.Map<OrderRequest, OrderDto>(request);
```

## Conditional Mapping

Skip a property mapping when a condition is not met — the destination property keeps its default or existing value.

```csharp
public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>()
            .ForMember(d => d.Discount, o => o.Condition(s => s.IsPremiumCustomer))
            .ForMember(d => d.Notes, o => o.Condition(s => s.Notes != null));
    }
}
```

Conditions work alongside `MapFrom` and `NullSubstitute` — the condition is evaluated first, and if false the member option is skipped entirely. `Ignore` and `Condition` cannot be combined on the same member; use `Condition` alone to conditionally skip mapping.

## Performance

DeltaMapper's source generator produces code as fast as hand-written — and on collections, faster than every competitor tested.

| What's being mapped | DeltaMapper | vs Mapperly | vs AutoMapper |
|---|---:|---|---|
| Simple object (5 properties) | **7 ns** | Same speed | 7x faster |
| Nested object (parent + child) | **24 ns** | Same speed, 33% less memory | 2x faster |
| Collection (10 items) | **22 ns** | 5x faster, 8x less memory | 8x faster |

> .NET 10. Times are per single mapping operation.
> DeltaMapper allocates only the destination object — no framework overhead.

[Full benchmark results and methodology](BENCHMARKS.md)

## Documentation

| Guide | Description |
|---|---|
| [API Reference](docs/api-reference.md) | MapperConfiguration, Profile, IMapper, conventions, flattening, assembly scanning, type converters, middleware, DI |
| [Source Generator](docs/source-generator.md) | `[GenerateMap]`, direct calls, analyzer diagnostics |
| [EF Core Integration](docs/efcore-integration.md) | Proxy detection, lazy loading safety |
| [OpenTelemetry Tracing](docs/opentelemetry.md) | Activity spans, zero-overhead fast path |
| [Migration from AutoMapper](docs/migration-from-automapper.md) | Concept mapping table, rename scripts |

## Getting Help

- [GitHub Issues](https://github.com/OrodruinLabs/DeltaMapper/issues) — bug reports and feature requests
- [GitHub Discussions](https://github.com/OrodruinLabs/DeltaMapper/discussions) — questions and community support

## Contributing

Contributions are welcome. Please open an issue first to discuss what you'd like to change.

1. Fork the repo
2. Create a branch (`git checkout -b my-feature`)
3. Make your changes and add tests
4. Run `dotnet test` to verify all tests pass
5. Open a pull request

## License

MIT. See [LICENSE](LICENSE).
