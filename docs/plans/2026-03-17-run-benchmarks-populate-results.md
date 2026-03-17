# Run Benchmarks & Populate BENCHMARKS.md

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Run the BenchmarkDotNet suite, capture real performance numbers, and replace all `<pending>` placeholders in BENCHMARKS.md with actual data. Add a CI workflow to run benchmarks on-demand.

**Architecture:** Run benchmarks locally via `dotnet run -c Release`, parse BenchmarkDotNet's markdown output from `BenchmarkDotNet.Artifacts/results/`, and use a shell script to update BENCHMARKS.md. Add a GitHub Actions workflow with `workflow_dispatch` for on-demand benchmark runs.

**Tech Stack:** BenchmarkDotNet, .NET 10, GitHub Actions, shell scripting

---

### Task 1: Verify benchmarks compile and source-gen path works

**Files:**
- Check: `tests/DeltaMapper.Benchmarks/Models/BenchmarkModels.cs`
- Check: `tests/DeltaMapper.Benchmarks/Benchmarks/FlatObjectBenchmark.cs`

**Step 1: Build the benchmark project**

Run: `dotnet build tests/DeltaMapper.Benchmarks/ -c Release`
Expected: 0 errors (warnings about AutoMapper vulnerability are OK)

**Step 2: Smoke-test the flat benchmark with a short dry run**

Run: `cd tests/DeltaMapper.Benchmarks && dotnet run -c Release -- --job Dry --filter "*FlatObject*"`
Expected: BenchmarkDotNet runs a quick dry-run (no warmup/measurement, just verifies all 5 methods execute without throwing). If the source-gen path throws `DeltaMapperException: No mapping registered`, the `[ModuleInitializer]` isn't firing — fix by adding a runtime profile fallback.

**Step 3: If dry run fails for source-gen, fix the setup**

In each benchmark's `[GlobalSetup]`, change the source-gen mapper from:
```csharp
_deltaMapperSourceGen = DmConfig.Create(_ => { }).CreateMapper();
```
to:
```csharp
_deltaMapperSourceGen = DmConfig.Create(cfg => cfg.AddProfile<FlatRuntimeProfile>()).CreateMapper();
```
This still uses generated delegates when available (GeneratedMapRegistry is checked first) but falls back to runtime if not.

Apply to all 4 benchmark files if needed.

**Step 4: Re-run dry test**

Run: `cd tests/DeltaMapper.Benchmarks && dotnet run -c Release -- --job Dry --filter "*"`
Expected: All 4 benchmark classes pass dry-run (19 methods total: 5+5+5+4)

**Step 5: Commit if any fixes were needed**

```bash
git add tests/DeltaMapper.Benchmarks/
git commit -m "fix: benchmark source-gen path fallback for GeneratedMapRegistry"
```

---

### Task 2: Run full benchmark suite and capture results

**Files:**
- Output: `tests/DeltaMapper.Benchmarks/BenchmarkDotNet.Artifacts/results/`

**Step 1: Run the full benchmark suite**

Run:
```bash
cd tests/DeltaMapper.Benchmarks
dotnet run -c Release -- --filter "*" --exporters GitHub
```

This will take 5-15 minutes depending on hardware. BenchmarkDotNet will:
- Warm up each method
- Run multiple iterations
- Produce markdown tables in `BenchmarkDotNet.Artifacts/results/`

Expected: Console output showing Mean, Error, StdDev, Gen0, Allocated for each benchmark method. GitHub exporter produces `.md` files.

**Step 2: Verify results files exist**

Run: `ls tests/DeltaMapper.Benchmarks/BenchmarkDotNet.Artifacts/results/*.md`
Expected: One `.md` file per benchmark class (4 files)

**Step 3: Capture environment info from BenchmarkDotNet output**

The console output includes environment info (OS, CPU, .NET SDK, BDN version). Copy this for BENCHMARKS.md.

---

### Task 3: Update BENCHMARKS.md with real data

**Files:**
- Modify: `BENCHMARKS.md`

**Step 1: Read each results file from BenchmarkDotNet.Artifacts/results/**

Each file contains a markdown table with columns: Method, Mean, Error, StdDev, Gen0, Allocated.

**Step 2: Replace each placeholder table in BENCHMARKS.md**

For each of the 4 scenarios (FlatObject, NestedObject, Collection, Patch):
1. Read the corresponding BDN results `.md` file
2. Extract the table rows
3. Replace the `<pending>` rows in BENCHMARKS.md with real data

**Step 3: Fill in the Environment section**

Replace:
```
| OS | (your OS) |
| CPU | (your CPU) |
| .NET SDK | (version) |
| BenchmarkDotNet | (version) |
```
with actual values from BenchmarkDotNet output header.

**Step 4: Update README.md inline table**

Replace the `<pending>` values in the README.md Benchmarks section with the Mean and Allocated values from the Flat Object scenario (representative headline number).

**Step 5: Verify the document renders correctly**

Visually inspect BENCHMARKS.md for formatting issues.

**Step 6: Commit**

```bash
git add BENCHMARKS.md README.md
git commit -m "docs: populate BENCHMARKS.md with real benchmark results"
```

---

### Task 4: Add on-demand benchmark CI workflow

**Files:**
- Create: `.github/workflows/benchmarks.yml`

**Step 1: Write the failing test — verify no benchmark workflow exists**

Run: `ls .github/workflows/benchmarks.yml`
Expected: No such file

**Step 2: Create the workflow**

```yaml
name: Benchmarks

on:
  workflow_dispatch:
    inputs:
      filter:
        description: 'BenchmarkDotNet filter (e.g. "*FlatObject*" or "*")'
        required: false
        default: '*'

permissions:
  contents: read

jobs:
  benchmark:
    runs-on: ubuntu-latest
    timeout-minutes: 30

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET 10
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
          dotnet-quality: 'preview'

      - name: Run benchmarks
        working-directory: tests/DeltaMapper.Benchmarks
        run: dotnet run -c Release -- --filter "$FILTER" --exporters GitHub
        env:
          FILTER: ${{ inputs.filter }}

      - name: Upload results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: benchmark-results
          path: tests/DeltaMapper.Benchmarks/BenchmarkDotNet.Artifacts/
```

**Step 3: Verify workflow YAML is valid**

Run: `python3 -c "import yaml; yaml.safe_load(open('.github/workflows/benchmarks.yml'))"`
Expected: No errors

**Step 4: Commit**

```bash
git add .github/workflows/benchmarks.yml
git commit -m "ci: add on-demand benchmark workflow"
```

---

### Task 5: Add BenchmarkDotNet.Artifacts to .gitignore

**Files:**
- Modify: `.gitignore`

**Step 1: Check if already ignored**

Run: `grep BenchmarkDotNet .gitignore`
Expected: Should already have `BenchmarkDotNet.Artifacts/` (added in FEAT-006)

**Step 2: If not present, add it**

Add under the `## Benchmarks` section:
```
BenchmarkDotNet.Artifacts/
```

**Step 3: Commit if changed**

```bash
git add .gitignore
git commit -m "chore: ignore BenchmarkDotNet artifacts"
```

---

### Task 6: Push and create PR

**Step 1: Push branch**

```bash
git push -u origin feat/FEAT-006-benchmark-results
```

**Step 2: Create PR**

```bash
gh pr create --base master --title "Populate BENCHMARKS.md with real benchmark results" \
  --body "Runs the full BenchmarkDotNet suite and replaces all <pending> placeholders with actual performance data. Adds on-demand CI workflow for future benchmark runs."
```
