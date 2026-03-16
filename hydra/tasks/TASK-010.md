# TASK-010: Record and Init-Only Property Support

## Description
Enhance the expression compilation engine in MapperConfiguration to support record types and init-only properties. Detect init-only setters via `MethodInfo.ReturnParameter.GetRequiredCustomModifiers()` checking for `IsExternalInit`. When all writable properties are init-only (record pattern), use constructor injection path: find primary constructor, map parameters by name (case-insensitive) to source properties, build expression calling `new TDst(param1, param2, ...)`. Support ForMember overrides on record constructor parameters.

## Status
DONE

## Metadata
- **Task ID**: TASK-010
- **Group**: 5
- **Wave**: 5
- **Depends on**: TASK-008
- **Retry count**: 0/3
- **Files modified**: src/DeltaMapper.Core/MapperConfiguration.cs, tests/DeltaMapper.UnitTests/RecordMappingTests.cs
- **delegates_to**: implementer
- **traces_to**: TRD Section 3.5 (Record/init-only support), TRD AC-11, TRD AC-12

## File Scope

### Creates
- `tests/DeltaMapper.UnitTests/RecordMappingTests.cs`

### Modifies
- `src/DeltaMapper.Core/MapperConfiguration.cs`

## Acceptance Criteria
1. C# `record` destinations map via constructor injection -- primary constructor parameters matched by name (case-insensitive) to source properties
2. Classes with `init`-only setters (non-record) map correctly using init setter assignment
3. Tests REC-01 through REC-05 pass (record via constructor, record with extra init props, init-only class, record-to-record, record with ForMember override)

## Test Requirements
- `RecordMappingTests.cs`: Tests REC-01 (record maps via constructor), REC-02 (record with additional properties), REC-03 (init-only class), REC-04 (record to record), REC-05 (record with ForMember)

## Pattern Reference
- TRD Section 3.5 "Record/init-only support" subsection
- docs/DELTAMAP_PLAN.md:228-230 for detection approach
