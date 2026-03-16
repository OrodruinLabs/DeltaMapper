# TASK-004: IMapper Interface and MapperContext

## Description
Create the `IMapper` public interface with all 5 overloads (inferred-source generic, explicit generic, existing-destination, list projection, non-generic). Create the `MapperContext` sealed class with circular reference tracking using `ReferenceEqualityComparer.Instance` and a `Dictionary<object, object>` for visited objects. MapperContext is internal with internal constructor, TryGetMapped, and Register methods.

## Status
DONE

## Metadata
- **Task ID**: TASK-004
- **Group**: 2
- **Wave**: 2
- **Depends on**: TASK-001
- **Retry count**: 0/3
- **Files modified**: src/DeltaMapper.Core/IMapper.cs, src/DeltaMapper.Core/MapperContext.cs, tests/DeltaMapper.UnitTests/MapperContextTests.cs
- **delegates_to**: implementer
- **traces_to**: TRD Section 3.1 (IMapper), TRD Section 3.7 (MapperContext), TRD AC-2, TRD AC-13

## File Scope

### Creates
- `src/DeltaMapper.Core/IMapper.cs`
- `src/DeltaMapper.Core/MapperContext.cs`
- `tests/DeltaMapper.UnitTests/MapperContextTests.cs`

### Modifies
- (none)

## Acceptance Criteria
1. `IMapper` interface declares all 5 overloads exactly matching TRD Section 3.1 signatures with XML doc comments
2. `MapperContext` uses `ReferenceEqualityComparer.Instance` for identity-based tracking, has `TryGetMapped` and `Register` methods
3. MapperContext unit tests verify: registering an object allows TryGetMapped to find it, same reference returns same mapped object, different reference returns false

## Test Requirements
- `MapperContextTests.cs`: Test TryGetMapped returns false for unvisited object, returns true after Register, returns correct mapped destination

## Pattern Reference
- TRD Section 3.1 for IMapper signature
- TRD Section 3.7 for MapperContext implementation
