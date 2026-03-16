# TASK-014: Non-Generic Map, Existing Destination, and Error Handling Tests

## Description
Add remaining test coverage: non-generic Map(object, Type, Type) overload tests, inferred-source Map<TDest>(object) tests, existing destination mapping tests (update properties on existing object, preserve unmapped, apply ForMember), and error handling tests (missing mapping throws DeltaMapperException with type names and resolution hint, null source throws ArgumentNullException).

## Status
DONE

## Metadata
- **Task ID**: TASK-014
- **Group**: 6
- **Wave**: 6
- **Depends on**: TASK-008
- **Retry count**: 0/3
- **Files modified**: tests/DeltaMapper.UnitTests/NonGenericMapTests.cs, tests/DeltaMapper.UnitTests/ExistingDestinationTests.cs, tests/DeltaMapper.UnitTests/ErrorHandlingTests.cs
- **delegates_to**: implementer
- **traces_to**: TRD AC-14 (non-generic), TRD AC-16 (DeltaMapperException), TRD AC-20

## File Scope

### Creates
- `tests/DeltaMapper.UnitTests/NonGenericMapTests.cs`
- `tests/DeltaMapper.UnitTests/ExistingDestinationTests.cs`

### Modifies
- `tests/DeltaMapper.UnitTests/ErrorHandlingTests.cs`

## Acceptance Criteria
1. Non-generic tests NG-01 through NG-03 pass (Map with Type params, unregistered throws, inferred source)
2. Existing destination tests ED-01 through ED-03 pass (updates properties, preserves unmapped, applies ForMember)
3. Error handling tests E-01 through E-04 pass (no mapping throws DeltaMapperException, message contains type names, message contains hint, null source throws ArgumentNullException)

## Test Requirements
- `NonGenericMapTests.cs`: Tests NG-01 to NG-03 from test plan
- `ExistingDestinationTests.cs`: Tests ED-01 to ED-03 from test plan
- `ErrorHandlingTests.cs` (modify): Add tests E-01 to E-04 from test plan (E-05 already in TASK-002)

## Pattern Reference
- Test Plan Sections 2.9, 2.10, 2.13 for test scenarios
