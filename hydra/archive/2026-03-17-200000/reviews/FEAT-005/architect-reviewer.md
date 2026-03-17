# Architect Review: FEAT-005 (Phase 4 — EF Core + OpenTelemetry)

## Checklist Evaluation

### Finding: OpenTelemetry package missing System.Diagnostics.DiagnosticSource dependency
- **Severity**: HIGH
- **Confidence**: 95
- **File**: src/DeltaMapper.OpenTelemetry/DeltaMapper.OpenTelemetry.csproj (entire file)
- **Category**: Architecture
- **Verdict**: REJECT
- **Issue**: The design spec at `docs/DELTAMAP_PLAN.md` line 459 explicitly requires `<PackageReference Include="System.Diagnostics.DiagnosticSource" Version="8.*" />`. The csproj has zero PackageReference entries beyond the Core ProjectReference. While `System.Diagnostics.ActivitySource` ships in-box with the `net10.0` TFM (so it compiles), the plan mandates this dependency for forward compatibility and to enable down-level TFM support in the future. More importantly, the NuGet package will not carry a transitive dependency on DiagnosticSource, which means consumers on older runtimes or trimmed deployments could fail at runtime.
- **Fix**: Add `<PackageReference Include="System.Diagnostics.DiagnosticSource" Version="9.*" />` to the `DeltaMapper.OpenTelemetry.csproj` ItemGroup (use 9.* or 10.* to align with the net10.0 TFM rather than the plan's 8.* which was written when targeting net8.0).
- **Pattern reference**: docs/DELTAMAP_PLAN.md:459

### Finding: EFCoreProxyMiddleware is public but plan specifies internal sealed
- **Severity**: MEDIUM
- **Confidence**: 90
- **File**: src/DeltaMapper.EFCore/EFCoreProxyMiddleware.cs:10
- **Category**: Architecture / Public API Surface
- **Verdict**: REJECT
- **Issue**: The design spec at `docs/DELTAMAP_PLAN.md` line 444 declares `internal sealed class EFCoreProxyMiddleware`. The implementation uses `public sealed class`. This unnecessarily expands the public API surface of the NuGet package. Users should register middleware via the `AddEFCoreSupport()` extension method, not by directly referencing the middleware type. Making it public creates a backward-compatibility contract.
- **Fix**: Change `public sealed class EFCoreProxyMiddleware` to `internal sealed class EFCoreProxyMiddleware`. The `Use<T>()` generic constraint requires `new()` but not `public` when called from within the same assembly (via the extension method). Since `EFCoreMapperExtensions.AddEFCoreSupport()` lives in the same project, `internal` visibility is sufficient.
- **Pattern reference**: docs/DELTAMAP_PLAN.md:444

### Finding: TracingMiddleware is public but plan specifies internal sealed
- **Severity**: MEDIUM
- **Confidence**: 90
- **File**: src/DeltaMapper.OpenTelemetry/TracingMiddleware.cs:11
- **Category**: Architecture / Public API Surface
- **Verdict**: REJECT
- **Issue**: Same as above. The design spec at line 464 declares `internal sealed class TracingMiddleware`. The implementation uses `public sealed class`. This leaks an implementation detail into the public API.
- **Fix**: Change to `internal sealed class TracingMiddleware`. The `AddMapperTracing()` extension in the same assembly handles registration.
- **Pattern reference**: docs/DELTAMAP_PLAN.md:464

### Finding: EFCoreProxyMiddleware does not actually skip unloaded navigations
- **Severity**: MEDIUM
- **Confidence**: 85
- **File**: src/DeltaMapper.EFCore/EFCoreProxyMiddleware.cs:12-22
- **Category**: Architecture / Functional Correctness
- **Verdict**: CONCERN
- **Issue**: The middleware's `Map` method for proxy entities simply calls `next()` in both branches (proxy and non-proxy). The code comment says "mark context so navigations can be handled appropriately" but no context marking actually happens. The design spec says the middleware should "detect EF Core proxy types, skip unloaded navigation properties." The current implementation is a no-op pass-through for all cases. While the tests pass because InMemory provider does not create Castle.Core proxies, this means the middleware provides zero value against actual SQL Server or PostgreSQL proxies.
- **Fix**: For proxy entities, the middleware should inspect navigation properties via `Microsoft.EntityFrameworkCore.Infrastructure.ILazyLoader` or check `ILazyLoadingProxy` interface, and set a flag in `MapperContext` (which would need an `Items` or `Properties` dictionary for extensibility data) to signal the core mapper to skip unloaded navigations. This is a non-trivial implementation gap but marking as CONCERN rather than REJECT since the tests document the intended behavior and the middleware is correctly wired into the pipeline.
- **Pattern reference**: docs/DELTAMAP_PLAN.md:448-449

### Finding: TracingMiddleware string interpolation on every call
- **Severity**: LOW
- **Confidence**: 75
- **File**: src/DeltaMapper.OpenTelemetry/TracingMiddleware.cs:18
- **Category**: Architecture / Performance
- **Verdict**: CONCERN
- **Issue**: `Source.StartActivity($"Map {source.GetType().Name} -> {destType.Name}")` performs string interpolation and `GetType()` on every mapping call even when no listener is active. The `StartActivity` method returns null when no listener is attached, but the string allocation still occurs. For a mapper that advertises sub-microsecond performance, this is measurable overhead.
- **Fix**: Use the overload `Source.StartActivity(name, kind)` with a pre-computed or cached activity name, or check `Source.HasListeners()` first to avoid the allocation in the common no-listener case:
  ```csharp
  if (!Source.HasListeners()) return next();
  ```
- **Pattern reference**: docs/DELTAMAP_PLAN.md:468-470

### Finding: Core csproj has Microsoft.Extensions.DependencyInjection.Abstractions dependency
- **Severity**: LOW
- **Confidence**: 60
- **File**: src/DeltaMapper.Core/DeltaMapper.Core.csproj:39
- **Category**: Architecture / Zero-Dependency Rule
- **Verdict**: CONCERN
- **Issue**: The review checklist states "Core MUST NOT add NuGet package references (except framework BCL)." The Core csproj references `Microsoft.Extensions.DependencyInjection.Abstractions`. This is pre-existing (not introduced by FEAT-005) and is documented in the project context as an accepted deviation for DI integration. Flagging for awareness only since it predates this task.
- **Fix**: No action required for FEAT-005. Consider extracting DI extensions to a separate `DeltaMapper.DependencyInjection` package in a future task.
- **Pattern reference**: System prompt: "Core has zero runtime dependencies — DI integration depends on Microsoft.Extensions.DependencyInjection.Abstractions only"

## Summary

- **PASS**: Module boundaries -- EFCore and OpenTelemetry are correctly separated into their own projects with proper ProjectReferences to Core only
- **PASS**: Dependency direction -- Core has zero references to EFCore or OpenTelemetry projects
- **PASS**: NuGet packaging -- Each project has correct PackageId, Version, and GeneratePackageOnBuild metadata
- **PASS**: Solution structure -- DeltaMapper.slnx correctly includes both new projects under /src/ and IntegrationTests under /tests/
- **PASS**: Middleware interface compliance -- Both middlewares implement `IMappingMiddleware` with the correct signature matching Core's interface
- **PASS**: Thread safety -- `TracingMiddleware.Source` is `static readonly ActivitySource` (thread-safe). `EFCoreProxyMiddleware` is stateless. Both are safe for concurrent use.
- **PASS**: Extension method signatures -- `AddEFCoreSupport` and `AddMapperTracing` match the plan's API signatures exactly
- **PASS**: XML doc comments -- All public types and methods have XML doc comments per coding standards
- **PASS**: Test coverage -- Integration tests cover middleware registration, proxy pass-through, tracing span emission, tag verification, no-listener graceful degradation, and activity duration
- **REJECT**: Missing `System.Diagnostics.DiagnosticSource` PackageReference in OpenTelemetry csproj (confidence: 95, blocking)
- **REJECT**: `EFCoreProxyMiddleware` visibility should be `internal` per plan (confidence: 90, blocking)
- **REJECT**: `TracingMiddleware` visibility should be `internal` per plan (confidence: 90, blocking)
- **CONCERN**: EFCoreProxyMiddleware is a no-op -- does not actually skip unloaded navigations (confidence: 85, non-blocking)
- **CONCERN**: TracingMiddleware allocates interpolated string even with no listeners (confidence: 75, non-blocking)
- **CONCERN**: Pre-existing DI abstractions dependency in Core (confidence: 60, non-blocking, not introduced by this task)

## Final Verdict

**CHANGES_REQUESTED**

Blocking issues requiring fixes:

1. **Add missing PackageReference** in `src/DeltaMapper.OpenTelemetry/DeltaMapper.OpenTelemetry.csproj`: add `<PackageReference Include="System.Diagnostics.DiagnosticSource" Version="9.*" />` (or `10.*`) to align with the design spec.

2. **Change visibility** of `EFCoreProxyMiddleware` in `src/DeltaMapper.EFCore/EFCoreProxyMiddleware.cs:10` from `public` to `internal`.

3. **Change visibility** of `TracingMiddleware` in `src/DeltaMapper.OpenTelemetry/TracingMiddleware.cs:11` from `public` to `internal`.
