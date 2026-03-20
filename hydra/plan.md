# Plan — Multi-Target .NET 8/9/10

## Objective

Multi-target all production and test packages across net8.0, net9.0, and net10.0 for broader NuGet adoption. Add Directory.Build.props, conditional dependency versions, and multi-TFM CI matrix.

## Status Summary

| Status | Count |
|--------|-------|
| DONE   | 5     |
| TOTAL  | 5     |

## Recovery Pointer

**Next**: Objective complete — all tasks done, PR pending
**State**: 852 tests passing on all 3 TFMs, packages at v1.0.0-rc.4 with net8.0/net9.0/net10.0
**Last updated**: 2026-03-20

## Tasks

| ID | Title | Status | Wave | Depends On |
|----|-------|--------|------|------------|
| TASK-058 | Directory.Build.props + multi-target production packages | DONE | 1 | -- |
| TASK-059 | Multi-target test projects | DONE | 1 | -- |
| TASK-060 | Update CI/CD workflows for multi-TFM | DONE | 2 | TASK-058, TASK-059 |
| TASK-061 | Full validation — build, test, pack across all TFMs | DONE | 3 | TASK-060 |
| TASK-062 | Update docs + version bump for multi-target release | DONE | 3 | TASK-061 |

## Wave Groups

### Wave 1 (parallel — independent)
- TASK-058: Directory.Build.props + production .csproj multi-targeting
- TASK-059: Test project multi-targeting

### Wave 2 (after Wave 1)
- TASK-060: CI/CD workflow updates

### Wave 3 (after Wave 2)
- TASK-061: Full validation
- TASK-062: Docs + version bump

## Task Details

### TASK-058: Directory.Build.props + multi-target production packages

**Scope:**
1. Create `Directory.Build.props` at repo root with shared `Nullable`, `ImplicitUsings`, `LangVersion`
2. Multi-target `DeltaMapper.Core` — change `<TargetFramework>net10.0</TargetFramework>` to `<TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>`, add conditional `Microsoft.Extensions.DependencyInjection.Abstractions` versions (8.*/9.*/10.* per TFM), remove redundant properties inherited from Directory.Build.props
3. Multi-target `DeltaMapper.EFCore` — same TFM change, conditional `Microsoft.EntityFrameworkCore` versions (8.*/9.*/10.*)
4. Multi-target `DeltaMapper.OpenTelemetry` — same TFM change, no conditional deps needed
5. Update `PackageTags` in all four production .csproj files — replace `net10` with `net8;net9;net10`
6. `DeltaMapper.SourceGen` stays `netstandard2.0` — only update tags
7. Remove `Nullable`, `ImplicitUsings`, `LangVersion` from all production .csproj files (inherited from Directory.Build.props)
8. `dotnet build -c Release` must succeed for all TFMs

**Files:**
- Create: `Directory.Build.props`
- Modify: `src/DeltaMapper.Core/DeltaMapper.Core.csproj`
- Modify: `src/DeltaMapper.EFCore/DeltaMapper.EFCore.csproj`
- Modify: `src/DeltaMapper.OpenTelemetry/DeltaMapper.OpenTelemetry.csproj`
- Modify: `src/DeltaMapper.SourceGen/DeltaMapper.SourceGen.csproj`

**Acceptance:**
- `Directory.Build.props` exists with shared Nullable/ImplicitUsings/LangVersion
- Core, EFCore, OpenTelemetry target `net8.0;net9.0;net10.0`
- SourceGen stays `netstandard2.0` with updated tags
- Conditional PackageReference groups for Microsoft deps per TFM
- `dotnet build -c Release` succeeds (all TFMs compile)
- No redundant properties in .csproj files

---

### TASK-059: Multi-target test projects

**Scope:**
1. Multi-target `DeltaMapper.TestFixtures` — `net8.0;net9.0;net10.0`
2. Multi-target `DeltaMapper.UnitTests` — `net8.0;net9.0;net10.0`, conditional `Microsoft.Extensions.DependencyInjection` versions (8.*/9.*/10.*)
3. Multi-target `DeltaMapper.IntegrationTests` — `net8.0;net9.0;net10.0`, conditional `Microsoft.EntityFrameworkCore.InMemory` versions (8.*/9.*/10.*)
4. Multi-target `DeltaMapper.SourceGen.Tests` — `net8.0;net9.0;net10.0`
5. `DeltaMapper.Benchmarks` stays `net10.0` only — just remove redundant properties inherited from Directory.Build.props
6. Remove `Nullable`, `ImplicitUsings`, `LangVersion` from all test .csproj files
7. `dotnet test -c Release` must pass on all TFMs

**Files:**
- Modify: `tests/DeltaMapper.TestFixtures/DeltaMapper.TestFixtures.csproj`
- Modify: `tests/DeltaMapper.UnitTests/DeltaMapper.UnitTests.csproj`
- Modify: `tests/DeltaMapper.IntegrationTests/DeltaMapper.IntegrationTests.csproj`
- Modify: `tests/DeltaMapper.SourceGen.Tests/DeltaMapper.SourceGen.Tests.csproj`
- Modify: `tests/DeltaMapper.Benchmarks/DeltaMapper.Benchmarks.csproj`

**Acceptance:**
- TestFixtures, UnitTests, IntegrationTests, SourceGen.Tests all target `net8.0;net9.0;net10.0`
- Benchmarks stays `net10.0` only
- Conditional PackageReference groups for Microsoft test deps per TFM
- `dotnet test -c Release` passes on all three TFMs
- No redundant properties in .csproj files

---

### TASK-060: Update CI/CD workflows for multi-TFM

**Scope:**
1. Update `.github/workflows/ci.yml` — install .NET 8, 9, 10 SDKs; add TFM test matrix (`net8.0`, `net9.0`, `net10.0`); use `--framework ${{ matrix.tfm }}` for test step; unique artifact names per TFM
2. Update `.github/workflows/publish.yml` — add .NET 8, 9 SDK setup steps before .NET 10
3. Update `.github/workflows/benchmarks.yml` — add .NET 8, 9 SDK setup steps (needed for restore even though benchmarks only run on .NET 10)

**Files:**
- Modify: `.github/workflows/ci.yml`
- Modify: `.github/workflows/publish.yml`
- Modify: `.github/workflows/benchmarks.yml`

**Acceptance:**
- CI workflow installs all 3 SDKs and runs tests per TFM in matrix
- Publish workflow installs all 3 SDKs for multi-TFM pack
- Benchmarks workflow installs all 3 SDKs for restore compatibility
- YAML syntax is valid

---

### TASK-061: Full validation — build, test, pack across all TFMs

**Scope:**
1. `dotnet clean && dotnet build -c Release` — all projects build for all TFMs
2. `dotnet test -c Release` — all tests pass on net8.0, net9.0, net10.0
3. `dotnet pack -c Release` — verify Core, EFCore, OpenTelemetry, SourceGen nupkg files contain correct TFM folders (`lib/net8.0/`, `lib/net9.0/`, `lib/net10.0/`)
4. Fix any compilation or test failures discovered

**Files:**
- Potentially any .csproj or .cs file if fixes are needed

**Acceptance:**
- `dotnet build -c Release` exits 0
- `dotnet test -c Release` exits 0 (all TFMs)
- `.nupkg` files contain `lib/net8.0/`, `lib/net9.0/`, `lib/net10.0/` folders
- No `#if` directives needed (all APIs available on .NET 8+)

---

### TASK-062: Update docs + version bump for multi-target release

**Scope:**
1. Update `README.md` — change ".NET 10+" requirement to ".NET 8+" in prerequisites/badges
2. Update `NUGET_README.md` — same requirement change
3. Update `CHANGELOG.md` — add entry for multi-target support
4. Bump `<Version>` in all four production .csproj files to next RC version
5. Update `<PackageReleaseNotes>` to mention multi-target support

**Files:**
- Modify: `README.md`
- Modify: `NUGET_README.md`
- Modify: `CHANGELOG.md`
- Modify: `src/DeltaMapper.Core/DeltaMapper.Core.csproj`
- Modify: `src/DeltaMapper.EFCore/DeltaMapper.EFCore.csproj`
- Modify: `src/DeltaMapper.OpenTelemetry/DeltaMapper.OpenTelemetry.csproj`
- Modify: `src/DeltaMapper.SourceGen/DeltaMapper.SourceGen.csproj`

**Acceptance:**
- README and NUGET_README say ".NET 8+" not ".NET 10+"
- CHANGELOG has multi-target entry
- All four .csproj files have bumped version
- `dotnet build -c Release` still succeeds
