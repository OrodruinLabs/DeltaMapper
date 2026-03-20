# Type Review: TASK-059 — Multi-Target Test Projects

**Reviewer**: Type Reviewer Agent
**Date**: 2026-03-20
**Diff**: `hydra/reviews/TASK-059/diff.patch`

---

## Type System Assessment

### Nullable Context Consistency

All test projects previously declared `<Nullable>enable</Nullable>` explicitly. These declarations are now removed and the value is inherited from `Directory.Build.props`. The effective nullable analysis mode is unchanged: all projects remain in `enable` mode. No nullable warnings are suppressed or introduced.

### LangVersion Per-TFM Resolution

With `<LangVersion>latest</LangVersion>` inherited from `Directory.Build.props`, each test TFM resolves to:
- `net8.0` → C# 12
- `net9.0` → C# 13
- `net10.0` → C# 14

Test code authored on C# 14 (the current TFM before this change) compiles cleanly on all three because the test files use no language features exclusive to C# 13 or C# 14 that are unavailable on C# 12. This is confirmed by 852 passing tests.

### Test Dependency Type Contracts

- `Microsoft.EntityFrameworkCore.InMemory` 8.*/9.*/10.*: The `InMemory` provider's public API is stable within each major version. Integration tests that exercise `DbContext` operations against the in-memory provider are type-compatible across all three versions because the test code targets `Microsoft.EntityFrameworkCore` abstractions (not internal types).
- `Microsoft.Extensions.DependencyInjection` 8.*/9.*/10.*: The `IServiceCollection` and `IServiceProvider` interfaces are stable across these major versions. Unit tests that exercise DI registration compile cleanly against all three versions.

### SourceGen.Tests: No TFM-Specific Type Concerns

`DeltaMapper.SourceGen.Tests` tests the Roslyn source generator, which targets `netstandard2.0`. The test project itself targets `net8.0/9.0/10.0` but exercises the generator through Roslyn's `CSharpGeneratorDriver` API, which is version-stable. No type mismatch possible.

### No C# Source Files Modified

All changes are `.csproj` configuration. Type safety validated by 284 tests per TFM.

---

## Final Verdict

**APPROVED**

Nullable context, language version inheritance, and per-TFM dependency type contracts are all correct. The test suite's 852 passing results across all three TFMs confirm no type-system regressions.
