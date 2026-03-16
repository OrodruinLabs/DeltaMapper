# TASK-013: DI Integration (ServiceCollectionExtensions)

## Description
Create `ServiceCollectionExtensions` with `AddDeltaMapper(this IServiceCollection, Action<MapperConfigurationBuilder>)` extension method. Registers `MapperConfiguration` as singleton (immutable, thread-safe) and `IMapper` as singleton (stateless, MapperContext is per-call). Add comprehensive DI integration tests.

## Status
PLANNED

## Metadata
- **Task ID**: TASK-013
- **Group**: 6
- **Wave**: 6
- **Depends on**: TASK-008
- **Retry count**: 0/3
- **Files modified**: src/DeltaMapper.Core/ServiceCollectionExtensions.cs, tests/DeltaMapper.UnitTests/DependencyInjectionTests.cs
- **delegates_to**: implementer
- **traces_to**: TRD Section 3.10 (DI Integration), TRD AC-17

## File Scope

### Creates
- `src/DeltaMapper.Core/ServiceCollectionExtensions.cs`
- `tests/DeltaMapper.UnitTests/DependencyInjectionTests.cs`

### Modifies
- (none)

## Acceptance Criteria
1. `AddDeltaMapper` registers `MapperConfiguration` as singleton and `IMapper` as singleton, both resolvable from `ServiceProvider`
2. Resolved `IMapper` can successfully execute mappings from profiles registered in the configure delegate
3. DI tests DI-01 through DI-05 pass (registers IMapper, registers MapperConfiguration, singleton behavior, mapping works, multiple profiles)

## Test Requirements
- `DependencyInjectionTests.cs`: Tests DI-01 (registers IMapper), DI-02 (registers MapperConfiguration), DI-03 (singleton), DI-04 (mapping works via DI), DI-05 (multiple profiles)

## Pattern Reference
- TRD Section 3.10 for ServiceCollectionExtensions implementation
- docs/DELTAMAP_PLAN.md:203-224 for DI pattern
