# TASK-001: Solution and Project Files Setup

## Description
Create the .NET solution file, DeltaMapper.Core project (net8.0;net9.0 multi-target), and DeltaMapper.UnitTests project (net9.0). Configure all MSBuild properties: nullable enable, ImplicitUsings enable, LangVersion latest, RootNamespace DeltaMapper, GeneratePackageOnBuild. Add NuGet package references for test project (xunit, FluentAssertions, Microsoft.NET.Test.Sdk, xunit.runner.visualstudio, Microsoft.Extensions.DependencyInjection). Add DI abstractions reference to Core project.

## Status
DONE

## Metadata
- **Task ID**: TASK-001
- **Group**: 1
- **Wave**: 1
- **Depends on**: none
- **Retry count**: 0/3
- **Files modified**: DeltaMapper.sln, src/DeltaMapper.Core/DeltaMapper.Core.csproj, tests/DeltaMapper.UnitTests/DeltaMapper.UnitTests.csproj
- **delegates_to**: implementer
- **traces_to**: TRD Section 2.1 (Project Structure), TRD Section 2.2 (Target Frameworks), TRD Section 2.3 (Dependencies), TRD AC-1, TRD AC-19

## File Scope

### Creates
- `DeltaMapper.sln`
- `src/DeltaMapper.Core/DeltaMapper.Core.csproj`
- `tests/DeltaMapper.UnitTests/DeltaMapper.UnitTests.csproj`

### Modifies
- (none)

## Acceptance Criteria
1. `dotnet build -c Release` succeeds with zero warnings for both net8.0 and net9.0 targets
2. `dotnet test` discovers zero tests but exits successfully (test infrastructure works)
3. Core csproj has: TargetFrameworks net8.0;net9.0, Nullable enable, ImplicitUsings enable, LangVersion latest, RootNamespace DeltaMapper, GeneratePackageOnBuild true, PackageId DeltaMapper, zero runtime deps except Microsoft.Extensions.DependencyInjection.Abstractions

## Test Requirements
- No tests needed for this task (infrastructure only) -- verify by running `dotnet build` and `dotnet test`

## Pattern Reference
- TRD Section 2.1-2.3 for project structure and dependencies
- docs/DELTAMAP_PLAN.md:84-98 for csproj template
