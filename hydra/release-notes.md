# Release Notes — DeltaMapper v0.1.0-alpha.1

**Date**: 2026-03-17
**Status**: Pre-release (alpha)
**NuGet package**: `DeltaMapper` 0.1.0-alpha.1
**Tag (pending)**: `v0.1.0-alpha.1`

---

## What's in This Release

This is the first pre-release of DeltaMapper, shipping the Phase 1 runtime core only. It is suitable for early adopters and internal evaluation. The API is not yet stable.

### Phase 1 — Runtime Core

Delivered via PRs #1 and #2 (commits 39b783a and 1dee2c6).

**Mapper engine**
- Expression-compiled delegates built once at configuration time — no per-call reflection.
- `FrozenDictionary`-backed mapper registry for O(1) lookup at call time.
- `IMapper` public interface with generic `Map<TDestination>`, `Map<TSource, TDestination>`, `Map<TSource, TDestination>(source, destination)`, `MapList`, and non-generic `Map` overloads.
- `Mapper` concrete implementation.

**Profile and fluent configuration**
- `MappingProfile` base class — define maps in the constructor via `CreateMap<TSrc, TDst>()`.
- `MappingExpression<TSource, TDestination>` — fluent API: `ForMember`, `Ignore`, `MapFrom`.
- `MapperConfiguration` / `MapperConfigurationBuilder` — assemble and freeze configuration from one or more profiles.

**Dependency injection**
- `IServiceCollection.AddDeltaMapper(Action<MapperConfigurationBuilder>)` — registers `MapperConfiguration` and `IMapper` as singletons.

**Middleware pipeline**
- `IMappingMiddleware` interface.
- `MappingPipeline` — composable pre/post-mapping hooks.
- `MapperContext` — carries ambient data through the pipeline.

**Structured exceptions**
- `DeltaMapperException` — always includes source type, destination type, and a resolution hint.

**Quality**
- 79 unit tests, all passing on .NET 10.
- Zero runtime dependencies (DI abstractions package is a light interface-only reference).
- `net10.0` target, C# 14, nullable enabled throughout.

---

## Packages

| Package ID | Version | Notes |
|---|---|---|
| `DeltaMapper` | 0.1.0-alpha.1 | Core runtime, the only package in this release |
| `DeltaMapper.SourceGen` | — | Planned Phase 3 |
| `DeltaMapper.EFCore` | — | Planned Phase 4 |
| `DeltaMapper.OpenTelemetry` | — | Planned Phase 4 |

---

## Known Gaps (by design — future phases)

- No `MappingDiff<T>` / PATCH diff support (Phase 2).
- No Roslyn source generator (Phase 3).
- No EF Core proxy detection (Phase 4).
- No OpenTelemetry activity spans (Phase 4).
- No documentation site yet (README is included in the NuGet package).

---

## To Publish

When ready to tag and publish:

```
git tag v0.1.0-alpha.1
git push origin v0.1.0-alpha.1
```

The `publish-nuget.yml` GitHub Actions workflow (to be created) will pick up the `v*` tag and push the `.nupkg` to NuGet.org.

The `.nupkg` is already built locally at:
`src/DeltaMapper.Core/bin/Release/DeltaMapper.0.1.0-alpha.1.nupkg`
