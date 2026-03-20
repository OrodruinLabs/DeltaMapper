# Multi-Target .NET 8/9/10 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Multi-target all production and test packages across net8.0, net9.0, and net10.0 for broader NuGet adoption.

**Architecture:** Add `Directory.Build.props` for shared settings. Switch each `.csproj` from `<TargetFramework>` to `<TargetFrameworks>`. Use conditional `<PackageReference>` groups for Microsoft dependencies that must match the TFM major version. Update CI to install all three SDKs and run a TFM test matrix.

**Tech Stack:** .NET 8/9/10, MSBuild, GitHub Actions

---

### Task 1: Create Directory.Build.props

**Files:**
- Create: `Directory.Build.props`

**Step 1: Create the file**

```xml
<Project>
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
</Project>
```

This centralizes `Nullable`, `ImplicitUsings`, and `LangVersion` so individual `.csproj` files don't repeat them.

**Step 2: Build to verify nothing breaks**

Run: `dotnet build -c Release`
Expected: SUCCESS — `Directory.Build.props` merges with existing `.csproj` settings (duplicates are harmless).

**Step 3: Commit**

```bash
git add Directory.Build.props
git commit -m "build: add Directory.Build.props for shared settings"
```

---

### Task 2: Multi-target DeltaMapper.Core

**Files:**
- Modify: `src/DeltaMapper.Core/DeltaMapper.Core.csproj`

**Step 1: Update the .csproj**

Replace:
```xml
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
```

With:
```xml
    <TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
```

Replace the unconditional PackageReference:
```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.*" />
  </ItemGroup>
```

With conditional groups:
```xml
  <ItemGroup Condition="'$(TargetFramework)' == 'net8.0'">
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.*" />
  </ItemGroup>
  <ItemGroup Condition="'$(TargetFramework)' == 'net9.0'">
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="9.*" />
  </ItemGroup>
  <ItemGroup Condition="'$(TargetFramework)' == 'net10.0'">
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.*" />
  </ItemGroup>
```

Also update `PackageTags` — replace `net10` with `net8;net9;net10`:
```xml
    <PackageTags>mapper;object-mapper;dto;diff;patch;source-generator;net8;net9;net10;csharp</PackageTags>
```

**Step 2: Build all TFMs**

Run: `dotnet build src/DeltaMapper.Core -c Release`
Expected: SUCCESS — builds for all three TFMs. Watch for any .NET 10-only API errors on net8.0/net9.0.

**Step 3: Commit**

```bash
git add src/DeltaMapper.Core/DeltaMapper.Core.csproj
git commit -m "build: multi-target DeltaMapper.Core for net8.0/net9.0/net10.0"
```

---

### Task 3: Multi-target DeltaMapper.EFCore

**Files:**
- Modify: `src/DeltaMapper.EFCore/DeltaMapper.EFCore.csproj`

**Step 1: Update the .csproj**

Replace:
```xml
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
```

With:
```xml
    <TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
```

Replace the unconditional EFCore PackageReference:
```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.*" />
  </ItemGroup>
```

With conditional groups:
```xml
  <ItemGroup Condition="'$(TargetFramework)' == 'net8.0'">
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.*" />
  </ItemGroup>
  <ItemGroup Condition="'$(TargetFramework)' == 'net9.0'">
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.*" />
  </ItemGroup>
  <ItemGroup Condition="'$(TargetFramework)' == 'net10.0'">
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.*" />
  </ItemGroup>
```

Update `PackageTags` — replace `net10` with `net8;net9;net10`:
```xml
    <PackageTags>mapper;efcore;entity-framework;proxy;navigation;net8;net9;net10;csharp</PackageTags>
```

**Step 2: Build all TFMs**

Run: `dotnet build src/DeltaMapper.EFCore -c Release`
Expected: SUCCESS

**Step 3: Commit**

```bash
git add src/DeltaMapper.EFCore/DeltaMapper.EFCore.csproj
git commit -m "build: multi-target DeltaMapper.EFCore for net8.0/net9.0/net10.0"
```

---

### Task 4: Multi-target DeltaMapper.OpenTelemetry

**Files:**
- Modify: `src/DeltaMapper.OpenTelemetry/DeltaMapper.OpenTelemetry.csproj`

**Step 1: Update the .csproj**

Replace:
```xml
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
```

With:
```xml
    <TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
```

No conditional dependencies needed — OpenTelemetry has no Microsoft-versioned package references.

Update `PackageTags` — replace `net10` with `net8;net9;net10`:
```xml
    <PackageTags>mapper;opentelemetry;tracing;activity;observability;net8;net9;net10;csharp</PackageTags>
```

**Step 2: Build all TFMs**

Run: `dotnet build src/DeltaMapper.OpenTelemetry -c Release`
Expected: SUCCESS

**Step 3: Commit**

```bash
git add src/DeltaMapper.OpenTelemetry/DeltaMapper.OpenTelemetry.csproj
git commit -m "build: multi-target DeltaMapper.OpenTelemetry for net8.0/net9.0/net10.0"
```

---

### Task 5: Update SourceGen package tags (no TFM change)

**Files:**
- Modify: `src/DeltaMapper.SourceGen/DeltaMapper.SourceGen.csproj`

SourceGen stays `netstandard2.0` — Roslyn analyzers must target netstandard. Only update the tags.

**Step 1: Update PackageTags**

Replace `net10` with `net8;net9;net10` in the `PackageTags` line:
```xml
    <PackageTags>mapper;source-generator;roslyn;analyzer;dto;diff;patch;net8;net9;net10;csharp</PackageTags>
```

**Step 2: Build**

Run: `dotnet build src/DeltaMapper.SourceGen -c Release`
Expected: SUCCESS

**Step 3: Commit**

```bash
git add src/DeltaMapper.SourceGen/DeltaMapper.SourceGen.csproj
git commit -m "build: update SourceGen package tags for multi-TFM visibility"
```

---

### Task 6: Multi-target TestFixtures

**Files:**
- Modify: `tests/DeltaMapper.TestFixtures/DeltaMapper.TestFixtures.csproj`

**Step 1: Update the .csproj**

Replace:
```xml
    <TargetFramework>net10.0</TargetFramework>
```

With:
```xml
    <TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
```

Remove `Nullable` and `ImplicitUsings` lines if present (now in `Directory.Build.props`).

**Step 2: Build**

Run: `dotnet build tests/DeltaMapper.TestFixtures -c Release`
Expected: SUCCESS

**Step 3: Commit**

```bash
git add tests/DeltaMapper.TestFixtures/DeltaMapper.TestFixtures.csproj
git commit -m "build: multi-target TestFixtures for net8.0/net9.0/net10.0"
```

---

### Task 7: Multi-target UnitTests

**Files:**
- Modify: `tests/DeltaMapper.UnitTests/DeltaMapper.UnitTests.csproj`

**Step 1: Update the .csproj**

Replace:
```xml
    <TargetFramework>net10.0</TargetFramework>
```

With:
```xml
    <TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
```

Remove `Nullable` and `ImplicitUsings` lines (now in `Directory.Build.props`).

Replace the unconditional DI PackageReference:
```xml
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="10.*" />
```

With conditional groups (keep other unconditional refs like xunit, FluentAssertions, Test.Sdk):
```xml
  </ItemGroup>
  <ItemGroup Condition="'$(TargetFramework)' == 'net8.0'">
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.*" />
  </ItemGroup>
  <ItemGroup Condition="'$(TargetFramework)' == 'net9.0'">
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.*" />
  </ItemGroup>
  <ItemGroup Condition="'$(TargetFramework)' == 'net10.0'">
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="10.*" />
  </ItemGroup>
```

**Step 2: Run tests on all TFMs**

Run: `dotnet test tests/DeltaMapper.UnitTests -c Release`
Expected: All tests pass on all three TFMs.

**Step 3: Commit**

```bash
git add tests/DeltaMapper.UnitTests/DeltaMapper.UnitTests.csproj
git commit -m "build: multi-target UnitTests for net8.0/net9.0/net10.0"
```

---

### Task 8: Multi-target IntegrationTests

**Files:**
- Modify: `tests/DeltaMapper.IntegrationTests/DeltaMapper.IntegrationTests.csproj`

**Step 1: Update the .csproj**

Replace:
```xml
    <TargetFramework>net10.0</TargetFramework>
```

With:
```xml
    <TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
```

Remove `Nullable` and `ImplicitUsings` lines (now in `Directory.Build.props`).

Replace the unconditional EFCore InMemory PackageReference:
```xml
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.*" />
```

With conditional groups (keep other unconditional refs):
```xml
  </ItemGroup>
  <ItemGroup Condition="'$(TargetFramework)' == 'net8.0'">
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.*" />
  </ItemGroup>
  <ItemGroup Condition="'$(TargetFramework)' == 'net9.0'">
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="9.*" />
  </ItemGroup>
  <ItemGroup Condition="'$(TargetFramework)' == 'net10.0'">
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.*" />
  </ItemGroup>
```

**Step 2: Run tests on all TFMs**

Run: `dotnet test tests/DeltaMapper.IntegrationTests -c Release`
Expected: All tests pass on all three TFMs.

**Step 3: Commit**

```bash
git add tests/DeltaMapper.IntegrationTests/DeltaMapper.IntegrationTests.csproj
git commit -m "build: multi-target IntegrationTests for net8.0/net9.0/net10.0"
```

---

### Task 9: Multi-target SourceGen.Tests

**Files:**
- Modify: `tests/DeltaMapper.SourceGen.Tests/DeltaMapper.SourceGen.Tests.csproj`

**Step 1: Update the .csproj**

Replace:
```xml
    <TargetFramework>net10.0</TargetFramework>
```

With:
```xml
    <TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
```

Remove `Nullable`, `ImplicitUsings`, and `LangVersion` lines (now in `Directory.Build.props`).

**Step 2: Run tests on all TFMs**

Run: `dotnet test tests/DeltaMapper.SourceGen.Tests -c Release`
Expected: All tests pass on all three TFMs.

**Step 3: Commit**

```bash
git add tests/DeltaMapper.SourceGen.Tests/DeltaMapper.SourceGen.Tests.csproj
git commit -m "build: multi-target SourceGen.Tests for net8.0/net9.0/net10.0"
```

---

### Task 10: Keep Benchmarks on .NET 10 only — clean up props

**Files:**
- Modify: `tests/DeltaMapper.Benchmarks/DeltaMapper.Benchmarks.csproj`

Benchmarks stay single-target `net10.0`. Only remove `Nullable`, `ImplicitUsings`, and `LangVersion` since they're now in `Directory.Build.props`.

**Step 1: Remove redundant properties**

Remove these lines from the PropertyGroup:
```xml
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
```

**Step 2: Build**

Run: `dotnet build tests/DeltaMapper.Benchmarks -c Release`
Expected: SUCCESS

**Step 3: Commit**

```bash
git add tests/DeltaMapper.Benchmarks/DeltaMapper.Benchmarks.csproj
git commit -m "build: clean up Benchmarks csproj — inherit from Directory.Build.props"
```

---

### Task 11: Clean up redundant properties from production .csproj files

**Files:**
- Modify: `src/DeltaMapper.Core/DeltaMapper.Core.csproj`
- Modify: `src/DeltaMapper.EFCore/DeltaMapper.EFCore.csproj`
- Modify: `src/DeltaMapper.OpenTelemetry/DeltaMapper.OpenTelemetry.csproj`
- Modify: `src/DeltaMapper.SourceGen/DeltaMapper.SourceGen.csproj`

Remove `Nullable`, `ImplicitUsings`, and `LangVersion` from each file since they're inherited from `Directory.Build.props`. These were already removed as part of the TFM changes in Tasks 2-5 — verify and clean up any that remain.

**Step 1: Verify and remove any remaining redundant properties**

Check each file and remove `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, `<LangVersion>latest</LangVersion>` if still present.

**Step 2: Full solution build**

Run: `dotnet build -c Release`
Expected: SUCCESS — entire solution builds clean.

**Step 3: Commit**

```bash
git add src/ tests/
git commit -m "build: remove redundant properties — inherited from Directory.Build.props"
```

---

### Task 12: Update CI workflow for multi-TFM

**Files:**
- Modify: `.github/workflows/ci.yml`

**Step 1: Update the workflow**

Replace the entire file with:

```yaml
name: CI

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

permissions:
  contents: read

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    strategy:
      matrix:
        tfm: [net8.0, net9.0, net10.0]

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET 8
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Setup .NET 9
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Setup .NET 10
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
          dotnet-quality: 'preview'

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build -c Release --no-restore

      - name: Test (${{ matrix.tfm }})
        run: dotnet test -c Release --no-build --framework ${{ matrix.tfm }} --logger trx --results-directory TestResults

      - name: Upload test results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: test-results-${{ matrix.tfm }}
          path: TestResults/*.trx
```

**Step 2: Validate YAML syntax**

Run: `python3 -c "import yaml; yaml.safe_load(open('.github/workflows/ci.yml'))"`
Expected: No errors.

**Step 3: Commit**

```bash
git add .github/workflows/ci.yml
git commit -m "ci: add multi-TFM test matrix with .NET 8/9/10 SDKs"
```

---

### Task 13: Update publish workflow for multi-TFM

**Files:**
- Modify: `.github/workflows/publish.yml`

**Step 1: Add .NET 8 and .NET 9 SDK setup steps**

Add these steps after the checkout and before the existing .NET 10 setup:

```yaml
      - name: Setup .NET 8
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Setup .NET 9
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'
```

No other changes needed — `dotnet build`, `dotnet test`, and `dotnet pack` will automatically handle all TFMs.

**Step 2: Validate YAML syntax**

Run: `python3 -c "import yaml; yaml.safe_load(open('.github/workflows/publish.yml'))"`
Expected: No errors.

**Step 3: Commit**

```bash
git add .github/workflows/publish.yml
git commit -m "ci: add .NET 8/9 SDKs to publish workflow for multi-TFM pack"
```

---

### Task 14: Update benchmarks workflow (add .NET 8/9 SDKs for restore)

**Files:**
- Modify: `.github/workflows/benchmarks.yml`

Even though benchmarks only run on .NET 10, the solution restore needs all SDKs since other projects now multi-target.

**Step 1: Add .NET 8 and .NET 9 SDK setup steps**

Add these steps after checkout and before the existing .NET 10 setup:

```yaml
      - name: Setup .NET 8
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Setup .NET 9
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'
```

**Step 2: Validate YAML syntax**

Run: `python3 -c "import yaml; yaml.safe_load(open('.github/workflows/benchmarks.yml'))"`
Expected: No errors.

**Step 3: Commit**

```bash
git add .github/workflows/benchmarks.yml
git commit -m "ci: add .NET 8/9 SDKs to benchmarks workflow for multi-TFM restore"
```

---

### Task 15: Full validation — build, test, pack

**Step 1: Clean and rebuild the entire solution**

Run: `dotnet clean && dotnet build -c Release`
Expected: SUCCESS — all projects build for all TFMs.

**Step 2: Run all tests on all TFMs**

Run: `dotnet test -c Release --no-build`
Expected: All tests pass on net8.0, net9.0, and net10.0.

**Step 3: Pack and verify NuGet contents**

Run: `dotnet pack -c Release --no-build`
Then verify the Core package has all three TFM folders:
Run: `unzip -l src/DeltaMapper.Core/bin/Release/DeltaMapper.*.nupkg | grep lib/`
Expected: `lib/net8.0/`, `lib/net9.0/`, `lib/net10.0/` folders present.

**Step 4: Commit (if any fixes were needed)**

```bash
git add -A
git commit -m "build: final multi-TFM validation fixes"
```
