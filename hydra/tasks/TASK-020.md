---
id: TASK-020
title: NullSubstitute Patch integration, JSON serialization, edge cases
status: READY
depends_on: [TASK-018, TASK-019]
wave: 4
delegates_to: implementer
traces_to: "Phase 2 spec section 2.5 (Tests: NullSubstitute, JSON serialization)"
files_to_create:
  - tests/DeltaMapper.UnitTests/PatchEdgeCaseTests.cs
files_to_modify: []
acceptance_criteria:
  - Patch with null source property + NullSubstitute produces PropertyChange with correct substituted To value
  - MappingDiff<T> serializes to JSON with System.Text.Json without errors and includes Result + Changes
  - Patch with all properties ignored returns HasChanges == false
---

**Retry count**: 0/3

## Description
Test edge cases for the Patch method: NullSubstitute interaction (where source property is null but NullSubstitute provides a default), JSON serialization of MappingDiff<T>, and scenarios where no properties change due to Ignore() configuration.

## Implementation Notes
- This task is test-only — no production code changes expected (all infrastructure is built in TASK-015 through TASK-019)
- If any minor adjustments to DiffEngine are needed for edge cases, make them here
- Tests:
  1. **NullSubstitute**: Configure `ForMember(d => d.Nickname, o => { o.NullSubstitute("N/A"); })`, source has null Nickname → PropertyChange should show From=null (or previous), To="N/A"
  2. **JSON serialization**: `JsonSerializer.Serialize(diff)` produces valid JSON, deserialize back and verify Changes count
  3. **All ignored**: Map where all destination properties are `.Ignore()` → empty Changes, HasChanges == false
- Uses `ProductWithNullable`/`ProductWithNullableDto` from DiffModels for NullSubstitute test
- Use `System.Text.Json.JsonSerializer` (already available in .NET 10)

### Pattern Reference
- `tests/DeltaMapper.UnitTests/ForMemberTests.cs` — NullSubstitute test patterns
- `tests/DeltaMapper.UnitTests/ExistingDestinationTests.cs` — profile-per-test pattern
