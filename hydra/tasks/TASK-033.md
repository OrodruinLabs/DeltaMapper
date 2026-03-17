# TASK-033: Integration test project + EF Core proxy tests

**Status**: READY
**Depends on**: TASK-032
**Wave**: 3
**Retry count**: 0/3
**Delegates to**: implementer
**Traces to**: Phase 4.1 (docs/DELTAMAP_PLAN.md:425-453)
**Files modified**: tests/DeltaMapper.IntegrationTests/DeltaMapper.IntegrationTests.csproj, tests/DeltaMapper.IntegrationTests/EFCoreProxyTests.cs, DeltaMapper.slnx

## Description

Create the integration test project and write comprehensive EF Core proxy middleware tests:

1. Create `tests/DeltaMapper.IntegrationTests/` project targeting net10.0 with:
   - ProjectReferences to DeltaMapper.Core, DeltaMapper.EFCore, DeltaMapper.OpenTelemetry
   - PackageReferences: xunit, FluentAssertions, Microsoft.EntityFrameworkCore.InMemory 10.*, Microsoft.NET.Test.Sdk
2. Write `EFCoreProxyTests.cs` covering:
   - `EFCore01_ProxyEntityMapsWithoutNavigations` — entity with unloaded navigation property: navigation is null/default on destination
   - `EFCore02_NonProxyEntityMapsNormally` — regular POCO maps all properties including navigation-like references
   - `EFCore03_LoadedNavigationMapsThrough` — entity with loaded (non-null) navigation property: navigation IS mapped to destination
   - `EFCore04_MiddlewareRegisteredViaAddEFCoreSupport` — verify `AddEFCoreSupport()` registers the middleware and it executes in the pipeline
3. Add the integration test project to `DeltaMapper.slnx` under `/tests/` folder.

Test models: define inline `Blog`, `Post` entities with a `BlogDbContext : DbContext` using InMemory provider. Use `UseLazyLoadingProxies()` if available, or simulate proxy behavior by creating entities through the DbContext (which produces change-tracked entities).

## File Scope

### Creates
- `tests/DeltaMapper.IntegrationTests/DeltaMapper.IntegrationTests.csproj`
- `tests/DeltaMapper.IntegrationTests/EFCoreProxyTests.cs`

### Modifies
- `DeltaMapper.slnx` — add integration test project to /tests/ folder

## Pattern Reference

- Test conventions: `tests/DeltaMapper.UnitTests/MiddlewarePipelineTests.cs:1-89`
- Test project structure: `tests/DeltaMapper.UnitTests/DeltaMapper.UnitTests.csproj:1-18`
- Test naming: `TestCategory##_Description` pattern

## Acceptance Criteria

1. `dotnet test tests/DeltaMapper.IntegrationTests/ -c Release` passes all EF Core proxy tests with zero failures
2. Tests verify that unloaded navigation properties on EF-tracked entities are skipped (null on destination) while loaded navigations are mapped
3. Tests verify that `AddEFCoreSupport()` correctly registers and activates the middleware in the mapping pipeline

## Review Evidence

- Test run output (trx or console)
- Test coverage of proxy vs non-proxy paths
