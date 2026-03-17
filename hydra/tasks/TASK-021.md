---
id: TASK-021
title: SourceGen project scaffold and solution registration
status: IMPLEMENTED
depends_on: []
wave: 1
files_to_create:
  - src/DeltaMapper.SourceGen/DeltaMapper.SourceGen.csproj
files_to_modify:
  - DeltaMapper.slnx
acceptance_criteria:
  - src/DeltaMapper.SourceGen/DeltaMapper.SourceGen.csproj targets netstandard2.0 with Microsoft.CodeAnalysis.CSharp 4.* (PrivateAssets=all) and EnforceExtendedAnalyzerRules=true
  - DeltaMapper.slnx includes the new project under /src/ folder
  - "dotnet build src/DeltaMapper.SourceGen -c Release" succeeds with zero errors
---

- **Status**: IMPLEMENTED

**Retry count**: 0/3

## Description

Create the `src/DeltaMapper.SourceGen/` project that will host the Roslyn IIncrementalGenerator.

Key requirements:
- Target `netstandard2.0` (required for Roslyn analyzers/generators)
- `LangVersion` set to `latest` (Roslyn SDK supports this on netstandard2.0)
- `EnforceExtendedAnalyzerRules` set to `true`
- `Microsoft.CodeAnalysis.CSharp` version `4.*` with `PrivateAssets="all"`
- `IsPackable` set to `true` — this will become the `DeltaMapper.SourceGen` NuGet package
- The project must NOT reference `DeltaMapper.Core` directly (generators run at compile time in a different process)
- Add the project to `DeltaMapper.slnx` under the `/src/` folder

## Pattern Reference

Follow the existing csproj pattern from `src/DeltaMapper.Core/DeltaMapper.Core.csproj` for NuGet metadata, but target `netstandard2.0` instead of `net10.0`.

## Test Requirements

Verify the project builds cleanly with `dotnet build src/DeltaMapper.SourceGen -c Release`. The full solution `dotnet build -c Release` should also pass.

## Traces To

docs/DELTAMAP_PLAN.md section 3.1 (Project Setup)
