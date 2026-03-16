# TASK-006: Middleware Pipeline Stubs

## Description
Create the `IMappingMiddleware` interface and `MappingPipeline` internal class. The pipeline chains registered middleware in order with the innermost `next()` being the actual compiled delegate invocation. When no middleware is registered, the pipeline is bypassed entirely (zero overhead). These are stubs for Phase 1 -- no built-in middleware ships.

## Status: DONE

## Metadata
- **Task ID**: TASK-006
- **Group**: 2
- **Wave**: 2
- **Depends on**: TASK-001
- **Retry count**: 0/3
- **Files modified**: src/DeltaMapper.Core/Middleware/IMappingMiddleware.cs, src/DeltaMapper.Core/Middleware/MappingPipeline.cs, tests/DeltaMapper.UnitTests/MiddlewarePipelineTests.cs
- **delegates_to**: implementer
- **traces_to**: TRD Section 3.8 (Middleware Pipeline)

## File Scope

### Creates
- `src/DeltaMapper.Core/Middleware/IMappingMiddleware.cs`
- `src/DeltaMapper.Core/Middleware/MappingPipeline.cs`
- `tests/DeltaMapper.UnitTests/MiddlewarePipelineTests.cs`

### Modifies
- (none)

## Acceptance Criteria
1. `IMappingMiddleware` interface declares `object Map(object source, Type destType, MapperContext ctx, Func<object> next)` with XML doc comments
2. `MappingPipeline` executes middleware in registration order, with innermost next() calling the core delegate; when no middleware registered, directly invokes the core delegate
3. Unit tests verify: empty pipeline calls core directly, single middleware wraps core, multiple middleware execute in order

## Test Requirements
- `MiddlewarePipelineTests.cs`: Test empty pipeline invokes core delegate, test single middleware can intercept and call next, test middleware execution order (first registered runs first)

## Pattern Reference
- TRD Section 3.8 for IMappingMiddleware and MappingPipeline design
