# Hydra Plan

## Objective
Implement MappingDiff<T> and Patch — Phase 2: PropertyChange record, MappingDiff<T> type, IMapper.Patch<TSource, TDestination>() method, snapshot-compare diff algorithm, nested object dot-notation paths, collection diff (add/remove/modify), and full test coverage.

## Status Summary
- Total tasks: 6
- DONE: 2 | READY: 1 | PLANNED: 3
- Current iteration: 1/40
- Active task: TASK-017

## Wave Groups

### Wave 1 — Core Types and Test Models — COMPLETE
- [x] TASK-015, TASK-016

### Wave 2 — Diff Engine, IMapper.Patch, and Basic Tests
- [ ] TASK-017

### Wave 3 — Nested Objects and Collection Diff
- [ ] TASK-018, TASK-019

### Wave 4 — Edge Cases, NullSubstitute, JSON Serialization
- [ ] TASK-020

## Ready
- [ ] TASK-017: DiffEngine, IMapper.Patch method, and basic Patch tests -> READY

## Planned
- [ ] TASK-018: Nested object diff with dot-notation paths -> PLANNED
- [ ] TASK-019: Collection diff (add/remove/modify by index) -> PLANNED
- [ ] TASK-020: NullSubstitute Patch integration, JSON serialization, edge cases -> PLANNED

## Completed
- [x] TASK-015: ChangeKind enum, PropertyChange record, MappingDiff<T> -> DONE
- [x] TASK-016: Phase 2 test models for diff scenarios -> DONE

## Blocked
(none)

## Recovery Pointer
- **Current Task:** TASK-017
- **Last Action:** Wave 1 complete (TASK-015, TASK-016 DONE)
- **Next Action:** Implement TASK-017 — DiffEngine + IMapper.Patch
- **Last Checkpoint:** none
- **Last Commit:** (pending Wave 1 commit)
