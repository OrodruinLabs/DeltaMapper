# TASK-037: BENCHMARKS.md placeholder document

## Metadata
- **Status**: PLANNED
- **Wave**: 2
- **Depends on**: TASK-035
- **Delegates to**: implementer
- **Traces to**: Phase 5.1 (docs/DELTAMAP_PLAN.md:530-535)
- **Files modified**: BENCHMARKS.md
- **Retry count**: 0/3
- **Review evidence**: hydra/reviews/TASK-037-review.md

## Description

Create `BENCHMARKS.md` in the repo root with a structured placeholder benchmark results document. Include four tables (one per scenario: flat, nested, collection, patch) with columns for Method, Mean, Error, StdDev, Allocated. All numeric cells contain `<pending>` placeholders. Add instructions for running benchmarks locally, environment info template, and methodology notes.

## File Scope

### Creates
- `BENCHMARKS.md`

### Modifies
(none)

## Pattern Reference

- `README.md` (lines 46-48) — existing link to BENCHMARKS.md and description of what it should contain

## Acceptance Criteria

1. `BENCHMARKS.md` exists at repo root and contains four named benchmark tables: Flat Object, Nested Object, Collection, and Patch
2. Each table has rows for all five competitors (DeltaMapper Runtime, DeltaMapper SourceGen, Mapperly, AutoMapper, Hand-written) with `<pending>` in numeric columns
3. Document includes a "Run locally" section with the exact `dotnet run` command to execute benchmarks

## Test Requirements

No code tests — documentation file. Acceptance verified by file existence and content structure.

## Implementation Notes

- Tables follow BenchmarkDotNet default output format: Method | Mean | Error | StdDev | Gen0 | Allocated
- Include environment template: OS, CPU, .NET SDK version, BenchmarkDotNet version
- Add note: "Results below are placeholders. Run the benchmark suite locally or in CI to populate actual numbers."
- Mention `--filter` flag for running individual scenarios
- Link back to README.md benchmarks section
