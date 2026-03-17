# TASK-031: DeltaMapper.OpenTelemetry project scaffolding + TracingMiddleware

**Status**: IMPLEMENTED
**claimed_at**: 2026-03-17T18:35:00Z
**implemented_at**: 2026-03-17T18:40:00Z
**Base SHA**: 2d9605877b3cd47b19f026167fa7e88f8379804f
**Depends on**: --
**Wave**: 1
**Retry count**: 0/3
**Delegates to**: implementer
**Traces to**: Phase 4.2 (docs/DELTAMAP_PLAN.md:455-485)
**Files modified**: src/DeltaMapper.OpenTelemetry/DeltaMapper.OpenTelemetry.csproj, src/DeltaMapper.OpenTelemetry/TracingMiddleware.cs

## Description

Create the DeltaMapper.OpenTelemetry class library project with NuGet metadata matching the Core package pattern. Implement `TracingMiddleware : IMappingMiddleware` that:

1. Holds a `static readonly ActivitySource Source = new("DeltaMapper")` field.
2. On each `Map()` call, starts an `Activity` named `"Map {sourceTypeName} -> {destTypeName}"`.
3. After `next()` returns, sets tags: `mapper.source_type` (full name), `mapper.dest_type` (full name).
4. Disposes the activity via `using` pattern (measures wall-clock time of the mapping).
5. If no listener is subscribed, `StartActivity` returns null — zero allocation overhead.

The middleware is thread-safe (static ActivitySource, Activity is per-call).

## File Scope

### Creates
- `src/DeltaMapper.OpenTelemetry/DeltaMapper.OpenTelemetry.csproj` — class library targeting net10.0, ProjectReference to DeltaMapper.Core (no external package references — DiagnosticSource is in-box for net10.0)
- `src/DeltaMapper.OpenTelemetry/TracingMiddleware.cs` — the middleware implementation

### Modifies
- None

## Pattern Reference

- Middleware interface: `src/DeltaMapper.Core/Middleware/IMappingMiddleware.cs:1-19`
- Design spec: `docs/DELTAMAP_PLAN.md:462-485`
- NuGet metadata pattern: `src/DeltaMapper.Core/DeltaMapper.Core.csproj:1-44`

## Acceptance Criteria

1. `dotnet build src/DeltaMapper.OpenTelemetry/ -c Release` compiles without errors or warnings
2. `TracingMiddleware` implements `IMappingMiddleware`, creates an `ActivitySource("DeltaMapper")`, and wraps `next()` in an Activity span with source/dest type tags
3. When no `ActivityListener` is registered, `StartActivity` returns null and the middleware adds zero allocation overhead beyond the null check

## Review Evidence

- Build output log
- Code review of Activity lifecycle
