# TASK-001: Retarget Projects to net10.0

## Status: DONE

## Metadata
- **Task ID**: TASK-001
- **Wave**: 1
- **Depends on**: none
- **Retry count**: 0/3
- **Files modified**: src/DeltaMapper.Core/DeltaMapper.Core.csproj, tests/DeltaMapper.UnitTests/DeltaMapper.UnitTests.csproj

## Acceptance Criteria
1. Both csproj files target net10.0 only (singular TargetFramework)
2. Microsoft.Extensions packages upgraded to 10.*
3. `dotnet build -c Release` succeeds with 0 errors on net10.0
4. `dotnet test -c Release --no-build` passes all 79 tests
