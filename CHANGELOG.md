# Changelog

All notable changes to DeltaMapper are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
DeltaMapper uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Planned
- `MappingDiff<T>` and `IMapper.Patch<TSource, TDestination>()` (Phase 2)
- Roslyn source generator with `[GenerateMap]` attribute (Phase 3)
- EF Core proxy-aware middleware (Phase 4)
- OpenTelemetry `TracingMiddleware` (Phase 4)
- BenchmarkDotNet results published to `BENCHMARKS.md` (Phase 5)

---

## [0.1.0-alpha.1] - 2026-03-17

Initial release. Covers Phase 1 (runtime core) and the .NET 10 / C# 14 migration.

### Added

#### Core mapping engine
- `MapperConfiguration.Create(Action<MapperConfigurationBuilder>)` — static factory; compiles all maps at startup into a `FrozenDictionary`, never at call time
- `MapperConfigurationBuilder.AddProfile<TProfile>()` and `AddProfile(MappingProfile)` — register profiles by generic type or instance
- `MapperConfigurationBuilder.Use<TMiddleware>()` — register `IMappingMiddleware` implementations
- `MapperConfiguration.CreateMapper()` — produces an `IMapper` singleton backed by the frozen registry

#### `IMapper` interface and `Mapper` implementation
- `Map<TDestination>(object source)` — source type inferred from runtime type
- `Map<TSource, TDestination>(TSource source)` — compile-time source type
- `Map<TSource, TDestination>(TSource source, TDestination destination)` — map onto an existing destination instance
- `MapList<TSource, TDestination>(IEnumerable<TSource> source)` — maps a sequence, returns `IReadOnlyList<TDestination>`
- `Map(object source, Type sourceType, Type destinationType)` — non-generic overload for dynamic scenarios

#### Fluent profile API
- `MappingProfile` base class with `CreateMap<TSrc, TDst>()` returning `IMappingExpression<TSrc, TDst>`
- `ForMember(dst => dst.Prop, o => o.MapFrom(src => ...))` — custom value resolver via expression
- `ForMember(dst => dst.Prop, o => o.Ignore())` — exclude a destination member
- `ForMember(dst => dst.Prop, o => o.NullSubstitute(value))` — fallback value when source is null
- `BeforeMap(Action<TSrc, TDst>)` — hook invoked before property assignment
- `AfterMap(Action<TSrc, TDst>)` — hook invoked after property assignment
- `ReverseMap()` — registers a convention-matched reverse map (`TDst -> TSrc`)

#### Convention matching (zero configuration required)
- Same property name (case-insensitive) + same or assignable type — direct assign
- Same property name + safe numeric widening (e.g., `int` to `long`, `float` to `double`) — `Convert.ChangeType`
- Same property name + `IEnumerable<T>` on both sides — per-element recursive mapping; produces `List<T>` or `T[]`
- Same property name + complex reference type — recursive map lookup

#### Records and init-only properties
- Automatic detection of `init`-only setters via `IsExternalInit` modreq
- Constructor injection path: selects the best-matching public constructor by parameter name (case-insensitive)
- Init-only properties not covered by the constructor are assigned after construction

#### Circular reference detection
- `MapperContext` tracks source-to-destination pairs using `ReferenceEqualityComparer`; returns the already-mapped instance on re-entry

#### Middleware pipeline
- `IMappingMiddleware` interface — `Map(source, destType, ctx, next)`
- `MappingPipeline` chains middleware inside-out around the core delegate; zero overhead when no middleware is registered

#### DI integration
- `IServiceCollection.AddDeltaMapper(Action<MapperConfigurationBuilder>)` extension method
- Registers `MapperConfiguration` and `IMapper` as singletons

#### Error handling
- `DeltaMapperException` — always includes source type name, destination type name, and a resolution hint
- `DeltaMapperException.ForMissingMapping(Type, Type)` factory for missing-registration errors

### Changed
- Target framework: `net10.0` only (no multi-targeting)
- `Microsoft.Extensions.DependencyInjection.Abstractions` dependency pinned to `10.*`
- `ServiceCollectionExtensions` uses C# 14 extension members (`extension(IServiceCollection services) { ... }`)
- Primary constructors adopted throughout (`MappingPipeline`, `Mapper`, internal types)
- Collection expressions (`[...]`) replace `new List<T>()` where applicable
- Null-conditional assignment (`??=`) adopted where applicable

[Unreleased]: https://github.com/OrodruinLabs/DeltaMapper/compare/v0.1.0-alpha.1...HEAD
[0.1.0-alpha.1]: https://github.com/OrodruinLabs/DeltaMapper/releases/tag/v0.1.0-alpha.1
