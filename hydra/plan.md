# Plan — Phase 4: Ecosystem Integrations (FEAT-005)

─── ◈ HYDRA ▸ PLANNING ─────────────────────────────

## Objective

Implement Phase 4 — Ecosystem Integrations: DeltaMapper.EFCore package with EFCoreProxyMiddleware that detects EF Core proxy types and skips unloaded navigation properties, DeltaMapper.OpenTelemetry package with TracingMiddleware that emits Activity spans for every mapping call, DI extension methods (AddEFCoreSupport, AddMapperTracing), and full test coverage for both packages.

## Status Summary

| Status | Count |
|--------|-------|
| READY  | 2     |
| IN_PROGRESS | 0 |
| IMPLEMENTED | 3     |
| DONE   | 0     |
| BLOCKED | 0    |
| TOTAL  | 5     |

## Recovery Pointer

**Next**: TASK-033, TASK-034 (Wave 3)
**State**: TASK-032 IMPLEMENTED
**Last updated**: 2026-03-17T18:40:00Z

## Tasks

| ID | Title | Status | Wave | Depends On |
|----|-------|--------|------|------------|
| TASK-030 | DeltaMapper.EFCore project scaffolding + EFCoreProxyMiddleware | IMPLEMENTED | 1 | -- |
| TASK-031 | DeltaMapper.OpenTelemetry project scaffolding + TracingMiddleware | IMPLEMENTED | 1 | -- |
| TASK-032 | DI extension methods (AddEFCoreSupport, AddMapperTracing) | IMPLEMENTED | 2 | TASK-030, TASK-031 |
| TASK-033 | Integration test project + EF Core proxy tests | READY | 3 | TASK-032 |
| TASK-034 | OpenTelemetry tracing tests + solution wiring | READY | 3 | TASK-032 |

## Wave Groups

### Wave 1
- TASK-030, TASK-031 (independent packages, no file overlap)

### Wave 2
- TASK-032 (depends on TASK-030 and TASK-031, adds DI extensions to both packages)

### Wave 3
- TASK-033, TASK-034 (independent test suites, share only the new integration test project file creation — but TASK-033 creates the project, TASK-034 only adds to it; sequenced within wave if needed)

## Design Notes

- EFCoreProxyMiddleware detects Castle.Core proxy types via `type.Assembly.GetName().Name` containing "DynamicProxyGenAssembly" or type name containing "Proxy" and base type matching the entity type. Navigation properties are identified via EF Core's `INavigation` metadata when available, or by convention (reference/collection types with known entity base).
- TracingMiddleware uses `System.Diagnostics.ActivitySource` — no external OpenTelemetry SDK dependency needed. The BCL ActivitySource/Activity API is the .NET native observability primitive.
- Both middleware classes are thread-safe (stateless or use static readonly fields).
- DeltaMapper.EFCore targets net10.0 with Microsoft.EntityFrameworkCore 10.* dependency.
- DeltaMapper.OpenTelemetry targets net10.0 (System.Diagnostics.DiagnosticSource is in-box for net10.0).
- Integration tests use Microsoft.EntityFrameworkCore.InMemory for EF Core proxy testing.
- OpenTelemetry tests use ActivityListener from System.Diagnostics for span assertions.
