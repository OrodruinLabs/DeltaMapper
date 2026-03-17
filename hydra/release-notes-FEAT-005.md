# Release Notes — FEAT-005: Phase 4 Ecosystem Integrations

**Version**: 0.4.0-alpha.1
**Date**: 2026-03-17

## New Packages

### DeltaMapper.EFCore
- `EFCoreProxyMiddleware` detects Castle.Core dynamic proxies (EF Core lazy loading) and skips unloaded navigation properties
- Register via `builder.AddEFCoreSupport()` extension method
- Zero overhead for non-proxy entities (fast path)
- Depends on `Microsoft.EntityFrameworkCore` 10.*

### DeltaMapper.OpenTelemetry
- `TracingMiddleware` emits `System.Diagnostics.Activity` spans for every mapping operation
- Span name: `"Map {SourceType} -> {DestType}"`
- Tags: `mapper.source_type`, `mapper.dest_type` (set before mapping for error visibility)
- Error recording: `ActivityStatusCode.Error` + exception event on failure
- `HasListeners()` fast path — zero overhead when no ActivityListener subscribed
- Register via `builder.AddMapperTracing()` extension method
- No external dependencies (System.Diagnostics is in-box for net10.0)

## Infrastructure
- GitHub Actions CI workflow (`.github/workflows/ci.yml`) — build + test on PRs and master pushes

## Test Coverage
- 9 new integration tests (4 EF Core + 5 tracing)
- Total: 145 tests (95 unit + 9 integration + 41 source gen)

## Breaking Changes
None.

## Package Dependency Matrix

| Package | Depends On | External Deps |
|---------|-----------|---------------|
| DeltaMapper (Core) | — | Microsoft.Extensions.DependencyInjection.Abstractions 10.* |
| DeltaMapper.SourceGen | — | Microsoft.CodeAnalysis.CSharp 4.* |
| DeltaMapper.EFCore | DeltaMapper | Microsoft.EntityFrameworkCore 10.* |
| DeltaMapper.OpenTelemetry | DeltaMapper | (none — BCL only) |

## What's Next
- Phase 5: Benchmarks + full docs site
