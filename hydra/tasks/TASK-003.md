# TASK-003: Collection Expressions

## Status: PLANNED

## Metadata
- **Task ID**: TASK-003
- **Wave**: 3
- **Depends on**: TASK-002
- **Retry count**: 0/3
- **Files modified**: src/DeltaMapper.Core/MappingProfile.cs, src/DeltaMapper.Core/MapperConfiguration.cs, src/DeltaMapper.Core/MapperConfigurationBuilder.cs

## Acceptance Criteria
1. All `new List<T>()` and `Array.Empty<T>()` replaced with `[]` collection expressions
2. All 79 tests pass
