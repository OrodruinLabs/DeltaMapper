---
id: TASK-022
title: GeneratedMapRegistry in DeltaMapper.Core
status: READY
depends_on: []
wave: 1
files_to_create:
  - src/DeltaMapper.Core/Runtime/GeneratedMapRegistry.cs
files_to_modify:
  - src/DeltaMapper.Core/Configuration/MapperConfiguration.cs
  - src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs
  - src/DeltaMapper.Core/Properties/AssemblyInfo.cs
acceptance_criteria:
  - GeneratedMapRegistry exposes static Register<TSrc,TDst>(Action<TSrc,TDst>) and TryGet methods with thread-safe storage
  - MapperConfiguration.ExecuteCore checks GeneratedMapRegistry BEFORE the compiled expression fallback
  - Existing 91 unit tests continue to pass with zero regressions
---

- **Status**: READY

**Retry count**: 0/3

## Description

Add `GeneratedMapRegistry` to `DeltaMapper.Core/Runtime/` — this is the runtime bridge that source-generated code registers into via `[ModuleInitializer]`.

### GeneratedMapRegistry Design

```csharp
namespace DeltaMapper.Runtime;

public static class GeneratedMapRegistry
{
    // Thread-safe: populated by [ModuleInitializer] before Main() runs
    private static readonly Dictionary<(Type, Type), Delegate> _registry = new();

    public static void Register<TSrc, TDst>(Action<TSrc, TDst> mapAction) { ... }
    public static bool TryGet<TSrc, TDst>(out Action<TSrc, TDst>? mapAction) { ... }
    public static bool TryGet(Type srcType, Type dstType, out Delegate? mapAction) { ... }
}
```

### MapperConfiguration Integration

Modify `ExecuteCore` in `MapperConfiguration.cs`:
1. Check `GeneratedMapRegistry.TryGet(srcType, dstType, out var generatedAction)` first
2. If found, create destination (or use existing), invoke the generated delegate, return
3. If not found, fall back to existing `_registry` FrozenDictionary lookup

### InternalsVisibleTo

Add `InternalsVisibleTo("DeltaMapper.SourceGen.Tests")` to `AssemblyInfo.cs` so the test project can verify internal state.

## Pattern Reference

Follow existing `CompiledMap.cs` pattern at `src/DeltaMapper.Core/Runtime/CompiledMap.cs` for namespace and style conventions.

## Test Requirements

Run `dotnet test` — all 91 existing tests must pass. Add 2-3 unit tests in the existing test project verifying:
- `GeneratedMapRegistry.Register` + `TryGet` round-trips correctly
- `MapperConfiguration` uses a generated delegate when one is registered (mock via direct `Register` call)

## Traces To

docs/DELTAMAP_PLAN.md section 3.3 (Generator Output — GeneratedMapRegistry and MapperConfiguration fallback)
