# Type Review: TASK-058 — Directory.Build.props + Multi-Target Production Packages

**Reviewer**: Type Reviewer Agent
**Date**: 2026-03-20
**Diff**: `hydra/reviews/TASK-058/diff.patch`

---

## Type System Assessment

### Nullable Annotations

`Directory.Build.props` sets `<Nullable>enable</Nullable>` globally. This was previously set per-project on all four production packages. The effective behavior is unchanged — all projects were already in nullable-enabled mode. No nullable analysis gaps are introduced.

`DeltaMapper.SourceGen` previously declared `<Nullable>enable</Nullable>` explicitly; that property is now removed and inherited. Roslyn honors `Directory.Build.props` for `netstandard2.0` targets for the compiler's nullability context even though no global usings are generated. The nullable analysis on the source generator remains active.

### LangVersion Inheritance

`<LangVersion>latest</LangVersion>` is inherited from `Directory.Build.props`. For `net8.0` targets, `latest` resolves to C# 12; for `net9.0` it resolves to C# 13; for `net10.0` it resolves to C# 14. This is the intended behavior — each TFM gets the latest language features available for that runtime. No type-system incompatibilities arise because the codebase does not use features unavailable on the lower TFMs (this is validated by the 852 passing tests).

### ImplicitUsings

`<ImplicitUsings>enable</ImplicitUsings>` generates global using directives from the SDK's default set for each TFM. The global using sets for `net8.0`, `net9.0`, and `net10.0` are identical for `Microsoft.NET.Sdk` — the additions in newer SDKs are non-breaking additions, not removals. No type resolution conflicts.

### No Source File Changes

No C# source files were modified. All type signatures, generic constraints, expression trees, and interface implementations are unchanged. The multi-TFM change cannot introduce type errors in existing code — the compiler validates each TFM independently, and test results (852 passing) confirm no type errors across all three.

### Per-TFM Package Version Alignment

The conditional `PackageReference` version bounds (`8.*`, `9.*`, `10.*`) ensure each TFM resolves the matching major version of `Microsoft.Extensions.DependencyInjection.Abstractions` and `Microsoft.EntityFrameworkCore`. This prevents misaligned interface versions that could cause `MissingMethodException` at runtime. The type contracts between `net8.0` and `net10.0` builds of these packages are additive (Microsoft guarantees binary compatibility within a major version).

---

## Final Verdict

**APPROVED**

Nullable context, `LangVersion`, and `ImplicitUsings` are correctly inherited with no behavioral change. Per-TFM package versioning correctly aligns type contracts to each runtime. No C# source files modified — type safety is validated by 852 passing tests.
