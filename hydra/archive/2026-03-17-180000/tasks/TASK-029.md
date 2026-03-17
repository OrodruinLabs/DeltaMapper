---
id: TASK-029
title: Full generator test coverage and compile verification
status: IMPLEMENTED
depends_on:
  - TASK-026
  - TASK-027
  - TASK-028
wave: 5
files_to_create:
  - tests/DeltaMapper.SourceGen.Tests/FlatMappingGeneratorTests.cs
  - tests/DeltaMapper.SourceGen.Tests/NestedMappingGeneratorTests.cs
  - tests/DeltaMapper.SourceGen.Tests/CollectionMappingGeneratorTests.cs
  - tests/DeltaMapper.SourceGen.Tests/IgnoreAttributeGeneratorTests.cs
  - tests/DeltaMapper.SourceGen.Tests/DiagnosticTests.cs
  - tests/DeltaMapper.SourceGen.Tests/CompileVerificationTests.cs
files_to_modify: []
acceptance_criteria:
  - All 6 Phase 3 test scenarios from the spec pass (flat pair, nested types, List/array, Ignore, DM001, compile-without-warnings)
  - "dotnet test" across the full solution passes with zero failures (existing 91 + new generator tests)
  - Generated code for each scenario compiles without warnings when verified via CSharpCompilation in tests
---

- **Status**: IMPLEMENTED

**Retry count**: 0/3

## Description

Consolidate and expand generator test coverage to fulfill all Phase 3 test requirements from docs/DELTAMAP_PLAN.md section 3.5.

### Required Test Scenarios

1. **FlatMappingGeneratorTests**: Generator emits correct file for simple flat type pair (3+ properties, same name/type). Verify file name, content structure, assignment statements.

2. **NestedMappingGeneratorTests**: Generator handles nested types — emits recursive `Map_X_To_Y` call. Verify the nested mapping method is also generated.

3. **CollectionMappingGeneratorTests**: Generator handles `List<T>` and `T[]` destination properties. Verify Select+ToList / Select+ToArray emission. Verify primitive collections use direct assignment.

4. **IgnoreAttributeGeneratorTests**: Generator respects `[Ignore]` (or `[DeltaMapperIgnore]`) attribute on destination property — property is not in generated code.

5. **DiagnosticTests**: Analyzer emits DM001 for unmapped property. DM002 for unresolvable type. Verify severity, id, and message content.

6. **CompileVerificationTests**: Generated code compiles without warnings. Take the generated source from each test scenario, add it to a CSharpCompilation along with the input source and DeltaMapper.Core references, and verify `EmitResult.Success == true` with zero warning diagnostics.

### Test Pattern

Each test uses `GeneratorTestHelper` (from TASK-023) to:
1. Provide input source text
2. Run the generator via `CSharpGeneratorDriver`
3. Assert on `GeneratorRunResult` — generated sources, diagnostics
4. Optionally compile the combined output to verify no warnings

## Pattern Reference

Follow test conventions from `tests/DeltaMapper.UnitTests/` — xUnit classes, FluentAssertions, descriptive method names.

## Test Requirements

All tests pass. Zero failures across the entire solution. This is the final verification gate for Phase 3.

## Traces To

docs/DELTAMAP_PLAN.md section 3.5 (Phase 3 Tests — all 6 scenarios)
