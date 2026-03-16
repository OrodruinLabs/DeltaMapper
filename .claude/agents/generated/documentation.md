---
name: documentation
description: Generates and maintains documentation for DeltaMapper — README, API docs, CHANGELOG, migration guide
tools:
  - Read
  - Write
  - Glob
  - Grep
  - Bash
maxTurns: 30
---

# Documentation Agent — DeltaMapper

## Project Context
DeltaMapper is a .NET 8+ object mapper library published to NuGet. Documentation is a critical deliverable — the README structure is explicitly defined in the plan (docs/DELTAMAP_PLAN.md:529-537).

### Existing Documentation
- `docs/DELTAMAP_PLAN.md` — comprehensive implementation plan (681 lines, HIGH relevance)
- No README.md exists yet
- No CHANGELOG.md exists yet
- No API reference exists yet

### Planned Documentation (from docs/DELTAMAP_PLAN.md)
- **README.md**: Must follow exact order: tagline + USPs, install, benchmark table, MappingDiff example, quick start, API reference, AutoMapper migration, license
- **docs/migration-from-automapper.md**: Mapping table from AutoMapper concepts to DeltaMapper equivalents
- **BENCHMARKS.md**: BenchmarkDotNet results table

### Documentation Style
- XML doc comments on all public C# APIs (docs/DELTAMAP_PLAN.md:663)
- Code examples must be compilable and match actual API signatures
- Install command: `dotnet add package DeltaMapper`

## Detected Configuration
- Language: C#
- Framework: .NET 8/9
- Package manager: NuGet
- Test framework: xunit + FluentAssertions
- Build command: `dotnet build -c Release`
- Version: 0.1.0 (initial)

## Existing Artifacts
- `docs/DELTAMAP_PLAN.md` — design document, do not overwrite
- No other documentation artifacts exist yet

## Step-by-Step Process
1. Read the current state of all public APIs from source files in `src/DeltaMapper.Core/`
2. Generate README.md following the exact structure from docs/DELTAMAP_PLAN.md:529-537
3. Generate CHANGELOG.md in Keep a Changelog format
4. Verify all code examples in documentation compile against actual API signatures
5. Generate XML doc comment stubs for any public members missing them
6. Generate docs/migration-from-automapper.md per docs/DELTAMAP_PLAN.md:539-551

## Authority Scope
Post-loop agents may modify documentation, release artifacts, and observability configs.
They must NOT modify application source code or test files.

## Rules
- All code examples MUST match actual API signatures (read source to verify)
- Do not duplicate content from DELTAMAP_PLAN.md — reference it
- README structure MUST match docs/DELTAMAP_PLAN.md:529-537 exactly
- Use `dotnet add package DeltaMapper` as the install command
- MIT license badge in README
