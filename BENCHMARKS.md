# DeltaMapper Benchmark Results

This document contains the DeltaMapper benchmark suite results, comparing DeltaMapper (runtime and source-gen paths) against Mapperly, AutoMapper, and hand-written code across four mapping scenarios: flat objects, nested objects, collections, and patch operations.

> **Note:** Results below are placeholders. Run the benchmark suite locally or in CI to populate actual numbers.

---

## Environment

| Property | Value |
|----------|-------|
| OS | (your OS) |
| CPU | (your CPU) |
| .NET SDK | (version) |
| BenchmarkDotNet | (version) |

---

## Results

### Flat Object (5 properties)

| Method | Mean | Error | StdDev | Gen0 | Allocated |
|--------|------|-------|--------|------|-----------|
| DeltaMapper_Runtime | `<pending>` | `<pending>` | `<pending>` | `<pending>` | `<pending>` |
| DeltaMapper_SourceGen | `<pending>` | `<pending>` | `<pending>` | `<pending>` | `<pending>` |
| Mapperly | `<pending>` | `<pending>` | `<pending>` | `<pending>` | `<pending>` |
| AutoMapper | `<pending>` | `<pending>` | `<pending>` | `<pending>` | `<pending>` |
| HandWritten | `<pending>` | `<pending>` | `<pending>` | `<pending>` | `<pending>` |

---

### Nested Object (2 levels — parent + Address child)

| Method | Mean | Error | StdDev | Gen0 | Allocated |
|--------|------|-------|--------|------|-----------|
| DeltaMapper_Runtime | `<pending>` | `<pending>` | `<pending>` | `<pending>` | `<pending>` |
| DeltaMapper_SourceGen | `<pending>` | `<pending>` | `<pending>` | `<pending>` | `<pending>` |
| Mapperly | `<pending>` | `<pending>` | `<pending>` | `<pending>` | `<pending>` |
| AutoMapper | `<pending>` | `<pending>` | `<pending>` | `<pending>` | `<pending>` |
| HandWritten | `<pending>` | `<pending>` | `<pending>` | `<pending>` | `<pending>` |

---

### Collection (10 items)

| Method | Mean | Error | StdDev | Gen0 | Allocated |
|--------|------|-------|--------|------|-----------|
| DeltaMapper_Runtime | `<pending>` | `<pending>` | `<pending>` | `<pending>` | `<pending>` |
| DeltaMapper_SourceGen | `<pending>` | `<pending>` | `<pending>` | `<pending>` | `<pending>` |
| Mapperly | `<pending>` | `<pending>` | `<pending>` | `<pending>` | `<pending>` |
| AutoMapper | `<pending>` | `<pending>` | `<pending>` | `<pending>` | `<pending>` |
| HandWritten | `<pending>` | `<pending>` | `<pending>` | `<pending>` | `<pending>` |

---

### Patch (MappingDiff + change tracking)

| Method | Mean | Error | StdDev | Gen0 | Allocated |
|--------|------|-------|--------|------|-----------|
| DeltaMapper_Patch_Runtime | `<pending>` | `<pending>` | `<pending>` | `<pending>` | `<pending>` |
| DeltaMapper_Patch_SourceGen | `<pending>` | `<pending>` | `<pending>` | `<pending>` | `<pending>` |
| AutoMapper_Map | `<pending>` | `<pending>` | `<pending>` | `<pending>` | `<pending>` |
| HandWritten_Overwrite | `<pending>` | `<pending>` | `<pending>` | `<pending>` | `<pending>` |

> **Note:** Patch is a DeltaMapper-unique feature (maps + returns property diff). Competitors perform map-onto-existing as the nearest equivalent. Mapperly has no equivalent API.

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
