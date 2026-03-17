# Technical Requirements Document — DeltaMapper Phase 1: Runtime Core

**Version**: 1.0
**Date**: 2026-03-16
**Status**: Generated
**Scope**: Phase 1 only (runtime mapper, no source generation, no diff/patch, no EF Core/OTel)

---

## 1. Overview

DeltaMapper Phase 1 delivers a fully functional .NET 8/9 object mapping library with expression-compiled delegates, FrozenDictionary-backed lookup, fluent profile configuration, DI integration, and zero runtime dependencies. The output is a NuGet-publishable `DeltaMapper.Core` package.

**Prior Documentation**: `docs/DELTAMAP_PLAN.md` (authoritative design document, sections 1.1-1.9)

---

## 2. Solution Architecture

### 2.1 Project Structure

```
DeltaMapper/
  DeltaMapper.sln
  src/
    DeltaMapper.Core/
      DeltaMapper.Core.csproj
      IMapper.cs
      Mapper.cs
      MappingProfile.cs
      MappingExpression.cs
      MapperConfiguration.cs
      MapperConfigurationBuilder.cs
      MapperContext.cs
      ServiceCollectionExtensions.cs
      Middleware/
        IMappingMiddleware.cs
        MappingPipeline.cs
      Exceptions/
        DeltaMapperException.cs
  tests/
    DeltaMapper.UnitTests/
      DeltaMapper.UnitTests.csproj
```

### 2.2 Target Frameworks

- **Library**: `net8.0;net9.0` multi-target
- **Tests**: `net9.0` (single target, latest)
- **Language**: C# latest (`LangVersion>latest`)
- **Nullable**: Enabled globally
- **ImplicitUsings**: Enabled

### 2.3 Dependencies

| Project | Dependency | Version | Purpose |
|---------|-----------|---------|---------|
| DeltaMapper.Core | *none* | -- | Zero runtime dependencies |
| DeltaMapper.Core | Microsoft.Extensions.DependencyInjection.Abstractions | 8.* | DI extension method (`AddDeltaMapper`) |
| DeltaMapper.UnitTests | xunit | 2.* | Test framework |
| DeltaMapper.UnitTests | FluentAssertions | 7.* | Readable assertions |
| DeltaMapper.UnitTests | Microsoft.NET.Test.Sdk | latest | Test host |
| DeltaMapper.UnitTests | xunit.runner.visualstudio | latest | Test runner |
| DeltaMapper.UnitTests | Microsoft.Extensions.DependencyInjection | 8.* | DI container for integration-style tests |

**Note on DI Abstractions**: The core library depends on `Microsoft.Extensions.DependencyInjection.Abstractions` (not the full container) to provide `ServiceCollectionExtensions`. This is the standard pattern for .NET libraries offering DI integration.

---

## 3. Component Design

### 3.1 IMapper — Public Interface

File: `src/DeltaMapper.Core/IMapper.cs`

```csharp
public interface IMapper
{
    TDestination Map<TDestination>(object source);
    TDestination Map<TSource, TDestination>(TSource source);
    TDestination Map<TSource, TDestination>(TSource source, TDestination destination);
    IReadOnlyList<TDestination> MapList<TSource, TDestination>(IEnumerable<TSource> source);
    object Map(object source, Type sourceType, Type destinationType);
}
```

Five overloads covering: inferred-source generic, explicit generic, existing-destination, list projection, and non-generic (for dynamic/reflection scenarios).

### 3.2 Mapper — Runtime Executor

File: `src/DeltaMapper.Core/Mapper.cs`

- `sealed class Mapper : IMapper`
- Holds a reference to `MapperConfiguration` (injected via constructor)
- Each `Map()` call:
  1. Creates a fresh `MapperContext` (per-call lifetime)
  2. Delegates to `MapperConfiguration.Execute(source, srcType, dstType, ctx)`
  3. Casts result to `TDestination`
- `MapList` iterates source, calls `Map` per element, returns `List<TDestination>` pre-sized to source count

### 3.3 MappingProfile — User Configuration Base Class

File: `src/DeltaMapper.Core/MappingProfile.cs`

- `abstract class MappingProfile`
- Protected method: `IMappingExpression<TSrc, TDst> CreateMap<TSrc, TDst>()`
- Stores a list of `TypeMapConfiguration` entries created by `CreateMap` calls
- Profiles are instantiated once at configuration time and never again

### 3.4 MappingExpression — Fluent API

File: `src/DeltaMapper.Core/MappingExpression.cs`

Fluent interface `IMappingExpression<TSrc, TDst>` with methods:

| Method | Purpose |
|--------|---------|
| `ForMember(dst => dst.Prop, opt => opt.MapFrom(src => ...))` | Custom member mapping via lambda |
| `ForMember(dst => dst.Prop, opt => opt.Ignore())` | Skip this destination property |
| `ForMember(dst => dst.Prop, opt => opt.NullSubstitute(value))` | Default when source is null |
| `BeforeMap(Action<TSrc, TDst>)` | Pre-mapping hook |
| `AfterMap(Action<TSrc, TDst>)` | Post-mapping hook |
| `ReverseMap()` | Auto-register the inverse mapping `TDst -> TSrc` |

Member options interface `IMemberOptions<TSrc, TDst>`:
- `MapFrom<TResult>(Expression<Func<TSrc, TResult>>)` -- custom resolver
- `Ignore()` -- skip property
- `NullSubstitute(object value)` -- fallback value

Internal storage: each `ForMember` call stores a `MemberConfiguration` record containing the destination property expression, the resolver/ignore/null-substitute configuration.

### 3.5 MapperConfiguration — Startup Registry & Compiler

File: `src/DeltaMapper.Core/MapperConfiguration.cs`

**Factory method**: `static MapperConfiguration Create(Action<MapperConfigurationBuilder> configure)`

**Startup flow**:
1. Instantiate `MapperConfigurationBuilder`
2. Call user-provided `configure` delegate (adds profiles)
3. Iterate all profiles, extract `TypeMapConfiguration` entries
4. For each type pair `(TSrc, TDst)`:
   a. Match destination properties to source properties by convention (name-insensitive, type-compatible)
   b. Apply `ForMember` overrides from fluent config
   c. Build an `Expression<Func<object, object?, MapperContext, object>>` combining all property assignments
   d. Compile the expression to a `Func<object, object?, MapperContext, object>` delegate
   e. Store as `CompiledMap` keyed by `(Type, Type)`
5. Freeze the dictionary: `maps.ToFrozenDictionary()` producing `FrozenDictionary<(Type, Type), CompiledMap>`
6. Return immutable `MapperConfiguration`

**Convention matching rules** (in priority order):
1. Same property name (case-insensitive) + same type: direct assign
2. Same property name + assignable type: direct assign
3. Same property name + complex object type: recursive mapping (lookup nested CompiledMap)
4. Same property name + `IEnumerable<T>` source + collection destination: map each element

**Expression tree compilation** (per property pair):
- Getter: `(object src) => ((TSrc)src).PropertyName` compiled to `Func<object, object?>`
- Setter: `(object dst, object val) => ((TDst)dst).PropertyName = (TProp)val` compiled to `Action<object, object?>`
- Combined into a single delegate per type pair that executes all assignments sequentially

**Record/init-only support**:
- Detect init-only setters via `MethodInfo.ReturnParameter.GetRequiredCustomModifiers()` checking for `System.Runtime.CompilerServices.IsExternalInit`
- When all writable properties are init-only (record pattern): use constructor injection path
- Find primary constructor, map parameters by name (case-insensitive) to source properties
- Build expression that calls `new TDst(param1, param2, ...)` instead of `new TDst() { Prop1 = ..., Prop2 = ... }`

**`Execute` method**: looks up `CompiledMap` by `(srcType, dstType)`, invokes the compiled delegate, throws `DeltaMapperException` if not found.

**`CreateMapper` method**: returns `new Mapper(this)`.

### 3.6 MapperConfigurationBuilder

File: `src/DeltaMapper.Core/MapperConfigurationBuilder.cs`

- `AddProfile<TProfile>() where TProfile : MappingProfile, new()` -- instantiates and registers a profile
- `AddProfile(MappingProfile profile)` -- registers an existing instance
- `Use<TMiddleware>() where TMiddleware : IMappingMiddleware, new()` -- registers middleware (pipeline support for Phase 4)
- `Build()` -- triggers compilation, returns `MapperConfiguration`

### 3.7 MapperContext — Circular Reference Tracking

File: `src/DeltaMapper.Core/MapperContext.cs`

```csharp
public sealed class MapperContext
{
    internal MapperConfiguration Config { get; }
    private readonly Dictionary<object, object> _visited = new(ReferenceEqualityComparer.Instance);

    internal MapperContext(MapperConfiguration config) => Config = config;

    internal bool TryGetMapped(object source, out object? mapped)
        => _visited.TryGetValue(source, out mapped);

    internal void Register(object source, object dest)
        => _visited[source] = dest;
}
```

- Uses `ReferenceEqualityComparer.Instance` -- identity comparison, not structural
- Created per top-level `Map()` call, disposed implicitly (no `IDisposable`)
- When a recursive mapping encounters an already-visited source object, it returns the previously-mapped destination instead of recursing

### 3.8 Middleware Pipeline (Stub for Phase 1)

Files: `src/DeltaMapper.Core/Middleware/IMappingMiddleware.cs`, `MappingPipeline.cs`

Phase 1 defines the interfaces and pipeline executor but ships no built-in middleware. The pipeline wraps the core `Execute` call:

```csharp
public interface IMappingMiddleware
{
    object Map(object source, Type destType, MapperContext ctx, Func<object> next);
}
```

`MappingPipeline` chains registered middleware in order, with the innermost `next()` being the actual compiled delegate invocation. If no middleware is registered, the pipeline is bypassed entirely (zero overhead).

### 3.9 DeltaMapperException

File: `src/DeltaMapper.Core/Exceptions/DeltaMapperException.cs`

- `sealed class DeltaMapperException : Exception`
- Always includes source type, destination type, and a resolution hint in the message
- Example: `"No mapping registered from 'User' to 'UserDto'. Register a mapping in a MappingProfile or call CreateMap<User, UserDto>()."`
- Used for: missing mappings, ambiguous constructor resolution, unmappable property types

### 3.10 DI Integration

File: `src/DeltaMapper.Core/ServiceCollectionExtensions.cs`

```csharp
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
```

- `MapperConfiguration` registered as singleton (immutable, thread-safe after construction)
- `IMapper` registered as singleton (stateless -- `MapperContext` is per-call, not per-instance)

---

## 4. Data Flow

```
User code: mapper.Map<User, UserDto>(user)
  |
  v
Mapper.Map<TSource, TDestination>(source)
  |-- Creates MapperContext (new per call)
  |-- Calls config.Execute(source, typeof(User), typeof(UserDto), ctx)
  |
  v
MapperConfiguration.Execute(source, srcType, dstType, ctx)
  |-- Middleware pipeline (if any registered): wraps the core call
  |-- Lookup FrozenDictionary[(typeof(User), typeof(UserDto))]
  |-- If not found: throw DeltaMapperException
  |
  v
CompiledMap.Invoke(source, null, ctx)
  |-- Execute compiled expression delegate
  |-- For each property:
  |     |-- Check for ForMember override (custom resolver / ignore / null-substitute)
  |     |-- Get value from source via compiled getter
  |     |-- If complex type: ctx.TryGetMapped(value) -> return cached OR recurse
  |     |-- If collection: iterate, map each element via recursive call
  |     |-- Set value on destination via compiled setter (or constructor param for records)
  |-- ctx.Register(source, destination) -- for circular ref tracking
  |
  v
Return TDestination to caller
```

---

## 5. Non-Functional Requirements

### 5.1 Performance

- All reflection occurs at `MapperConfiguration.Create()` time -- never at `Map()` call time
- `FrozenDictionary` for O(1) type-pair lookup with zero locking overhead
- Expression-compiled delegates approach hand-written assignment speed
- Collection mapping pre-sizes destination collections to avoid resizing
- No boxing for value-type properties within the compiled delegate (where possible)

### 5.2 Thread Safety

- `MapperConfiguration` is immutable after construction -- safe for concurrent reads
- `Mapper` is stateless -- safe for concurrent use (registered as singleton)
- `MapperContext` is per-call -- no sharing between threads
- `FrozenDictionary` is inherently thread-safe for reads

### 5.3 Error Handling

- `DeltaMapperException` with actionable messages for all failure modes
- No silent failures -- unmapped properties either throw or are explicitly ignored
- Stack traces preserve original exception context via inner exceptions

### 5.4 Coding Standards (from design doc)

- `nullable enable` everywhere
- No `dynamic`, no `object` casts outside core mapping engine internals
- Every public API has XML doc comments
- No reflection at call time
- Exceptions always include source/destination type names and a resolution hint

---

## 6. Acceptance Criteria

| # | Criterion | Verification |
|---|-----------|-------------|
| AC-1 | Solution builds on `net8.0` and `net9.0` without warnings | `dotnet build -c Release` exits 0, no warnings |
| AC-2 | `IMapper` interface exposes all 5 overloads | Compilation of test consuming all overloads |
| AC-3 | Convention mapping works for same-name/same-type properties | Unit test: flat POCO mapping |
| AC-4 | Nested object mapping works recursively | Unit test: parent with child objects |
| AC-5 | Collection mapping works for `List<T>`, `T[]`, `IEnumerable<T>` | Unit tests: each collection type |
| AC-6 | `ForMember` with `MapFrom` applies custom resolvers | Unit test: concatenated name |
| AC-7 | `ForMember` with `Ignore` skips properties | Unit test: ignored property is default |
| AC-8 | `ForMember` with `NullSubstitute` provides fallback | Unit test: null source -> substitute |
| AC-9 | `BeforeMap` / `AfterMap` hooks execute | Unit test: side-effect verification |
| AC-10 | `ReverseMap` auto-registers inverse mapping | Unit test: map both directions |
| AC-11 | Record types map via constructor injection | Unit test: C# record mapping |
| AC-12 | Init-only properties map correctly | Unit test: class with `init` setters |
| AC-13 | Circular references detected without stack overflow | Unit test: A -> B -> A cycle |
| AC-14 | Non-generic `Map(object, Type, Type)` works | Unit test: dynamic type resolution |
| AC-15 | `MapList` maps 0, 1, and N items | Unit tests: empty, single, multiple |
| AC-16 | Missing mapping throws `DeltaMapperException` with helpful message | Unit test: exception message contains type names |
| AC-17 | `AddDeltaMapper` registers `IMapper` in DI container | Unit test: resolve `IMapper` from `ServiceProvider` |
| AC-18 | `MapperConfiguration` uses `FrozenDictionary` internally | Code review / reflection test |
| AC-19 | NuGet package builds cleanly | `dotnet pack` exits 0, `.nupkg` produced |
| AC-20 | All unit tests pass | `dotnet test` exits 0, 0 failures |

---

## 7. Out of Scope (Phase 1)

These are explicitly deferred to later phases:

- `MappingDiff<T>` and `Patch()` (Phase 2)
- `[GenerateMap]` source generation (Phase 3)
- EF Core proxy awareness (Phase 4)
- OpenTelemetry Activity spans (Phase 4)
- Benchmark suite (Phase 5)
- README, migration guide, Docusaurus docs (Phase 5)

The middleware pipeline interface and executor are included in Phase 1 as infrastructure, but no built-in middleware ships until Phase 4.

---

## 8. Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|-----------|
| Expression tree compilation complexity for edge cases (nullable value types, nested generics) | Mapping failures at runtime | Comprehensive unit tests for each property type combination; clear exception messages |
| Record constructor resolution ambiguity (multiple constructors) | Incorrect property assignment | Use primary constructor (single public constructor with matching parameter names); throw `DeltaMapperException` if ambiguous |
| `FrozenDictionary` not available on older runtimes | Build failure | Multi-target `net8.0;net9.0` ensures availability; `FrozenDictionary` is in `System.Collections.Frozen` namespace available from .NET 8 |
| Init-only detection relies on compiler-emitted modreq | Fragile across compiler versions | Test with both .NET 8 and .NET 9 SDKs; fall back to constructor injection path |

---

## 9. File Inventory

| File | Purpose | Public API |
|------|---------|-----------|
| `DeltaMapper.sln` | Solution file | -- |
| `src/DeltaMapper.Core/DeltaMapper.Core.csproj` | Project file | -- |
| `src/DeltaMapper.Core/IMapper.cs` | Mapper interface | `IMapper` |
| `src/DeltaMapper.Core/Mapper.cs` | Mapper implementation | -- (internal constructor) |
| `src/DeltaMapper.Core/MappingProfile.cs` | Profile base class | `MappingProfile`, `CreateMap<,>()` |
| `src/DeltaMapper.Core/MappingExpression.cs` | Fluent API | `IMappingExpression<,>`, `IMemberOptions<,>` |
| `src/DeltaMapper.Core/MapperConfiguration.cs` | Startup registry | `MapperConfiguration.Create()`, `CreateMapper()` |
| `src/DeltaMapper.Core/MapperConfigurationBuilder.cs` | Builder pattern | `AddProfile<>()`, `Build()` |
| `src/DeltaMapper.Core/MapperContext.cs` | Per-call state | -- (internal) |
| `src/DeltaMapper.Core/ServiceCollectionExtensions.cs` | DI registration | `AddDeltaMapper()` |
| `src/DeltaMapper.Core/Middleware/IMappingMiddleware.cs` | Middleware interface | `IMappingMiddleware` |
| `src/DeltaMapper.Core/Middleware/MappingPipeline.cs` | Pipeline executor | -- (internal) |
| `src/DeltaMapper.Core/Exceptions/DeltaMapperException.cs` | Custom exception | `DeltaMapperException` |
| `tests/DeltaMapper.UnitTests/DeltaMapper.UnitTests.csproj` | Test project file | -- |
