# Code Review: Enum Mapping in MapperConfigurationBuilder

**Branch**: feat/enum-mapping
**File**: src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs
**Reviewer**: DeltaMapper Code Reviewer
**Date**: 2026-03-18

---

### Finding: InvalidOperationException used instead of DeltaMapperException
- **Severity**: MEDIUM
- **Confidence**: 95
- **File**: src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:197,406,484,595,610,620,626,655
- **Category**: Code Quality
- **Verdict**: REJECT
- **Issue**: All eight new `throw` statements use `InvalidOperationException`. The project convention (seen at line 352 and in `MapperConfiguration.cs:91`) is to use `DeltaMapperException` for mapping errors. `InvalidOperationException` is a generic BCL exception that does not let consumers catch DeltaMapper-specific failures distinctly.
- **Fix**: Replace all `throw new InvalidOperationException(...)` with `throw new DeltaMapperException(...)`. The messages are already actionable, so only the exception type needs to change.
- **Pattern reference**: src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:352

---

### Finding: ToUInt64 will throw OverflowException for negative enum values
- **Severity**: MEDIUM
- **Confidence**: 90
- **File**: src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:661-665
- **Category**: Code Quality
- **Verdict**: REJECT
- **Issue**: `Convert.ToUInt64(enumValue)` throws `OverflowException` when the enum has a signed underlying type and negative member values (e.g., `[Flags] enum Perms : int { Special = -1 }`). While negative flags are uncommon, this is an unhandled crash path with no descriptive error message. The `DecomposeFlagsAndMap` method uses `ToUInt64` both for the composite value and for each member value, so a single negative member will crash the entire mapping.
- **Fix**: Either (a) convert through `unchecked((ulong)Convert.ToInt64(enumValue))` to handle signed underlying types, or (b) catch the `OverflowException` and wrap it in a `DeltaMapperException` with a message indicating negative flag values are not supported.
- **Pattern reference**: N/A -- new code

---

### Finding: Bypassing compiled setter for null enum values
- **Severity**: LOW
- **Confidence**: 85
- **File**: src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:199,218
- **Category**: Code Quality
- **Verdict**: CONCERN
- **Issue**: In the `IsSameEnumNullabilityDiff` branch (line 199), when `value == null` and destination is nullable, the code falls back to `dstPropCaptured.SetValue(dst, null)` via reflection instead of using the compiled `setter`. Similarly in the cross-enum branch (line 218). This is inconsistent with the rest of the file where `setter(dst, null)` is used (e.g., the numeric widening branch at line 178). The compiled setter was built specifically to avoid slow reflection calls.
- **Fix**: Replace `dstPropCaptured.SetValue(dst, null)` with `setter(dst, null)` on lines 199 and 218. Note: the compiled setter uses `Expression.Unbox` for value types, which will throw on null input for `Nullable<T>` -- verify by checking whether `CompileSetter` handles nullable value types correctly. If `Expression.Unbox` on `Nullable<T>` works with null (it does -- unboxing null to `Nullable<T>` yields `default`), then `setter(dst, null)` is safe and preferred.
- **Pattern reference**: src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:178 (numeric widening null path)

---

### Finding: Duplicated enum resolution logic across three mapping paths
- **Severity**: LOW
- **Confidence**: 80
- **File**: src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:185-223,396-421,472-503
- **Category**: Code Quality
- **Verdict**: CONCERN
- **Issue**: The same pattern -- check `IsSameEnumNullabilityDiff`, then check `IsEnumMapping`, build nameMap, call `ResolveEnumValue` -- is repeated three times: in the property-setter path (line 185), the constructor-param path (line 396), and the init-only property path (line 472). This triplication increases maintenance risk; a bug fix in one path could easily be missed in the others.
- **Fix**: Consider extracting a helper like `CreateEnumAssignment(PropertyInfo src, Type dstType, string dstName)` that returns an `Action<object, object, MapperContext>` or `Func<object, MapperContext, object?>`, encapsulating the enum-type detection and delegate creation. This is non-blocking but recommended before adding more enum features.
- **Pattern reference**: N/A -- structural suggestion

---

### Finding: Missing XML doc comments on new helper methods
- **Severity**: LOW
- **Confidence**: 95
- **File**: src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:584,589,633,661,667,674
- **Category**: Code Quality
- **Verdict**: CONCERN
- **Issue**: Six new methods (`GetOrCreateEnumNameMap`, `ResolveEnumValue`, `DecomposeFlagsAndMap`, `ToUInt64`, `IsSameEnumNullabilityDiff`, `IsEnumMapping`) are all `private static` and lack XML doc comments. The project convention is `/// <summary>` on all public types and methods. While these are private, the existing private methods in this file (e.g., `IsNumericWidening` at line 703, `IsComplexType` at line 710) do have `/// <summary>` comments. For consistency within this file, the new methods should follow suit.
- **Fix**: Add `/// <summary>` comments to each of the six new methods.
- **Pattern reference**: src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:699-703

---

### Finding: new List<string>() instead of collection expression in DecomposeFlagsAndMap
- **Severity**: LOW
- **Confidence**: 95
- **File**: src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:637
- **Category**: Code Quality
- **Verdict**: CONCERN
- **Issue**: `var parts = new List<string>();` should be `List<string> parts = [];` per the project convention for empty collection initialization (see lines 17, 18, 55, 102, etc.).
- **Fix**: Change `var parts = new List<string>();` to `List<string> parts = [];`.
- **Pattern reference**: src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:102

---

### Finding: Enum name cache is a static ConcurrentDictionary on an instance-scoped builder
- **Severity**: LOW
- **Confidence**: 70
- **File**: src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:582
- **Category**: Code Quality
- **Verdict**: PASS
- **Issue**: `_enumNameCache` is `private static readonly`, meaning it persists across all `MapperConfigurationBuilder` instances for the lifetime of the AppDomain. This is actually fine for enum name lookups (enum definitions do not change at runtime), and using `ConcurrentDictionary` with `FrozenSet<string>` values is a good thread-safe, read-optimized choice. No action needed.

---

### Finding: value.ToString() null safety in ResolveEnumValue
- **Severity**: LOW
- **Confidence**: 75
- **File**: src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:618
- **Category**: Code Quality
- **Verdict**: PASS
- **Issue**: `value.ToString()` on an enum is never null (the BCL guarantees a string representation), and the subsequent `string.IsNullOrWhiteSpace` check handles the theoretical edge. The null guard on `value` at line 592 ensures `value` itself is non-null at this point. This is safe.

---

## Summary

- PASS: Enum name cache design (static + ConcurrentDictionary + FrozenSet) -- correct and well-optimized
- PASS: IsSameEnumNullabilityDiff / IsEnumMapping logic -- clean, correct type checks
- PASS: value.ToString() null safety -- properly guarded
- PASS: File-scoped namespace, sealed class, naming conventions -- all followed
- PASS: Build succeeds with zero new warnings
- CONCERN: Bypassing compiled setter for null enum values -- use `setter(dst, null)` instead of `SetValue` reflection (confidence: 85/100, non-blocking)
- CONCERN: Duplicated enum resolution logic across three paths -- extract shared helper (confidence: 80/100, non-blocking)
- CONCERN: Missing XML doc comments on six new private methods (confidence: 95/100, non-blocking)
- CONCERN: `new List<string>()` instead of collection expression `[]` (confidence: 95/100, non-blocking)
- REJECT: InvalidOperationException used instead of DeltaMapperException in all eight throw sites (confidence: 95/100, blocking)
- REJECT: ToUInt64 crashes on negative enum values with no descriptive error (confidence: 90/100, blocking)

## Final Verdict

**CHANGES_REQUESTED** -- Two blocking issues found: wrong exception type (8 occurrences) and unhandled overflow in `ToUInt64` for signed negative enum values.
