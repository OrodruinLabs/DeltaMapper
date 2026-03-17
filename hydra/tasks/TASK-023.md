---
id: TASK-023
title: SourceGen test project scaffold
status: IMPLEMENTED
depends_on: []
wave: 1
files_to_create:
  - tests/DeltaMapper.SourceGen.Tests/DeltaMapper.SourceGen.Tests.csproj
  - tests/DeltaMapper.SourceGen.Tests/Helpers/GeneratorTestHelper.cs
files_to_modify:
  - DeltaMapper.slnx
acceptance_criteria:
  - Test project targets net10.0 with xunit, FluentAssertions, Microsoft.CodeAnalysis.CSharp, and a ProjectReference to DeltaMapper.SourceGen as analyzer
  - GeneratorTestHelper provides a RunGenerator method that uses CSharpGeneratorDriver and returns GeneratorDriverRunResult
  - "dotnet build tests/DeltaMapper.SourceGen.Tests -c Release" succeeds
---

- **Status**: IMPLEMENTED

**Retry count**: 0/3

## Description

Create `tests/DeltaMapper.SourceGen.Tests/` — the test project for verifying source generator output.

### Project Setup

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="xunit" Version="2.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
    <PackageReference Include="FluentAssertions" Version="7.*" />
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.*" />
  </ItemGroup>
  <ItemGroup>
    <!-- Reference generator as analyzer so tests can drive it -->
    <ProjectReference Include="..\..\src\DeltaMapper.SourceGen\DeltaMapper.SourceGen.csproj"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false" />
    <!-- Also need a regular reference to instantiate the generator in test driver -->
    <ProjectReference Include="..\..\src\DeltaMapper.SourceGen\DeltaMapper.SourceGen.csproj" />
    <ProjectReference Include="..\..\src\DeltaMapper.Core\DeltaMapper.Core.csproj" />
  </ItemGroup>
</Project>
```

### GeneratorTestHelper

A helper class that:
1. Accepts source code strings
2. Creates a `CSharpCompilation` with necessary references (System, DeltaMapper.Core)
3. Creates `CSharpGeneratorDriver` with the `MapperGenerator` instance
4. Runs the driver and returns `GeneratorDriverRunResult`
5. Provides assertion-friendly methods: `ShouldGenerateFile(name)`, `ShouldHaveNoDiagnostics()`, etc.

## Pattern Reference

Follow test project conventions from `tests/DeltaMapper.UnitTests/DeltaMapper.UnitTests.csproj`.

## Test Requirements

Build succeeds. A placeholder test verifying the helper compiles and the generator type can be instantiated.

## Traces To

docs/DELTAMAP_PLAN.md section 3.5 (Phase 3 Tests)
