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

## Benchmarks at a Glance

| Scenario | DeltaMapper | Mapperly | AutoMapper | Hand-written |
|----------|------------:|---------:|-----------:|-------------:|
| **Flat object** (5 props) | **7 ns** / 48 B | 7 ns / 48 B | 47 ns / 48 B | 7 ns / 48 B |
| **Nested object** (2 levels) | **24 ns** / 80 B | 21 ns / 120 B | 55 ns / 120 B | 19 ns / 120 B |
| **Collection** (10 items) | **22 ns** / 64 B | 101 ns / 520 B | 183 ns / 712 B | 121 ns / 592 B |

> Source-generated `[GenerateMap]` benchmarks: flat scenario uses direct calls; nested/collection use the `IMapper` source-gen path. Numbers are rounded from [BENCHMARKS.md](BENCHMARKS.md). Apple M1 Max, .NET 10.

---

## Why DeltaMapper?

- **Near-zero overhead** — source-generated direct calls run at 7 ns, comparable to hand-written code
- **`MappingDiff<T>`** — map _and_ get a structured change set in one call
- **Source generator** — `[GenerateMap]` emits assignment code at build time, zero reflection; `[IgnoreMember]`, `[MapMember]`, and `[NullSubstitute]` attributes customize maps without runtime Profiles
- **Full IMapper pipeline** — DI, middleware, hooks, EF Core proxy detection, OpenTelemetry tracing
- **`ProjectTo<T>()`** — translate profile maps into EF Core-compatible SQL projections via `IQueryable`

## Get Started

```bash
dotnet add package DeltaMapper
```

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

<details>
<summary>Optional packages</summary>

```bash
dotnet add package DeltaMapper.SourceGen          # compile-time codegen
dotnet add package DeltaMapper.EFCore             # EF Core proxy awareness + ProjectTo
dotnet add package DeltaMapper.OpenTelemetry      # Activity spans
```

</details>

Requires .NET 8+ (ships net8.0, net9.0, and net10.0 assets).

## Built-in Change Tracking

```csharp
var diff = mapper.Patch(updateDto, existingUser);

if (diff.HasChanges)
    await auditLog.RecordAsync(userId, diff.Changes);
```

`diff.Changes` is `IReadOnlyList<PropertyChange>` — each entry has `PropertyName`, `From`, `To`, `ChangeKind`. Nested paths use dot-notation (`"Address.City"`).

## EF Core ProjectTo

Project directly from an `IQueryable` to a DTO using your existing profile — no separate projection configuration needed. The mapping expression is translated to SQL by EF Core.

```bash
dotnet add package DeltaMapper.EFCore
```

```csharp
var config = MapperConfiguration.Create(cfg => cfg.AddProfile<OrderProfile>());

var dtos = await dbContext.Orders
    .Where(o => o.IsActive)
    .ProjectTo<Order, OrderDto>(config)
    .ToListAsync();
```

`ProjectTo` supports convention matching, `ForMember`/`MapFrom`, `Ignore`, `NullSubstitute`, flattening, nested objects, and collection navigations. `BeforeMap`, `AfterMap`, `ConstructUsing`, and `Condition` are not supported in projection context.

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

## Source Generator Attributes

`DeltaMapper.SourceGen` attributes let you customize compile-time maps directly on the profile class — no runtime Profile or `ForMember` calls required.

```bash
dotnet add package DeltaMapper.SourceGen
```

```csharp
[GenerateMap(typeof(User), typeof(UserDto))]
[IgnoreMember(typeof(User), typeof(UserDto), nameof(UserDto.InternalId))]
[MapMember(typeof(User), typeof(UserDto), nameof(UserDto.FullName), nameof(User.Name))]
[NullSubstitute(typeof(User), typeof(UserDto), nameof(UserDto.DisplayName), "Anonymous")]
public partial class UserMappingProfile;
```

| Attribute | Effect |
|---|---|
| `[IgnoreMember(src, dst, member)]` | Exclude a destination member from the generated map |
| `[MapMember(src, dst, dstMember, srcMember)]` | Rename: map a source member to a differently named destination member |
| `[NullSubstitute(src, dst, member, value)]` | Use `value` when the source member is null |

Each attribute takes explicit `(Type sourceType, Type destinationType, ...)` so a single profile class can carry attributes for multiple type pairs without ambiguity.

New diagnostics added alongside these attributes:

| Code | Severity | Description |
|---|---|---|
| DM003 | Warning | Attribute references a property that does not exist on the type |
| DM004 | Warning | `[MapMember]` source and destination property types are incompatible |

## Performance

DeltaMapper's source generator produces code comparable to hand-written — and on collections, faster than every competitor tested.

| What's being mapped | DeltaMapper | vs Mapperly | vs AutoMapper |
|---|---:|---|---|
| Simple object (5 properties) | **7 ns** | Comparable (7 ns vs 7 ns) | 7x faster |
| Nested object (parent + child) | **24 ns** | Within 15%, 33% less memory | 2x faster |
| Collection (10 items) | **22 ns** | 5x faster, 8x less memory | 8x faster |

> .NET 10. Times are per single mapping operation.
> DeltaMapper allocates only the destination object — no framework overhead.

[Full benchmark results and methodology](BENCHMARKS.md)

## Documentation

| Guide | Description |
|---|---|
| [API Reference](docs/api-reference.md) | MapperConfiguration, Profile, IMapper, conventions, flattening, assembly scanning, type converters, middleware, DI |
| [Source Generator](docs/source-generator.md) | `[GenerateMap]`, source gen attributes, direct calls, analyzer diagnostics |
| [EF Core Integration](docs/efcore-integration.md) | Proxy detection, lazy loading safety, `ProjectTo` |
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
