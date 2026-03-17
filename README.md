# DeltaMapper

> Fast, diff-aware .NET object mapper. MIT licensed. Minimal dependencies.

- **Expression-compiled delegates + `FrozenDictionary`** — all reflection happens once at startup, never at call time
- **`MappingDiff<T>`** — maps an object _and_ returns a structured change set in a single call
- **Roslyn source generator** — optional `[GenerateMap]` attribute emits assignment code at build time with zero reflection
- **EF Core proxy awareness** — safely maps lazy-loaded proxy entities without triggering navigation loads
- **OpenTelemetry tracing** — zero-overhead `Activity` spans when no listener is attached
- **MIT licensed, no paid tiers, forever**

[![NuGet](https://img.shields.io/nuget/v/DeltaMapper.svg)](https://www.nuget.org/packages/DeltaMapper)
[![Build](https://github.com/OrodruinLabs/DeltaMapper/actions/workflows/ci.yml/badge.svg)](https://github.com/OrodruinLabs/DeltaMapper/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

---

## Install

```
dotnet add package DeltaMapper
```

For compile-time source generation (optional):

```
dotnet add package DeltaMapper.SourceGen
```

For EF Core proxy awareness (optional):

```
dotnet add package DeltaMapper.EFCore
```

For OpenTelemetry tracing (optional):

```
dotnet add package DeltaMapper.OpenTelemetry
```

Requires .NET 10+.

---

## Benchmarks

DeltaMapper SourceGen vs. competitors (Apple M1 Max, .NET 10):

| Scenario | DeltaMapper SourceGen | Mapperly | AutoMapper | Hand-written |
|---|---:|---:|---:|---:|
| Flat Object (5 props) | 79.5 ns / 456 B | 7.2 ns / 48 B | 45.5 ns / 48 B | 7.2 ns / 48 B |
| Nested Object (2 levels) | 85.9 ns / 488 B | 18.3 ns / 120 B | 55.8 ns / 120 B | 18.8 ns / 120 B |
| Collection (10 items) | 82.0 ns / 472 B | 109.9 ns / 520 B | 186.7 ns / 712 B | 126.1 ns / 592 B |
| Patch (diff tracking) | 523.1 ns / 1,776 B | — | 48.9 ns / 48 B | 8.6 ns / 48 B |

See [BENCHMARKS.md](BENCHMARKS.md) for full results and methodology.

---

## `MappingDiff<T>` — the hook

```csharp
// PATCH endpoint — map and audit in one call
[HttpPatch("{id}")]
public async Task<IActionResult> PatchUser(int id, UpdateUserDto dto)
{
    var user = await db.Users.FindAsync(id);
    var diff = mapper.Patch(dto, user);   // MappingDiff<User>

    if (!diff.HasChanges)
        return NoContent();

    await auditLog.RecordAsync(id, diff.Changes);  // IReadOnlyList<PropertyChange>
    await db.SaveChangesAsync();
    return Ok(diff.Result);
}
```

`diff.Changes` is an `IReadOnlyList<PropertyChange>` where each entry carries `PropertyName`, `From`, `To`, and `ChangeKind` (`Modified` / `Added` / `Removed`). Nested changes use dot-notation paths (`"Address.City"`).

---

## Source Generator

Install `DeltaMapper.SourceGen`, add `[GenerateMap]` to a `partial` class, and the mapper emits direct assignment code at build time — no reflection at all:

```csharp
[GenerateMap(typeof(UserDto))]
public partial class User
{
    public int Id { get; set; }
    public string Email { get; set; }
}
```

The generated map is registered automatically via `GeneratedMapRegistry` and picked up by the same `IMapper` interface. Analyzer diagnostics catch misuse at compile time:

| Code | Diagnostic |
|---|---|
| DM001 | Class must be `partial` |
| DM002 | Class must have a parameterless constructor |
| DM003 | Ambiguous mapping detected |

---

## EF Core Integration

Install `DeltaMapper.EFCore` and call `AddEFCoreSupport()` when building the mapper. The middleware detects Castle.Core dynamic proxies emitted by EF Core and skips unloaded navigation properties so lazy loading is never triggered during a mapping call.

```csharp
var config = MapperConfiguration.Create(cfg =>
{
    cfg.AddProfile<UserProfile>();
    cfg.AddEFCoreSupport();   // from DeltaMapper.EFCore
});

IMapper mapper = config.CreateMapper();

// Proxy entities are mapped safely — no lazy load triggered
var dto = mapper.Map<User, UserDto>(proxyUser);
```

`AddEFCoreSupport()` is an extension on `MapperConfigurationBuilder` and returns the builder for fluent chaining alongside other configuration calls.

---

## OpenTelemetry Tracing

Install `DeltaMapper.OpenTelemetry` and call `AddMapperTracing()` to emit an `Activity` span for every mapping operation. The `ActivitySource` name is `"DeltaMapper"`.

```csharp
var config = MapperConfiguration.Create(cfg =>
{
    cfg.AddProfile<UserProfile>();
    cfg.AddMapperTracing();   // from DeltaMapper.OpenTelemetry
});

IMapper mapper = config.CreateMapper();
```

Each span is named `"Map {SourceType} -> {DestType}"` and carries two tags:

| Tag | Value |
|---|---|
| `mapper.source_type` | Fully-qualified source type name |
| `mapper.dest_type` | Fully-qualified destination type name |

If a mapping throws, the span status is set to `Error` and an `"exception"` event is recorded with `exception.type` and `exception.message` tags.

The middleware uses `ActivitySource.HasListeners()` as a fast path: when no OpenTelemetry listener is attached the entire tracing path is bypassed with zero allocation overhead.

Wire up the `"DeltaMapper"` source in your OpenTelemetry SDK setup:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("DeltaMapper")
        .AddOtlpExporter());
```

---

## Quick start

```csharp
// 1. Define a profile
public class UserProfile : MappingProfile
{
    public UserProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(d => d.FullName, o => o.MapFrom(s => $"{s.First} {s.Last}"))
            .ForMember(d => d.InternalId, o => o.Ignore())
            .ReverseMap();
    }
}

// 2. Build configuration (once at startup)
var config = MapperConfiguration.Create(cfg =>
{
    cfg.AddProfile<UserProfile>();
});

// 3. Create the mapper
IMapper mapper = config.CreateMapper();

// 4. Map
var dto = mapper.Map<User, UserDto>(user);
```

---

## API reference

### `MapperConfiguration`

```csharp
// Static factory — scans profiles, compiles all maps, freezes the registry
MapperConfiguration config = MapperConfiguration.Create(cfg =>
{
    cfg.AddProfile<UserProfile>();          // add by generic type (new TProfile())
    cfg.AddProfile(new OrderProfile());     // add existing instance
    cfg.Use<MyLoggingMiddleware>();         // add pipeline middleware
});

IMapper mapper = config.CreateMapper();
```

All compilation happens inside `Create()`. The internal registry is a `FrozenDictionary` — read access after construction has no locking overhead.

### `MappingProfile`

Subclass `MappingProfile` and configure maps in the constructor.

```csharp
public class OrderProfile : MappingProfile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>()
            .ForMember(d => d.Total,    o => o.MapFrom(s => s.Lines.Sum(l => l.Amount)))
            .ForMember(d => d.Secret,   o => o.Ignore())
            .ForMember(d => d.Customer, o => o.NullSubstitute("Anonymous"))
            .BeforeMap((src, dst) => dst.MappedAt = DateTimeOffset.UtcNow)
            .AfterMap((src, dst)  => dst.Validate())
            .ReverseMap();
    }
}
```

| Fluent method | Effect |
|---|---|
| `ForMember(dst => dst.Prop, o => o.MapFrom(src => ...))` | Custom value resolver |
| `ForMember(dst => dst.Prop, o => o.Ignore())` | Skip this destination member |
| `ForMember(dst => dst.Prop, o => o.NullSubstitute(value))` | Use `value` when source is null |
| `BeforeMap((src, dst) => ...)` | Hook runs before property assignment |
| `AfterMap((src, dst) => ...)` | Hook runs after property assignment |
| `ReverseMap()` | Registers convention-matched reverse map (`TDst -> TSrc`) |

### `IMapper`

```csharp
// Map to a new instance — source type inferred from runtime type
TDestination Map<TDestination>(object source);

// Map to a new instance — source type resolved at compile time
TDestination Map<TSource, TDestination>(TSource source);

// Map onto an existing destination instance (updates in place)
TDestination Map<TSource, TDestination>(TSource source, TDestination destination);

// Map a sequence — returns IReadOnlyList<TDestination>
IReadOnlyList<TDestination> MapList<TSource, TDestination>(IEnumerable<TSource> source);

// Non-generic overload for dynamic / reflection scenarios
object Map(object source, Type sourceType, Type destinationType);

// Map and return a structured diff of what changed
MappingDiff<TDestination> MapWithDiff<TSource, TDestination>(TSource source);

// Update destination in place and return a diff of what changed
MappingDiff<TDestination> Patch<TSource, TDestination>(TSource source, TDestination destination);
```

### Convention matching

Properties are matched by name (case-insensitive) without any configuration. Convention rules applied in order:

1. Same name + same type (or assignable) — direct assign
2. Same name + safe numeric widening (e.g., `int` to `long`) — `Convert.ChangeType`
3. Same name + `IEnumerable<T>` on both sides — map each element, produce `List<T>` or `T[]`
4. Same name + complex object type — recursive map lookup

### Records and init-only properties

DeltaMapper detects `init`-only setters via `IsExternalInit` modreq and routes those types through constructor injection automatically. Primary constructor parameter names are matched to source properties by name (case-insensitive).

```csharp
public record UserDto(int Id, string Email, string FullName);

// No special configuration required — constructor injection is automatic
var dto = mapper.Map<User, UserDto>(user);
```

### Middleware pipeline

Implement `IMappingMiddleware` to intercept every mapping call in the pipeline.

```csharp
public sealed class LoggingMiddleware : IMappingMiddleware
{
    public object Map(object source, Type destType, MapperContext ctx, Func<object> next)
    {
        Console.WriteLine($"Mapping {source.GetType().Name} -> {destType.Name}");
        var result = next();
        Console.WriteLine("Done");
        return result;
    }
}

// Register:
var config = MapperConfiguration.Create(cfg =>
{
    cfg.AddProfile<UserProfile>();
    cfg.Use<LoggingMiddleware>();
});
```

When no middleware is registered the core delegate is invoked directly with zero overhead.

### DI integration (ASP.NET Core / Generic Host)

```csharp
// Program.cs
builder.Services.AddDeltaMapper(cfg =>
{
    cfg.AddProfile<UserProfile>();
    cfg.AddProfile<OrderProfile>();
});

// Inject IMapper anywhere
public class UserService(IMapper mapper)
{
    public UserDto GetUser(User user) => mapper.Map<User, UserDto>(user);
}
```

`AddDeltaMapper` registers both `MapperConfiguration` and `IMapper` as singletons.

### Error handling

When no mapping is registered `DeltaMapperException` is thrown with a clear, actionable message:

```
No mapping registered from 'User' to 'UserDto'. Register a mapping in a MappingProfile using CreateMap<User, UserDto>().
```

---

## Migration from AutoMapper

See [docs/migration-from-automapper.md](docs/migration-from-automapper.md) for a full mapping table and profile rename script.

---

## Roadmap

| Phase | Deliverable | Status |
|---|---|---|
| 1 | Runtime core — profiles, convention mapping, DI | Done |
| 2 | `MappingDiff<T>` — structured change sets + `Patch()` | Done |
| 3 | Roslyn source generator — zero-overhead paths | Done |
| 4 | EF Core proxy awareness + OpenTelemetry spans | Done |
| 5 | Benchmarks + full docs site | Done |

Full design specification: [docs/DELTAMAP_PLAN.md](docs/DELTAMAP_PLAN.md)

---

## License

MIT. See [LICENSE](LICENSE).
