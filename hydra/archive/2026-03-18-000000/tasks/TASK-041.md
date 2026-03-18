# TASK-041: Compiled expression delegates

**Status**: READY
**Wave**: 2
**Depends on**: --

## Description
Replace `PropertyInfo.GetValue/SetValue` with compiled Expression delegates. Replace `Activator.CreateInstance` with compiled factory.

## Files
- Modify: `src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs`

## Acceptance Criteria
1. All tests pass
2. No PropertyInfo.GetValue/SetValue in hot path
