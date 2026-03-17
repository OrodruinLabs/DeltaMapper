# DeltaMapper Benchmark Results

This document contains the DeltaMapper benchmark suite results, comparing DeltaMapper (runtime and source-gen paths) against Mapperly, AutoMapper, and hand-written code across four mapping scenarios: flat objects, nested objects, collections, and patch operations.

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
| HandWritten | 7.213 ns | 0.1447 ns | 0.1282 ns | 0.0076 | 48 B |
| Mapperly | 7.190 ns | 0.1681 ns | 0.1491 ns | 0.0076 | 48 B |
| AutoMapper | 45.488 ns | 0.4007 ns | 0.3128 ns | 0.0076 | 48 B |
| DeltaMapper_SourceGen | 79.534 ns | 1.6329 ns | 2.2891 ns | 0.0726 | 456 B |
| DeltaMapper_Runtime | 195.361 ns | 3.9186 ns | 6.8632 ns | 0.0842 | 528 B |

---

### Nested Object (2 levels — parent + Address child)

| Method | Mean | Error | StdDev | Gen0 | Allocated |
|--------|-----:|------:|-------:|-----:|----------:|
| HandWritten | 18.79 ns | 0.400 ns | 0.548 ns | 0.0191 | 120 B |
| Mapperly | 18.28 ns | 0.199 ns | 0.166 ns | 0.0191 | 120 B |
| AutoMapper | 55.75 ns | 0.545 ns | 0.484 ns | 0.0191 | 120 B |
| DeltaMapper_SourceGen | 85.90 ns | 1.736 ns | 3.217 ns | 0.0777 | 488 B |
| DeltaMapper_Runtime | 266.16 ns | 2.566 ns | 2.004 ns | 0.1135 | 712 B |

---

### Collection (10 items)

| Method | Mean | Error | StdDev | Gen0 | Allocated |
|--------|-----:|------:|-------:|-----:|----------:|
| DeltaMapper_SourceGen | 82.02 ns | 1.668 ns | 2.227 ns | 0.0752 | 472 B |
| Mapperly | 109.88 ns | 3.129 ns | 9.027 ns | 0.0829 | 520 B |
| HandWritten | 126.05 ns | 2.522 ns | 5.209 ns | 0.0942 | 592 B |
| AutoMapper | 186.71 ns | 3.479 ns | 6.786 ns | 0.1135 | 712 B |
| DeltaMapper_Runtime | 1,897.50 ns | 36.883 ns | 40.995 ns | 0.7057 | 4,448 B |

---

### Patch (MappingDiff + change tracking)

| Method | Mean | Error | StdDev | Gen0 | Allocated |
|--------|-----:|------:|-------:|-----:|----------:|
| HandWritten_Overwrite | 8.570 ns | 0.1662 ns | 0.1298 ns | 0.0076 | 48 B |
| AutoMapper_Map | 48.860 ns | 0.9556 ns | 1.4302 ns | 0.0076 | 48 B |
| DeltaMapper_Patch_SourceGen | 523.100 ns | 8.5272 ns | 7.5591 ns | 0.2823 | 1,776 B |
| DeltaMapper_Patch_Runtime | 678.331 ns | 12.6479 ns | 11.8309 ns | 0.2937 | 1,848 B |

> **Note:** Patch is a DeltaMapper-unique feature — it maps **and** returns a structured `MappingDiff<T>` with per-property change tracking. Competitors perform map-onto-existing as the nearest equivalent but produce no diff. The extra cost reflects diff computation and `PropertyChange` allocations.

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
