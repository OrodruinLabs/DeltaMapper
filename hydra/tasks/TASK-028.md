---
id: TASK-028
title: DM001/DM002/DM003 analyzer diagnostics
status: READY
depends_on:
  - TASK-025
wave: 4
files_to_create:
  - src/DeltaMapper.SourceGen/Diagnostics/DiagnosticDescriptors.cs
  - src/DeltaMapper.SourceGen/Diagnostics/MappingAnalyzer.cs
files_to_modify:
  - src/DeltaMapper.SourceGen/MapperGenerator.cs
acceptance_criteria:
  - DM001 (Warning) is reported when a destination property has no matching source property and is not ignored
  - DM002 (Error) is reported when a source or destination type referenced in [GenerateMap] cannot be found/resolved
  - Generator test verifies DM001 diagnostic with correct severity, id, message, and location pointing to the [GenerateMap] attribute or the unmatched property
---

- **Status**: READY

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

This is harder to detect at generator time (requires whole-program analysis). For this task, implement DM003 as a best-effort check: if a `[GenerateMap]` is declared but the profile class is never referenced in any `AddProfile<>()` call in the compilation. If this is too complex, document it as deferred and implement DM001 + DM002 only.

### Integration with Generator

The diagnostics are reported via `SourceProductionContext.ReportDiagnostic()` during the generator's source output step. DM001 is emitted alongside the generated code (the mapping still generates, but with a warning). DM002 prevents code generation for that specific attribute.

## Pattern Reference

Standard Roslyn `DiagnosticDescriptor` pattern. Place descriptors in a static class for reuse.

## Test Requirements

1. Test DM001: destination has property `Foo` not in source — verify warning diagnostic reported with correct id and message
2. Test DM002: `[GenerateMap(typeof(DoesNotExist), typeof(Dto))]` — verify error diagnostic
3. Test that DM001 is NOT reported for properties with `[Ignore]` attribute
4. All diagnostics have correct severity (Warning vs Error as specified)

## Traces To

docs/DELTAMAP_PLAN.md section 3.4 (Analyzer Rules DM001, DM002, DM003)
