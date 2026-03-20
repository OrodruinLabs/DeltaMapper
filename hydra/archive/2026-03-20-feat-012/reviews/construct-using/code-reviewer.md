# Code Review: ConstructUsing (Task 3)

## Spec Compliance Checklist

1. **Interface method on `IMappingExpression<TSrc, TDst>`** -- Present at line 31 with correct signature `ConstructUsing(Func<TSrc, TDst> factory)`. Returns fluent `IMappingExpression<TSrc, TDst>`. XML doc comment present.
2. **`CustomFactory` property on `TypeMapConfiguration`** -- Present at line 13 as `Func<object, object>?`. Correct.
3. **Implementation in `MappingExpression.cs`** -- Lines 51-56. Wraps typed `Func<TSrc, TDst>` into untyped `Func<object, object>` via cast. Has `ArgumentNullException.ThrowIfNull`. Correct.
4. **`CompileTypeMap` skips `NeedsConstructorInjection` when `CustomFactory` is set** -- Lines 155-162. The if/else-if structure ensures that when `CustomFactory != null`, the constructor injection path is bypassed and execution falls through to the property-assignment path. Correct.
5. **Custom factory used instead of `CompileFactory`/`Activator.CreateInstance`** -- Lines 476-481. `defaultFactory` is only compiled when `customFactory` is null. At runtime, `customFactory(src)` is called when present. Source object is correctly passed to the factory. Correct.
6. **ForMember overrides apply on top of factory-created object** -- Lines 219-222 skip convention-matched properties when `CustomFactory` is set, but explicit `ForMember` configs (where `memberConfig != null`) still generate assignments that run after construction. Confirmed by `ConstructUsing_with_ForMember_applies_overrides_after_factory` test.
7. **Money class with private ctor** -- Test class defines `Money` with private constructor and `static Create` factory method. Test `ConstructUsing_calls_factory_method` verifies it works.
8. **Three tests covering spec requirements** -- Present: factory invocation, ForMember after factory, source passed to factory.
9. **All existing tests pass** -- 199 unit + 12 integration + 43 source gen = 254 total, all passing.

## Findings

### Finding: Tests use Assert.Equal instead of FluentAssertions
- **Severity**: LOW
- **Confidence**: 90
- **File**: `tests/DeltaMapper.UnitTests/ConstructUsingTests.cs:52-68`
- **Category**: Code Quality
- **Verdict**: CONCERN
- **Issue**: The test file uses xUnit `Assert.Equal`/`Assert.True` while the rest of the test suite (557 occurrences across 39 files) consistently uses FluentAssertions `.Should()` pattern.
- **Fix**: Replace `Assert.Equal(99.99m, result.Amount)` with `result.Amount.Should().Be(99.99m)` and similar for all assertions.
- **Pattern reference**: `tests/DeltaMapper.UnitTests/ForMemberTests.cs` (uses `.Should()` throughout)

### Finding: Test naming deviates from convention
- **Severity**: LOW
- **Confidence**: 85
- **File**: `tests/DeltaMapper.UnitTests/ConstructUsingTests.cs:48-95`
- **Category**: Code Quality
- **Verdict**: CONCERN
- **Issue**: Test names use `ConstructUsing_calls_factory_method` pattern (verb phrase) instead of the project convention `Method_Scenario_ExpectedBehavior`. For example, `ConstructUsing_PrivateCtorEntity_CreatesViaFactory` would match the convention better.
- **Fix**: Rename tests to follow `Method_Scenario_ExpectedBehavior` pattern.
- **Pattern reference**: Other test files in `tests/DeltaMapper.UnitTests/` (e.g., `ForMemberTests.cs`)

### Finding: Null-forgiving operator on factory return
- **Severity**: LOW
- **Confidence**: 60
- **File**: `src/DeltaMapper.Core/Configuration/MappingExpression.cs:54`
- **Category**: Code Quality
- **Verdict**: PASS
- **Issue**: `factory((TSrc)src)!` uses null-forgiving operator. If a user's factory returns null, this silently converts it to a non-nullable `object`. However, the typed signature `Func<TSrc, TDst>` already implies non-null when TDst is a non-nullable reference type, so the `!` is acceptable here to satisfy the `Func<object, object>` target delegate type.
- **Fix**: No fix needed. The `!` is a reasonable bridge between the typed and untyped delegate worlds.

## Summary
- PASS: Interface method -- correct signature, XML doc, fluent return
- PASS: TypeMapConfiguration.CustomFactory -- correctly typed as `Func<object, object>?`
- PASS: MappingExpression.ConstructUsing -- null guard, typed-to-untyped wrapping
- PASS: CompileTypeMap -- skips NeedsConstructorInjection, uses custom factory, skips convention matching for non-ForMember properties
- PASS: Factory receives source object -- confirmed by test and code at line 481
- PASS: ForMember overrides after factory -- explicit member configs still apply
- PASS: Money with private ctor -- tested with static factory method
- PASS: All 254 tests pass with zero warnings (excluding pre-existing benchmark warning)
- CONCERN: Tests use `Assert.Equal` instead of FluentAssertions `.Should()` (confidence: 90/100, non-blocking)
- CONCERN: Test naming does not follow `Method_Scenario_ExpectedBehavior` convention (confidence: 85/100, non-blocking)

## Final Verdict
**APPROVED**

All spec requirements are correctly implemented. The two concerns are stylistic (test assertion style and naming convention) and non-blocking. The core logic is sound: the factory is correctly wired through the type-erased delegate, constructor injection is properly bypassed, convention matching is skipped for non-explicit members, and ForMember overrides still apply after factory construction.
