# Release Notes — DeltaMapper v0.3.0-alpha.1 (FEAT-004)

Release date: 2026-03-17
Git tag: v0.3.0-alpha.1
Packages: DeltaMapper, DeltaMapper.SourceGen

---

## Summary

Phase 3 adds a Roslyn incremental source generator that emits mapping code at build time.
Consumers who annotate a profile class with `[GenerateMap]` get a fully generated, strongly-typed
assignment method with zero reflection at the call site. A `GeneratedMapRegistry` with a
`ModuleInitializer` auto-registers all generated maps before `Main` runs, with a transparent
fallback to the runtime `MapperConfiguration` when no generated map is found.

This release also stabilises the Phase 2 work (MappingDiff / Patch) that shipped in v0.2.0-alpha.1
but was not yet tagged.

### Phase 2 deliverables (FEAT-003 — included in this tag)
- `MappingDiff<T>` value object capturing per-property before/after pairs
- `IMapper.Patch<TSource, TDestination>()` applying only changed properties
- Fluent `ForMember` diff overrides and `NullSubstitute` support in patch paths

### Phase 3 deliverables (FEAT-004)
- `[GenerateMap(typeof(TSource), typeof(TDst))]` attribute (emitted as source text by the generator itself)
- `DeltaMapperIncrementalGenerator` — `IIncrementalGenerator` implementation targeting `netstandard2.0`
- Generated `Map_TSource_TDst(TSource src, TDst dst)` static methods in a `DeltaMapper.Generated` partial class
- Support for nested complex types, `IEnumerable<T>` collections, and `[Ignore]` on individual members
- `GeneratedMapRegistry` in `DeltaMapper.Core` with `ModuleInitializer` auto-registration
- Fallback path: `MapperConfiguration` checks `GeneratedMapRegistry` before compiling a runtime delegate
- Analyzer diagnostics
  - `DM001` — decorated class is missing the `partial` keyword
  - `DM002` — source or destination type is not accessible to the generator
  - `DM003` — duplicate / conflicting `[GenerateMap]` declarations on the same class
- 136 passing tests across core, source-gen, and integration suites (up from 95 at Phase 2)

---

## Breaking changes

None. All public API surface from v0.1.0-alpha.1 and v0.2.0-alpha.1 is preserved.
The `GeneratedMapRegistry` is additive; existing configurations that do not use `[GenerateMap]`
are unaffected.

---

## Migration notes

### Adopting the source generator (optional)

1. Add a package reference to `DeltaMapper.SourceGen` (same version as `DeltaMapper`).
2. Make your profile class `partial` and annotate it:

```csharp
[GenerateMap(typeof(OrderDto), typeof(Order))]
public partial class OrderProfile : MappingProfile { }
```

3. Build. The generator emits `DeltaMapper.Generated.g.cs` into your project's `obj/` tree.
   The `ModuleInitializer` registers all generated maps automatically before `Main` runs.
4. No changes to your `MapperConfiguration.Create(...)` call are required.

### Projects that do NOT use [GenerateMap]

No action needed. `DeltaMapper.SourceGen` is an optional add-on package.
The core `DeltaMapper` package has no dependency on it.

---

## Package dependency matrix

| Package | Version | Depends on |
|---|---|---|
| `DeltaMapper` | 0.3.0-alpha.1 | `Microsoft.Extensions.DependencyInjection.Abstractions 10.*` |
| `DeltaMapper.SourceGen` | 0.3.0-alpha.1 | `Microsoft.CodeAnalysis.CSharp 4.*` (PrivateAssets=all) |

`DeltaMapper.SourceGen` does NOT carry a runtime dependency on `DeltaMapper`.
The generated code references types from `DeltaMapper` directly, which must already be in the
consumer's project graph.

---

## What's next — Phase 4

- `DeltaMapper.EFCore` — proxy-aware middleware that detects change-tracked entity state
  before mapping, skipping unchanged navigation properties
- `DeltaMapper.OpenTelemetry` — `TracingMiddleware` that emits mapping spans and tag sets
  compatible with the OTel semantic conventions

Phase 4 tracking task: FEAT-005 (not yet planned).
