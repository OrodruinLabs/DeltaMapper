# Security Review: feat/enum-mapping -- MapperConfigurationBuilder enum changes

**Reviewer**: Security Reviewer (DeltaMapper)
**Branch**: feat/enum-mapping
**File under review**: `src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs`
**Date**: 2026-03-18

---

## Findings

### Finding 1: Fallback to raw PropertyInfo.SetValue in null enum path
- **Severity**: LOW
- **Confidence**: 80
- **File**: `src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:199`
- **Category**: Security (Reflection abuse)
- **Verdict**: CONCERN
- **Issue**: In the `IsSameEnumNullabilityDiff` branch (line 199) and the `IsEnumMapping` branch (line 218), when the value is null, the code falls back to `dstPropCaptured.SetValue(dst, null)` instead of using the compiled setter. The compiled setter was already created at lines 189 and 209. The same pattern recurs in the init-only assignments at lines 486 and 496. This is inconsistent with the project convention of using compiled expressions rather than raw reflection at call time.
- **Fix**: Use `setter(dst, null)` instead of `dstPropCaptured.SetValue(dst, null)`. For nullable value-type properties, the compiled setter's `Expression.Unbox` may fail on null -- if that is the reason for the fallback, add a comment explaining why, or fix the setter to handle null for nullable value types.
- **Pattern reference**: `src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:164` (direct assign path uses `setter(dst, getter(src))` consistently)

### Finding 2: ToUInt64 overflow on negative enum values
- **Severity**: LOW
- **Confidence**: 70
- **File**: `src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:661-665`
- **Category**: Security (Type casting safety)
- **Verdict**: CONCERN
- **Issue**: `Convert.ToUInt64(enumValue)` will throw `OverflowException` for enums with negative underlying values (e.g., `[Flags] enum Perms : int { Special = -1 }`). While negative flag values are unusual, this is an unhandled exception path that would surface as an uncontrolled runtime error rather than a `DeltaMapperException` or `InvalidOperationException` with an actionable message. This is a robustness/DoS concern rather than a direct exploit.
- **Fix**: Wrap in a try-catch and throw `InvalidOperationException` with an actionable message, consistent with the other error paths in `ResolveEnumValue` and `DecomposeFlagsAndMap`.

### Finding 3: ConcurrentDictionary enum name cache is unbounded
- **Severity**: LOW
- **Confidence**: 60
- **File**: `src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:582`
- **Category**: Security (Denial of service)
- **Verdict**: PASS
- **Issue**: `_enumNameCache` is a static `ConcurrentDictionary<Type, FrozenSet<string>>` that grows without bound. In theory, if a consumer maps many distinct enum types, this cache grows indefinitely. However, in practice the number of enum types in any application is finite and small (bounded by compiled types in loaded assemblies). This is not exploitable in a library context since the consumer controls which types are mapped.
- **Fix**: No fix required. The cache is bounded by the finite set of enum types in the application domain.

### Finding 4: Thread safety of static _enumNameCache
- **Severity**: LOW
- **Confidence**: 95
- **File**: `src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:582-587`
- **Category**: Security (Thread safety)
- **Verdict**: PASS
- **Issue**: The `_enumNameCache` uses `ConcurrentDictionary.GetOrAdd` which is thread-safe. The factory lambda `t => Enum.GetNames(t).ToFrozenSet()` may execute more than once for the same key under contention, but `FrozenSet<string>` is immutable after construction, so this is harmless (worst case: duplicate work, no corruption). Correct pattern.

### Finding 5: No user-controlled strings in expression trees
- **Severity**: LOW
- **Confidence**: 95
- **File**: `src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:544-570`
- **Category**: Security (Expression.Lambda safety)
- **Verdict**: PASS
- **Issue**: The new enum code does not introduce any new expression tree construction. All expression compilation (`CompileGetter`, `CompileSetter`) operates on `PropertyInfo` objects obtained via reflection on known types. No user-supplied strings are injected into expression trees.

### Finding 6: Enum.Parse safety -- validated before parsing
- **Severity**: MEDIUM
- **Confidence**: 90
- **File**: `src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:607,630,658`
- **Category**: Security (Type casting safety)
- **Verdict**: PASS
- **Issue**: All calls to `Enum.Parse(dstEnumType, ...)` are preceded by validation that the name(s) exist in `dstNames` (the FrozenSet of valid enum names). This prevents parsing arbitrary strings. The `long.TryParse` check at line 619 also prevents numeric string injection (where `Enum.Parse` would accept "42" as a valid enum value even without a named member). This is correct and secure.

### Finding 7: Error messages expose type names only
- **Severity**: LOW
- **Confidence**: 95
- **File**: `src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:596,611,621,627,656`
- **Category**: Security (Secret exposure)
- **Verdict**: PASS
- **Issue**: Error messages include `dstEnumType.Name`, `srcEnumType.Name`, property names, and the `value.ToString()` of the enum. These are all type metadata that the consumer already knows (they defined the mapping). No internal state, file paths, or secrets are leaked.

### Finding 8: DecomposeFlagsAndMap does not sort descending
- **Severity**: LOW
- **Confidence**: 75
- **File**: `src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:639-644`
- **Category**: Security (Type casting safety -- correctness)
- **Verdict**: CONCERN
- **Issue**: The current `DecomposeFlagsAndMap` (HEAD, lines 639-644) sorts members descending by value for greedy matching, which is correct. However, the comment on line 639 says "sorted descending" but I want to confirm the actual iteration. Looking at lines 640-644: `Enum.GetNames(srcEnumType).Select(...).OrderByDescending(m => m.Value)` -- yes, this is correctly sorted descending. No issue after verification.
- **Fix**: None needed. Verified correct.

---

## Summary

- PASS: **Expression.Lambda safety** -- no user strings enter expression trees
- PASS: **Enum.Parse validation** -- all parse calls are guarded by name existence checks and numeric string rejection
- PASS: **Thread safety** -- ConcurrentDictionary + FrozenSet is correct concurrent pattern
- PASS: **Error message safety** -- only type/property metadata exposed, no secrets
- PASS: **Cache boundedness** -- bounded by finite enum types in app domain
- CONCERN: **Raw PropertyInfo.SetValue fallback** -- inconsistent with compiled-expression convention; likely a workaround for nullable unboxing in the compiled setter but lacks documentation (confidence: 80/100, non-blocking)
- CONCERN: **ToUInt64 overflow on negative enum values** -- unhandled OverflowException for exotic negative-valued flags; should be caught and wrapped (confidence: 70/100, non-blocking)

## Final Verdict

**APPROVED**

No blocking security issues found. The two concerns are non-blocking: the `SetValue` fallback is a performance/convention inconsistency (not a vulnerability), and the `ToUInt64` overflow is an edge case that produces an exception (not a security hole) for pathological enum definitions. Both are recommended for future cleanup.
