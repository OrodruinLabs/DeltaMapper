# TASK-002: Primary Constructors

## Status: PLANNED

## Metadata
- **Task ID**: TASK-002
- **Wave**: 2
- **Depends on**: TASK-001
- **Retry count**: 0/3
- **Files modified**: src/DeltaMapper.Core/MapperConfiguration.cs, src/DeltaMapper.Core/Mapper.cs, src/DeltaMapper.Core/MapperContext.cs, src/DeltaMapper.Core/Middleware/MappingPipeline.cs

## Acceptance Criteria
1. CompiledMap, Mapper, MapperContext, MappingPipeline use primary constructors
2. No explicit backing fields for constructor-injected dependencies
3. All 79 tests pass
