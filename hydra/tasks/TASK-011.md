# TASK-011: Collection Mapping and Nested Object Mapping Tests

## Description
Add comprehensive tests for collection mapping (List<T>, T[], IEnumerable<T>, empty, null collections) and nested object mapping (recursive mapping, null nested, deep 3-level nesting, missing nested mapping throws). These test the expression compilation engine's handling of complex types and collections.

## Status
DONE

## Metadata
- **Task ID**: TASK-011
- **Group**: 5
- **Wave**: 5
- **Depends on**: TASK-008
- **Retry count**: 0/3
- **Files modified**: tests/DeltaMapper.UnitTests/CollectionMappingTests.cs, tests/DeltaMapper.UnitTests/NestedMappingTests.cs
- **delegates_to**: implementer
- **traces_to**: TRD AC-4 (nested), TRD AC-5 (collections), TRD AC-15 (MapList)

## File Scope

### Creates
- `tests/DeltaMapper.UnitTests/CollectionMappingTests.cs`
- `tests/DeltaMapper.UnitTests/NestedMappingTests.cs`

### Modifies
- (none)

## Acceptance Criteria
1. Collection mapping tests CL-01 through CL-08 pass (List to List, List to Array, IEnumerable to List, empty collection, null collection, MapList empty/single/multiple)
2. Nested mapping tests N-01 through N-04 pass (recursive nested, null nested, 3-level deep, missing nested mapping throws DeltaMapperException)
3. Collections are pre-sized when source count is known; null collections map to null (not empty)

## Test Requirements
- `CollectionMappingTests.cs`: Tests CL-01 to CL-08 from test plan
- `NestedMappingTests.cs`: Tests N-01 to N-04 from test plan

## Pattern Reference
- Test Plan Sections 2.2 and 2.3 for test scenarios
- TRD Section 3.5 convention rules 3 and 4
