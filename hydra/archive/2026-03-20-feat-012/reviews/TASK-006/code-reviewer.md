# Code Review: TASK-006 -- AutoMapper Parity Integration Test + MapperContext Bugfix

## Checklist Verification

### 1. Integration test exercises all 5 features

The test in `AutoMapperParityTests.cs` covers:

| # | Feature | How exercised |
|---|---------|---------------|
| 1 | Nullable coercion | `Guid? TrackingId = null` maps to `Guid TrackingId` (asserts `Guid.Empty`) |
| 2 | Collection Map overload | `mapper.Map<OrderSource, OrderDto>(sources)` on `List<OrderSource>` |
| 3 | ConstructUsing + nested MapFrom | `ConstructUsing` on `MoneyDto.Create`, `MapFrom(s => s)` for nested resolution |
| 4 | Nullable -> value type | Same as #1 (Guid? -> Guid) |
| 5 | Profile base class | `OrderProfile : Profile` |

Features 1 and 4 overlap (both are nullable coercion). The five distinct AutoMapper parity features from the codebase are: nullable coercion, collection Map, ConstructUsing, nested MapFrom, and Profile. All are present.

### 2. MapperContext bugfix correctness

The original cache keyed on `object source` alone using `ReferenceEqualityComparer`. When `LineItemSource` maps to both `LineItemDto` (via property mapping) and `MoneyDto` (via `MapFrom(s => s)`), the second lookup for the same source object returned the already-cached `LineItemDto` instead of triggering a new `MoneyDto` construction.

The fix changes the key to `(object source, Type destType)` with a custom `SourceTypeKeyComparer` that:
- Uses `ReferenceEquals` for source identity (preserving the original semantics)
- Uses standard equality for `Type` (correct, since `Type` objects are singletons per type)
- Uses `RuntimeHelpers.GetHashCode` for reference-based hashing of the source object
- Uses `HashCode.Combine` for the composite key

This is correct and minimal.

### 3. Circular reference detection still works

The fix preserves circular reference detection because:
- Same source + same dest type still hits the cache (the common circular reference case)
- Same source + different dest type correctly bypasses the cache
- All 260 existing tests pass (verified via `dotnet test`)
- The existing `MapperContextTests` were updated to pass `Type` and two new tests were added for the multi-type scenario

### 4. Changes to MapperConfiguration.cs and MapperConfigurationBuilder.cs

Minimal and correct. Four call sites updated:
- `MapperConfiguration.cs:73` -- `TryGetMapped` now passes `dstType`
- `MapperConfiguration.cs:86` -- `Register` now passes `dstType`
- `MapperConfigurationBuilder.cs:512` -- `Register` in standard map delegate
- `MapperConfigurationBuilder.cs:777` -- `Register` in constructor-injected map delegate

All call sites have `dstType` properly in scope. No other callers of `Register` or `TryGetMapped` exist in the codebase.

### 5. No extra/unneeded work

The commit is tightly scoped: one integration test file, one bugfix in `MapperContext`, call site updates, and corresponding unit test updates. No unrelated changes.

---

## Findings

### Finding: AutoMapperParityTests class is not sealed
- **Severity**: LOW
- **Confidence**: 90
- **File**: tests/DeltaMapper.UnitTests/AutoMapperParityTests.cs:6
- **Category**: Code Quality
- **Verdict**: CONCERN
- **Issue**: The test class is `public class AutoMapperParityTests` but the project convention is to seal non-abstract implementation classes.
- **Fix**: Change to `public sealed class AutoMapperParityTests`.
- **Pattern reference**: tests/DeltaMapper.UnitTests/MapperContextTests.cs:8 -- also not sealed, so this appears to be an accepted test convention. Downgrading to non-blocking.

### Finding: Test uses xUnit Assert instead of FluentAssertions
- **Severity**: LOW
- **Confidence**: 95
- **File**: tests/DeltaMapper.UnitTests/AutoMapperParityTests.cs:89-101
- **Category**: Code Quality
- **Verdict**: CONCERN
- **Issue**: The integration test uses `Assert.Equal` and `Assert.Empty` instead of FluentAssertions `.Should()` pattern used in the rest of the test suite (e.g., `MapperContextTests.cs`).
- **Fix**: Convert assertions to FluentAssertions: `results.Count.Should().Be(2)`, `results[0].TrackingId.Should().Be(Guid.Empty)`, etc.
- **Pattern reference**: tests/DeltaMapper.UnitTests/MapperContextTests.cs:22 -- `.Should().BeFalse()`

### Finding: Inner test model classes are not sealed
- **Severity**: LOW
- **Confidence**: 70
- **File**: tests/DeltaMapper.UnitTests/AutoMapperParityTests.cs:9-42
- **Category**: Code Quality
- **Verdict**: PASS
- **Issue**: `OrderSource`, `LineItemSource`, `OrderDto`, `LineItemDto` are `private class` not `private sealed class`. These are test-only DTOs so this is negligible.

---

## Summary

- PASS: MapperContext bugfix -- Cache key correctly changed to `(object, Type)` tuple with custom comparer preserving reference equality semantics. `RuntimeHelpers.GetHashCode` and `HashCode.Combine` are the right primitives.
- PASS: All 5 parity features exercised -- Nullable coercion, collection Map, ConstructUsing, nested MapFrom, Profile base class all present in a single test.
- PASS: Circular reference detection preserved -- Existing tests updated and passing (260 total).
- PASS: Call site changes minimal -- Exactly 4 call sites updated, no missed callers.
- PASS: No extra/unneeded work -- Commit is tightly scoped.
- CONCERN: FluentAssertions not used -- Test uses `Assert.Equal` instead of `.Should()` convention (confidence: 95/100, non-blocking).
- CONCERN: Test class not sealed -- Minor convention deviation, matches existing test file patterns so acceptable (confidence: 90/100, non-blocking).

## Final Verdict

**APPROVED**

The bugfix is correct, minimal, and well-tested. The integration test exercises all five AutoMapper parity features. The two concerns are stylistic and non-blocking.
