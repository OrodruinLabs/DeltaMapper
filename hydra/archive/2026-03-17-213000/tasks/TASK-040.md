# TASK-040: Skip pipeline closure when no middleware

**Status**: READY
**Wave**: 1
**Depends on**: --

## Description
When no middleware is registered, `Execute()` still creates a lambda. Skip pipeline entirely with a `_hasMiddleware` branch.

## Files
- Modify: `src/DeltaMapper.Core/Configuration/MapperConfiguration.cs`

## Implementation
- Add `private readonly bool _hasMiddleware` field
- In `Execute()`: if `!_hasMiddleware`, call `ExecuteCore()` directly
- Add `internal bool HasMiddleware => _hasMiddleware;`

## Acceptance Criteria
1. All tests pass (including middleware pipeline tests)
2. No lambda allocation when middleware count is 0
