# TASK-034: OpenTelemetry tracing tests + solution wiring

**Status**: DONE
**Depends on**: TASK-032
**Wave**: 3
**Retry count**: 0/3
**Delegates to**: implementer
**Traces to**: Phase 4.2 (docs/DELTAMAP_PLAN.md:455-485)
**Files modified**: tests/DeltaMapper.IntegrationTests/TracingMiddlewareTests.cs

## Description

Write comprehensive OpenTelemetry tracing middleware tests in the integration test project (created by TASK-033):

1. Write `TracingMiddlewareTests.cs` covering:
   - `Tracing01_MappingEmitsActivitySpan` — register an `ActivityListener` for source "DeltaMapper", map an object, assert that an Activity was started with the correct operation name pattern `"Map {SourceType} -> {DestType}"`
   - `Tracing02_ActivityHasSourceAndDestTypeTags` — verify `mapper.source_type` and `mapper.dest_type` tags are set with full type names
   - `Tracing03_NoListenerNoActivity` — without an ActivityListener, mapping still succeeds and no Activity is created (verifies zero-overhead path)
   - `Tracing04_MiddlewareRegisteredViaAddMapperTracing` — verify `AddMapperTracing()` registers the middleware and it executes
   - `Tracing05_ActivityDurationReflectsMappingTime` — verify the activity has a non-zero duration (stopped after next() returns)

2. Use `System.Diagnostics.ActivityListener` for test assertions — subscribe to "DeltaMapper" source, collect activities, assert on name/tags/duration.

3. Test models: simple inline `Source`/`Dest` POCOs with a few properties.

## File Scope

### Creates
- `tests/DeltaMapper.IntegrationTests/TracingMiddlewareTests.cs`

### Modifies
- None (integration test project and slnx already created by TASK-033)

## Pattern Reference

- Test conventions: `tests/DeltaMapper.UnitTests/MiddlewarePipelineTests.cs:1-89`
- ActivitySource/ActivityListener API: `System.Diagnostics` namespace (BCL)
- TracingMiddleware design: `docs/DELTAMAP_PLAN.md:462-485`

## Acceptance Criteria

1. `dotnet test tests/DeltaMapper.IntegrationTests/ --filter "FullyQualifiedName~Tracing" -c Release` passes all tracing tests with zero failures
2. Tests verify Activity spans are emitted with correct operation names and tags when an ActivityListener is subscribed
3. Tests verify zero-overhead path: no Activity created when no listener is registered

## Review Evidence

- Test run output (trx or console)
- Test coverage of listener/no-listener paths
