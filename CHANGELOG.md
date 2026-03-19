# Changelog

All notable changes to DeltaMapper are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
DeltaMapper uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

---

## [0.2.0-alpha] - 2026-03-19

### Added

#### Flattening

- Automatic flattening of nested properties by convention — `Order.Customer.Name` maps to destination property `CustomerName` with no configuration required
- Multi-level chains are supported: `Order.Customer.Address.Zip` → `CustomerAddressZip`
- Null-safe access throughout the flattened chain — a null intermediate object returns null rather than throwing
- Flattened path getters are compiled into expression delegates at build time; no reflection overhead at map time

#### Unflattening

- Automatic reverse of flattening — flat source properties (`CustomerName`, `CustomerEmail`) are grouped into a nested destination object (`Customer.Name`, `Customer.Email`) by convention, using the destination property name as the prefix to match
- Triggered automatically when the destination exposes a complex-type property and the source has flat properties whose names start with that property name
- Works alongside regular convention mapping on the same type map
- Round-trip support: flatten a nested object to a flat DTO and unflatten back to recover the original structure

#### Assembly Scanning

- `cfg.AddProfilesFromAssembly(Assembly assembly)` — scans an assembly for all concrete `MappingProfile` subclasses that have parameterless constructors and registers them automatically
- `cfg.AddProfilesFromAssemblyContaining<T>()` — convenience overload that resolves the assembly from `typeof(T).Assembly`
- Abstract profiles, generic profile definitions, and profiles without a parameterless constructor are silently skipped during scanning
- Compatible with explicit `AddProfile<T>()` — both can be used together in the same configuration block

#### Type Converters

- `cfg.CreateTypeConverter<TSource, TDest>(Func<TSource, TDest> converter)` — registers a global type converter that is applied across all maps whenever a source property of type `TSource` maps to a destination property of type `TDest`
- Multiple converters for different type pairs can be registered in one configuration; all apply independently
- Convention direct-assign path is unchanged for same-type properties — converters only activate when the source and destination types differ
- Null source values are handled safely: the converter delegate is not invoked and the destination receives null/default

### Fixed

- Assembly scanning now skips open generic profile types, preventing a false-positive `MissingMethodException` on registration
- Flattening: skip properties with incompatible leaf types instead of runtime `InvalidCastException`
- Flattening: allow `Nullable<T>` ↔ `T` assignments for value types
- EF Core proxy middleware: implemented actual collection navigation skipping (was a no-op stub)
- Build() validation: throws `DeltaMapperException` when no type maps are registered (fail-fast)

---

## [0.1.0-alpha] - 2026-03-18

Initial release.

### Core

- `MapperConfiguration.Create()` — static factory; compiles all maps at startup into a `FrozenDictionary`
- `IMapper` with `Map<T>()`, `Map<TSrc, TDst>()`, `MapList<>()`, and non-generic overloads
- Fluent profile API — `CreateMap`, `ForMember`, `Ignore`, `NullSubstitute`, `BeforeMap`, `AfterMap`, `ReverseMap`
- Convention matching — case-insensitive property name matching, numeric widening, collection mapping, recursive complex types
- Records and init-only properties — automatic constructor injection
- Circular reference detection via `ReferenceEqualityComparer`
- Middleware pipeline with zero overhead when no middleware is registered
- `AddDeltaMapper()` DI extension for ASP.NET Core / Generic Host
- `DeltaMapperException` with actionable error messages

### MappingDiff

- `MappingDiff<T>` — structured change set with `Result`, `HasChanges`, and `Changes`
- `Patch<TSrc, TDst>()` — map onto existing instance and return per-property changes
- `PropertyChange` record with `PropertyName`, `From`, `To`, `ChangeKind`
- Dot-notation paths for nested changes, index-based collection tracking

### Source Generator (`DeltaMapper.SourceGen`)

- `[GenerateMap]` attribute for compile-time mapping code generation
- Zero-reflection direct-call methods (`Profile.MapXToY()`) at ~7 ns
- `GeneratedMapRegistry` with `[ModuleInitializer]` auto-registration
- Fast-path bypass in `IMapper.Map<>()` when no middleware/profile override
- `DM001` (unmapped destination property) and `DM002` (unresolvable type) diagnostics

### EF Core (`DeltaMapper.EFCore`)

- `EFCoreProxyMiddleware` — detects Castle.Core dynamic proxies
- `AddEFCoreSupport()` extension method

### OpenTelemetry (`DeltaMapper.OpenTelemetry`)

- `TracingMiddleware` — `Activity` spans with `mapper.source_type` and `mapper.dest_type` tags
- `AddMapperTracing()` extension method
- `HasListeners()` fast path — zero allocation when no listener attached

### Performance

- Compiled expression delegates replace `PropertyInfo.GetValue/SetValue` reflection
- Compiled `Expression.New()` factory replaces `Activator.CreateInstance`
- Lazy `MapperContext` — no `Dictionary` allocation for flat mappings
- Pipeline closure skipped when no middleware registered
- Object initializer pattern in source-gen factory methods
- Cached fast-path routing per type pair

### Infrastructure

- GitHub Actions CI (build + test on PRs and pushes to main)
- On-demand benchmark workflow via `workflow_dispatch`
- BenchmarkDotNet suite comparing against Mapperly, AutoMapper, and hand-written code

[Unreleased]: https://github.com/OrodruinLabs/DeltaMapper/compare/v0.2.0-alpha...HEAD
[0.2.0-alpha]: https://github.com/OrodruinLabs/DeltaMapper/compare/v0.1.0-alpha...v0.2.0-alpha
[0.1.0-alpha]: https://github.com/OrodruinLabs/DeltaMapper/releases/tag/v0.1.0-alpha
