# DeltaMapper — Full Implementation Plan

> A .NET 8+ object mapper built for speed, PATCH/diff workflows, and zero licensing drama.  
> MIT licensed. NuGet: `DeltaMapper`.

---

## Vision

DeltaMapper is the mapper that fills the gaps every other library ignores:

- **Fastest runtime mapper** via expression-compiled delegates + FrozenDictionary
- **Optional source generation** for Mapperly-level zero-overhead paths
- **`MappingDiff<T>`** — the first mapper to return a structured change set alongside the mapped result
- **EF Core proxy awareness** — skips unloaded navigation properties without triggering lazy loads
- **OpenTelemetry built-in** — every mapping call is an Activity span
- **MIT licensed, no paid tiers, forever**

---

## Repository Structure

```
DeltaMapper/
├── src/
│   ├── DeltaMapper.Core/              ← runtime library (Phase 1-2)
│   │   ├── IMapper.cs
│   │   ├── MappingProfile.cs
│   │   ├── MappingExpression.cs
│   │   ├── MappingDiff.cs
│   │   ├── PropertyChange.cs
│   │   ├── MapperContext.cs
│   │   ├── MapperConfiguration.cs
│   │   ├── MapperConfigurationBuilder.cs
│   │   ├── Mapper.cs
│   │   ├── ServiceCollectionExtensions.cs
│   │   ├── Middleware/
│   │   │   ├── IMappingMiddleware.cs
│   │   │   └── MappingPipeline.cs
│   │   └── Exceptions/
│   │       └── DeltaMapperException.cs
│   ├── DeltaMapper.SourceGen/         ← Roslyn generator (Phase 3)
│   │   ├── MapperGenerator.cs
│   │   ├── GenerateMapAttribute.cs
│   │   └── SyntaxReceiver.cs
│   ├── DeltaMapper.EFCore/            ← EF Core integration (Phase 4)
│   │   └── EFCoreMapperExtensions.cs
│   └── DeltaMapper.OpenTelemetry/     ← OTel spans (Phase 4)
│       └── TracingMiddleware.cs
├── tests/
│   ├── DeltaMapper.UnitTests/
│   ├── DeltaMapper.IntegrationTests/
│   └── DeltaMapper.Benchmarks/        ← BenchmarkDotNet (critical for README)
├── samples/
│   ├── PatchEndpointSample/           ← ASP.NET Core PATCH example
│   └── AuditLogSample/
├── docs/                              ← Docusaurus site
├── .github/
│   └── workflows/
│       ├── ci.yml
│       └── publish-nuget.yml
├── DeltaMapper.sln
└── README.md
```

---

## Target Framework & Dependencies

- **Target**: `net8.0` (minimum)
- **Multi-target**: `net8.0;net9.0`
- **Core has zero runtime dependencies**
- Test projects: `xunit`, `FluentAssertions`, `BenchmarkDotNet`
- SourceGen project: `Microsoft.CodeAnalysis.CSharp`

---

## Phase 1 — Runtime Core

**Goal**: A fully working mapper with no source generation. All features work. NuGet publishable.

### 1.1 Project Setup

```xml
<!-- src/DeltaMapper.Core/DeltaMapper.Core.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net8.0;net9.0</TargetFrameworks>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
    <RootNamespace>DeltaMapper</RootNamespace>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
    <PackageId>DeltaMapper</PackageId>
    <Version>0.1.0</Version>
    <Description>Fast, diff-aware .NET object mapper. MIT licensed.</Description>
  </PropertyGroup>
</Project>
```

### 1.2 Core Interfaces

```csharp
// IMapper.cs
public interface IMapper
{
    TDestination Map<TDestination>(object source);
    TDestination Map<TSource, TDestination>(TSource source);
    TDestination Map<TSource, TDestination>(TSource source, TDestination destination);
    IReadOnlyList<TDestination> MapList<TSource, TDestination>(IEnumerable<TSource> source);
    object Map(object source, Type sourceType, Type destinationType);
}
```

### 1.3 Fluent Profile API

Users subclass `MappingProfile` and call `CreateMap<TSrc, TDst>()` inside the constructor.

```csharp
// Usage pattern to support:
public class UserProfile : MappingProfile
{
    public UserProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(d => d.FullName, o => o.MapFrom(s => $"{s.First} {s.Last}"))
            .ForMember(d => d.InternalId, o => o.Ignore())
            .ForMember(d => d.Email, o => o.NullSubstitute("unknown@example.com"))
            .BeforeMap((src, dst) => { /* pre-map hook */ })
            .AfterMap((src, dst)  => { /* post-map hook */ })
            .ReverseMap();
    }
}
```

**Implement:**
- `MappingProfile` base class with `CreateMap<TSrc, TDst>()` returning `IMappingExpression<TSrc, TDst>`
- `IMappingExpression<TSrc, TDst>` fluent interface: `ForMember`, `BeforeMap`, `AfterMap`, `ReverseMap`
- `IMemberOptions<TSrc, TDst>`: `MapFrom<T>(Func<TSrc, T>)`, `Ignore()`, `NullSubstitute(object)`

### 1.4 MapperConfiguration — Startup Registry

```csharp
// Usage:
var config = MapperConfiguration.Create(cfg =>
{
    cfg.AddProfile<UserProfile>();
    cfg.AddProfile<OrderProfile>();
});

var mapper = config.CreateMapper();
```

**Implement:**
- `MapperConfiguration` scans profiles, compiles all maps at startup
- Use `System.Collections.Frozen.FrozenDictionary<(Type, Type), CompiledMap>` for the type registry
- `CompiledMap` holds: `Func<object, object?, MapperContext, object>` delegate
- Compile step uses `System.Linq.Expressions` to build typed getter/setter delegates per property
- Compilation happens once at startup — never at call time

**Convention rules (auto-matched without config):**
1. Same property name (case-insensitive) + same type → direct assign
2. Same property name + assignable type → direct assign
3. Same property name + complex object type → recurse (lookup nested map)
4. Same property name + `IEnumerable<T>` source + `IEnumerable<T>` / `T[]` / `List<T>` destination → map each element

### 1.5 Mapper — Runtime Executor

```csharp
// Mapper.cs — the IMapper implementation
public sealed class Mapper : IMapper
{
    private readonly MapperConfiguration _config;

    public Mapper(MapperConfiguration config) => _config = config;

    public TDestination Map<TSource, TDestination>(TSource source)
    {
        var ctx = new MapperContext(_config);
        return (TDestination)_config.Execute(source!, typeof(TSource), typeof(TDestination), ctx);
    }
    // ... etc
}
```

### 1.6 MapperContext — Per-Call State

```csharp
// Tracks objects already mapped in this call to break circular references
public sealed class MapperContext
{
    internal MapperConfiguration Config { get; }
    private readonly Dictionary<object, object> _visited = new(ReferenceEqualityComparer.Instance);

    internal bool TryGetMapped(object source, out object? mapped) => _visited.TryGetValue(source, out mapped);
    internal void Register(object source, object dest) => _visited[source] = dest;
}
```

### 1.7 DI Integration

```csharp
// ServiceCollectionExtensions.cs
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDeltaMapper(
        this IServiceCollection services,
        Action<MapperConfigurationBuilder> configure)
    {
        var builder = new MapperConfigurationBuilder();
        configure(builder);
        var config = builder.Build();
        services.AddSingleton(config);
        services.AddSingleton<IMapper>(sp => new Mapper(sp.GetRequiredService<MapperConfiguration>()));
        return services;
    }
}

// Usage in Program.cs:
builder.Services.AddDeltaMapper(cfg =>
{
    cfg.AddProfile<UserProfile>();
});
```

### 1.8 Records and Init-Only Properties

- Detect `init`-only setters via `MethodInfo.ReturnParameter` custom attributes
- Use constructor injection path when all writable members are init-only (records)
- For records: find the primary constructor, map matching parameters by name

### 1.9 Phase 1 Tests

Cover in unit tests:
- Convention mapping: same name/type
- Convention mapping: nested objects
- Convention mapping: collections (`List<T>`, arrays, `IEnumerable<T>`)
- `ForMember` with `MapFrom` resolver
- `ForMember` with `Ignore`
- `ForMember` with `NullSubstitute`
- `BeforeMap` / `AfterMap` hooks
- `ReverseMap`
- Record type mapping
- Init-only property mapping
- Circular reference detection (no stack overflow)
- `Map(object, Type, Type)` overload (non-generic)
- `MapList<TSrc, TDst>` — list of 0, 1, N items
- Error: no mapping registered → `DeltaMapperException` with helpful message
- DI registration via `AddDeltaMapper`

---

## Phase 2 — MappingDiff\<T\> (The Killer Feature)

**Goal**: Return both the mapped result AND a structured change set. No other mapper does this.

### 2.1 Types

```csharp
// PropertyChange.cs
public sealed record PropertyChange(
    string PropertyName,
    object? From,
    object? To,
    ChangeKind Kind
);

public enum ChangeKind { Modified, Added, Removed }

// MappingDiff.cs
public sealed class MappingDiff<T>
{
    public T Result { get; init; }
    public IReadOnlyList<PropertyChange> Changes { get; init; }
    public bool HasChanges => Changes.Count > 0;
}
```

### 2.2 IMapper Extension

```csharp
// Add to IMapper and Mapper:
MappingDiff<TDestination> Patch<TSource, TDestination>(
    TSource source,
    TDestination destination);
```

### 2.3 Diff Algorithm

```
For each destination property:
  1. Get current value from destination (before map)
  2. Run the mapping (apply source → destination)
  3. Get new value from destination (after map)
  4. If !Equals(before, after) → emit PropertyChange { From: before, To: after }
```

**Deep diff rules:**
- Primitive types + string: direct `Equals` comparison
- Nested complex objects: recurse, flatten changes with dot notation (`"Address.City"`)
- Collections: compare by index; emit Added/Removed for length differences

### 2.4 Usage Examples to Support

```csharp
// PATCH endpoint pattern
[HttpPatch("{id}")]
public async Task<IActionResult> PatchUser(int id, UpdateUserDto dto)
{
    var user = await db.Users.FindAsync(id);
    var diff = mapper.Patch(dto, user);

    if (!diff.HasChanges)
        return NoContent();

    await auditLog.RecordAsync(id, diff.Changes);
    await db.SaveChangesAsync();

    return Ok(diff.Result);
}

// Event sourcing pattern
var diff = mapper.Patch(command, aggregate);
eventStore.Append(new UserUpdatedEvent
{
    Changes = diff.Changes.Select(c => new FieldChange(c.PropertyName, c.From, c.To))
});
```

### 2.5 Phase 2 Tests

- Patch with single changed property
- Patch with no changes (returns empty list, `HasChanges = false`)
- Patch with multiple changes
- Patch with nested object changes (dot-notation paths)
- Patch with collection: item added
- Patch with collection: item removed
- Patch with collection: item modified
- Patch with null source property + `NullSubstitute`
- `MappingDiff<T>` serializes cleanly to JSON

---

## Phase 3 — Source Generation

**Goal**: `[GenerateMap]` attribute triggers a Roslyn `ISourceGenerator` that emits plain C# assignment code at build time. Zero overhead — identical to hand-written code.

### 3.1 Project Setup

```xml
<!-- src/DeltaMapper.SourceGen/DeltaMapper.SourceGen.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.*" PrivateAssets="all"/>
  </ItemGroup>
</Project>
```

### 3.2 Attribute

```csharp
// GenerateMapAttribute.cs — emitted by the generator itself as source
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class GenerateMapAttribute : Attribute
{
    public GenerateMapAttribute(Type source, Type destination) { }
}
```

### 3.3 Generator Output

Given:
```csharp
[GenerateMap(typeof(User), typeof(UserDto))]
public partial class UserProfile : MappingProfile { }
```

Emits:
```csharp
// <auto-generated />
public partial class UserProfile
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void RegisterGeneratedMaps()
    {
        GeneratedMapRegistry.Register<User, UserDto>(static (src, dst) =>
        {
            dst.Id        = src.Id;
            dst.Email     = src.Email;
            dst.FirstName = src.FirstName;
            dst.LastName  = src.LastName;
        });
    }
}
```

- Generated file is readable — users can inspect exactly what it does
- `ModuleInitializer` registers the delegate before `Main` runs
- `MapperConfiguration` checks `GeneratedMapRegistry` first before using expression-compiled fallback
- Build-time analyzer emits warnings for unmapped destination properties

### 3.4 Analyzer Rules

| Rule ID | Severity | Description |
|---|---|---|
| `DM001` | Warning | Destination property has no matching source property |
| `DM002` | Error | Source type not found |
| `DM003` | Warning | Mapping registered but never used |

### 3.5 Phase 3 Tests

- Generator emits correct file for simple flat type pair
- Generator handles nested types (emits recursive call)
- Generator handles `List<T>` and array destination properties
- Generator respects `[Ignore]` attribute on destination property
- Analyzer emits `DM001` for unmapped property
- Generated code compiles without warnings

---

## Phase 4 — Ecosystem Integrations

### 4.1 EF Core Proxy Awareness

```xml
<!-- DeltaMapper.EFCore.csproj -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.*" />
```

```csharp
// EFCoreMapperExtensions.cs
public static MapperConfigurationBuilder AddEFCoreSupport(
    this MapperConfigurationBuilder builder)
{
    builder.Use<EFCoreProxyMiddleware>();
    return builder;
}

// Middleware: detect EF Core proxy types, skip unloaded navigation properties
internal sealed class EFCoreProxyMiddleware : IMappingMiddleware
{
    public object Map(object source, Type destType, MapperContext ctx, Func<object> next)
    {
        // If source is an EF proxy, wrap it to intercept property access
        // Skip any navigation property where IsLoaded == false
        return next();
    }
}
```

### 4.2 OpenTelemetry

```xml
<!-- DeltaMapper.OpenTelemetry.csproj -->
<PackageReference Include="System.Diagnostics.DiagnosticSource" Version="8.*" />
```

```csharp
// TracingMiddleware.cs
internal sealed class TracingMiddleware : IMappingMiddleware
{
    private static readonly ActivitySource Source = new("DeltaMapper");

    public object Map(object source, Type destType, MapperContext ctx, Func<object> next)
    {
        using var activity = Source.StartActivity($"Map {source.GetType().Name} → {destType.Name}");
        var result = next();
        activity?.SetTag("mapper.source_type", source.GetType().FullName);
        activity?.SetTag("mapper.dest_type", destType.FullName);
        return result;
    }
}

// DI extension:
public static MapperConfigurationBuilder AddMapperTracing(
    this MapperConfigurationBuilder builder)
{
    builder.Use<TracingMiddleware>();
    return builder;
}
```

### 4.3 Middleware Pipeline

```csharp
// IMappingMiddleware.cs
public interface IMappingMiddleware
{
    object Map(object source, Type destType, MapperContext ctx, Func<object> next);
}

// MapperConfigurationBuilder.cs
public MapperConfigurationBuilder Use<TMiddleware>()
    where TMiddleware : IMappingMiddleware, new()
{
    _middlewares.Add(new TMiddleware());
    return this;
}
```

---

## Phase 5 — Benchmarks, Docs, Community

### 5.1 BenchmarkDotNet Suite

**This is non-negotiable — it's the primary marketing asset.**

File: `tests/DeltaMapper.Benchmarks/MapperBenchmarks.cs`

Benchmark scenarios:
- `FlatObject_1M` — 1,000,000 mappings of a 5-property POCO
- `NestedObject_100k` — 100,000 mappings with 2-level nesting
- `Collection_10k` — 10,000 mappings of `List<T>` with 10 items each
- `Patch_100k` — 100,000 `Patch()` calls

Compare against: `DeltaMapper (runtime)`, `DeltaMapper (source-gen)`, `Mapperly`, `AutoMapper`, `Hand-written`

Publish results as `BENCHMARKS.md` in the repo root.

### 5.2 README Structure

The README must follow this exact order:
1. Tagline + 3 bullet USPs
2. Install snippet (`dotnet add package DeltaMapper`)
3. Benchmark table (linked from `BENCHMARKS.md`)
4. `MappingDiff<T>` code example — the hook
5. Quick start (5 lines)
6. Full API reference
7. Migration guide from AutoMapper
8. License

### 5.3 AutoMapper Migration Guide

Produce `docs/migration-from-automapper.md` covering:

| AutoMapper | DeltaMapper |
|---|---|
| `CreateMap<Src, Dst>()` | `CreateMap<Src, Dst>()` |
| `cfg.AssertConfigurationIsValid()` | `MapperConfiguration.Validate()` |
| `IMapper.Map<T>(src)` | `IMapper.Map<T>(src)` |
| `IMapper.ProjectTo<T>(query)` | Not supported — use Mapster for projections |
| `Profile` | `MappingProfile` |
| `MapperConfiguration` | `MapperConfiguration.Create(cfg => ...)` |

Include a sed/regex script for bulk profile renaming.

---

## Speed Architecture — Implementation Notes

### The Hybrid Decision Tree

Every `Map()` call resolves a strategy in this order:

```
1. Check GeneratedMapRegistry (FrozenDictionary lookup)
   → If found: invoke the generated static delegate directly
   
2. Check CompiledMapRegistry (FrozenDictionary lookup)
   → If found: invoke the compiled expression delegate
   
3. Neither found → throw DeltaMapperException with actionable message
   (never build at call time — all compilation is at startup)
```

### FrozenDictionary

```csharp
// MapperConfiguration — after all profiles compiled:
_registry = maps.ToFrozenDictionary(
    kvp => kvp.Key,
    kvp => kvp.Value
);
```

`FrozenDictionary` (.NET 8+) is optimized for read-only access after construction. Faster than `ConcurrentDictionary` because it has no locking overhead.

### Expression Tree Compilation

For each property pair at startup:

```csharp
// Getter: (object src) => ((TSrc)src).PropertyName
var srcParam = Expression.Parameter(typeof(object), "src");
var cast     = Expression.Convert(srcParam, typeof(TSrc));
var access   = Expression.Property(cast, propInfo);
var boxed    = Expression.Convert(access, typeof(object));
var getter   = Expression.Lambda<Func<object, object?>>(boxed, srcParam).Compile();

// Setter: (object dst, object val) => ((TDst)dst).PropertyName = (TProp)val
var dstParam  = Expression.Parameter(typeof(object), "dst");
var valParam  = Expression.Parameter(typeof(object), "val");
var castDst   = Expression.Convert(dstParam, typeof(TDst));
var castVal   = Expression.Convert(valParam, propInfo.PropertyType);
var assign    = Expression.Assign(Expression.Property(castDst, propInfo), castVal);
var setter    = Expression.Lambda<Action<object, object?>>(assign, dstParam, valParam).Compile();
```

Compiled once at `MapperConfiguration.Create()` time, never again.

### Zero-Alloc Collection Paths

For `List<T>` destinations, pre-size with source count:
```csharp
var dest = new List<TDest>(sourceList.Count);
```

For array destinations, use `Array.CreateInstance` + index assignment — no intermediate list.

For struct mappings, keep everything on the stack — no heap allocation.

---

## CI/CD

### `.github/workflows/ci.yml`

```yaml
name: CI
on: [push, pull_request]
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.x'
      - run: dotnet restore
      - run: dotnet build --no-restore -c Release
      - run: dotnet test --no-build -c Release --logger trx
      - run: dotnet pack src/DeltaMapper.Core -c Release --no-build
```

### `.github/workflows/publish-nuget.yml`

Triggers on `v*` tags. Publishes all four packages to NuGet.org.

---

## NuGet Packages

| Package | Description |
|---|---|
| `DeltaMapper` | Runtime core. Zero dependencies. Start here. |
| `DeltaMapper.SourceGen` | Roslyn source generator. Add for maximum speed. |
| `DeltaMapper.EFCore` | EF Core proxy detection and navigation skip. |
| `DeltaMapper.OpenTelemetry` | Activity spans for every mapping operation. |

---

## Coding Standards

- `nullable enable` everywhere
- No `dynamic`, no `object` casts outside the core mapping engine internals
- Every public API has XML doc comments
- No reflection at call time — reflection only during `MapperConfiguration.Create()`
- Exceptions always include the source/destination type names and a resolution hint
- Tests use `FluentAssertions` for readable assertions
- Benchmarks use `BenchmarkDotNet` with `MemoryDiagnoser` — allocations matter

---

## Phase Order

| Phase | Deliverable | Gate |
|---|---|---|
| 1 | Runtime core + full tests | All tests green, NuGet ID reserved |
| 2 | `MappingDiff<T>` + Patch | Patch tests green, sample app working |
| 3 | Source generator | Generated code compiles, benchmarks show parity with Mapperly |
| 4 | EF Core + OTel + Middleware | Integration tests green |
| 5 | Benchmarks + README + docs | Benchmark table published, migration guide live |

Start with Phase 1. Do not move to Phase 2 until all Phase 1 tests are green and the NuGet package builds cleanly.
