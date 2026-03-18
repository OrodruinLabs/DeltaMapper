<p align="center">
  <img src="icon.png" alt="DeltaMapper" width="200" />
  <br />
  <h1 align="center">DeltaMapper</h1>
  <p align="center"><em>Fast, diff-aware .NET object mapper. MIT licensed. Minimal dependencies.</em></p>
  <p align="center">
    <a href="https://www.nuget.org/packages/DeltaMapper"><img src="https://img.shields.io/nuget/v/DeltaMapper.svg" alt="NuGet" /></a>
    <a href="https://github.com/OrodruinLabs/DeltaMapper/actions/workflows/ci.yml"><img src="https://github.com/OrodruinLabs/DeltaMapper/actions/workflows/ci.yml/badge.svg" alt="Build" /></a>
    <a href="LICENSE"><img src="https://img.shields.io/badge/License-MIT-blue.svg" alt="License: MIT" /></a>
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

Requires .NET 10+.

## Quick Start

```csharp
// 1. Define a profile
public class UserProfile : MappingProfile
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
| [API Reference](docs/api-reference.md) | MapperConfiguration, MappingProfile, IMapper, conventions, records, middleware, DI |
| [Source Generator](docs/source-generator.md) | `[GenerateMap]`, direct calls, analyzer diagnostics |
| [EF Core Integration](docs/efcore-integration.md) | Proxy detection, lazy loading safety |
| [OpenTelemetry Tracing](docs/opentelemetry.md) | Activity spans, zero-overhead fast path |
| [Migration from AutoMapper](docs/migration-from-automapper.md) | Concept mapping table, rename scripts |

## What's Included

- Runtime core — profiles, convention matching, DI integration
- `MappingDiff<T>` — structured change sets with `Patch()`
- Roslyn source generator with zero-overhead direct call methods
- EF Core proxy detection middleware
- OpenTelemetry tracing middleware
- BenchmarkDotNet suite comparing against Mapperly, AutoMapper, and hand-written code

## License

MIT. See [LICENSE](LICENSE).
