# DeltaMapper Benchmark Results

This document contains the DeltaMapper benchmark suite results, comparing DeltaMapper (runtime, source-gen via IMapper, and direct static call) against Mapperly, AutoMapper, and hand-written code.

---

## Environment

| Property | Value |
|----------|-------|
| OS | macOS Tahoe 26.2 (Darwin 25.2.0) |
| CPU | Apple M1 Max, 10 cores |
| .NET SDK | 10.0.103 |
| Runtime | .NET 10.0.3, Arm64 RyuJIT |
| BenchmarkDotNet | v0.15.8 |

---

## Results

### Flat Object (5 properties)

| Method | Mean | Error | StdDev | Gen0 | Allocated |
|--------|-----:|------:|-------:|-----:|----------:|
| HandWritten | 6.839 ns | 0.1321 ns | 0.1236 ns | 0.0076 | 48 B |
| Mapperly | 6.808 ns | 0.1204 ns | 0.1126 ns | 0.0076 | 48 B |
| **DeltaMapper_DirectCall** | **7.191 ns** | 0.1755 ns | 0.4102 ns | 0.0076 | **48 B** |
| DeltaMapper_SourceGen | 23.923 ns | 0.5111 ns | 1.0325 ns | 0.0076 | 48 B |
| AutoMapper | 46.910 ns | 0.9729 ns | 2.5799 ns | 0.0076 | 48 B |
| DeltaMapper_Runtime | 110.331 ns | 2.2318 ns | 5.3042 ns | 0.0675 | 424 B |

> **DeltaMapper DirectCall matches Mapperly and hand-written code** — 7.2ns / 48B with zero overhead. The IMapper path adds ~17ns for pipeline routing but allocates the same 48B.

---

### Nested Object (2 levels — parent + Address child)

| Method | Mean | Error | StdDev | Gen0 | Allocated |
|--------|-----:|------:|-------:|-----:|----------:|
| HandWritten | 19.40 ns | 0.422 ns | 0.705 ns | 0.0191 | 120 B |
| Mapperly | 20.51 ns | 0.464 ns | 1.323 ns | 0.0191 | 120 B |
| DeltaMapper_SourceGen | 23.62 ns | 0.509 ns | 0.878 ns | 0.0127 | 80 B |
| AutoMapper | 54.93 ns | 1.083 ns | 0.905 ns | 0.0191 | 120 B |
| DeltaMapper_Runtime | 137.92 ns | 2.692 ns | 3.100 ns | 0.0801 | 504 B |

> DeltaMapper SourceGen allocates **less** than Mapperly on nested objects (80B vs 120B) and is within 3 ns.

---

### Collection (10 items)

| Method | Mean | Error | StdDev | Gen0 | Allocated |
|--------|-----:|------:|-------:|-----:|----------:|
| **DeltaMapper_SourceGen** | **21.91 ns** | 0.379 ns | 0.336 ns | 0.0102 | **64 B** |
| Mapperly | 101.25 ns | 1.955 ns | 1.829 ns | 0.0829 | 520 B |
| HandWritten | 120.94 ns | 2.406 ns | 2.471 ns | 0.0942 | 592 B |
| AutoMapper | 183.46 ns | 2.813 ns | 2.632 ns | 0.1135 | 712 B |
| DeltaMapper_Runtime | 1,128.73 ns | 13.039 ns | 10.889 ns | 0.5264 | 3,304 B |

> DeltaMapper SourceGen is **4.6x faster than Mapperly** and **5.5x faster than hand-written** on collections, with 8x less allocation.

---

### Patch (MappingDiff + change tracking)

| Method | Mean | Error | StdDev | Gen0 | Allocated |
|--------|-----:|------:|-------:|-----:|----------:|
| HandWritten_Overwrite | 8.222 ns | 0.1264 ns | 0.1182 ns | 0.0076 | 48 B |
| AutoMapper_Map | 48.488 ns | 0.6123 ns | 0.5727 ns | 0.0076 | 48 B |
| DeltaMapper_Patch_SourceGen | 524.275 ns | 9.5217 ns | 17.6490 ns | 0.2661 | 1,672 B |
| DeltaMapper_Patch_Runtime | 544.200 ns | 10.5277 ns | 16.0769 ns | 0.2775 | 1,744 B |

> Patch is a DeltaMapper-unique feature — it maps **and** returns a structured `MappingDiff<T>` with per-property change tracking. Competitors have no equivalent.

---

## Performance Tiers

DeltaMapper offers two call patterns depending on your needs:

| Tier | Call Pattern | Flat Mean | Features |
|------|-------------|-----------|----------|
| **Direct Call** | `FlatGenProfile.MapFlatSourceToFlatDest(src)` | **7.2 ns** | Zero overhead, Mapperly parity |
| **IMapper** | `mapper.Map<Src, Dest>(src)` | **24 ns** | DI, middleware, hooks, Patch |
| **Runtime** | `mapper.Map<Src, Dest>(src)` (no source-gen) | **110 ns** | Full feature set, no codegen |

---

## Run Locally

```bash
cd tests/DeltaMapper.Benchmarks
dotnet run -c Release
```

Run a specific scenario:

```bash
dotnet run -c Release -- --filter "*FlatObject*"
dotnet run -c Release -- --filter "*NestedObject*"
dotnet run -c Release -- --filter "*Collection*"
dotnet run -c Release -- --filter "*Patch*"
```

---

## Methodology

- **Framework**: [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet)
- **Memory diagnostics**: `[MemoryDiagnoser]` tracks `Gen0` collections and `Allocated` bytes
- **Warmup**: Automatic warmup; results from stable measurement phase only
- **Platform**: Release configuration required (`dotnet run -c Release`)
- **Isolation**: Each competitor uses its own mapper instance, initialized once in `[GlobalSetup]`
- **Statistics**: `Mean`, `Error` (99.9% CI), `StdDev` per BenchmarkDotNet defaults

---

## See Also

- [README.md](README.md) — overview, install instructions, and quick start
- `tests/DeltaMapper.Benchmarks/` — benchmark source code
