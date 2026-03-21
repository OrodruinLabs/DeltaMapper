# Changelog

All notable changes to DeltaMapper are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
DeltaMapper uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

---

## [1.0.0-rc.7] — 2026-03-21

### Added
- `MemberList` enum (`Destination`, `Source`, `None`) for per-map validation control
- `CreateMap<TSrc, TDst>(MemberList)` overload on `Profile`
- Source member consumption tracking for `MemberList.Source` validation

---

## [1.0.0-rc.6] — 2026-03-21

### Added
- `IMapper.Map(object source, object destination)` — non-generic map-into-existing overload
- `IMapper.Map<TDest>(object source, TDest destination)` — semi-generic map-into-existing overload
- `MapperConfiguration.AssertConfigurationIsValid()` — runtime configuration validation

---

## [1.0.0-rc.5] — 2026-03-20

### Changed
- **BREAKING:** Consolidated consumer-facing types into root `DeltaMapper` namespace
  - `IMapper`, `Profile`, `MapperConfiguration` — previously in `DeltaMapper.Abstractions` / `DeltaMapper.Configuration`
  - `MappingDiff<T>`, `PropertyChange`, `ChangeKind` — previously in `DeltaMapper.Diff`
  - `DeltaMapperException` — previously in `DeltaMapper.Exceptions`
  - `AddDeltaMapper()` — previously in `DeltaMapper.Extensions`
- Migration: replace all old `using` statements with single `using DeltaMapper;`

---

## [1.0.0-rc.4] — 2026-03-20

### Added
- Multi-target support for .NET 8, .NET 9, and .NET 10 across all packages
- `Directory.Build.props` for centralized build configuration
- CI test matrix running full test suite on all three TFMs

### Changed
- Minimum supported framework lowered from .NET 10 to .NET 8 LTS
- Conditional Microsoft.Extensions and EntityFrameworkCore package versions per TFM
- GitHub Actions workflows install .NET 8, 9, and 10 SDKs

---

## [1.0.0-rc.3] - 2026-03-20

### Fixed

- **Nested MapFrom resolution in `CompileConstructorMap`** — DTOs with `{ get; init; }` properties route through `CompileConstructorMap` which was missing the recursive type map check. `ForMember(d => d.Brand, o => o.MapFrom(s => s.InstrumentBrand))` now correctly applies the registered `InstrumentBrand → BasicEntityDto` map instead of throwing `ArgumentException`.
- Extracted `NeedsRecursiveMapping()` shared helper — eliminates 3 inline copies of the same check across `CompileTypeMap` and `CompileConstructorMap`.

---

## [1.0.0-rc.2] - 2026-03-20

Migration friction release. Closes the top friction points discovered during a real-world migration to DeltaMapper.

### Breaking Changes

- **`MappingProfile` renamed to `Profile`** — shorter base class name eliminates "class has same name as base class" static analysis warning. Update: `: MappingProfile` → `: Profile`
- **`MapList` removed** — superseded by `Map<S,D>(IEnumerable<S>)` collection overload. Update: `mapper.MapList<S,D>(list)` → `mapper.Map<S,D>(list)`

### Added

- **`Map<S,D>(IEnumerable<S>)`** — collection mapping overload returning `List<D>`
- **`ConstructUsing(Func<TSrc, TDst>)`** — custom factory construction for DDD entities with private constructors and static factory methods
- **Nested type resolution in `MapFrom`** — `ForMember(d => d.Nav, o => o.MapFrom(s => s.Nav))` now auto-resolves registered type maps instead of assigning raw source type
- **`Nullable<T>` → `T` auto-coercion** — assigns `default(T)` instead of skipping (e.g., `Guid?` → `Guid.Empty`)
- **Smart single-generic collection mapping** — `mapper.Map<IEnumerable<Dest>>(list)` auto-detects collection intent and routes to element-wise mapping
- **`AddProfilesFromAssembly(assembly, includeReferencedAssemblies: true)`** — opt-in scanning of referenced assemblies for Profile subclasses

### Fixed

- Circular reference cache now keys on `(source, destType)` instead of just source — fixes incorrect cache hits when same source maps to different destination types
- `ConstructUsing` preserves convention mapping — factory creates the object, then convention + ForMember mappings run on top
- Null factory guard scoped to `ConstructUsing` path only
- `IsAssignableFrom` check on nested `MapFrom` resolution prevents unnecessary recursive mapping for derived types

---

## [1.0.0-rc.1] - 2026-03-19

First release candidate. Consolidates all features from 0.1.0-alpha and 0.2.0-alpha.

### Core

- `MapperConfiguration.Create()` — static factory; compiles all maps at startup into a `FrozenDictionary`
- `IMapper` with `Map<T>()`, `Map<TSrc, TDst>()`, `Map<>(IEnumerable)`, and non-generic overloads
- Fluent profile API — `CreateMap`, `ForMember`, `Ignore`, `NullSubstitute`, `BeforeMap`, `AfterMap`, `ReverseMap`
- Convention matching — case-insensitive property name matching, numeric widening, collection mapping, recursive complex types
- Records and init-only properties — automatic constructor injection
- Circular reference detection via `ReferenceEqualityComparer`
- Middleware pipeline with zero overhead when no middleware is registered
- `AddDeltaMapper()` DI extension for ASP.NET Core / Generic Host
- `DeltaMapperException` with actionable error messages

### Flattening and Unflattening

- Automatic flattening of nested properties by convention (`Order.Customer.Name` → `CustomerName`)
- Automatic unflattening to rebuild nested objects from flat sources
- Multi-level chains, null-safe access, round-trip support
- Compiled expression delegates — no reflection overhead at map time

### Assembly Scanning

- `AddProfilesFromAssembly(Assembly)` and `AddProfilesFromAssemblyContaining<T>()` for bulk profile registration
- Abstract profiles, generic definitions, and missing parameterless constructors are silently skipped

### Type Converters

- `CreateTypeConverter<TSource, TDest>(Func<TSource, TDest>)` — global type pair conversion across all maps
- Null-safe: converter is not invoked for null source values

### Conditional Mapping

- `.Condition(Expression<Func<TSrc, bool>>)` — skip a property mapping when the predicate returns false
- Works with `MapFrom` and `NullSubstitute`. Combining with `Ignore` throws `InvalidOperationException` at configuration time

### MappingDiff and Patch

- `MappingDiff<T>` — structured change set with `Result`, `HasChanges`, and `Changes`
- `Patch<TSrc, TDst>()` — map onto existing instance and return per-property changes
- `PropertyChange` record with dot-notation paths for nested changes

### Source Generator (`DeltaMapper.SourceGen`)

- `[GenerateMap]` attribute for compile-time mapping code generation
- Zero-reflection direct-call methods at ~7 ns
- `GeneratedMapRegistry` with `[ModuleInitializer]` auto-registration
- `DM001` and `DM002` analyzer diagnostics

### EF Core (`DeltaMapper.EFCore`)

- `EFCoreProxyMiddleware` — detects Castle.Core dynamic proxies and skips unloaded navigation properties
- `AddEFCoreSupport()` extension method

### OpenTelemetry (`DeltaMapper.OpenTelemetry`)

- `TracingMiddleware` — `Activity` spans with source/destination type tags
- `AddMapperTracing()` extension method with `HasListeners()` fast path

### Performance

- Compiled expression delegates replace `PropertyInfo.GetValue/SetValue` reflection
- Compiled `Expression.New()` factory replaces `Activator.CreateInstance`
- Lazy `MapperContext` — no `Dictionary` allocation for flat mappings
- Pipeline closure skipped when no middleware registered
- Object initializer pattern in source-gen factory methods
- Cached fast-path routing per type pair

### Fixed

- Assembly scanning now skips open generic profile types, preventing a false-positive `MissingMethodException` on registration
- Flattening: skip properties with incompatible leaf types instead of runtime `InvalidCastException`
- Flattening: allow `Nullable<T>` ↔ `T` assignments for value types
- EF Core proxy middleware: implemented actual collection navigation skipping (was a no-op stub)
- Build() validation: throws `DeltaMapperException` when no type maps are registered (fail-fast)

### Infrastructure

- GitHub Actions CI (build + test on PRs and pushes to main)
- On-demand benchmark workflow via `workflow_dispatch`
- BenchmarkDotNet suite comparing against Mapperly, AutoMapper, and hand-written code

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
- `IMapper` with `Map<T>()`, `Map<TSrc, TDst>()`, `Map<>(IEnumerable)`, and non-generic overloads
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

[Unreleased]: https://github.com/OrodruinLabs/DeltaMapper/compare/v1.0.0-rc.7...HEAD
[1.0.0-rc.7]: https://github.com/OrodruinLabs/DeltaMapper/compare/v1.0.0-rc.6...v1.0.0-rc.7
[1.0.0-rc.6]: https://github.com/OrodruinLabs/DeltaMapper/compare/v1.0.0-rc.5...v1.0.0-rc.6
[1.0.0-rc.5]: https://github.com/OrodruinLabs/DeltaMapper/compare/v1.0.0-rc.4...v1.0.0-rc.5
[1.0.0-rc.4]: https://github.com/OrodruinLabs/DeltaMapper/compare/v1.0.0-rc.3...v1.0.0-rc.4
[1.0.0-rc.3]: https://github.com/OrodruinLabs/DeltaMapper/compare/v1.0.0-rc.2...v1.0.0-rc.3
[1.0.0-rc.2]: https://github.com/OrodruinLabs/DeltaMapper/compare/v1.0.0-rc.1...v1.0.0-rc.2
[1.0.0-rc.1]: https://github.com/OrodruinLabs/DeltaMapper/compare/v0.2.0-alpha...v1.0.0-rc.1
[0.2.0-alpha]: https://github.com/OrodruinLabs/DeltaMapper/compare/v0.1.0-alpha...v0.2.0-alpha
[0.1.0-alpha]: https://github.com/OrodruinLabs/DeltaMapper/releases/tag/v0.1.0-alpha
