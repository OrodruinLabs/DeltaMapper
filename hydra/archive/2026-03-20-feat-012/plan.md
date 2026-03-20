# Plan — v1.0.0 Release

## Objective

Promote DeltaMapper from 0.2.0-alpha to stable 1.0.0 across all four NuGet packages via an RC soak period. API audit → docs → test hardening → RC publish.

## Status Summary

| Status | Count |
|--------|-------|
| DONE   | 5     |
| TOTAL  | 5     |

## Recovery Pointer

**Next**: Objective complete — all tasks done, RC published through v1.0.0-rc.3
**State**: All PRs merged, all tags/releases created
**Last updated**: 2026-03-20

## Tasks

| ID | Title | Status | Wave | Branch | PR Target | Depends On |
|----|-------|--------|------|--------|-----------|------------|
| TASK-053 | API surface audit (XML docs + namespace verification) | DONE | 1 | `feat/FEAT-012/TASK-053` | main | -- | PR #20 (merged) |
| TASK-054 | Documentation pass (README, NUGET_README, CHANGELOG) | DONE | 1 | `feat/FEAT-012/TASK-054` | main | -- | PR #21 (merged) |
| TASK-055 | Test hardening (cross-feature, edge cases, EFCore) | DONE | 1 | `feat/FEAT-012/TASK-055` | main | -- | PR #22 (merged) |
| TASK-056 | Version bump to 1.0.0-rc.1 + full verification | DONE | 2 | `feat/FEAT-012/TASK-056` | main | TASK-053, TASK-054, TASK-055 | Superseded: at rc.3 |
| TASK-057 | Tag + GitHub pre-release for RC | DONE | 3 | -- | main | TASK-056 | Tags rc.1/rc.2/rc.3 + releases exist |
