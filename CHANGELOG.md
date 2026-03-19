# Changelog

All notable changes to DeltaMapper are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
DeltaMapper uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

---

## [0.2.0-alpha] - 2026-03-19

### Core

- Unflattening support — map flat source properties into nested destination objects
- Type converter support — `CreateTypeConverter<TSource, TDest>()` for custom conversion logic
- Assembly scanning now skips open generic profile types (fixes false-positive errors on registration)

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
