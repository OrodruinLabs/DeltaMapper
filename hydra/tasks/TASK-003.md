# TASK-003: Shared Test Models

## Description
Create all reusable test model classes in `tests/DeltaMapper.UnitTests/TestModels/`. These are used across all test files. Includes flat POCOs (User, UserDto, UserSummaryDto), nested objects (Order, OrderDto, Customer, CustomerDto, Address, AddressDto), collections (Classroom, ClassroomDto, Student, StudentDto), circular references (Parent, Child, ParentDto, ChildDto, TreeNode, TreeNodeDto), records (PersonRecord, PersonRecordDto), and init-only classes (PersonInitOnly).

## Status
DONE

## Metadata
- **Task ID**: TASK-003
- **Group**: 1
- **Wave**: 1
- **Depends on**: none
- **Retry count**: 0/3
- **Files modified**: tests/DeltaMapper.UnitTests/TestModels/FlatModels.cs, tests/DeltaMapper.UnitTests/TestModels/NestedModels.cs, tests/DeltaMapper.UnitTests/TestModels/CollectionModels.cs, tests/DeltaMapper.UnitTests/TestModels/CircularModels.cs, tests/DeltaMapper.UnitTests/TestModels/RecordModels.cs
- **delegates_to**: implementer
- **traces_to**: Test Plan Section 3 (Test Models), TRD AC-3 through AC-15

## File Scope

### Creates
- `tests/DeltaMapper.UnitTests/TestModels/FlatModels.cs`
- `tests/DeltaMapper.UnitTests/TestModels/NestedModels.cs`
- `tests/DeltaMapper.UnitTests/TestModels/CollectionModels.cs`
- `tests/DeltaMapper.UnitTests/TestModels/CircularModels.cs`
- `tests/DeltaMapper.UnitTests/TestModels/RecordModels.cs`

### Modifies
- (none)

## Acceptance Criteria
1. All test model classes from Test Plan Section 3 are defined with correct properties and types
2. Record types use C# `record` syntax with positional parameters
3. Init-only class uses `init` setters, not regular setters

## Test Requirements
- No tests for test models themselves -- they compile as part of the test project build

## Pattern Reference
- Test Plan Section 3 for complete model definitions
