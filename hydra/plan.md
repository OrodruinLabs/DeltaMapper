# Hydra Plan

## Objective
Implement MappingDiff<T> and Patch — Phase 2: PropertyChange record, MappingDiff<T> type, IMapper.Patch<TSource, TDestination>() method, snapshot-compare diff algorithm, nested object dot-notation paths, collection diff (add/remove/modify), and full test coverage.

## Status Summary
- Total tasks: 6
- DONE: 6 | READY: 0
- Current iteration: 4/40
- Active task: none — ALL TASKS COMPLETE

## Wave Groups

### Wave 1 — Core Types and Test Models — COMPLETE
- [x] TASK-015, TASK-016

### Wave 2 — Diff Engine, IMapper.Patch, and Basic Tests — COMPLETE
- [x] TASK-017

### Wave 3 — Nested Objects and Collection Diff — COMPLETE
- [x] TASK-018, TASK-019

### Wave 4 — Edge Cases, NullSubstitute, JSON Serialization — COMPLETE
- [x] TASK-020

## Completed
- [x] TASK-015: ChangeKind enum, PropertyChange record, MappingDiff<T> -> DONE
- [x] TASK-016: Phase 2 test models for diff scenarios -> DONE
- [x] TASK-017: DiffEngine, IMapper.Patch method, and basic Patch tests -> DONE (82 tests)
- [x] TASK-018: Nested object diff with dot-notation paths -> DONE (85 tests)
- [x] TASK-019: Collection diff (add/remove/modify by index) -> DONE (88 tests)
- [x] TASK-020: NullSubstitute Patch, JSON serialization, edge cases -> DONE (91 tests)

## Blocked
(none)

## Recovery Pointer
- **Current Task:** POST-LOOP
- **Last Action:** All 6 tasks DONE, 91 tests passing
- **Next Action:** Post-loop agents (documentation, release-manager), then PR
- **Last Checkpoint:** none
- **Last Commit:** (pending Wave 4 commit)
