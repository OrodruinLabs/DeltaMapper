# Code Review: TASK-058 — Directory.Build.props + Multi-Target Production Packages

**Reviewer**: Code Reviewer Agent
**Date**: 2026-03-20
**Diff**: `hydra/reviews/TASK-058/diff.patch`

---

## Build Verification

`dotnet build -c Release` — **Build succeeded, 0 errors.**

Warnings present are all pre-existing (NU1903 AutoMapper vulnerability in Benchmarks, DM001 diagnostics in Benchmarks). No new warnings introduced by this task.

All three TFMs confirmed to produce output binaries:
- `DeltaMapper.Core` → net8.0, net9.0, net10.0
- `DeltaMapper.EFCore` → net8.0, net9.0, net10.0
- `DeltaMapper.OpenTelemetry` → net8.0, net9.0, net10.0
- `DeltaMapper.SourceGen` → netstandard2.0 (unchanged)

---

## Checklist

### 1. TargetFrameworks set correctly (plural, net8.0;net9.0;net10.0)

- **DeltaMapper.Core**: `<TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>` — PASS
- **DeltaMapper.EFCore**: `<TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>` — PASS
- **DeltaMapper.OpenTelemetry**: `<TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>` — PASS
- **DeltaMapper.SourceGen**: `<TargetFramework>netstandard2.0</TargetFramework>` (singular, unchanged) — PASS

### 2. Conditional PackageReference groups per TFM

- **DeltaMapper.Core**: Three `ItemGroup` blocks with `Condition="'$(TargetFramework)' == 'netX.0'"` referencing `Microsoft.Extensions.DependencyInjection.Abstractions` versions `8.*`, `9.*`, `10.*` — PASS
- **DeltaMapper.EFCore**: Three `ItemGroup` blocks with same pattern for `Microsoft.EntityFrameworkCore` versions `8.*`, `9.*`, `10.*` — PASS
- **DeltaMapper.OpenTelemetry**: No third-party `PackageReference` existed before this task, and none was added. The package uses only `System.Diagnostics.Activity` from the BCL — no conditional groups needed — PASS

### 3. SourceGen stays on netstandard2.0

`TargetFramework` remains `netstandard2.0` (singular). Only `LangVersion` and `Nullable` were removed (now inherited from `Directory.Build.props`). `EnforceExtendedAnalyzerRules`, `IsRoslynComponent`, and all packaging properties are intact — PASS.

**Note on `ImplicitUsings` inheritance**: `Directory.Build.props` sets `<ImplicitUsings>enable</ImplicitUsings>` globally. `ImplicitUsings` on `netstandard2.0` is technically a no-op (the SDK does not generate global usings for netstandard TFMs), so there is no behavioral change for SourceGen. This is safe.

### 4. Redundant properties removed from production projects

All four production csproj files had `<Nullable>`, `<ImplicitUsings>`, and `<LangVersion>` removed. These are now inherited from `Directory.Build.props` at the repo root — PASS.

### 5. No accidental changes to non-production files

The diff touches exactly: `Directory.Build.props` (new), and the four production csproj files. All five test project csproj files are unchanged from the prior commit — PASS.

**Observation (non-blocking)**: Test projects `DeltaMapper.UnitTests`, `DeltaMapper.IntegrationTests`, and `DeltaMapper.SourceGen.Tests` still declare `<Nullable>enable</Nullable>` and/or `<ImplicitUsings>enable</ImplicitUsings>` explicitly. These are now redundant (they override the inherited value with the same value), but they do not cause any incorrect behavior. Cleaning them up in a follow-on task would be consistent with the spirit of this change.

---

## Findings

### Finding: Redundant properties in test csproj files (post-inheritance)
- **Severity**: LOW
- **Confidence**: 85
- **File**: `tests/DeltaMapper.UnitTests/DeltaMapper.UnitTests.csproj:4-6`, `tests/DeltaMapper.IntegrationTests/DeltaMapper.IntegrationTests.csproj:4-6`, `tests/DeltaMapper.SourceGen.Tests/DeltaMapper.SourceGen.Tests.csproj:4-7`
- **Category**: Code Quality
- **Verdict**: PASS (non-blocking observation)
- **Issue**: After `Directory.Build.props` was added at the repo root, three test projects still explicitly declare `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, and in one case `<LangVersion>latest</LangVersion>`. These are now redundant overrides with the same value as the inherited default.
- **Fix**: Remove the redundant properties from those three test csproj files in a follow-on cleanup task.
- **Pattern reference**: `src/DeltaMapper.Core/DeltaMapper.Core.csproj` (production projects correctly removed all three properties in this task)

---

## Summary

- PASS: `TargetFrameworks` pluralized correctly on all three production packages (net8.0;net9.0;net10.0)
- PASS: Conditional `ItemGroup` per TFM correctly maps versioned `PackageReference` for Core and EFCore
- PASS: OpenTelemetry package has no third-party dependency requiring conditional groups
- PASS: SourceGen remains on `netstandard2.0` (singular `TargetFramework`), `LangVersion`/`Nullable` removal is safe
- PASS: `Directory.Build.props` is correct and minimal (Nullable, ImplicitUsings, LangVersion only)
- PASS: Build is clean — 0 errors, 0 new warnings introduced by this task
- PASS: No accidental changes to non-production or test project files
- CONCERN: Redundant `Nullable`/`ImplicitUsings`/`LangVersion` remain in three test csproj files — non-blocking, recommend follow-on cleanup (confidence: 85/100)

---

## Final Verdict

**APPROVED**

All five checklist items pass. The multi-target implementation is correct and consistent: plural `TargetFrameworks`, properly conditioned per-TFM package version ranges, SourceGen untouched on `netstandard2.0`, redundant global properties removed from production projects, and no collateral changes to test or benchmark projects. Build verifies clean across all TFMs with zero new errors or warnings.
