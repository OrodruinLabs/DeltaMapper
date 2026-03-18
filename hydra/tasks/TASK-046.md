# TASK-046: Generate public static Map methods

**Status**: READY
**Wave**: 3

## Description
Generate a public static Map method on each profile class for zero-overhead direct calls that bypass IMapper entirely. Matches Mapperly's call pattern exactly.

## Files
- Modify: `src/DeltaMapper.SourceGen/EmitHelper.cs`
- Modify: `src/DeltaMapper.SourceGen/MapperGenerator.cs`
