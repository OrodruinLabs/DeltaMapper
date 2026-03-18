# TASK-039: Lazy MapperContext

**Status**: READY
**Wave**: 1
**Depends on**: --

## Description
Make `_visited` Dictionary in MapperContext lazy-initialized. Currently allocated eagerly on every `Map()` call.

## Files
- Modify: `src/DeltaMapper.Core/Runtime/MapperContext.cs`

## Implementation
- Change `_visited` to `Dictionary<object, object>? _visited` (nullable)
- `TryGetMapped`: if `_visited is null`, return false
- `Register`: `_visited ??= new Dictionary<object, object>(ReferenceEqualityComparer.Instance)`

## Acceptance Criteria
1. All 145 tests pass
2. No Dictionary allocated for flat object mappings
