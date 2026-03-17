# Plan — Phase 5: Benchmarks, Docs, Community (FEAT-006)

─── ◈ HYDRA ▸ PLANNING ─────────────────────────────

## Objective

Implement BenchmarkDotNet suite comparing DeltaMapper (runtime), DeltaMapper (source-gen), Mapperly, AutoMapper, and hand-written code across flat/nested/collection/patch scenarios. Publish BENCHMARKS.md with placeholder table structure. Polish AutoMapper migration guide and finalize README.

## Status Summary

| Status | Count |
|--------|-------|
| PLANNED | 2 |
| IN_PROGRESS | 0 |
| IMPLEMENTED | 2 |
| DONE | 0 |
| BLOCKED | 0 |
| TOTAL | 4 |

## Recovery Pointer

**Next**: TASK-036 — Benchmark scenarios (Wave 2, remaining) or TASK-038 after TASK-037 review
**State**: TASK-037 IMPLEMENTED — BENCHMARKS.md placeholder document complete
**Last updated**: 2026-03-17T21:10:00Z

## Tasks

| ID | Title | Status | Wave | Depends On |
|----|-------|--------|------|------------|
| TASK-035 | Benchmark project scaffold + shared models | IMPLEMENTED | 1 | -- |
| TASK-036 | Benchmark scenarios (flat/nested/collection/patch) | PLANNED | 2 | TASK-035 |
| TASK-037 | BENCHMARKS.md placeholder document | IMPLEMENTED | 2 | TASK-035 |
| TASK-038 | README final polish + migration guide update | PLANNED | 3 | TASK-037 |

## Wave Groups

### Wave 1
- TASK-035 (benchmark project foundation — no dependencies)

### Wave 2
- TASK-036, TASK-037 (independent: TASK-036 writes benchmark .cs files, TASK-037 writes BENCHMARKS.md — no file overlap)

### Wave 3
- TASK-038 (depends on TASK-037 for benchmark table content to reference in README)

## Design Notes

- Benchmark project is a console app (`<OutputType>Exe</OutputType>`) not a test project
- Uses `[MemoryDiagnoser]` on all benchmark classes — allocations are a primary metric
- Competitors (AutoMapper, Mapperly) are PackageReferences in the benchmark project only
- Source-gen benchmarks use `[GenerateMap]` attribute from DeltaMapper.SourceGen
- BENCHMARKS.md contains placeholder `<pending>` values — actual numbers require CI/local run
- Migration guide at `docs/migration-from-automapper.md` already exists and is comprehensive — TASK-038 does light polish only
- README already has good structure — TASK-038 adds benchmark result table inline and updates roadmap status
