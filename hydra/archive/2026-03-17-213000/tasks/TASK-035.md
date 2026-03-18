# TASK-035: Benchmark project scaffold + shared models

## Metadata
- **Status**: READY
- **Wave**: 1
- **Depends on**: --
- **Delegates to**: implementer
- **Traces to**: Phase 5.1 (docs/DELTAMAP_PLAN.md:507-520)
- **Files modified**: tests/DeltaMapper.Benchmarks/DeltaMapper.Benchmarks.csproj, tests/DeltaMapper.Benchmarks/Program.cs, tests/DeltaMapper.Benchmarks/Models/BenchmarkModels.cs, DeltaMapper.slnx
- **Retry count**: 0/3
- **Review evidence**: hydra/reviews/TASK-035-review.md

## Description

Create the `tests/DeltaMapper.Benchmarks/` project as a console application targeting net10.0. Add PackageReferences for BenchmarkDotNet, AutoMapper, and Riok.Mapperly. Add ProjectReferences to DeltaMapper.Core and DeltaMapper.SourceGen. Define shared benchmark model classes (flat POCO, nested POCO, collection container) used across all benchmark scenarios. Wire the project into DeltaMapper.slnx. Create `Program.cs` with the BenchmarkSwitcher entry point.

## File Scope

### Creates
- `tests/DeltaMapper.Benchmarks/DeltaMapper.Benchmarks.csproj`
- `tests/DeltaMapper.Benchmarks/Program.cs`
- `tests/DeltaMapper.Benchmarks/Models/BenchmarkModels.cs`

### Modifies
- `DeltaMapper.slnx` — add benchmark project to `/tests/` folder

## Pattern Reference

- `tests/DeltaMapper.UnitTests/DeltaMapper.UnitTests.csproj` (lines 1-18) — csproj structure pattern for test projects
- `DeltaMapper.slnx` (lines 1-13) — solution file pattern for adding projects

## Acceptance Criteria

1. `dotnet build tests/DeltaMapper.Benchmarks/ -c Release` succeeds with zero errors and zero warnings
2. `DeltaMapper.slnx` contains a `<Project>` entry for `tests/DeltaMapper.Benchmarks/DeltaMapper.Benchmarks.csproj` inside the `/tests/` folder
3. `BenchmarkModels.cs` defines at minimum: `FlatSource`/`FlatDest` (5 properties each), `NestedSource`/`NestedDest` (with `Address` child), and a `[GenerateMap]` partial class for source-gen benchmark path

## Test Requirements

Build verification only — benchmark projects are console apps, not test projects. The acceptance criteria verify via `dotnet build`.

## Implementation Notes

- csproj must have `<OutputType>Exe</OutputType>` (console app, not class library)
- PackageReferences: `BenchmarkDotNet 0.*`, `AutoMapper 13.*`, `Riok.Mapperly 4.*`
- ProjectReferences: `../../src/DeltaMapper.Core/DeltaMapper.Core.csproj`, `../../src/DeltaMapper.SourceGen/DeltaMapper.SourceGen.csproj`
- SourceGen reference needs analyzer attributes: `OutputItemType="Analyzer" ReferenceOutputAssembly="false"`
- Program.cs: `BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);`
- Models should include `[GenerateMap(typeof(FlatDest))]` on `FlatSource` partial class for source-gen path
- Use `[MemoryDiagnoser]` as a global assembly attribute or on each benchmark class
