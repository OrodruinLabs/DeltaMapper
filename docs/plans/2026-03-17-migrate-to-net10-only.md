# Migrate to .NET 10 / C# 14 — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Drop net8.0/net9.0, target net10.0 only, and adopt C# 14 language features throughout the codebase.

**Architecture:** Retarget both projects to net10.0, upgrade packages, then refactor source code to use C# 14 features: extension members (replace static extension class), `field` keyword (backed properties), null-conditional assignment, and collection expressions. All 79 tests must continue passing after each task.

**Tech Stack:** .NET 10 SDK (10.0.103), C# 14, xunit, FluentAssertions

---

## C# 14 Features Applicable to DeltaMapper

| Feature | Where it applies |
|---|---|
| **Extension members** | `ServiceCollectionExtensions.AddDeltaMapper` → new `extension` block syntax |
| **Null-conditional assignment** | `MapperConfigurationBuilder` — simplify null-check-then-assign patterns in property mapping |
| **`field` keyword** | `MemberConfiguration`, `MemberOptions` — eliminate boilerplate backing fields |
| **Collection expressions** | Replace `new List<T>()`, `Array.Empty<T>()`, `new Dictionary<>()` with `[]` syntax |
| **Primary constructors** (C# 12, but not yet used) | `CompiledMap`, `MapperContext`, `Mapper`, `MappingPipeline` — eliminate boilerplate constructor + field |

---

### Task 1: Retarget Projects to net10.0

**Files:**
- Modify: `src/DeltaMapper.Core/DeltaMapper.Core.csproj`
- Modify: `tests/DeltaMapper.UnitTests/DeltaMapper.UnitTests.csproj`

**Step 1: Update Core csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
    <RootNamespace>DeltaMapper</RootNamespace>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
    <PackageId>DeltaMapper</PackageId>
    <Version>0.1.0</Version>
    <Description>Fast, diff-aware .NET object mapper. MIT licensed.</Description>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.*" />
  </ItemGroup>
</Project>
```

Changes: `TargetFrameworks` (plural) → `TargetFramework` (singular) = `net10.0`. DI abstractions `8.*` → `10.*`.

**Step 2: Update Test csproj**

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
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="10.*" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\DeltaMapper.Core\DeltaMapper.Core.csproj" />
  </ItemGroup>
</Project>
```

Changes: `net9.0` → `net10.0`. DI `8.*` → `10.*`.

**Step 3: Clean, restore, build, test**

Run: `dotnet clean && dotnet build -c Release`
Expected: Build succeeds on net10.0 only.

Run: `dotnet test -c Release --no-build`
Expected: 79 passed, 0 failed.

**Step 4: Commit**

```bash
git add src/DeltaMapper.Core/DeltaMapper.Core.csproj tests/DeltaMapper.UnitTests/DeltaMapper.UnitTests.csproj
git commit -m "build: retarget to net10.0 only, upgrade packages to 10.x"
```

---

### Task 2: Primary Constructors — Eliminate Constructor Boilerplate

**Files:**
- Modify: `src/DeltaMapper.Core/MapperConfiguration.cs`
- Modify: `src/DeltaMapper.Core/Mapper.cs`
- Modify: `src/DeltaMapper.Core/MapperContext.cs`
- Modify: `src/DeltaMapper.Core/Middleware/MappingPipeline.cs`

Primary constructors (C# 12) capture parameters as fields without explicit declaration. We haven't used them yet.

**Step 1: Refactor CompiledMap**

Before:
```csharp
internal sealed class CompiledMap
{
    private readonly Func<object, object?, MapperContext, object> _mapFunc;

    internal CompiledMap(
        Func<object, object?, MapperContext, object> mapFunc)
    {
        _mapFunc = mapFunc;
    }

    internal object Execute(object source, object? existingDest, MapperContext ctx)
    {
        return _mapFunc(source, existingDest, ctx);
    }
}
```

After:
```csharp
internal sealed class CompiledMap(Func<object, object?, MapperContext, object> mapFunc)
{
    internal object Execute(object source, object? existingDest, MapperContext ctx)
        => mapFunc(source, existingDest, ctx);
}
```

**Step 2: Refactor Mapper**

Before:
```csharp
public sealed class Mapper : IMapper
{
    private readonly MapperConfiguration _config;

    internal Mapper(MapperConfiguration config) => _config = config;
    // ... methods use _config
}
```

After:
```csharp
public sealed class Mapper(MapperConfiguration config) : IMapper
{
    // ... methods use config directly (captured parameter)
}
```

Replace all `_config` references with `config` in the method bodies.

**Step 3: Refactor MapperContext**

Before:
```csharp
public sealed class MapperContext
{
    internal MapperConfiguration Config { get; }
    private readonly Dictionary<object, object> _visited = new(ReferenceEqualityComparer.Instance);

    internal MapperContext(MapperConfiguration config) => Config = config;
```

After:
```csharp
public sealed class MapperContext(MapperConfiguration config)
{
    internal MapperConfiguration Config { get; } = config;
    private readonly Dictionary<object, object> _visited = new(ReferenceEqualityComparer.Instance);
```

Note: `Config` must remain a property (accessed externally), so we use `= config` initializer.

**Step 4: Refactor MappingPipeline**

Before:
```csharp
internal sealed class MappingPipeline
{
    private readonly IReadOnlyList<IMappingMiddleware> _middlewares;

    internal MappingPipeline(IReadOnlyList<IMappingMiddleware> middlewares)
    {
        _middlewares = middlewares;
    }
```

After:
```csharp
internal sealed class MappingPipeline(IReadOnlyList<IMappingMiddleware> middlewares)
{
```

Replace `_middlewares` with `middlewares` in the `Execute` method body.

**Step 5: Build and test**

Run: `dotnet build -c Release && dotnet test -c Release --no-build`
Expected: 79 passed, 0 failed.

**Step 6: Commit**

```bash
git add src/DeltaMapper.Core/
git commit -m "refactor: adopt primary constructors (C# 12+) across core types"
```

---

### Task 3: Collection Expressions — Modern Initialization Syntax

**Files:**
- Modify: `src/DeltaMapper.Core/MappingProfile.cs`
- Modify: `src/DeltaMapper.Core/MapperConfiguration.cs`
- Modify: `src/DeltaMapper.Core/MapperConfigurationBuilder.cs`

Collection expressions (`[]`) replace `new List<T>()`, `Array.Empty<T>()`, `new()` for collections.

**Step 1: MappingProfile.cs**

Before:
```csharp
internal List<TypeMapConfiguration> TypeMaps { get; } = new();
```
After:
```csharp
internal List<TypeMapConfiguration> TypeMaps { get; } = [];
```

Same for `MemberConfigurations` in `TypeMapConfiguration`:
```csharp
public List<MemberConfiguration> MemberConfigurations { get; } = [];
```

**Step 2: MapperConfiguration.cs — empty config constructor**

Before:
```csharp
_registry = new Dictionary<(Type, Type), CompiledMap>().ToFrozenDictionary();
_pipeline = new MappingPipeline(Array.Empty<IMappingMiddleware>());
```
After:
```csharp
_registry = new Dictionary<(Type, Type), CompiledMap>().ToFrozenDictionary();
_pipeline = new MappingPipeline([]);
```

**Step 3: MapperConfigurationBuilder.cs**

Before:
```csharp
private readonly List<MappingProfile> _profiles = new();
private readonly List<IMappingMiddleware> _middlewares = new();
```
After:
```csharp
private readonly List<MappingProfile> _profiles = [];
private readonly List<IMappingMiddleware> _middlewares = [];
```

Also update any `new List<>()` inside method bodies:
- `var allTypeMaps = new List<TypeMapConfiguration>();` → `List<TypeMapConfiguration> allTypeMaps = [];`
- `var reverseMaps = new List<TypeMapConfiguration>();` → `List<TypeMapConfiguration> reverseMaps = [];`
- `var assignments = new List<Action<object, object, MapperContext>>();` → `List<Action<object, object, MapperContext>> assignments = [];`
- `var items = new List<object>();` → `List<object> items = [];`
- Similar for `paramResolvers`, `initOnlyAssignments` in `CompileConstructorMap`.

**Step 4: Build and test**

Run: `dotnet build -c Release && dotnet test -c Release --no-build`
Expected: 79 passed, 0 failed.

**Step 5: Commit**

```bash
git add src/DeltaMapper.Core/
git commit -m "refactor: adopt collection expressions (C# 12+)"
```

---

### Task 4: Null-Conditional Assignment

**Files:**
- Modify: `src/DeltaMapper.Core/MapperConfigurationBuilder.cs`

C# 14 allows `obj?.Property = value;` — only assigns if left side is non-null. Look for patterns like `if (x != null) x.Prop = value;` or where we check null before invoking.

**Step 1: Scan for applicable patterns in MapperConfigurationBuilder.cs**

The BeforeMap/AfterMap invocations in the compiled delegates:

Before:
```csharp
beforeMap?.Invoke(src, dst);
```

This already uses null-conditional invocation, which is fine. The null-conditional *assignment* applies to patterns like:

```csharp
if (srcValue == null)
{
    dstPropCaptured.SetValue(dst, null);
    return;
}
```

These use `SetValue` (reflection), not direct property assignment, so null-conditional assignment doesn't help here — it's for `obj?.Prop = value` syntax on actual properties, not reflection.

**Step 2: Check test files and other source files for applicable patterns**

Scan all `.cs` files for `if (x != null) { x.Something = ...` or similar. If none found, skip this task — don't force the feature where it doesn't apply.

**Step 3: Build and test** (if any changes made)

Run: `dotnet build -c Release && dotnet test -c Release --no-build`
Expected: 79 passed.

**Step 4: Commit** (only if changes made)

```bash
git commit -am "refactor: adopt null-conditional assignment where applicable"
```

---

### Task 5: Extension Members — Modernize ServiceCollectionExtensions

**Files:**
- Modify: `src/DeltaMapper.Core/ServiceCollectionExtensions.cs`

C# 14 extension members use `extension(Type)` blocks instead of static classes with `this` parameters. This is the signature C# 14 feature.

**Step 1: Rewrite ServiceCollectionExtensions.cs**

Before:
```csharp
using Microsoft.Extensions.DependencyInjection;

namespace DeltaMapper;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDeltaMapper(
        this IServiceCollection services,
        Action<MapperConfigurationBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new MapperConfigurationBuilder();
        configure(builder);
        var config = builder.Build();
        services.AddSingleton(config);
        services.AddSingleton<IMapper>(sp => new Mapper(sp.GetRequiredService<MapperConfiguration>()));
        return services;
    }
}
```

After:
```csharp
using Microsoft.Extensions.DependencyInjection;

namespace DeltaMapper;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers DeltaMapper services: MapperConfiguration (singleton) and IMapper (singleton).
        /// </summary>
        public IServiceCollection AddDeltaMapper(Action<MapperConfigurationBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            var builder = new MapperConfigurationBuilder();
            configure(builder);
            var config = builder.Build();
            services.AddSingleton(config);
            services.AddSingleton<IMapper>(sp => new Mapper(sp.GetRequiredService<MapperConfiguration>()));
            return services;
        }
    }
}
```

Key differences:
- `extension(IServiceCollection services)` block replaces `this IServiceCollection services` parameter
- `services` is now the receiver — no need for null guard on it (compiler enforces non-null receiver)
- `configure` still needs its null guard

**IMPORTANT:** If the compiler rejects this syntax (extension members in static classes may have constraints), fall back to keeping the old syntax. Extension members are new and may have edge cases with returning `this`. Test the build carefully.

**Step 2: Build and test**

Run: `dotnet build -c Release`

If build fails with extension member syntax errors, **revert** and keep the traditional `this` parameter style. Document why in the commit message.

If build succeeds:
Run: `dotnet test -c Release --no-build`
Expected: 79 passed (DI tests DI-01 through DI-05 must pass).

**Step 3: Commit**

```bash
git add src/DeltaMapper.Core/ServiceCollectionExtensions.cs
git commit -m "refactor: adopt C# 14 extension members for DI registration"
```

---

### Task 6: Update Documentation and Push

**Files:**
- Modify: `docs/DELTAMAP_PLAN.md`

**Step 1: Update target framework references**

In the "Target Framework & Dependencies" section (~line 70):
- `**Target**: net8.0 (minimum)` → `**Target**: net10.0`
- `**Multi-target**: net8.0;net9.0` → delete this line
- csproj template (~line 84): `<TargetFrameworks>net8.0;net9.0</TargetFrameworks>` → `<TargetFramework>net10.0</TargetFramework>`

**Step 2: Final build and test verification**

Run: `dotnet clean && dotnet build -c Release && dotnet test -c Release --no-build`
Expected: 79 passed, 0 failed, 0 warnings.

Run: `dotnet pack src/DeltaMapper.Core -c Release --no-build`
Expected: `.nupkg` targeting net10.0.

**Step 3: Commit and push**

```bash
git add -A
git commit -m "build: complete migration to .NET 10 / C# 14"
git push
```
