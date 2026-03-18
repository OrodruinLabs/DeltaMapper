# Plan — Performance Parity with Mapperly (FEAT-009)

## Objective

Close the 18ns gap: object initializer emit, cached fast-path routing, public static Map methods. Target: SourceGen IMapper ~12ns, direct call ~7ns.

## Status Summary

| Status | Count |
|--------|-------|
| READY  | 5     |
| DONE   | 0     |
| TOTAL  | 5     |

## Recovery Pointer

**Next**: TASK-044 (Wave 1)
**State**: PLANNING_COMPLETE
**Last updated**: 2026-03-18T00:00:00Z

## Tasks

| ID | Title | Status | Wave | Depends On |
|----|-------|--------|------|------------|
| TASK-044 | Emit object initializer pattern in factory methods | READY | 1 | -- |
| TASK-045 | Cache fast-path routing decision per type pair | READY | 2 | TASK-044 |
| TASK-046 | Generate public static Map methods | READY | 3 | TASK-044 |
| TASK-047 | Add direct-call benchmark + run full suite | READY | 4 | TASK-045, TASK-046 |
| TASK-048 | Update BENCHMARKS.md and README.md | READY | 5 | TASK-047 |

## Wave Groups

### Wave 1
- TASK-044: Object initializer in factory methods (-4-6ns)

### Wave 2
- TASK-045: Cached routing decision (-3-5ns)

### Wave 3
- TASK-046: Public static Map methods (direct call ~7ns)

### Wave 4
- TASK-047: Benchmark validation

### Wave 5
- TASK-048: Docs update
