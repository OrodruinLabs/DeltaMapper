# Code Review: Collection Map Overload

**Commit**: 7329c7b
**Reviewed**: 2026-03-20
**Files**: `IMapper.cs`, `Mapper.cs`, `CollectionMapOverloadTests.cs`, `ErrorHandlingTests.cs`

---

## Spec Compliance

1. **Method signature on IMapper** -- Correct. `List<TDestination> Map<TSource, TDestination>(IEnumerable<TSource> source)` at line 34.
2. **Method signature on Mapper** -- Correct. `public List<TDestination> Map<TSource, TDestination>(IEnumerable<TSource> source)` at line 85.
3. **Null source handling** -- Correct. `ArgumentNullException.ThrowIfNull(source)` at Mapper.cs:87.
4. **Null element handling** -- Correct. Throws `ArgumentNullException` for null elements at Mapper.cs:93-94.
5. **Empty collection handling** -- Correct. Loop simply doesn't execute, returns empty list.
6. **Return type is `List<T>`** -- Correct. Not `IReadOnlyList<T>`.
7. **4 required tests present** -- Correct. List mapping, array mapping, empty collection, null throws.
8. **ErrorHandlingTests fix** -- Appropriate. Cast `(User)null!` disambiguates the overload correctly.

---

## Findings

### Finding: Test file does not use FluentAssertions
- **Severity**: LOW
- **Confidence**: 95
- **File**: tests/DeltaMapper.UnitTests/CollectionMapOverloadTests.cs:1-63
- **Category**: Code Quality
- **Verdict**: CONCERN
- **Issue**: The new test file uses raw `Assert.Equal`/`Assert.Single`/`Assert.Empty`/`Assert.Throws` instead of the project convention of FluentAssertions `.Should()` syntax. Every other test file in the project uses FluentAssertions.
- **Fix**: Rewrite assertions using FluentAssertions. For example, `Assert.Equal(2, result.Count)` becomes `result.Should().HaveCount(2)`.
- **Pattern reference**: tests/DeltaMapper.UnitTests/ErrorHandlingTests.cs:50 (`exception.Message.Should().Contain(...)`)

### Finding: Test file missing FluentAssertions using directive
- **Severity**: LOW
- **Confidence**: 95
- **File**: tests/DeltaMapper.UnitTests/CollectionMapOverloadTests.cs:1
- **Category**: Code Quality
- **Verdict**: CONCERN
- **Issue**: Missing `using FluentAssertions;` import, which is consistent with the missing FluentAssertions usage above.
- **Fix**: Add `using FluentAssertions;` and convert all assertions.

### Finding: Test class not using project test naming convention
- **Severity**: LOW
- **Confidence**: 90
- **File**: tests/DeltaMapper.UnitTests/CollectionMapOverloadTests.cs:20-61
- **Category**: Code Quality
- **Verdict**: CONCERN
- **Issue**: Test method names use `Map_list_returns_List_of_destinations` (lowercase words) instead of the project convention `Method_Scenario_ExpectedBehavior` with PascalCase segments (e.g., `Map_ListInput_ReturnsListOfDestinations`).
- **Fix**: Rename to PascalCase segments: `Map_ListSource_ReturnsListOfDestinations`, `Map_ArraySource_ReturnsListOfDestinations`, `Map_EmptyCollection_ReturnsEmptyList`, `Map_NullCollection_ThrowsArgumentNullException`.
- **Pattern reference**: tests/DeltaMapper.UnitTests/ErrorHandlingTests.cs:16 (`Map_NoMappingRegistered_ThrowsDeltaMapperException`)

### Finding: Mapper.Map collection overload duplicates MapList logic
- **Severity**: LOW
- **Confidence**: 85
- **File**: src/DeltaMapper.Core/Runtime/Mapper.cs:85-98
- **Category**: Code Quality
- **Verdict**: CONCERN
- **Issue**: The new `Map<TSource, TDestination>(IEnumerable<TSource>)` method at lines 85-98 is a near-exact copy of `MapList` at lines 69-82. The only difference is the return type (`List<T>` vs `IReadOnlyList<T>`). This could be simplified by having `MapList` delegate to `Map` and cast, or vice versa.
- **Fix**: Have `MapList` delegate: `public IReadOnlyList<TDestination> MapList<TSource, TDestination>(IEnumerable<TSource> source) => Map<TSource, TDestination>(source);`. Since `List<T>` implements `IReadOnlyList<T>`, this is safe.

### Finding: XML doc comment says "read-only list" but should not
- **Severity**: LOW
- **Confidence**: 70
- **File**: src/DeltaMapper.Core/Abstractions/IMapper.cs:26-27
- **Category**: Code Quality
- **Verdict**: PASS
- **Issue**: The XML doc on the existing `MapList` method says "returning a read-only list" which is accurate for that method. The new method's doc at line 32-33 says "returning a list" which is also accurate. No issue.

---

## Summary

- PASS: Method signature on IMapper and Mapper -- correct `List<TDestination> Map<TSource, TDestination>(IEnumerable<TSource> source)`
- PASS: Null source handling -- uses `ArgumentNullException.ThrowIfNull(source)`
- PASS: Null element handling -- throws with descriptive message
- PASS: Empty collection handling -- returns empty list naturally
- PASS: Return type is `List<T>` -- confirmed
- PASS: All 4 required tests present -- list, array, empty, null
- PASS: ErrorHandlingTests fix -- `(User)null!` cast is the correct disambiguation for the new overload
- PASS: Build succeeds with 0 errors, no new warnings introduced
- PASS: 196/196 unit tests, 12/12 integration tests, 43/43 source gen tests all pass
- CONCERN: Test file uses raw xUnit assertions instead of FluentAssertions `.Should()` convention (confidence: 95/100, non-blocking)
- CONCERN: Test method names use lowercase words instead of PascalCase segments (confidence: 90/100, non-blocking)
- CONCERN: `Map` collection overload duplicates `MapList` body; consider delegating (confidence: 85/100, non-blocking)

## Final Verdict

**APPROVED**

All spec requirements are met. The implementation is correct, null-safe, and all tests pass. The concerns are stylistic consistency items that do not affect correctness.
