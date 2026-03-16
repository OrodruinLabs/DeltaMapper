# TASK-012: Circular Reference Detection Tests

## Description
Add comprehensive tests for circular reference detection via MapperContext. Verify that direct circular references (A->B->A), self-referencing nodes, and deep circular chains (A->B->C->A) complete without StackOverflowException. Verify that previously mapped instances are reused (reference identity preserved in the mapped graph).

## Status
DONE

## Metadata
- **Task ID**: TASK-012
- **Group**: 5
- **Wave**: 5
- **Depends on**: TASK-008
- **Retry count**: 0/3
- **Files modified**: tests/DeltaMapper.UnitTests/CircularReferenceTests.cs
- **delegates_to**: implementer
- **traces_to**: TRD Section 3.7 (MapperContext), TRD AC-13

## File Scope

### Creates
- `tests/DeltaMapper.UnitTests/CircularReferenceTests.cs`

### Modifies
- (none)

## Acceptance Criteria
1. Direct circular reference (Parent->Child->Parent) maps without StackOverflowException
2. Mapped circular reference preserves identity: `mappedA.B.A` is the same reference as `mappedA`
3. Deep circular chain (A->B->C->A) and self-referencing TreeNode both resolve correctly

## Test Requirements
- `CircularReferenceTests.cs`: Tests CR-01 (direct circular no stack overflow), CR-02 (returns previously mapped instance), CR-03 (self-referencing node), CR-04 (deep 3-node cycle)

## Pattern Reference
- Test Plan Section 2.8 for test scenarios
- TRD Section 3.7 for MapperContext circular reference tracking
