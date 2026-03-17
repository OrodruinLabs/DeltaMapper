---
id: TASK-017
title: DiffEngine, IMapper.Patch method, and basic Patch tests
status: PLANNED
depends_on: [TASK-015, TASK-016]
wave: 2
delegates_to: implementer
traces_to: "Phase 2 spec sections 2.2 (IMapper Extension), 2.3 (Diff Algorithm), 2.5 (Tests: single change, no changes, multiple changes)"
files_to_create:
  - src/DeltaMapper.Core/Diff/DiffEngine.cs
  - tests/DeltaMapper.UnitTests/PatchBasicTests.cs
files_to_modify:
  - src/DeltaMapper.Core/Abstractions/IMapper.cs
  - src/DeltaMapper.Core/Runtime/Mapper.cs
acceptance_criteria:
  - IMapper has Patch<TSource, TDestination>(TSource, TDestination) returning MappingDiff<TDestination>
  - Patch with one changed property returns exactly one PropertyChange with correct From/To/Kind
  - Patch with no changes returns empty Changes list and HasChanges == false
---

**Retry count**: 0/3

## Description
Implement the core diff algorithm and wire it into the mapper. The `Patch` method snapshots destination properties before mapping, runs the existing `Map(source, destination)`, then compares before/after to produce `PropertyChange` entries.

The `DiffEngine` is an internal static class that handles the snapshot-compare logic. In this task, only flat (primitive + string) property comparison is needed. Nested object recursion and collection diff are added in TASK-018 and TASK-019.

## Implementation Notes
- Add `Patch<TSource, TDestination>` to `IMapper` interface (after existing methods)
- Implement in `Mapper.cs`:
  1. Snapshot all destination property values (via reflection, cached PropertyInfo[])
  2. Call existing `Map<TSource, TDestination>(source, destination)`
  3. Snapshot again
  4. Call `DiffEngine.Compare(beforeSnapshot, afterSnapshot, destinationType)` to get changes
  5. Return `new MappingDiff<TDestination> { Result = destination, Changes = changes }`
- `DiffEngine` (internal static class in `DeltaMapper.Diff` namespace):
  - `Compare(Dictionary<string, object?> before, Dictionary<string, object?> after, Type destType)` -> `List<PropertyChange>`
  - For now: flat comparison only using `object.Equals(before, after)` — nested/collection handled in later tasks
  - Skip properties where both before and after are null
- Use `PropertyInfo.GetValue()` for snapshotting — performance optimization is out of scope for Phase 2
- Tests go in `PatchBasicTests.cs` following existing test file patterns (Fact, FluentAssertions, inner profile classes)

### Pattern Reference
- `src/DeltaMapper.Core/Runtime/Mapper.cs:25-29` — Map<TSource, TDestination> implementation pattern
- `tests/DeltaMapper.UnitTests/ExistingDestinationTests.cs:12-38` — test structure with inner profiles
