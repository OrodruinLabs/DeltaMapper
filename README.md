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

- **Mapperly-speed direct calls** — 7.2 ns flat mapping, same as hand-written code
- **`MappingDiff<T>`** — map _and_ get a structured change set in one call (no other mapper does this)
- **Source generator** — `[GenerateMap]` emits assignment code at build time, zero reflection
- **Full IMapper pipeline** — DI, middleware, hooks, EF Core proxy safety, OpenTelemetry tracing
- **MIT licensed, no paid tiers, forever**

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

## MappingDiff — the killer feature

```csharp
var diff = mapper.Patch(updateDto, existingUser);

if (diff.HasChanges)
    await auditLog.RecordAsync(userId, diff.Changes);
```

`diff.Changes` is `IReadOnlyList<PropertyChange>` — each entry has `PropertyName`, `From`, `To`, `ChangeKind`. Nested paths use dot-notation (`"Address.City"`).

## Benchmarks

| Scenario | DeltaMapper Direct | Mapperly | AutoMapper | Hand-written |
|---|---:|---:|---:|---:|
| Flat (5 props) | **7.2 ns / 48 B** | 6.8 ns / 48 B | 47 ns / 48 B | 6.8 ns / 48 B |
| Nested (2 levels) | — | 21 ns / 120 B | 55 ns / 120 B | 19 ns / 120 B |
| Collection (10) | **22 ns / 64 B** | 101 ns / 520 B | 183 ns / 712 B | 121 ns / 592 B |

[Full benchmark results and methodology](BENCHMARKS.md)

## Documentation

| Guide | Description |
|---|---|
| [API Reference](docs/api-reference.md) | MapperConfiguration, MappingProfile, IMapper, conventions, records, middleware, DI |
| [Source Generator](docs/source-generator.md) | `[GenerateMap]`, direct calls, analyzer diagnostics |
| [EF Core Integration](docs/efcore-integration.md) | Proxy detection, lazy loading safety |
| [OpenTelemetry Tracing](docs/opentelemetry.md) | Activity spans, zero-overhead fast path |
| [Migration from AutoMapper](docs/migration-from-automapper.md) | Concept mapping table, rename scripts |
| [Design Specification](docs/DELTAMAP_PLAN.md) | Full architecture and phased delivery plan |

## Roadmap

| Phase | Deliverable | Status |
|---|---|---|
| 1 | Runtime core — profiles, convention mapping, DI | Done |
| 2 | `MappingDiff<T>` — structured change sets + `Patch()` | Done |
| 3 | Roslyn source generator — zero-overhead paths | Done |
| 4 | EF Core proxy awareness + OpenTelemetry spans | Done |
| 5 | Benchmarks + full docs site | Done |

## License

MIT. See [LICENSE](LICENSE).
