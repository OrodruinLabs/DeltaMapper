# DeltaMapper

Fast, diff-aware .NET object mapper. MIT licensed. Minimal dependencies.

## Why DeltaMapper?

- **Near-zero overhead** — source-generated direct calls run at 7 ns, same as hand-written code
- **`MappingDiff<T>`** — map and get a structured change set in one call
- **Source generator** — `[GenerateMap]` emits assignment code at build time, zero reflection
- **Full IMapper pipeline** — DI, middleware, hooks, EF Core proxy detection, OpenTelemetry tracing

## Install

```bash
dotnet add package DeltaMapper                    # core runtime
dotnet add package DeltaMapper.SourceGen          # optional: compile-time codegen
dotnet add package DeltaMapper.EFCore             # optional: EF Core proxy awareness
dotnet add package DeltaMapper.OpenTelemetry      # optional: Activity spans
```

Requires .NET 8+ (binaries for net8.0, net9.0, and net10.0).

## Quick Start

```csharp
public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(d => d.FullName, o => o.MapFrom(s => $"{s.First} {s.Last}"))
            .ReverseMap();
    }
}

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

## Flattening and Unflattening

Nested objects are flattened to flat DTOs and back automatically — no configuration required.

```csharp
// Order.Customer.Name → CustomerName (flattening)
CreateMap<Order, OrderFlatDto>();

// CustomerName → Customer.Name (unflattening)
CreateMap<OrderFlatDto, Order>();
```

## Assembly Scanning

```csharp
// Register all profiles in an assembly in one call
cfg.AddProfilesFromAssemblyContaining<UserProfile>();
```

## Type Converters

```csharp
// Apply a conversion across every map that has a matching type pair
cfg.CreateTypeConverter<string, DateTime>(s => DateTime.Parse(s));
```

## Conditional Mapping

```csharp
// Skip mapping a property when a condition is not met
CreateMap<Order, OrderDto>()
    .ForMember(d => d.Discount, o => o.Condition(s => s.IsPremiumCustomer));
```

## Performance

| What's being mapped | DeltaMapper | vs Mapperly | vs AutoMapper |
|---|---:|---|---|
| Simple object (5 properties) | **7 ns** | Same speed | 7x faster |
| Nested object (parent + child) | **24 ns** | Same speed, 33% less memory | 2x faster |
| Collection (10 items) | **22 ns** | 5x faster, 8x less memory | 8x faster |

## Getting Help

- [GitHub Issues](https://github.com/OrodruinLabs/DeltaMapper/issues) — bug reports and feature requests
- [GitHub Discussions](https://github.com/OrodruinLabs/DeltaMapper/discussions) — questions

## Links

- [GitHub](https://github.com/OrodruinLabs/DeltaMapper)
- [API Reference](https://github.com/OrodruinLabs/DeltaMapper/blob/main/docs/api-reference.md)
- [Source Generator Guide](https://github.com/OrodruinLabs/DeltaMapper/blob/main/docs/source-generator.md)
- [Migration from AutoMapper](https://github.com/OrodruinLabs/DeltaMapper/blob/main/docs/migration-from-automapper.md)
- [Benchmarks](https://github.com/OrodruinLabs/DeltaMapper/blob/main/BENCHMARKS.md)
