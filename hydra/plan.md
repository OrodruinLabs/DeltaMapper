# Hydra Plan

## Objective
Implement MappingDiff<T> and Patch — Phase 2: PropertyChange record, MappingDiff<T> type, IMapper.Patch<TSource, TDestination>() method, snapshot-compare diff algorithm, nested object dot-notation paths, collection diff (add/remove/modify), and full test coverage.

## Status Summary
- Total tasks: 6
- DONE: 0 | READY: 2 | PLANNED: 4
- Current iteration: 0/40
- Active task: TASK-015

## Wave Groups

### Wave 1 — Core Types and Test Models
- [ ] TASK-015, TASK-016

### Wave 2 — Diff Engine, IMapper.Patch, and Basic Tests
- [ ] TASK-017

### Wave 3 — Nested Objects and Collection Diff
- [ ] TASK-018, TASK-019

### Wave 4 — Edge Cases, NullSubstitute, JSON Serialization
- [ ] TASK-020

## Ready
- [ ] TASK-015: ChangeKind enum, PropertyChange record, MappingDiff<T> -> READY
- [ ] TASK-016: Phase 2 test models for diff scenarios -> READY

## Planned
- [ ] TASK-017: DiffEngine, IMapper.Patch method, and basic Patch tests -> PLANNED
- [ ] TASK-018: Nested object diff with dot-notation paths -> PLANNED
- [ ] TASK-019: Collection diff (add/remove/modify by index) -> PLANNED
- [ ] TASK-020: NullSubstitute Patch integration, JSON serialization, edge cases -> PLANNED

## Blocked
(none)

## Completed
(none)

## Recovery Pointer
- **Current Task:** TASK-015
- **Last Action:** Plan created
- **Next Action:** Begin implementation of Wave 1 (TASK-015, TASK-016 in parallel)
- **Last Checkpoint:** none
- **Last Commit:** none
