# DeltaMapper Performance Parity with Mapperly

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Close the remaining 18ns gap between DeltaMapper SourceGen (24.6ns) and Mapperly (6.6ns) on flat object mapping.

**Architecture:** Three targeted optimizations: (1) emit object-initializer pattern in generated factory methods for better JIT inlining, (2) cache the fast-path routing decision per type pair to eliminate double dictionary lookups, (3) generate public static `Map` methods users can call directly for zero-overhead scenarios.

**Tech Stack:** C# source generators (Roslyn IIncrementalGenerator), System.Linq.Expressions, BenchmarkDotNet

---

### Task 1: Emit Object Initializer Pattern in Factory Methods

The generated factory currently does two-step creation (`new Dst(); dst.Prop = src.Prop;`). Change to single-expression object initializer (`new Dst { Prop = src.Prop }`) which the JIT can inline and vectorize better.

**Files:**
- Modify: `src/DeltaMapper.SourceGen/EmitHelper.cs:62-98`
- Test: `tests/DeltaMapper.SourceGen.Tests/MapperGeneratorFlatTests.cs`

**Step 1: Write the failing test**

In `tests/DeltaMapper.SourceGen.Tests/MapperGeneratorFlatTests.cs`, add a test that verifies the factory method uses object initializer syntax:

```csharp
[Fact]
public void Factory_EmitsObjectInitializer()
{
    // Arrange — use a simple flat pair
    var source = @"
using DeltaMapper;
[GenerateMap(typeof(Src), typeof(Dst))]
public partial class TestProfile { }
public class Src { public int Id { get; set; } }
public class Dst { public int Id { get; set; } }
";
    // Act — run the generator
    var (diagnostics, trees) = RunGenerator(source);

    // Assert — factory method should use "=> new Dst" pattern
    var factoryTree = trees.First(t => t.Contains("Create_Src_To_Dst"));
    factoryTree.Should().Contain("=> new Dst");
    factoryTree.Should().NotContain("var dst = new Dst()");
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/DeltaMapper.SourceGen.Tests/ --filter "Factory_EmitsObjectInitializer" -v`
Expected: FAIL (current code emits `var dst = new Dst()` pattern)

**Step 3: Implement — change EmitFactoryMethod**

In `src/DeltaMapper.SourceGen/EmitHelper.cs`, modify `EmitFactoryMethod()` to emit:

For **simple flat types** (all properties are direct assignments with no nested/collection logic):
```csharp
private static DstType Create_Src_To_Dst(SrcType src) => new()
{
    Id = src.Id,
    Name = src.Name,
    // ...
};
```

For **complex types** (nested objects, collections) keep the two-step pattern since object initializer can't contain control flow.

This requires a new helper method `CanUseObjectInitializer()` that checks if all assignment lines are simple `dst.Prop = src.Prop;` patterns (no `if`, no `.Select()`, no nested `Map_` calls).

Add `BuildInitializerLines()` that emits `Prop = src.Prop,` instead of `dst.Prop = src.Prop;`.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/DeltaMapper.SourceGen.Tests/ --filter "Factory_EmitsObjectInitializer" -v`
Expected: PASS

**Step 5: Run all tests**

Run: `dotnet test -c Release`
Expected: 145 tests pass

**Step 6: Commit**

```bash
git add src/DeltaMapper.SourceGen/EmitHelper.cs tests/DeltaMapper.SourceGen.Tests/
git commit -m "perf: emit object initializer in factory methods for better JIT inlining"
```

---

### Task 2: Cache Fast-Path Routing Decision per Type Pair

Currently `Map<TSource, TDest>()` does TWO lookups every call: `HasMap()` (FrozenDictionary) + `TryGetFactory()` (ConcurrentDictionary). Cache the result so subsequent calls for the same type pair do ONE lookup.

**Files:**
- Modify: `src/DeltaMapper.Core/Runtime/Mapper.cs`
- Test: `tests/DeltaMapper.UnitTests/MapperTests.cs`

**Step 1: Write the failing test**

```csharp
[Fact]
public void Map_SourceGen_SecondCallSameSpeed()
{
    // This is a behavioral test — ensure caching doesn't change results
    var config = MapperConfiguration.Create(cfg => { });
    var mapper = config.CreateMapper();

    // Assuming FlatGenProfile [GenerateMap] is registered via ModuleInitializer
    // Call twice — both should return correct results
    var source = new FlatSource { Id = 1, Name = "Test" };
    var result1 = mapper.Map<FlatSource, FlatDest>(source);
    var result2 = mapper.Map<FlatSource, FlatDest>(source);

    result1.Id.Should().Be(1);
    result2.Id.Should().Be(1);
}
```

**Step 2: Run test to verify it passes (this is a correctness test)**

Run: `dotnet test tests/DeltaMapper.UnitTests/ --filter "Map_SourceGen_SecondCallSameSpeed" -v`
Expected: PASS (existing code already works)

**Step 3: Implement the cache**

In `Mapper.cs`, add a `ConcurrentDictionary` that caches the routing decision:

```csharp
private readonly ConcurrentDictionary<(Type, Type), Delegate?> _factoryCache = new();

public TDestination Map<TSource, TDestination>(TSource source)
{
    ArgumentNullException.ThrowIfNull(source);

    if (_fastPathEnabled)
    {
        var key = (typeof(TSource), typeof(TDestination));
        var cached = _factoryCache.GetOrAdd(key, static (k, config) =>
        {
            if (config.HasMap(k.Item1, k.Item2))
                return null; // Has compiled map — use full pipeline
            if (GeneratedMapRegistry.TryGetFactory(k.Item1, k.Item2, out var factory))
                return factory;
            return null;
        }, _config);

        if (cached is Func<TSource, TDestination> factory)
            return factory(source);
    }

    var ctx = new MapperContext(_config);
    return (TDestination)_config.Execute(source, typeof(TSource), typeof(TDestination), ctx);
}
```

Wait — `GetOrAdd` with the static lambda approach needs a non-generic `TryGetFactory`. We need to add a non-generic overload to `GeneratedMapRegistry` that returns `Delegate?` by type pair, OR we keep the generic version and use a different caching strategy.

**Better approach — use typed cache with lazy population:**

```csharp
// In Mapper, use a static generic class for per-type-pair caching (zero dictionary lookup after first call)
private static class FastPathCache<TSource, TDestination>
{
    public static Func<TSource, TDestination>? Factory;
    public static bool Resolved;
}

public TDestination Map<TSource, TDestination>(TSource source)
{
    ArgumentNullException.ThrowIfNull(source);

    if (_fastPathEnabled)
    {
        if (!FastPathCache<TSource, TDestination>.Resolved)
        {
            FastPathCache<TSource, TDestination>.Resolved = true;
            if (!_config.HasMap(typeof(TSource), typeof(TDestination))
                && GeneratedMapRegistry.TryGetFactory<TSource, TDestination>(out var f))
                FastPathCache<TSource, TDestination>.Factory = f;
        }

        if (FastPathCache<TSource, TDestination>.Factory is { } factory)
            return factory(source);
    }

    var ctx = new MapperContext(_config);
    return (TDestination)_config.Execute(source, typeof(TSource), typeof(TDestination), ctx);
}
```

**Problem:** Static generic class is shared across ALL Mapper instances, which breaks when different MapperConfigurations have different middleware settings.

**Final approach — instance-level typed dictionary:**

```csharp
private readonly ConcurrentDictionary<(Type, Type), object?> _factoryCache = new();

public TDestination Map<TSource, TDestination>(TSource source)
{
    ArgumentNullException.ThrowIfNull(source);

    if (_fastPathEnabled)
    {
        var key = (typeof(TSource), typeof(TDestination));
        if (!_factoryCache.TryGetValue(key, out var cached))
        {
            cached = !_config.HasMap(key.Item1, key.Item2)
                && GeneratedMapRegistry.TryGetFactory<TSource, TDestination>(out var f)
                ? (object)f : null;
            _factoryCache[key] = cached;
        }

        if (cached is Func<TSource, TDestination> factory)
            return factory(source);
    }

    var ctx = new MapperContext(_config);
    return (TDestination)_config.Execute(source, typeof(TSource), typeof(TDestination), ctx);
}
```

This does ONE `ConcurrentDictionary.TryGetValue` on the hot path (after first call). First call populates the cache.

**Step 4: Run all tests**

Run: `dotnet test -c Release`
Expected: 145 tests pass

**Step 5: Commit**

```bash
git add src/DeltaMapper.Core/Runtime/Mapper.cs tests/DeltaMapper.UnitTests/
git commit -m "perf: cache fast-path routing decision per type pair — single lookup"
```

---

### Task 3: Generate Public Static Map Methods

Generate user-callable `public static` methods alongside the private factory. This gives users a zero-overhead option that matches Mapperly's call pattern exactly.

**Files:**
- Modify: `src/DeltaMapper.SourceGen/EmitHelper.cs`
- Modify: `src/DeltaMapper.SourceGen/MapperGenerator.cs`
- Test: `tests/DeltaMapper.SourceGen.Tests/MapperGeneratorFlatTests.cs`

**Step 1: Write the failing test**

```csharp
[Fact]
public void Generator_EmitsPublicStaticMapMethod()
{
    var source = @"
using DeltaMapper;
[GenerateMap(typeof(Src), typeof(Dst))]
public partial class TestProfile { }
public class Src { public int Id { get; set; } public string Name { get; set; } }
public class Dst { public int Id { get; set; } public string Name { get; set; } }
";
    var (diagnostics, trees) = RunGenerator(source);

    // Should contain a public static method
    var factoryTree = trees.First(t => t.Contains("Create_Src_To_Dst"));
    factoryTree.Should().Contain("public static Dst Map(Src src)");
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/DeltaMapper.SourceGen.Tests/ --filter "Generator_EmitsPublicStaticMapMethod" -v`
Expected: FAIL

**Step 3: Implement**

In `EmitHelper.EmitFactoryMethod()`, add a public wrapper below the private factory:

```csharp
// At the end of the emitted class body, add:
{indent}    /// <summary>
{indent}    /// Maps <see cref="{srcFullName}"/> to <see cref="{dstFullName}"/> with zero overhead.
{indent}    /// Call this directly for maximum performance (bypasses IMapper pipeline).
{indent}    /// </summary>
{indent}    public static {dstFullName} Map({srcFullName} src) => {methodName}(src);
```

For profiles with multiple `[GenerateMap]` attributes, disambiguate:
```csharp
public static DstType Map(SrcType src) => Create_Src_To_Dst(src);
```

If there's only one pair, use `Map`. If multiple, use `MapSrcToDst` naming.

**Step 4: Run test**

Run: `dotnet test tests/DeltaMapper.SourceGen.Tests/ --filter "Generator_EmitsPublicStaticMapMethod" -v`
Expected: PASS

**Step 5: Run all tests + build**

Run: `dotnet build -c Release && dotnet test -c Release`
Expected: 0 errors, 145 tests pass

**Step 6: Commit**

```bash
git add src/DeltaMapper.SourceGen/ tests/DeltaMapper.SourceGen.Tests/
git commit -m "feat: generate public static Map methods for zero-overhead direct calls"
```

---

### Task 4: Add Direct-Call Benchmark

Add a new benchmark method that calls the generated static method directly (no IMapper) to prove parity with Mapperly.

**Files:**
- Modify: `tests/DeltaMapper.Benchmarks/Benchmarks/FlatObjectBenchmark.cs`

**Step 1: Add the benchmark method**

```csharp
[Benchmark]
public FlatDest DeltaMapper_DirectCall() => FlatGenProfile.Map(_source);
```

**Step 2: Build and dry-run**

Run: `cd tests/DeltaMapper.Benchmarks && dotnet run -c Release -- --job Dry --filter "*FlatObject*"`
Expected: All 6 methods execute without throwing

**Step 3: Run full benchmark for flat scenario**

Run: `cd tests/DeltaMapper.Benchmarks && dotnet run -c Release -- --filter "*FlatObject*" --exporters GitHub`
Expected: `DeltaMapper_DirectCall` should be ~6-8ns (matching Mapperly/hand-written)

**Step 4: Commit**

```bash
git add tests/DeltaMapper.Benchmarks/
git commit -m "bench: add direct-call benchmark to prove Mapperly parity"
```

---

### Task 5: Update BENCHMARKS.md and README.md with Final Results

**Files:**
- Modify: `BENCHMARKS.md`
- Modify: `README.md`

**Step 1: Run full benchmark suite**

Run: `cd tests/DeltaMapper.Benchmarks && dotnet run -c Release -- --filter "*" --exporters GitHub`

**Step 2: Update BENCHMARKS.md with new numbers**

Replace all tables with actual results. Add a "Direct Call" row for scenarios where it applies.

**Step 3: Update README.md inline table**

**Step 4: Commit**

```bash
git add BENCHMARKS.md README.md
git commit -m "docs: update benchmarks — SourceGen direct call matches Mapperly"
```

---

## Expected Results

| Optimization | Before | After | Savings |
|---|---|---|---|
| Object initializer pattern | 24.6ns | ~20ns | ~4-6ns |
| Cached routing decision | ~20ns | ~15ns | ~3-5ns |
| Direct static call (no IMapper) | ~15ns | ~7ns | ~8ns |

**Two performance tiers for users:**
1. `IMapper.Map<>()` — ~15ns, full feature set (middleware, hooks, DI)
2. `Profile.Map()` — ~7ns, zero overhead (matches Mapperly exactly)

## Implementation Order

| # | Task | Impact | Risk |
|---|------|--------|------|
| 1 | Object initializer pattern | -4-6ns | Low |
| 2 | Cached routing decision | -3-5ns | Low |
| 3 | Public static Map methods | -8ns (direct call) | Low |
| 4 | Direct-call benchmark | Proves parity | None |
| 5 | Update docs | — | None |
