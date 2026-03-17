# TASK-030: DeltaMapper.EFCore project scaffolding + EFCoreProxyMiddleware

**Status**: DONE
**Depends on**: --
**Wave**: 1
**Retry count**: 0/3
**Delegates to**: implementer
**Traces to**: Phase 4.1 (docs/DELTAMAP_PLAN.md:425-453)
**Files modified**: src/DeltaMapper.EFCore/DeltaMapper.EFCore.csproj, src/DeltaMapper.EFCore/EFCoreProxyMiddleware.cs

## Description

Create the DeltaMapper.EFCore class library project with NuGet metadata matching the Core package pattern. Implement `EFCoreProxyMiddleware : IMappingMiddleware` that:

1. Detects if the source object is an EF Core proxy type (Castle.Core dynamic proxy — the runtime type's base type is the actual entity type, and the proxy type lives in a dynamically generated assembly).
2. Before delegating to `next()`, inspects navigation properties on the source entity type.
3. Skips (nullifies on the destination) any navigation property where the lazy loader has not loaded the data — detected via checking if the navigation property value is the EF Core default/placeholder (null for reference navigations, or an uninitialized collection).
4. For non-proxy sources, passes through to `next()` with zero overhead.

The middleware must be thread-safe (it is instantiated once and shared across all mapping calls).

## File Scope

### Creates
- `src/DeltaMapper.EFCore/DeltaMapper.EFCore.csproj` — class library targeting net10.0, ProjectReference to DeltaMapper.Core, PackageReference to Microsoft.EntityFrameworkCore 10.*
- `src/DeltaMapper.EFCore/EFCoreProxyMiddleware.cs` — the middleware implementation

### Modifies
- None

## Pattern Reference

- Middleware interface: `src/DeltaMapper.Core/Middleware/IMappingMiddleware.cs:1-19`
- Middleware pipeline: `src/DeltaMapper.Core/Middleware/MappingPipeline.cs:1-32`
- NuGet metadata pattern: `src/DeltaMapper.Core/DeltaMapper.Core.csproj:1-44`
- Namespace convention: `DeltaMapper.EFCore` (matches project folder name)

## Acceptance Criteria

1. `dotnet build src/DeltaMapper.EFCore/ -c Release` compiles without errors or warnings
2. `EFCoreProxyMiddleware` implements `IMappingMiddleware` and correctly identifies Castle.Core proxy types by checking if the source type's base type differs from the source type and the source type name contains "Proxy"
3. The middleware skips navigation properties that are null (reference) or uninitialized (collections) on proxy entities, setting them to null/default on the destination

## Review Evidence

- Build output log
- Code review of proxy detection logic
