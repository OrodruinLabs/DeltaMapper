# Code Review: Dictionary Mapping

**Branch**: `feat/enum-mapping`
**Commits reviewed**: `ff5cb21` (enum mapping), `4be4a02` (dictionary mapping)
**Files reviewed**:
- `src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs`
- `tests/DeltaMapper.UnitTests/DictionaryMappingTests.cs`

**Build**: Release build succeeds with 0 errors, 0 new warnings.
**Tests**: All 9 dictionary tests pass.

---

## Findings

### Finding: IReadOnlyDictionary cast to IDictionary will throw InvalidCastException at runtime
- **Severity**: HIGH
- **Confidence**: 95
- **File**: `src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:233` (in diff; line 233 of the lambda in the diff hunk)
- **Category**: Code Quality / Runtime Correctness
- **Verdict**: REJECT
- **Issue**: The dictionary enumeration lambda in the diff originally cast `srcDict` to `System.Collections.IDictionary`. However, `IReadOnlyDictionary<TKey, TValue>` does NOT implement `System.Collections.IDictionary`. This means `Dict07_MapsReadOnlyDictionarySource` would fail with an `InvalidCastException` at runtime. The current file on disk has been fixed to use `KeyValuePair<,>` enumeration via `IEnumerable`, which resolves this. However, the fix introduced its own concern (see next finding). **Since the code on HEAD is now correct, this is resolved.** Marking as PASS given the fix is already merged.
- **Pattern reference**: Current HEAD at lines 224-244

### Finding: Reflection-based KeyValuePair enumeration adds per-entry overhead
- **Severity**: LOW
- **Confidence**: 85
- **File**: `src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:235-236`
- **Category**: Code Quality / Performance
- **Verdict**: CONCERN
- **Issue**: The dictionary mapping lambda calls `keyProp.GetValue(kvp)` and `valueProp.GetValue(kvp)` per entry. While the `PropertyInfo` objects for `Key` and `Value` are captured outside the loop (good), `GetValue` on a struct (`KeyValuePair<,>`) causes boxing of the kvp on each iteration. For large dictionaries, this adds allocation pressure. The rest of the codebase uses compiled expression delegates (`CompileGetter`/`CompileSetter`) for property access.
- **Fix**: Consider compiling a `Func<object, object>` for `KeyValuePair<TKey,TValue>.Key` and `.Value` at build time, similar to how `CompileGetter` works for regular properties. This is non-blocking since correctness is fine.

### Finding: Test model classes are not sealed
- **Severity**: LOW
- **Confidence**: 90
- **File**: `tests/DeltaMapper.UnitTests/DictionaryMappingTests.cs:10-31,186-190,203-204,213-214`
- **Category**: Code Quality / Convention
- **Verdict**: CONCERN
- **Issue**: The public test model classes (`DictSource`, `DictDest`, `DictNullSource`, `DictNullDest`, `DictChildSource`, `DictChildDest`, `DictComplexSource`, `DictComplexDest`, `DictReadOnlySource`, `DictReadOnlyDest`, `DictCloneSource`, `DictCloneDest`) are not `sealed`. The project convention is that all non-abstract implementation classes should be `sealed`.
- **Fix**: Add `sealed` to all public test model classes, or better yet, make them `file`-scoped where possible (the profiles already use `file class`).
- **Pattern reference**: The `file class DictComplexProfile` at line 192 follows the pattern correctly for profiles.

### Finding: Dict02 test asserts reference identity -- fragile contract
- **Severity**: MEDIUM
- **Confidence**: 88
- **File**: `tests/DeltaMapper.UnitTests/DictionaryMappingTests.cs:84`
- **Category**: Code Quality / Test Design
- **Verdict**: CONCERN
- **Issue**: `Dict02_SameTypeDictionaryIsReferenceAssigned` asserts `dest.Tags.Should().BeSameAs(source.Tags)`, meaning it tests that same-type dictionaries are reference-copied (shared). This is a surprising and potentially dangerous semantic -- mutations to the destination dictionary would affect the source. Meanwhile, `Dict08_ClonesDictionaryWhenTypesMatch` asserts `NotBeSameAs` for `IDictionary<,>` to `Dictionary<,>`. The behavior difference depends solely on whether `IsDirectlyAssignable` returns true (same concrete type) vs the `IsDictionaryMapping` branch (interface vs concrete). This is correct implementation behavior, but the reference-sharing for same-type dictionaries is worth documenting as an intentional design decision since users might expect deep-clone semantics.
- **Fix**: Add a comment to the test or to the mapper documentation clarifying that same-type dictionaries are reference-assigned (shallow copy) by design, and that cloning only occurs when dictionary types differ.

### Finding: Missing test coverage for null values inside dictionary
- **Severity**: LOW
- **Confidence**: 80
- **File**: `tests/DeltaMapper.UnitTests/DictionaryMappingTests.cs`
- **Category**: Code Quality / Test Coverage
- **Verdict**: CONCERN
- **Issue**: The dictionary lambda at line 238-240 handles `entryVal == null` by assigning `null`, but no test exercises a dictionary with null values (e.g., `Dictionary<string, string?> { ["k"] = null }`). This path is untested.
- **Fix**: Add a test case with null values in the dictionary to confirm the null-passthrough behavior.

### Finding: GetDictionaryTypes scans interfaces -- good for IReadOnlyDictionary
- **Severity**: N/A
- **Confidence**: 95
- **File**: `src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:732-758`
- **Category**: Code Quality
- **Verdict**: PASS
- **Issue**: None. The method correctly checks both the type itself and its implemented interfaces. The dual check for `IDictionary<,>` and `IReadOnlyDictionary<,>` covers the standard dictionary interface hierarchy.

### Finding: IsDictionaryMapping null-out pattern is correct
- **Severity**: N/A
- **Confidence**: 95
- **File**: `src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:720-730`
- **Category**: Code Quality
- **Verdict**: PASS
- **Issue**: None. The `out` parameters are initialized to `null` at the top. The `!` null-forgiving operators on lines 220-221 are justified because they are only reached after `IsDictionaryMapping` returns `true`, which guarantees non-null values.

### Finding: XML doc comments missing on dictionary helper methods
- **Severity**: LOW
- **Confidence**: 90
- **File**: `src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:720,732`
- **Category**: Code Quality / Convention
- **Verdict**: CONCERN
- **Issue**: `IsDictionaryMapping` and `GetDictionaryTypes` are private methods, so the convention for XML doc comments on "all public types and methods" does not strictly apply. However, the existing private methods like `IsNumericWidening` (line 782) do have XML doc comments, showing inconsistency.
- **Fix**: Add `/// <summary>` comments to `IsDictionaryMapping` and `GetDictionaryTypes` for consistency with other private helpers in the same file.

---

## Summary

- PASS: `IsDictionaryMapping` out-parameter pattern -- null safety is correct, `!` operators justified by control flow
- PASS: `GetDictionaryTypes` interface scanning -- correctly handles `Dictionary<,>`, `IDictionary<,>`, `IReadOnlyDictionary<,>`, and implemented interfaces
- PASS: Null dictionary handling -- lambda correctly checks `srcDict == null` and passes null through
- PASS: `Activator.CreateInstance(dstDictType)!` -- justified since `Dictionary<,>` always has a parameterless constructor
- PASS: Build -- no new warnings introduced
- PASS: Tests -- all 9 dictionary tests pass
- CONCERN: KeyValuePair reflection in hot loop adds per-entry boxing overhead; consider compiled delegates (confidence: 85/100, non-blocking)
- CONCERN: Test model classes should be `sealed` per project convention (confidence: 90/100, non-blocking)
- CONCERN: Dict02 asserts reference-sharing semantics which may surprise users; document the design decision (confidence: 88/100, non-blocking)
- CONCERN: No test for null values inside a dictionary (confidence: 80/100, non-blocking)
- CONCERN: Missing XML doc comments on `IsDictionaryMapping` and `GetDictionaryTypes` (confidence: 90/100, non-blocking)

## Final Verdict

**APPROVED**

No blocking issues found. The dictionary mapping implementation is functionally correct, handles null dictionaries, supports `IReadOnlyDictionary` sources via interface scanning, and correctly uses `KeyValuePair` enumeration to avoid the `IDictionary` cast problem. The concerns above are all non-blocking improvements for consistency and robustness.
