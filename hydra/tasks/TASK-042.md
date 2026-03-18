# TASK-042: Source-gen fast path

**Status**: READY
**Wave**: 3
**Depends on**: TASK-039, TASK-040

## Description
Add Func<TSource, TDest> factory registry. Mapper bypasses entire pipeline for source-gen maps when no middleware/profile override.

## Files
- Modify: `src/DeltaMapper.Core/Runtime/GeneratedMapRegistry.cs`
- Modify: `src/DeltaMapper.Core/Runtime/Mapper.cs`
- Modify: `src/DeltaMapper.Core/Configuration/MapperConfiguration.cs`
- Modify: `src/DeltaMapper.SourceGen/EmitHelper.cs`
- Modify: `src/DeltaMapper.SourceGen/MapperGenerator.cs`

## Acceptance Criteria
1. All tests pass
2. Source-gen fast path bypasses pipeline
3. Middleware still fires when registered
4. Profile-compiled maps take precedence
