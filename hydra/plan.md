# Hydra Plan

## Objective
Implement Phase 3 — Roslyn Source Generator: [GenerateMap] attribute, IIncrementalGenerator that emits assignment code at build time, GeneratedMapRegistry for runtime fallback integration, DM001/DM002/DM003 analyzer diagnostics, and generator test coverage.

## Status Summary
- Total tasks: 9
- IMPLEMENTED: 9
- Current iteration: 10/40
- Active task: none — ALL TASKS IMPLEMENTED, reviews complete

## Wave Groups

### Wave 1 — Project Scaffolding — COMPLETE
- [x] TASK-021: SourceGen project scaffold and solution registration
- [x] TASK-022: GeneratedMapRegistry in DeltaMapper.Core
- [x] TASK-023: SourceGen test project scaffold

### Wave 2 — Attribute and Core Generator — COMPLETE
- [x] TASK-024: GenerateMapAttribute emitted as source text
- [x] TASK-025: IIncrementalGenerator core — flat type pair emission

### Wave 3 — Advanced Generation — COMPLETE
- [x] TASK-026: Generator support for nested types, collections, and Ignore
- [x] TASK-027: ModuleInitializer registration and MapperConfiguration fallback

### Wave 4 — Analyzer Diagnostics — COMPLETE
- [x] TASK-028: DM001/DM002 analyzer diagnostics

### Wave 5 — Test Coverage Gate — COMPLETE
- [x] TASK-029: Full generator test coverage and compile verification

## Completed
- [x] TASK-021 -> IMPLEMENTED (95 tests)
- [x] TASK-022 -> IMPLEMENTED (95 tests)
- [x] TASK-023 -> IMPLEMENTED (95 tests)
- [x] TASK-024 -> IMPLEMENTED (96 tests)
- [x] TASK-025 -> IMPLEMENTED (106 tests)
- [x] TASK-026 -> IMPLEMENTED (127 tests)
- [x] TASK-027 -> IMPLEMENTED (127 tests)
- [x] TASK-028 -> IMPLEMENTED (136 tests)
- [x] TASK-029 -> IMPLEMENTED (136 tests)

## Blocked
(none)

## Recovery Pointer
- **Current Task:** none — HYDRA_COMPLETE
- **Last Action:** Post-loop complete — README, CHANGELOG, NuGet v0.3.0-alpha.1, release notes
- **Next Action:** none — objective complete
- **Last Checkpoint:** hydra/checkpoints/iteration-010.json
- **Last Commit:** 0dd0431 Post-loop: README, CHANGELOG, NuGet v0.3.0-alpha.1, release notes (FEAT-004)
