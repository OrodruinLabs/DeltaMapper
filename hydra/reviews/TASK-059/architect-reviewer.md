# Architect Review: TASK-059 — Multi-Target Test Projects

**Reviewer**: Architect Reviewer Agent
**Date**: 2026-03-20
**Diff**: `hydra/reviews/TASK-059/diff.patch`

---

## Architectural Assessment

### Test Coverage Strategy

Multi-targeting the test suite ensures each production TFM is exercised independently. The decision to multi-target `UnitTests`, `IntegrationTests`, `SourceGen.Tests`, and `TestFixtures` — while keeping `Benchmarks` at `net10.0` — is architecturally sound:

- **Test projects**: Multi-TFM validates behavior parity across .NET 8, 9, and 10. This catches any runtime behavioral differences (e.g., collection semantics, reflection changes, BCL behavior).
- **Benchmarks**: Benchmarks are not a correctness gate — they measure performance on the current production runtime. Keeping Benchmarks at `net10.0` avoids misleading cross-TFM performance comparisons and is the conventional approach for BenchmarkDotNet suites.

### TestFixtures: IsPackable Addition

`DeltaMapper.TestFixtures` gained `<IsPackable>false</IsPackable>`. This is correct and was previously absent — test fixture libraries should never be packed as NuGet packages. This is a beneficial hygiene fix bundled with the multi-target change.

### Dependency Alignment: IntegrationTests

`DeltaMapper.IntegrationTests` correctly maps `Microsoft.EntityFrameworkCore.InMemory` to matching major versions per TFM (8.*, 9.*, 10.*). This mirrors the pattern in `DeltaMapper.EFCore` production csproj and ensures the in-memory provider matches the EFCore major version under test.

### Dependency Alignment: UnitTests

`DeltaMapper.UnitTests` correctly maps `Microsoft.Extensions.DependencyInjection` to matching major versions per TFM (8.*, 9.*, 10.*). This matches the production package's `DependencyInjection.Abstractions` version alignment.

### Redundant Properties Removed

All test projects had `<Nullable>`, `<ImplicitUsings>`, and `<LangVersion>` removed where they duplicated the inherited value from `Directory.Build.props`. This is consistent with the cleanup done on production projects in TASK-058.

---

## Final Verdict

**APPROVED**

The test multi-targeting strategy is architecturally correct. All four test projects correctly receive per-TFM dependency version alignment, Benchmarks stays appropriately single-targeted, `IsPackable` is now correctly set on TestFixtures, and redundant inherited properties are removed consistently.
