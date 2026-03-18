# TASK-043: Re-run benchmarks and update BENCHMARKS.md

**Status**: READY
**Wave**: 4
**Depends on**: TASK-041, TASK-042

## Description
Run full BenchmarkDotNet suite, update BENCHMARKS.md and README.md with optimized numbers.

## Files
- Modify: `BENCHMARKS.md`
- Modify: `README.md`

## Acceptance Criteria
1. SourceGen flat < 15ns
2. Runtime flat < 50ns
3. No `<pending>` in BENCHMARKS.md
