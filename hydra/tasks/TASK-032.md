# TASK-032: DI extension methods (AddEFCoreSupport, AddMapperTracing)

**Status**: READY
**Depends on**: TASK-030, TASK-031
**Wave**: 2
**Retry count**: 0/3
**Delegates to**: implementer
**Traces to**: Phase 4.1-4.2 (docs/DELTAMAP_PLAN.md:436-484)
**Files modified**: src/DeltaMapper.EFCore/EFCoreMapperExtensions.cs, src/DeltaMapper.OpenTelemetry/OpenTelemetryMapperExtensions.cs, DeltaMapper.slnx

## Description

Add DI-friendly extension methods for both ecosystem packages and wire them into the solution file:

1. `EFCoreMapperExtensions.cs` in the EFCore package — static class with `AddEFCoreSupport(this MapperConfigurationBuilder builder)` that calls `builder.Use<EFCoreProxyMiddleware>()` and returns builder for fluent chaining.
2. `OpenTelemetryMapperExtensions.cs` in the OpenTelemetry package — static class with `AddMapperTracing(this MapperConfigurationBuilder builder)` that calls `builder.Use<TracingMiddleware>()` and returns builder.
3. Update `DeltaMapper.slnx` to include both new projects under the `/src/` folder.
4. Both extension classes must have XML doc comments on all public members.

## File Scope

### Creates
- `src/DeltaMapper.EFCore/EFCoreMapperExtensions.cs` — extension method class
- `src/DeltaMapper.OpenTelemetry/OpenTelemetryMapperExtensions.cs` — extension method class

### Modifies
- `DeltaMapper.slnx` — add EFCore and OpenTelemetry projects to /src/ folder

## Pattern Reference

- Existing DI extension pattern: `src/DeltaMapper.Core/Extensions/ServiceCollectionExtensions.cs:1-31`
- Builder Use<T> method: `src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:39-43`
- Solution file format: `DeltaMapper.slnx:1-10`

## Acceptance Criteria

1. `dotnet build DeltaMapper.slnx -c Release` succeeds with both new projects included — zero warnings
2. `AddEFCoreSupport()` is a public static extension method on `MapperConfigurationBuilder` that registers `EFCoreProxyMiddleware` and returns the builder
3. `AddMapperTracing()` is a public static extension method on `MapperConfigurationBuilder` that registers `TracingMiddleware` and returns the builder

## Review Evidence

- Full solution build output
- Verify slnx includes both projects
