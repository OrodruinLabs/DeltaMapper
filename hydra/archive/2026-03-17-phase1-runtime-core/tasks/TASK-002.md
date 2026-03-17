# TASK-002: DeltaMapperException Custom Exception

## Description
Create the custom exception type `DeltaMapperException` in the Exceptions directory. The exception must always include source type name, destination type name, and a resolution hint in the message. Provide constructors for standard exception patterns (message, inner exception). Include XML doc comments on all public members.

## Status: DONE

## Metadata
- **Task ID**: TASK-002
- **Group**: 1
- **Wave**: 1
- **Depends on**: none
- **Retry count**: 0/3
- **Files modified**: src/DeltaMapper.Core/Exceptions/DeltaMapperException.cs, tests/DeltaMapper.UnitTests/ErrorHandlingTests.cs
- **delegates_to**: implementer
- **traces_to**: TRD Section 3.9 (DeltaMapperException), TRD AC-16

## File Scope

### Creates
- `src/DeltaMapper.Core/Exceptions/DeltaMapperException.cs`
- `tests/DeltaMapper.UnitTests/ErrorHandlingTests.cs`

### Modifies
- (none)

## Acceptance Criteria
1. `DeltaMapperException` is a sealed class extending `Exception` with constructors for (string message), (string message, Exception inner), and a static factory method `ForMissingMapping(Type source, Type dest)` that produces a message containing both type names and a resolution hint
2. Exception message from `ForMissingMapping` contains both type names and text like "Register a mapping"
3. All public members have XML doc comments

## Test Requirements
- `ErrorHandlingTests.cs` with tests E-05 from test plan: DeltaMapperException_IsSerializable
- Test that ForMissingMapping produces message containing source and dest type names
- Test that message contains resolution hint text

## Pattern Reference
- TRD Section 3.9 for exception design
- docs/DELTAMAP_PLAN.md:665 for exception message format
