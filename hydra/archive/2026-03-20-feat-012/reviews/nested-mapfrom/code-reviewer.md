# Code Review: Nested Type Resolution in MapFrom

**Reviewer**: Code Reviewer (Claude Opus 4.6)
**Date**: 2026-03-20

---

## Spec Compliance Check

| Requirement | Status |
|---|---|
| 1. `ResolverReturnType` on `MemberConfiguration` | PASS |
| 2. `MemberOptions.MapFrom<TResult>` captures `typeof(TResult)` | PASS |
| 3. `MappingExpression.ForMember` passes it through | PASS |
| 4. CompileTypeMap CustomResolver block: recursive mapping when types differ | PASS (main path only) |
| 5. No double-map when user returns destination type | PASS |
| 6. Null handling assigns null | PASS |
| 7. Three tests covering all spec requirements | PASS |

---

## Findings

### Finding 1: Init-only property path missing recursive mapping logic
- **Severity**: MEDIUM
- **Confidence**: 92
- **File**: `src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:682-688`
- **Category**: Code Quality
- **Verdict**: CONCERN
- **Issue**: The init-only property assignment path (`BuildInitOnlyAssignments`) uses `setter.SetValue(dst, resolver(src))` without checking `ResolverReturnType` for recursive mapping. If a destination type uses init-only properties and a `MapFrom` resolver returns a different complex type, the nested mapping will be skipped and a type mismatch will occur at runtime.
- **Fix**: Mirror the recursive mapping check from the main `CompileTypeMap` path (lines 186-218) in the init-only path at line 682-688. Extract the logic into a shared helper to avoid duplication.
- **Pattern reference**: `MapperConfigurationBuilder.cs:186-218` (the correct pattern in the main path)

### Finding 2: Constructor parameter path missing recursive mapping logic
- **Severity**: MEDIUM
- **Confidence**: 90
- **File**: `src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:591-596`
- **Category**: Code Quality
- **Verdict**: CONCERN
- **Issue**: The constructor parameter resolver path also uses `resolver(src)` directly without checking `ResolverReturnType`. If a constructor parameter is mapped via `MapFrom` and the resolver returns a type that needs recursive mapping, it will fail at runtime with a type mismatch.
- **Fix**: Apply the same `needsRecursiveMap` check and `ctx.Config.Execute` call for the constructor parameter path. Consider extracting a `ResolveWithPotentialRecursion` helper used by all three code paths.
- **Pattern reference**: `MapperConfigurationBuilder.cs:186-218`

### Finding 3: Test class is not sealed
- **Severity**: LOW
- **Confidence**: 95
- **File**: `tests/DeltaMapper.UnitTests/NestedMapFromTests.cs:6`
- **Category**: Code Quality
- **Verdict**: CONCERN
- **Issue**: `NestedMapFromTests` is declared as `public class` but project convention is to use `sealed` on all non-abstract implementation classes.
- **Fix**: Change to `public sealed class NestedMapFromTests`.
- **Pattern reference**: Check other test files in the project for sealed usage.

### Finding 4: Tests use raw xUnit Assert instead of FluentAssertions
- **Severity**: LOW
- **Confidence**: 85
- **File**: `tests/DeltaMapper.UnitTests/NestedMapFromTests.cs:54-97`
- **Category**: Code Quality
- **Verdict**: CONCERN
- **Issue**: Project convention specifies FluentAssertions `.Should()` syntax for tests, but this file uses `Assert.Equal`, `Assert.NotNull`, `Assert.Null`.
- **Fix**: Convert to FluentAssertions: `result.Id.Should().Be(1)`, `result.Customer.Should().NotBeNull()`, `result.Customer.Should().BeNull()`, etc.
- **Pattern reference**: Project conventions specify `Method_Scenario_ExpectedBehavior` naming with FluentAssertions.

### Finding 5: Test method naming does not follow convention
- **Severity**: LOW
- **Confidence**: 80
- **File**: `tests/DeltaMapper.UnitTests/NestedMapFromTests.cs:43,61,89`
- **Category**: Code Quality
- **Verdict**: CONCERN
- **Issue**: Test names use underscored-phrase style (`MapFrom_with_registered_nested_map_resolves_via_type_map`) rather than the project convention of `Method_Scenario_ExpectedBehavior`.
- **Fix**: Rename to match the convention pattern, e.g., `MapFrom_RegisteredNestedTypeMap_ResolvesRecursively`.

---

## Summary

- PASS: `ResolverReturnType` property added to `MemberConfiguration` -- correctly typed as `Type?`
- PASS: `MemberOptions.MapFrom<TResult>` captures `typeof(TResult)` -- line 20 of MemberOptions.cs
- PASS: `MappingExpression.ForMember` passes `ResolverReturnType` through -- line 28 of MappingExpression.cs
- PASS: Main CustomResolver block implements recursive mapping correctly -- null check, `ctx.Config.Execute`, direct assignment fallback
- PASS: Three tests cover nested resolution, no-double-map, and null handling
- PASS: Build succeeds with zero new warnings
- CONCERN: Init-only property path skips recursive mapping logic (confidence: 92/100, non-blocking but risks runtime failure for init-only properties)
- CONCERN: Constructor parameter path skips recursive mapping logic (confidence: 90/100, non-blocking but risks runtime failure for constructor-mapped parameters)
- CONCERN: Test class not sealed (confidence: 95/100, non-blocking)
- CONCERN: Tests use xUnit Assert instead of FluentAssertions (confidence: 85/100, non-blocking)
- CONCERN: Test method naming does not follow `Method_Scenario_ExpectedBehavior` convention (confidence: 80/100, non-blocking)

## Final Verdict

**APPROVED**

The core implementation is spec-compliant and correct for the primary mapping path. The recursive mapping logic in `CompileTypeMap` correctly handles all three specified scenarios (nested resolution, no double-map, null). The missing recursive mapping in the init-only and constructor paths are real gaps but are non-blocking since the spec only required changes to the `CompileTypeMap` CustomResolver block, and the common case (mutable destination properties) is fully covered. These gaps should be addressed as a follow-up.
