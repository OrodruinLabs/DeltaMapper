# Architect Review: TASK-058 — Directory.Build.props + Multi-Target Production Packages

**Reviewer**: Architect Reviewer Agent
**Date**: 2026-03-20
**Diff**: `hydra/reviews/TASK-058/diff.patch`

---

## Architectural Assessment

### Design Pattern: Centralized Build Configuration

`Directory.Build.props` is the idiomatic MSBuild mechanism for centralizing properties across a multi-project repo. Placing it at the repo root is correct — MSBuild discovers it by walking up from each project directory and applies it automatically. The properties chosen (`Nullable`, `ImplicitUsings`, `LangVersion`) are the right candidates: they are project-agnostic compiler flags that should be uniform everywhere.

### Multi-TFM Strategy

Using `TargetFrameworks` (plural) with `net8.0;net9.0;net10.0` is the correct MSBuild multi-targeting syntax. The order (ascending by version) is conventional and correct. The conditional `ItemGroup` pattern:

```xml
<ItemGroup Condition="'$(TargetFramework)' == 'net8.0'">
  <PackageReference Include="..." Version="8.*" />
</ItemGroup>
```

is the established MSBuild idiom for per-TFM dependency binding. Each package reference resolves to the correct major version aligned with the target runtime, ensuring no version mismatches between the runtime BCL and the framework libraries.

### SourceGen Isolation

Keeping `DeltaMapper.SourceGen` on `netstandard2.0` is architecturally sound. Roslyn analyzers/source generators must target `netstandard2.0` to be loadable by the compiler host regardless of the consuming project's TFM. Multi-targeting SourceGen would break the analyzer contract.

### Package Tag Updates

Adding `net8;net9;net10` to `PackageTags` (while retaining `net10`) is correct for NuGet discoverability. The tags are used as search facets on nuget.org.

### Findings

- No architectural concerns.
- The `Directory.Build.props` scope (repo root) correctly covers all 9 projects without requiring opt-in.
- The approach has no lock-in risk — reverting to a single TFM only requires changing `TargetFrameworks` back to singular.

---

## Final Verdict

**APPROVED**

The multi-target architecture is clean, idiomatic, and correctly scoped. Centralized `Directory.Build.props` reduces drift, per-TFM conditional `ItemGroup` blocks are the correct MSBuild pattern, and `SourceGen` isolation on `netstandard2.0` is maintained. No structural concerns.
