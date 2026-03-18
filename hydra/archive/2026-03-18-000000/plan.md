# Plan — Performance Optimization (FEAT-008)

## Objective

Source-gen fast path, lazy MapperContext, skip pipeline closure, compiled expression delegates. Target: SourceGen <15ns, Runtime <50ns for flat objects.

## Status Summary

| Status | Count |
|--------|-------|
| READY  | 5     |
| DONE   | 0     |
| TOTAL  | 5     |

## Recovery Pointer

**Next**: TASK-039 + TASK-040 (Wave 1, parallel)
**State**: PLANNING_COMPLETE
**Last updated**: 2026-03-18T00:00:00Z

## Tasks

| ID | Title | Status | Wave | Depends On |
|----|-------|--------|------|------------|
| TASK-039 | Lazy MapperContext | READY | 1 | -- |
| TASK-040 | Skip pipeline closure when no middleware | READY | 1 | -- |
| TASK-041 | Compiled expression delegates | READY | 2 | -- |
| TASK-042 | Source-gen fast path (RegisterFactory + Mapper bypass) | READY | 3 | TASK-039, TASK-040 |
| TASK-043 | Re-run benchmarks and update BENCHMARKS.md | READY | 4 | TASK-041, TASK-042 |

## Wave Groups

### Wave 1 (parallel)
- TASK-039: Lazy MapperContext (-20ns runtime)
- TASK-040: Skip pipeline closure (-15ns runtime)

### Wave 2
- TASK-041: Compiled expression delegates (-65ns runtime)

### Wave 3
- TASK-042: Source-gen fast path (79.5ns → ~10ns)

### Wave 4
- TASK-043: Re-run benchmarks, update docs
