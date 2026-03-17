---
id: TASK-028
title: DM001/DM002/DM003 analyzer diagnostics
status: IMPLEMENTED
depends_on:
  - TASK-025
wave: 4
files_to_create:
  - src/DeltaMapper.SourceGen/Diagnostics/DiagnosticDescriptors.cs
  - src/DeltaMapper.SourceGen/Diagnostics/MappingAnalyzer.cs
  - tests/DeltaMapper.SourceGen.Tests/AnalyzerDiagnosticTests.cs
files_to_modify:
  - src/DeltaMapper.SourceGen/MapperGenerator.cs
  - src/DeltaMapper.SourceGen/DeltaMapper.SourceGen.csproj
acceptance_criteria:
  - DM001 (Warning) is reported when a destination property has no matching source property and is not ignored
  - DM002 (Error) is reported when a source or destination type referenced in [GenerateMap] cannot be found/resolved
  - Generator test verifies DM001 diagnostic with correct severity, id, message, and location pointing to the [GenerateMap] attribute or the unmatched property
---

- **Status**: IMPLEMENTED

**Retry count**: 0/3

## Description

Implement analyzer diagnostics as part of the source generator pipeline.

### DM001 — Unmapped Destination Property (Warning)

Reported when a destination writable property has no matching source property and no `[Ignore]` attribute.

```
DM001: Destination property 'FullName' on 'UserDto' has no matching source property on 'User'. Add a manual mapping or mark it with [Ignore].
```

Location: the `[GenerateMap]` attribute syntax node.

### DM002 — Type Not Found (Error)

Reported when the `Type` argument in `[GenerateMap(typeof(Foo), typeof(Bar))]` cannot be resolved to an `INamedTypeSymbol`.

```
DM002: Source type 'NonExistentType' could not be found. Verify the type exists and is accessible.
```

Location: the typeof expression in the attribute.

### DM003 — Mapping Never Used (Warning)

Deferred. Whole-program analysis is required to detect whether a profile class is referenced
in any `AddProfile<>()` call. This was assessed as too complex for source-generator time and
is documented as a future enhancement.

### Integration with Generator

The diagnostics are reported via `SourceProductionContext.ReportDiagnostic()` during the generator's
source output step. DM001 is emitted alongside the generated code (the mapping still generates, but
with a warning). DM002 prevents code generation for that specific attribute.

## Implementation Log

- Created `src/DeltaMapper.SourceGen/Diagnostics/DiagnosticDescriptors.cs` — static class with
  `DiagnosticDescriptor` constants for DM001 and DM002.
- Created `src/DeltaMapper.SourceGen/Diagnostics/MappingAnalyzer.cs` — static helper with
  `ReportUnmappedProperties` (DM001) and `ResolveAndValidateTypes` (DM002). Error type symbols
  (`TypeKind.Error`) are treated as unresolvable and trigger DM002.
- Modified `src/DeltaMapper.SourceGen/MapperGenerator.cs` — `MappingInfo` now carries raw
  `AttributeData` + `Location` pairs. `Execute` resolves types, reports DM002 for invalid types,
  reports DM001 for valid pairs, then emits code only for valid pairs.
- Added `NoWarn RS2008` to project file to suppress release-tracking advisory warning.
- Created `tests/DeltaMapper.SourceGen.Tests/AnalyzerDiagnosticTests.cs` — 9 tests covering:
  DM001 fires for unmatched property, DM001 fires for each unmatched property, DM001 not fired
  when all match, DM001 not fired for [Ignore]-attributed properties, DM001 location is valid,
  DM002 fires for unresolvable source type (error type), DM002 or CS0246 for unresolvable dest,
  DM002 prevents code gen, no diagnostics for perfect mapping.
- All 136 tests pass (95 unit + 41 source gen).

## Pattern Reference

Standard Roslyn `DiagnosticDescriptor` pattern. Place descriptors in a static class for reuse.

## Test Requirements

1. Test DM001: destination has property `Foo` not in source — verify warning diagnostic reported with correct id and message ✦
2. Test DM002: `[GenerateMap(typeof(DoesNotExist), typeof(Dto))]` — verify error diagnostic ✦
3. Test that DM001 is NOT reported for properties with `[Ignore]` attribute ✦
4. All diagnostics have correct severity (Warning vs Error as specified) ✦

## Traces To

docs/DELTAMAP_PLAN.md section 3.4 (Analyzer Rules DM001, DM002, DM003)
