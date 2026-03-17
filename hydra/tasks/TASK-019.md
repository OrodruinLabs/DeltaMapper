---
id: TASK-019
title: Collection diff (add/remove/modify by index)
status: READY
depends_on: [TASK-017]
wave: 3
delegates_to: implementer
traces_to: "Phase 2 spec section 2.3 (Deep diff rules - collections), 2.5 (Tests: collection add/remove/modify)"
files_to_create:
  - tests/DeltaMapper.UnitTests/PatchCollectionTests.cs
files_to_modify:
  - src/DeltaMapper.Core/Diff/DiffEngine.cs
acceptance_criteria:
  - Collection items added beyond original length emit PropertyChange with Kind=Added and path "Items[N]"
  - Collection items removed (shorter after) emit PropertyChange with Kind=Removed and path "Items[N]"
  - Collection items modified at same index emit PropertyChange with Kind=Modified and correct From/To
---

**Retry count**: 0/3

## Description
Extend `DiffEngine` to handle `IList` and `ICollection<T>` properties by comparing elements by index. When collections differ in length, extra items are Added or Removed.

## Implementation Notes
- In `DiffEngine`, when comparing a property value:
  - If the type implements `IList` (or `IList<T>`) → use index-based comparison
  - Compare element-by-element up to `min(before.Count, after.Count)`:
    - If elements are simple types → direct Equals, path = `"PropertyName[i]"`
    - If elements are complex types → recurse into element, path = `"PropertyName[i].SubProp"`
  - For indices `min..max(before.Count, after.Count)`:
    - Extra in after → `Kind = Added`, path = `"PropertyName[i]"`
    - Extra in before → `Kind = Removed`, path = `"PropertyName[i]"`
- Handle null collections (treat as empty)
- Tests use `Team`/`TeamDto` models with `List<Player>`/`List<PlayerDto>`:
  - Test: item added (after list is longer)
  - Test: item removed (after list is shorter)
  - Test: item modified (same index, different values)

### Pattern Reference
- `src/DeltaMapper.Core/Diff/DiffEngine.cs` — extending from TASK-017/TASK-018
- `tests/DeltaMapper.UnitTests/CollectionMappingTests.cs` — collection test patterns
