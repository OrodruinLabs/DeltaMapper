# TASK-036: Benchmark scenarios (flat/nested/collection/patch)

## Metadata
- **Status**: IMPLEMENTED
- **Wave**: 2
- **Depends on**: TASK-035
- **Delegates to**: implementer
- **Traces to**: Phase 5.1 (docs/DELTAMAP_PLAN.md:510-530)
- **Files modified**: tests/DeltaMapper.Benchmarks/Benchmarks/FlatObjectBenchmark.cs, tests/DeltaMapper.Benchmarks/Benchmarks/NestedObjectBenchmark.cs, tests/DeltaMapper.Benchmarks/Benchmarks/CollectionBenchmark.cs, tests/DeltaMapper.Benchmarks/Benchmarks/PatchBenchmark.cs, tests/DeltaMapper.Benchmarks/Competitors/AutoMapperSetup.cs, tests/DeltaMapper.Benchmarks/Competitors/MapperlyMapper.cs, tests/DeltaMapper.Benchmarks/Competitors/HandWrittenMapper.cs
- **Retry count**: 0/3
- **Review evidence**: hydra/reviews/TASK-036-review.md

## Description

Implement all four benchmark scenario classes per the design spec: `FlatObject_1M` (1M flat mappings), `NestedObject_100k` (100k nested mappings), `Collection_10k` (10k list-of-10 mappings), `Patch_100k` (100k patch calls). Each benchmark class compares five competitors: DeltaMapper runtime, DeltaMapper source-gen, Mapperly, AutoMapper, and hand-written. Create helper files for competitor setup (AutoMapper config, Mapperly static mapper, hand-written implementation).

## File Scope

### Creates
- `tests/DeltaMapper.Benchmarks/Benchmarks/FlatObjectBenchmark.cs`
- `tests/DeltaMapper.Benchmarks/Benchmarks/NestedObjectBenchmark.cs`
- `tests/DeltaMapper.Benchmarks/Benchmarks/CollectionBenchmark.cs`
- `tests/DeltaMapper.Benchmarks/Benchmarks/PatchBenchmark.cs`
- `tests/DeltaMapper.Benchmarks/Competitors/AutoMapperSetup.cs`
- `tests/DeltaMapper.Benchmarks/Competitors/MapperlyMapper.cs`
- `tests/DeltaMapper.Benchmarks/Competitors/HandWrittenMapper.cs`

### Modifies
(none)

## Pattern Reference

- `tests/DeltaMapper.UnitTests/MapperTests.cs` (lines 1-30) — how DeltaMapper runtime mapper is configured and used
- `src/DeltaMapper.SourceGen/GenerateMapAttribute.cs` — attribute usage for source-gen path

## Acceptance Criteria

1. `dotnet build tests/DeltaMapper.Benchmarks/ -c Release` succeeds — all four benchmark classes and three competitor files compile without errors
2. Each benchmark class has `[MemoryDiagnoser]` attribute and contains `[Benchmark]` methods for each applicable competitor with descriptive labels (Patch has 4 — no Mapperly equivalent)
3. Iteration counts match the spec: BenchmarkDotNet controls iteration counts automatically; each method performs one mapping per invocation

## Test Requirements

Build verification only. Benchmark classes are not xunit tests — they run via `dotnet run -c Release`. Acceptance is verified by successful compilation and inspection of benchmark method attributes.

## Implementation Notes

- Each benchmark class: `[MemoryDiagnoser]`, `[SimpleJob(RuntimeMoniker.Net100)]` or equivalent
- Use `[GlobalSetup]` to initialize all mappers (DeltaMapper config, AutoMapper config, etc.)
- Mapperly: define a `[Mapper]` partial class with `MapFlatSource` etc. methods
- Hand-written: simple static methods with direct property assignment
- Patch benchmark only applies to DeltaMapper (others get `Patch_AutoMapper` as N/A or equivalent mapping-then-diff)
- Use `[BenchmarkCategory]` to group by scenario for filtered runs
- Namespace: `DeltaMapper.Benchmarks.Benchmarks` and `DeltaMapper.Benchmarks.Competitors`
