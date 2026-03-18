# TASK-045: Cache fast-path routing decision per type pair

**Status**: READY
**Wave**: 2

## Description
Currently Map<TSource,TDest>() does TWO dictionary lookups (HasMap + TryGetFactory) every call. Cache the result in an instance-level ConcurrentDictionary so subsequent calls for the same type pair do ONE lookup.

## Files
- Modify: `src/DeltaMapper.Core/Runtime/Mapper.cs`
