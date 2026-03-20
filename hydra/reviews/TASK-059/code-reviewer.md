# Code Review: TASK-059 — Multi-Target Test Projects

**Reviewer**: Code Reviewer Agent
**Date**: 2026-03-20
**Diff**: `hydra/reviews/TASK-059/diff.patch`

---

## Checklist

### 1. TargetFrameworks set correctly (plural, net8.0;net9.0;net10.0)

- `DeltaMapper.UnitTests`: `<TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>` — PASS
- `DeltaMapper.IntegrationTests`: `<TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>` — PASS
- `DeltaMapper.SourceGen.Tests`: `<TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>` — PASS
- `DeltaMapper.TestFixtures`: `<TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>` — PASS
- `DeltaMapper.Benchmarks`: `<TargetFramework>net10.0</TargetFramework>` (singular, unchanged) — PASS

### 2. Conditional PackageReference groups per TFM

- `DeltaMapper.UnitTests`: Three `ItemGroup Condition` blocks for `Microsoft.Extensions.DependencyInjection` versions 8.*, 9.*, 10.* — PASS
- `DeltaMapper.IntegrationTests`: Three `ItemGroup Condition` blocks for `Microsoft.EntityFrameworkCore.InMemory` versions 8.*, 9.*, 10.* — PASS
- `DeltaMapper.SourceGen.Tests`: No per-TFM conditional package groups needed (no versioned framework deps) — PASS
- `DeltaMapper.TestFixtures`: No per-TFM conditional package groups needed (no versioned framework deps) — PASS

### 3. Benchmarks stays net10.0

`DeltaMapper.Benchmarks` remains single-targeted at `net10.0`. Only the redundant `<Nullable>`, `<ImplicitUsings>`, and `<LangVersion>` properties were removed (now inherited). No functional change — PASS.

### 4. Redundant properties removed consistently

All five test projects had duplicate `Nullable`/`ImplicitUsings`/`LangVersion` declarations removed. This aligns with the TASK-058 note that these were non-blocking observations on test projects at the time — now resolved — PASS.

### 5. TestFixtures: IsPackable added

`<IsPackable>false</IsPackable>` added to `DeltaMapper.TestFixtures`. This was a missing hygiene flag — beneficial addition — PASS.

### 6. No test logic modified

The diff touches only `.csproj` files under `tests/`. No `.cs` test files are modified. All 852 tests (284 per TFM x 3) continue to pass — PASS.

---

## Findings

No blocking findings.

**Observation (non-blocking)**: `DeltaMapper.SourceGen.Tests` does not have conditional per-TFM packages, which is correct — it only references the SourceGen project (netstandard2.0) and xunit/FluentAssertions which do not require major-version alignment. This is intentional and correct.

---

## Final Verdict

**APPROVED**

All five test projects are correctly configured: four multi-targeted with appropriate per-TFM dependency binding, Benchmarks remains net10.0-only, `IsPackable` hygiene is corrected on TestFixtures, and all redundant inherited properties are cleaned up. Build and test validation (852 tests, 3 TFMs) confirms correctness.
