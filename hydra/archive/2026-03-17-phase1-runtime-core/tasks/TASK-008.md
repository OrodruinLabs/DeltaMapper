# TASK-008: Mapper Runtime Executor and Convention Mapping Tests

## Description
Create the `Mapper` sealed class implementing `IMapper`. Each Map() call creates a fresh MapperContext, delegates to MapperConfiguration.Execute(), and casts the result. MapList iterates source, calls Map per element, returns pre-sized List<TDestination>. Add convention mapping tests for flat POCO mapping (same name/same type, case-insensitive, assignable types, unmapped property stays default, null source property).

## Status: DONE

## Metadata
- **Task ID**: TASK-008
- **Group**: 4
- **Wave**: 4
- **Depends on**: TASK-007
- **Retry count**: 0/3
- **Files modified**: src/DeltaMapper.Core/Mapper.cs, tests/DeltaMapper.UnitTests/ConventionMappingTests.cs
- **delegates_to**: implementer
- **traces_to**: TRD Section 3.2 (Mapper), TRD AC-3, TRD AC-14, TRD AC-20

## File Scope

### Creates
- `src/DeltaMapper.Core/Mapper.cs`
- `tests/DeltaMapper.UnitTests/ConventionMappingTests.cs`

### Modifies
- (none)

## Acceptance Criteria
1. `Mapper` sealed class implements all 5 `IMapper` overloads, creates fresh MapperContext per call, delegates to MapperConfiguration.Execute
2. Convention mapping tests C-01 through C-05 pass (same name/type, case-insensitive, assignable types int->long, unmapped stays default, null maps to null)
3. `MapList` pre-sizes destination list and correctly maps 0, 1, and N items

## Test Requirements
- `ConventionMappingTests.cs`: Tests C-01 (same name/type maps all), C-02 (case-insensitive matching), C-03 (assignable types int->long), C-04 (unmapped stays default), C-05 (null source property maps null)

## Pattern Reference
- TRD Section 3.2 for Mapper implementation
- docs/DELTAMAP_PLAN.md:170-183 for Mapper class
