# Hydra Plan

## Objective
Implement Phase 3 — Roslyn Source Generator: [GenerateMap] attribute, IIncrementalGenerator that emits assignment code at build time, GeneratedMapRegistry for runtime fallback integration, DM001/DM002/DM003 analyzer diagnostics, and generator test coverage.

## Status Summary
- Total tasks: 9
- DONE: 0 | READY: 9
- Current iteration: 0/40
- Active task: none — starting fresh

## Wave Groups

### Wave 1 — Project Scaffolding (parallel, no file overlap)
- [ ] TASK-021: SourceGen project scaffold and solution registration
- [ ] TASK-022: GeneratedMapRegistry in DeltaMapper.Core
- [ ] TASK-023: SourceGen test project scaffold

### Wave 2 — Attribute and Core Generator (depends on Wave 1)
- [x] TASK-024: GenerateMapAttribute emitted as source text (depends: TASK-021)
- [x] TASK-025: IIncrementalGenerator core — flat type pair emission (depends: TASK-021, TASK-024)

### Wave 3 — Advanced Generation (depends on Wave 2)
- [x] TASK-026: Generator support for nested types, collections, and Ignore (depends: TASK-025)
- [x] TASK-027: ModuleInitializer registration and MapperConfiguration fallback (depends: TASK-022, TASK-025)

### Wave 4 — Analyzer Diagnostics (depends on Wave 2)
- [x] TASK-028: DM001/DM002/DM003 analyzer diagnostics (depends: TASK-025)

### Wave 5 — Test Coverage Gate (depends on Waves 3-4)
- [ ] TASK-029: Full generator test coverage and compile verification (depends: TASK-026, TASK-027, TASK-028)

## Task Details

| Task | Title | Wave | Depends On | Files | Status |
|------|-------|------|------------|-------|--------|
| TASK-021 | SourceGen project scaffold | 1 | — | DeltaMapper.SourceGen.csproj, DeltaMapper.slnx | READY |
| TASK-022 | GeneratedMapRegistry in Core | 1 | — | GeneratedMapRegistry.cs, MapperConfiguration.cs, MapperConfigurationBuilder.cs | READY |
| TASK-023 | SourceGen test project scaffold | 1 | — | DeltaMapper.SourceGen.Tests.csproj, GeneratorTestHelper.cs | IMPLEMENTED |
| TASK-024 | GenerateMapAttribute source text | 2 | TASK-021 | GenerateMapAttributeSource.cs | IMPLEMENTED |
| TASK-025 | IIncrementalGenerator core (flat) | 2 | TASK-021, TASK-024 | MapperGenerator.cs, EmitHelper.cs | IMPLEMENTED |
| TASK-026 | Nested types, collections, Ignore | 3 | TASK-025 | EmitHelper.cs, MapperGenerator.cs | IMPLEMENTED |
| TASK-027 | ModuleInitializer + registry wiring | 3 | TASK-022, TASK-025 | MapperGenerator.cs, EmitHelper.cs | IMPLEMENTED |
| TASK-028 | DM001/DM002/DM003 diagnostics | 4 | TASK-025 | DiagnosticDescriptors.cs, MappingAnalyzer.cs, MapperGenerator.cs | IMPLEMENTED |
| TASK-029 | Full test coverage gate | 5 | TASK-026, TASK-027, TASK-028 | 6 test files | READY |

## Blocked
(none)

## Recovery Pointer
- **Current Task:** TASK-028
- **Last Action:** TASK-028 set to IMPLEMENTED — DM001/DM002 diagnostics + 9 tests, all 136 tests pass
- **Next Action:** TASK-029 is now READY (depends on TASK-026, TASK-027, TASK-028 — all IMPLEMENTED)
- **Last Checkpoint:** hydra/checkpoints/iteration-008.json
- **Last Commit:** f46d093 feat(FEAT-004): Wave 3 — nested/collection generation + ModuleInitializer
