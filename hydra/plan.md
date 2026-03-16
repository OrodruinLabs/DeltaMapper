# Hydra Plan

## Objective
Implement DeltaMapper Phase 1 — Runtime Core: project setup, core interfaces, fluent profile API, MapperConfiguration with expression-compiled delegates and FrozenDictionary, Mapper runtime executor, MapperContext for circular reference detection, DI integration, record/init-only property support, and full unit test coverage.

## Discovery Status
- [x] Discovery run: 2026-03-16T21:30:00Z
- [x] Classification: greenfield
- [x] Reviewers generated: architect-reviewer, code-reviewer, security-reviewer, type-reviewer
- [x] Context collected: greenfield-scope
- [x] Documents generated: TRD, test-plan

## Status Summary
<!-- Auto-updated by agents after each state transition.
     The stop hook and session-context hook read this section for quick state assessment.
     Every agent that changes task state MUST update these counters. -->
- Total tasks: 14
- DONE: 0 | READY: 3 | IN_PROGRESS: 0 | IN_REVIEW: 0 | CHANGES_REQUESTED: 0 | BLOCKED: 0 | PLANNED: 11
- Current iteration: 0/40
- Active task: none

## Wave Groups

### Wave 1
- TASK-001, TASK-002, TASK-003 (independent, no file overlap — foundational setup)

### Wave 2
- TASK-004, TASK-005, TASK-006 (depend on TASK-001, no file overlap with each other)

### Wave 3
- TASK-007 (depends on TASK-004, TASK-005, TASK-006 — compilation engine needs all interfaces)

### Wave 4
- TASK-008, TASK-009 (depend on TASK-007, no file overlap — Mapper executor + fluent API tests)

### Wave 5
- TASK-010, TASK-011, TASK-012 (depend on TASK-008, no file overlap — advanced mapping features + tests)

### Wave 6
- TASK-013, TASK-014 (depend on TASK-008, no file overlap — DI integration + remaining test coverage)

## Parallel Groups

### Group 1
- [ ] TASK-001: Solution and Project Files Setup (files: DeltaMapper.sln, src/DeltaMapper.Core/DeltaMapper.Core.csproj, tests/DeltaMapper.UnitTests/DeltaMapper.UnitTests.csproj) -> READY
- [ ] TASK-002: DeltaMapperException (files: src/DeltaMapper.Core/Exceptions/DeltaMapperException.cs, tests/DeltaMapper.UnitTests/ErrorHandlingTests.cs) -> READY
- [ ] TASK-003: Shared Test Models (files: tests/DeltaMapper.UnitTests/TestModels/*.cs) -> READY

### Group 2
- [ ] TASK-004: IMapper Interface and MapperContext (files: src/DeltaMapper.Core/IMapper.cs, src/DeltaMapper.Core/MapperContext.cs, tests/DeltaMapper.UnitTests/MapperContextTests.cs) -> PLANNED
- [ ] TASK-005: MappingProfile and MappingExpression Fluent API (files: src/DeltaMapper.Core/MappingProfile.cs, src/DeltaMapper.Core/MappingExpression.cs, tests/DeltaMapper.UnitTests/MappingProfileTests.cs) -> PLANNED
- [ ] TASK-006: Middleware Pipeline Stubs (files: src/DeltaMapper.Core/Middleware/IMappingMiddleware.cs, src/DeltaMapper.Core/Middleware/MappingPipeline.cs, tests/DeltaMapper.UnitTests/MiddlewarePipelineTests.cs) -> PLANNED

### Group 3
- [ ] TASK-007: MapperConfiguration and MapperConfigurationBuilder (files: src/DeltaMapper.Core/MapperConfiguration.cs, src/DeltaMapper.Core/MapperConfigurationBuilder.cs, tests/DeltaMapper.UnitTests/MapperConfigurationTests.cs) -> PLANNED

### Group 4
- [ ] TASK-008: Mapper Runtime Executor and Convention Mapping Tests (files: src/DeltaMapper.Core/Mapper.cs, tests/DeltaMapper.UnitTests/ConventionMappingTests.cs) -> PLANNED
- [ ] TASK-009: ForMember Resolvers, Hooks, and ReverseMap Tests (files: tests/DeltaMapper.UnitTests/ForMemberTests.cs, tests/DeltaMapper.UnitTests/MappingHooksTests.cs, tests/DeltaMapper.UnitTests/ReverseMapTests.cs) -> PLANNED

### Group 5
- [ ] TASK-010: Record and Init-Only Property Support (files: src/DeltaMapper.Core/MapperConfiguration.cs, tests/DeltaMapper.UnitTests/RecordMappingTests.cs) -> PLANNED
- [ ] TASK-011: Collection Mapping and Nested Object Mapping Tests (files: tests/DeltaMapper.UnitTests/CollectionMappingTests.cs, tests/DeltaMapper.UnitTests/NestedMappingTests.cs) -> PLANNED
- [ ] TASK-012: Circular Reference Detection Tests (files: tests/DeltaMapper.UnitTests/CircularReferenceTests.cs) -> PLANNED

### Group 6
- [ ] TASK-013: DI Integration (files: src/DeltaMapper.Core/ServiceCollectionExtensions.cs, tests/DeltaMapper.UnitTests/DependencyInjectionTests.cs) -> PLANNED
- [ ] TASK-014: Non-Generic Map, Existing Destination, Error Handling Tests (files: tests/DeltaMapper.UnitTests/NonGenericMapTests.cs, tests/DeltaMapper.UnitTests/ExistingDestinationTests.cs, tests/DeltaMapper.UnitTests/ErrorHandlingTests.cs) -> PLANNED

## Tasks

### TASK-001: Solution and Project Files Setup
- **Status**: READY
- **Group**: 1
- **Depends on**: none
- **Manifest**: hydra/tasks/TASK-001.md

### TASK-002: DeltaMapperException Custom Exception
- **Status**: READY
- **Group**: 1
- **Depends on**: none
- **Manifest**: hydra/tasks/TASK-002.md

### TASK-003: Shared Test Models
- **Status**: READY
- **Group**: 1
- **Depends on**: none
- **Manifest**: hydra/tasks/TASK-003.md

### TASK-004: IMapper Interface and MapperContext
- **Status**: PLANNED
- **Group**: 2
- **Depends on**: TASK-001
- **Manifest**: hydra/tasks/TASK-004.md

### TASK-005: MappingProfile and MappingExpression Fluent API
- **Status**: PLANNED
- **Group**: 2
- **Depends on**: TASK-001
- **Manifest**: hydra/tasks/TASK-005.md

### TASK-006: Middleware Pipeline Stubs
- **Status**: PLANNED
- **Group**: 2
- **Depends on**: TASK-001
- **Manifest**: hydra/tasks/TASK-006.md

### TASK-007: MapperConfiguration and MapperConfigurationBuilder
- **Status**: PLANNED
- **Group**: 3
- **Depends on**: TASK-004, TASK-005, TASK-006
- **Manifest**: hydra/tasks/TASK-007.md

### TASK-008: Mapper Runtime Executor and Convention Mapping Tests
- **Status**: PLANNED
- **Group**: 4
- **Depends on**: TASK-007
- **Manifest**: hydra/tasks/TASK-008.md

### TASK-009: ForMember Resolvers, Hooks, and ReverseMap Tests
- **Status**: PLANNED
- **Group**: 4
- **Depends on**: TASK-007
- **Manifest**: hydra/tasks/TASK-009.md

### TASK-010: Record and Init-Only Property Support
- **Status**: PLANNED
- **Group**: 5
- **Depends on**: TASK-008
- **Manifest**: hydra/tasks/TASK-010.md

### TASK-011: Collection Mapping and Nested Object Mapping Tests
- **Status**: PLANNED
- **Group**: 5
- **Depends on**: TASK-008
- **Manifest**: hydra/tasks/TASK-011.md

### TASK-012: Circular Reference Detection Tests
- **Status**: PLANNED
- **Group**: 5
- **Depends on**: TASK-008
- **Manifest**: hydra/tasks/TASK-012.md

### TASK-013: DI Integration (ServiceCollectionExtensions)
- **Status**: PLANNED
- **Group**: 6
- **Depends on**: TASK-008
- **Manifest**: hydra/tasks/TASK-013.md

### TASK-014: Non-Generic Map, Existing Destination, Error Handling Tests
- **Status**: PLANNED
- **Group**: 6
- **Depends on**: TASK-008
- **Manifest**: hydra/tasks/TASK-014.md

## Completed
<!-- Tasks moved here after ALL reviewers approve and status is set to DONE. -->

## Blocked
<!-- Tasks that hit max retries, have unresolvable issues, or require human intervention. -->

## Recovery Pointer
- **Current Task:** none
- **Last Action:** Planner agent completed task decomposition -- 14 tasks across 6 waves
- **Next Action:** Implementer to pick up Wave 1 tasks (TASK-001, TASK-002, TASK-003) in parallel
- **Last Checkpoint:** hydra/checkpoints/iteration-002.json
- **Last Commit:** unknown no commits yet
