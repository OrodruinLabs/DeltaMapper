# Security Review: FEAT-005 — EF Core + OpenTelemetry Middleware

**Reviewer**: Security Reviewer
**Date**: 2026-03-17
**Files reviewed**:
- `src/DeltaMapper.EFCore/EFCoreProxyMiddleware.cs`
- `src/DeltaMapper.EFCore/EFCoreMapperExtensions.cs`
- `src/DeltaMapper.OpenTelemetry/TracingMiddleware.cs`
- `src/DeltaMapper.OpenTelemetry/OpenTelemetryMapperExtensions.cs`
- `src/DeltaMapper.EFCore/DeltaMapper.EFCore.csproj`
- `src/DeltaMapper.OpenTelemetry/DeltaMapper.OpenTelemetry.csproj`

---

## Findings

### Finding: FullName in Activity tags may leak assembly-qualified type information
- **Severity**: LOW
- **Confidence**: 55
- **File**: `src/DeltaMapper.OpenTelemetry/TracingMiddleware.cs:22-23`
- **Category**: Security — Information Leakage
- **Verdict**: CONCERN
- **Issue**: `source.GetType().FullName` and `destType.FullName` are emitted as Activity tags. For proxy types or generic types, `FullName` can include assembly-qualified nested type names, version information, and namespace structure. In an OpenTelemetry context, these tags flow to configured exporters (Jaeger, OTLP, etc.) which may be accessible to operators who should not see internal type topology. However, since this is an in-process library and the consumer explicitly opts in to tracing by calling `AddMapperTracing()`, the risk is low.
- **Fix**: Consider using `Name` instead of `FullName` for tags, consistent with the Activity display name on line 18 which already uses `Name`. Alternatively, document that full type names are emitted so consumers can make an informed decision.
- **Pattern reference**: `TracingMiddleware.cs:18` already uses `.Name` for the Activity display name — tags should be consistent.

### Finding: Null dereference if source is null in TracingMiddleware
- **Severity**: MEDIUM
- **Confidence**: 85
- **File**: `src/DeltaMapper.OpenTelemetry/TracingMiddleware.cs:18`
- **Category**: Security — Null Safety
- **Verdict**: CONCERN
- **Issue**: `source.GetType()` on line 18 will throw `NullReferenceException` if `source` is null. The `IMappingMiddleware.Map` signature accepts `object source` (non-nullable in NRT context), but middleware sits in a pipeline where defensive null checks are good practice. A null source would crash the tracing middleware rather than propagating to the core mapper's own null handling.
- **Fix**: Add a null guard: `if (source is null) return next();` at the top of the method, or let it fall through with a safe activity name.

### Finding: Null dereference if source is null in EFCoreProxyMiddleware
- **Severity**: MEDIUM
- **Confidence**: 85
- **File**: `src/DeltaMapper.EFCore/EFCoreProxyMiddleware.cs:16`
- **Category**: Security — Null Safety
- **Verdict**: CONCERN
- **Issue**: `IsProxy(source)` calls `entity.GetType()` which will throw `NullReferenceException` if `source` is null. Same reasoning as the TracingMiddleware finding.
- **Fix**: Add a null guard: `if (source is null) return next();` at the top of `Map`.

### Finding: EF Core proxy detection uses Assembly name check
- **Severity**: LOW
- **Confidence**: 30
- **File**: `src/DeltaMapper.EFCore/EFCoreProxyMiddleware.cs:37-38`
- **Category**: Security — Type Safety
- **Verdict**: PASS
- **Issue**: The proxy detection checks `type.Assembly.GetName().Name` against known assembly names ("DynamicProxyGenAssembly2", "Castle.Core"). This is a read-only check on the type's own assembly metadata — it does not load any assembly or resolve types from strings. This is safe. The check is heuristic but cannot be exploited since it only gates a `return next()` call (both branches call `next()` currently).
- **Fix**: None required.

### Finding: No dangerous API usage detected
- **Severity**: LOW
- **Confidence**: 95
- **File**: All files
- **Category**: Security — Dangerous APIs
- **Verdict**: PASS
- **Issue**: Scanned for `Process.Start`, `Assembly.Load`, `Type.GetType`, `Activator.CreateInstance`, `File.*` I/O, `dynamic` keyword usage, and reflection-based invocation. None found. The `dynamic` hits in EFCore are only in XML doc comments, not executable code.
- **Fix**: None required.

### Finding: NuGet package metadata is clean
- **Severity**: LOW
- **Confidence**: 95
- **File**: Both `.csproj` files
- **Category**: Security — Supply Chain
- **Verdict**: PASS
- **Issue**: No secrets, API keys, or sensitive data in package metadata. Repository URLs point to the correct GitHub repository. License is MIT. No pre/post-build scripts that could execute arbitrary code. The EFCore project references `Microsoft.EntityFrameworkCore` with a floating `10.*` version — this is standard for preview-track development but should be pinned before GA release.
- **Fix**: None required now. Pin EF Core version before stable release.

### Finding: Thread safety is correct
- **Severity**: LOW
- **Confidence**: 90
- **File**: `src/DeltaMapper.OpenTelemetry/TracingMiddleware.cs:13`
- **Category**: Security — Thread Safety
- **Verdict**: PASS
- **Issue**: `ActivitySource` is a static readonly field, which is the documented thread-safe usage pattern. Both middleware classes are stateless (no instance fields), so they are inherently safe for concurrent use. `MapperContext` is per-call as confirmed by the existing codebase pattern.
- **Fix**: None required.

### Finding: No secrets or credentials in source
- **Severity**: LOW
- **Confidence**: 98
- **File**: All files
- **Category**: Security — Secrets
- **Verdict**: PASS
- **Issue**: No API keys, connection strings, tokens, or credentials found in any reviewed file.
- **Fix**: None required.

---

## Summary

- **PASS**: Dangerous API usage — no `Process.Start`, `Assembly.Load`, `Type.GetType`, `Activator.CreateInstance`, file I/O, or reflection found
- **PASS**: Assembly name check in proxy detection — read-only, no type loading
- **PASS**: Thread safety — stateless middleware, static `ActivitySource`
- **PASS**: NuGet metadata — clean, no secrets, correct URLs
- **PASS**: No secrets or credentials
- **CONCERN**: Null safety in `TracingMiddleware.Map` and `EFCoreProxyMiddleware.Map` — add null guards for defensive robustness (confidence: 85/100, non-blocking since NRT annotations on the interface suggest non-null, but middleware is a pipeline boundary)
- **CONCERN**: `FullName` in Activity tags — consider using `Name` for consistency and reduced information exposure (confidence: 55/100, non-blocking)

## Final Verdict

**APPROVED**

All security checks pass. The two CONCERN items are non-blocking:
1. Null guards are defensive improvements, not security vulnerabilities per se (the interface contract is non-nullable).
2. `FullName` in tags is an information-exposure consideration, not a vulnerability in an opt-in tracing library.

No blocking issues (no REJECT findings with confidence >= 80 and severity HIGH/MEDIUM).
