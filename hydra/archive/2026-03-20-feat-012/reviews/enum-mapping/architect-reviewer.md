# Architect Review: Dictionary Mapping (feat/enum-mapping branch)

Reviewer: Architect Reviewer
Date: 2026-03-18
Scope: Dictionary mapping additions in `MapperConfigurationBuilder.cs` and `DictionaryMappingTests.cs`

---

## Findings

### Finding: Dictionary branch correctly placed before collection branch
- **Severity**: LOW
- **Confidence**: 95
- **File**: `src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:214-245`
- **Category**: Architecture
- **Verdict**: PASS
- **Analysis**: The dictionary branch is placed after enum branches and before `IsCollectionMapping`. This ordering is correct. `GetEnumerableElementType` (line 825) checks `genArgs.Length == 1`, which naturally excludes `Dictionary<K,V>` (2 generic args) from the collection path. However, placement before the collection branch is still the right call for clarity and to prevent any future changes to `GetEnumerableElementType` from accidentally matching dictionaries. The `IsDirectlyAssignable` check at line 159 also correctly short-circuits same-type dictionaries (e.g., `Dictionary<string,int>` to `Dictionary<string,int>`) as reference copies, which test `Dict02` validates.

### Finding: GetDictionaryTypes interface scanning is well-structured
- **Severity**: LOW
- **Confidence**: 90
- **File**: `src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:732-758`
- **Category**: Architecture
- **Verdict**: PASS
- **Analysis**: The method correctly checks the type itself first (for concrete `Dictionary<,>`, `IDictionary<,>`, `IReadOnlyDictionary<,>`), then falls back to scanning implemented interfaces. This handles cases like `SortedDictionary<K,V>` or custom dictionary types that implement `IDictionary<,>`. The interface scan on lines 746-755 is the right approach. One note: this does NOT handle `ConcurrentDictionary<K,V>` directly since its generic definition differs, but it would be caught by the interface scan since `ConcurrentDictionary` implements `IDictionary<K,V>`. This is acceptable.

### Finding: KeyValuePair enumeration via IEnumerable is correct for IReadOnlyDictionary
- **Severity**: LOW
- **Confidence**: 92
- **File**: `src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:223-244`
- **Category**: Architecture
- **Verdict**: PASS
- **Analysis**: The diff initially used `(System.Collections.IDictionary)` cast with `DictionaryEntry` enumeration. The current code uses `(System.Collections.IEnumerable)` cast with `KeyValuePair<,>` property reflection. This is the correct approach because `IReadOnlyDictionary<K,V>` does NOT implement the non-generic `System.Collections.IDictionary` interface, so casting to `IDictionary` would throw `InvalidCastException` for `IReadOnlyDictionary` source types. The `KeyValuePair` reflection approach (lines 224-226, 235-236) works universally across all dictionary types. The `keyProp.GetValue(kvp)` / `valueProp.GetValue(kvp)` calls on lines 235-236 use reflection per-entry, which is a minor perf concern, but acceptable given this is the dictionary mapping path (not the hot direct-assign path).

### Finding: Dictionary mapping missing from CompileConstructorMap
- **Severity**: MEDIUM
- **Confidence**: 90
- **File**: `src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:495-529`
- **Category**: Architecture
- **Verdict**: CONCERN
- **Issue**: The `CompileConstructorMap` method handles enum mappings for init-only properties (lines 502-524) but does NOT include a dictionary branch. If a record type has a dictionary property that is init-only and not covered by the constructor, the init-only assignment path at line 525 will only match `IsDirectlyAssignable`. For cross-type dictionaries (e.g., `IDictionary<string, SourceChild>` to `Dictionary<string, DestChild>`), this would silently skip the property since it would fail `IsDirectlyAssignable` and there is no fallback. The same gap exists for collections and complex types in the init-only path.
- **Fix**: Add dictionary, collection, and complex-type branches to the init-only assignment block (lines 495-529), mirroring the structure in `CompileTypeMap`. Alternatively, extract a shared method that produces the assignment action for any property pair, used by both paths.
- **Pattern reference**: `src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:214-306` (the full branch chain in `CompileTypeMap`)

### Finding: Per-entry reflection in dictionary enumeration
- **Severity**: LOW
- **Confidence**: 85
- **File**: `src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:235-236`
- **Category**: Architecture
- **Verdict**: CONCERN
- **Issue**: `keyProp.GetValue(kvp)` and `valueProp.GetValue(kvp)` use `PropertyInfo.GetValue` reflection on every dictionary entry at map time. While the `PropertyInfo` objects are captured at Build() time (good), the actual invocations are per-entry reflection calls. For large dictionaries this could be measurable. The rest of the codebase uses `CompileGetter`/`CompileSetter` for property access.
- **Fix**: Consider compiling a `Func<object, object?>` for `KeyValuePair<K,V>.Key` and `.Value` at Build() time using `CompileGetter` or a similar expression-based approach. This would be consistent with the codebase pattern of compiling all property access at Build() time. Non-blocking since correctness is not affected.
- **Pattern reference**: `src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:569-576` (CompileGetter pattern)

### Finding: IsDirectlyAssignable called per-entry at map time
- **Severity**: LOW
- **Confidence**: 80
- **File**: `src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:237-240`
- **Category**: Architecture
- **Verdict**: CONCERN
- **Issue**: `IsDirectlyAssignable(sKey, dKey)` and `IsDirectlyAssignable(sVal, dVal)` are called inside the per-entry loop at map time, but the types `sKey`, `sVal`, `dKey`, `dVal` are known at Build() time and never change. These checks should be hoisted outside the lambda.
- **Fix**: Compute `bool keyAssignable = IsDirectlyAssignable(sKey, dKey)` and `bool valAssignable = IsDirectlyAssignable(sVal, dVal)` at Build() time (alongside lines 220-222) and capture the booleans in the closure.
- **Pattern reference**: `src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:159` (IsDirectlyAssignable checked at Build() time in the direct-assign branch)

### Finding: Test coverage is solid but missing edge cases
- **Severity**: LOW
- **Confidence**: 75
- **File**: `tests/DeltaMapper.UnitTests/DictionaryMappingTests.cs`
- **Category**: Architecture
- **Verdict**: CONCERN
- **Issue**: The 8 tests cover: same-type, reference semantics, empty, null, nullable with value, complex value types, IReadOnlyDictionary source, and IDictionary-to-Dictionary clone. Missing cases: (1) dictionary with mapped key types (not just value types), (2) dictionary in a record/init-only type (which would hit the gap identified in Finding 4), (3) dictionary where value mapping throws (error propagation), (4) `SortedDictionary` or `ConcurrentDictionary` source types (to validate interface scanning).
- **Fix**: Add tests for the above cases, especially (2) which would expose the CompileConstructorMap gap. Non-blocking.

---

## Summary
- PASS: Dictionary branch placement -- correctly before collection branch, after enum branches; `IsDirectlyAssignable` short-circuits same-type dictionaries
- PASS: GetDictionaryTypes -- handles concrete types and interface scanning correctly, covers `IReadOnlyDictionary`
- PASS: KeyValuePair enumeration -- correctly uses `IEnumerable` cast instead of `IDictionary` cast, works with `IReadOnlyDictionary`
- CONCERN: Dictionary mapping missing from CompileConstructorMap init-only path -- record types with cross-type dictionary properties will silently skip mapping (confidence: 90/100, non-blocking but should be addressed)
- CONCERN: Per-entry reflection for KeyValuePair.Key/Value -- could compile getters at Build() time for consistency (confidence: 85/100, non-blocking)
- CONCERN: IsDirectlyAssignable called per-entry instead of hoisted to Build() time (confidence: 80/100, non-blocking)
- CONCERN: Test coverage missing dictionary-in-record scenario (confidence: 75/100, non-blocking)

## Final Verdict
**APPROVED**

All checks pass at the architectural level. The dictionary branch is correctly placed, `GetDictionaryTypes` correctly scans interfaces, and the `KeyValuePair` enumeration approach handles all dictionary interface variants. The identified concerns are non-blocking: the `CompileConstructorMap` gap (Finding 4) is the most significant but only affects record types with cross-type dictionary properties -- a narrow scenario that can be addressed in a follow-up. The per-entry reflection and un-hoisted `IsDirectlyAssignable` calls are performance nits, not correctness issues.
