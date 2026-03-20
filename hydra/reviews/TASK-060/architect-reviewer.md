# Architect Review: TASK-060 — Update CI/CD Workflows for Multi-TFM

**Reviewer**: Architect Reviewer Agent
**Date**: 2026-03-20
**Diff**: `hydra/reviews/TASK-060/diff.patch`

---

## Architectural Assessment

### CI Strategy: Matrix vs Parallel Steps

The `ci.yml` change introduces a `strategy.matrix` with `tfm: [net8.0, net9.0, net10.0]`. This is architecturally correct — each matrix entry runs as an independent job, providing:
1. True parallel execution (GitHub Actions runs matrix legs concurrently by default)
2. Independent failure isolation (a net8.0 failure does not mask a net9.0 failure)
3. Separate artifact upload per TFM (`test-results-${{ matrix.tfm }}`) preventing artifact name collisions

Running `dotnet test --framework ${{ matrix.tfm }}` is the correct approach — it exercises only one TFM per job, avoiding redundant re-runs of the same TFM in a combined run.

### SDK Installation Strategy

Installing all three SDKs (8.0.x, 9.0.x, 10.0.x) in a sequential `setup-dotnet` step chain is the correct approach for multi-TFM builds. The `actions/setup-dotnet@v4` action supports multiple sequential installs — each SDK is added to the PATH without removing previous ones. This allows `dotnet build` to restore and compile all TFMs from a single build invocation, and then each matrix leg's `dotnet test --framework` selects the appropriate runtime.

### Publish Workflow

`publish.yml` installs all three SDKs before the pack/push step. This is correct — `dotnet pack` for a multi-TFM project requires all three SDKs to be present to compile all TFM-specific assemblies before packaging them into a single `.nupkg`.

### Benchmarks Workflow

`benchmarks.yml` installs all three SDKs even though Benchmarks targets only `net10.0`. This is slightly over-provisioned but harmless — the multi-target production packages (which Benchmarks references) require the other SDKs to restore. The extra SDK installations add ~30 seconds to the benchmark job startup, which is acceptable for an on-demand workflow.

---

## Final Verdict

**APPROVED**

The CI architecture is correct and idiomatic: matrix strategy for independent per-TFM test validation, artifact name deduplication, sequential SDK installation for multi-TFM builds, and correct `--framework` flag scoping. No architectural concerns.
