# DeltaMapper Benchmark Results

This document contains the DeltaMapper benchmark suite results, comparing DeltaMapper (runtime and source-gen paths) against Mapperly, AutoMapper, and hand-written code across four mapping scenarios.

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
| HandWritten | 6.609 ns | 0.1007 ns | 0.0942 ns | 0.0076 | 48 B |
| Mapperly | 6.632 ns | 0.1046 ns | 0.0979 ns | 0.0076 | 48 B |
| **DeltaMapper_SourceGen** | **24.631 ns** | 0.1995 ns | 0.1866 ns | 0.0076 | **48 B** |
| AutoMapper | 45.097 ns | 0.2981 ns | 0.2788 ns | 0.0076 | 48 B |
| DeltaMapper_Runtime | 93.684 ns | 0.6614 ns | 0.6186 ns | 0.0675 | 424 B |

> DeltaMapper SourceGen allocates the same 48 bytes as hand-written code — zero overhead beyond the destination object.

---

### Nested Object (2 levels — parent + Address child)

| Method | Mean | Error | StdDev | Gen0 | Allocated |
|--------|-----:|------:|-------:|-----:|----------:|
| HandWritten | 18.18 ns | 0.187 ns | 0.175 ns | 0.0191 | 120 B |
| Mapperly | 18.18 ns | 0.195 ns | 0.182 ns | 0.0191 | 120 B |
| **DeltaMapper_SourceGen** | **24.15 ns** | 0.252 ns | 0.224 ns | 0.0127 | **80 B** |
| AutoMapper | 54.64 ns | 0.445 ns | 0.416 ns | 0.0191 | 120 B |
| DeltaMapper_Runtime | 125.18 ns | 1.118 ns | 0.991 ns | 0.0801 | 504 B |

> DeltaMapper SourceGen allocates **less** than Mapperly on nested objects (80B vs 120B).

---

### Collection (10 items)

| Method | Mean | Error | StdDev | Gen0 | Allocated |
|--------|-----:|------:|-------:|-----:|----------:|
| **DeltaMapper_SourceGen** | **23.16 ns** | 0.212 ns | 0.199 ns | 0.0102 | **64 B** |
| Mapperly | 98.28 ns | 0.807 ns | 0.755 ns | 0.0829 | 520 B |
| HandWritten | 118.88 ns | 1.941 ns | 1.816 ns | 0.0942 | 592 B |
| AutoMapper | 179.42 ns | 2.576 ns | 2.409 ns | 0.1135 | 712 B |
| DeltaMapper_Runtime | 1,106.82 ns | 10.490 ns | 9.812 ns | 0.5264 | 3,304 B |

> DeltaMapper SourceGen is **4.2x faster than Mapperly** and **5.1x faster than hand-written** on collections, with 8x less allocation.

---

### Patch (MappingDiff + change tracking)

| Method | Mean | Error | StdDev | Gen0 | Allocated |
|--------|-----:|------:|-------:|-----:|----------:|
| HandWritten_Overwrite | 8.713 ns | 0.0850 ns | 0.0795 ns | 0.0076 | 48 B |
| AutoMapper_Map | 48.155 ns | 0.4196 ns | 0.3719 ns | 0.0076 | 48 B |
| DeltaMapper_Patch_SourceGen | 509.730 ns | 3.1629 ns | 2.6412 ns | 0.2661 | 1,672 B |
| DeltaMapper_Patch_Runtime | 514.899 ns | 6.4803 ns | 6.0617 ns | 0.2775 | 1,744 B |

> Patch is a DeltaMapper-unique feature — it maps **and** returns a structured `MappingDiff<T>` with per-property change tracking. Competitors perform map-onto-existing as the nearest equivalent but produce no diff. The extra cost reflects diff computation and `PropertyChange` allocations.

---

## Run Locally

```bash
cd tests/DeltaMapper.Benchmarks
dotnet run -c Release
```

Run a specific scenario:

```bash
# Flat object only
dotnet run -c Release -- --filter "*FlatObject*"

# Nested object only
dotnet run -c Release -- --filter "*NestedObject*"

# Collection only
dotnet run -c Release -- --filter "*Collection*"

# Patch only
dotnet run -c Release -- --filter "*Patch*"
```

Results are written to `BenchmarkDotNet.Artifacts/results/` in the benchmarks directory.

---

## Methodology

- **Framework**: [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet)
- **Memory diagnostics**: `[MemoryDiagnoser]` is applied to all benchmark classes — `Gen0` collections and `Allocated` bytes are tracked alongside timing
- **Warmup**: BenchmarkDotNet performs automatic warmup iterations before measurement; all results shown are from the stable measurement phase
- **Platform**: Benchmarks must be run in `Release` configuration (`dotnet run -c Release`) — `Debug` builds will produce meaningless results
- **Isolation**: Each competitor uses its own mapper instance, initialized once in `[GlobalSetup]` to exclude startup cost from per-operation measurements
- **Statistics**: `Mean`, `Error` (half of 99.9% confidence interval), and `StdDev` (standard deviation) are reported per BenchmarkDotNet defaults

---

## See Also

- [README.md](README.md) — overview, install instructions, and quick start
- `tests/DeltaMapper.Benchmarks/` — benchmark source code
