# Multi-Target .NET 8 / 9 / 10 Support

**Date:** 2026-03-20
**Status:** Approved
**Goal:** Broaden adoption by supporting .NET 8 LTS, .NET 9 STS, and .NET 10 (current)

## Decision Summary

- Multi-target `net8.0;net9.0;net10.0` across all production packages
- Single NuGet package per library (multi-TFM binaries inside)
- Identical public API surface on all TFMs
- Conditional dependency versions per TFM
- Full test suite runs on all three TFMs

## Architecture

All 4 production packages (`Core`, `EFCore`, `OpenTelemetry`, `SourceGen`) multi-target — except `SourceGen` which stays `netstandard2.0` (Roslyn analyzer requirement). Each `.nupkg` ships `lib/net8.0/`, `lib/net9.0/`, `lib/net10.0/` folders. NuGet resolves the correct binary automatically.

## Build Infrastructure

### Directory.Build.props (new)

Repo-root `Directory.Build.props` centralizes:
- `LangVersion` (latest)
- Common package metadata
- Conditional `<PackageReference>` version resolution per TFM

### Conditional Dependencies

| Package | net8.0 | net9.0 | net10.0 |
|---------|--------|--------|---------|
| `Microsoft.Extensions.DependencyInjection.Abstractions` | 8.x | 9.x | 10.x |
| `Microsoft.EntityFrameworkCore` | 8.x | 9.x | 10.x |

### .csproj Changes

Each production project switches from:
```xml
<TargetFramework>net10.0</TargetFramework>
```
to:
```xml
<TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
```

## Source Changes

Minimal `#if` directives expected. Key APIs are all available on .NET 8+:

| Feature | Available Since |
|---------|----------------|
| `FrozenDictionary` / `FrozenSet` | .NET 8 |
| `ArgumentNullException.ThrowIfNull()` | .NET 6 |
| Collection expressions (`[]`) | C# 12 (compiler feature) |
| Init-only properties | C# 9 (compiler feature) |
| Records | C# 9 (compiler feature) |

## Testing

- All test projects multi-target `net8.0;net9.0;net10.0`
- Benchmarks stay .NET 10 only (cross-TFM benchmarking adds noise)
- CI matrix runs full test suite on each TFM

## CI/CD

- GitHub Actions installs .NET 8, 9, and 10 SDKs
- Test step runs matrix: `[net8.0, net9.0, net10.0]`
- `dotnet pack` automatically produces multi-TFM packages

## Risk Assessment

**Low risk.** No .NET 10-exclusive runtime APIs detected beyond what's available in .NET 8. The compiler will catch any missed APIs immediately when building for `net8.0`. No polyfill packages needed.
