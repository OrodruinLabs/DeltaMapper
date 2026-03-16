# Architecture Map

## Entry Points
- `src/DeltaMapper.Core/ServiceCollectionExtensions.cs`: DI registration via `AddDeltaMapper()` — primary consumer entry point
- `src/DeltaMapper.Core/MapperConfiguration.cs`: `MapperConfiguration.Create()` — standalone (non-DI) entry point
- `src/DeltaMapper.Core/Mapper.cs`: `IMapper` implementation — runtime mapping executor

## Module Boundaries

### DeltaMapper.Core (Phase 1-2)
- Path: `src/DeltaMapper.Core/`
- Purpose: Runtime object mapping library with diff/patch support
- Key files:
  - `IMapper.cs` — public mapper interface
  - `Mapper.cs` — IMapper implementation
  - `MappingProfile.cs` — base class for user-defined mapping profiles
  - `MappingExpression.cs` — fluent `ForMember`, `BeforeMap`, `AfterMap`, `ReverseMap` API
  - `MapperConfiguration.cs` — startup registry, expression compilation, FrozenDictionary storage
  - `MapperConfigurationBuilder.cs` — builder pattern for configuration
  - `MapperContext.cs` — per-call state, circular reference tracking
  - `MappingDiff.cs` — diff result container
  - `PropertyChange.cs` — individual property change record
  - `ServiceCollectionExtensions.cs` — DI integration
  - `Middleware/IMappingMiddleware.cs` — middleware interface
  - `Middleware/MappingPipeline.cs` — middleware execution chain
  - `Exceptions/DeltaMapperException.cs` — custom exception type

### DeltaMapper.SourceGen (Phase 3)
- Path: `src/DeltaMapper.SourceGen/`
- Purpose: Roslyn ISourceGenerator for zero-overhead compile-time mapping
- Key files:
  - `MapperGenerator.cs` — the ISourceGenerator implementation
  - `GenerateMapAttribute.cs` — attribute emitted as source
  - `SyntaxReceiver.cs` — syntax tree walker

### DeltaMapper.EFCore (Phase 4)
- Path: `src/DeltaMapper.EFCore/`
- Purpose: EF Core proxy awareness — skip unloaded navigation properties
- Key files:
  - `EFCoreMapperExtensions.cs` — `AddEFCoreSupport()` extension

### DeltaMapper.OpenTelemetry (Phase 4)
- Path: `src/DeltaMapper.OpenTelemetry/`
- Purpose: Activity spans for every mapping operation
- Key files:
  - `TracingMiddleware.cs` — IMappingMiddleware that wraps calls in Activity spans

### Tests
- Path: `tests/`
- Purpose: Validation and benchmarks
  - `DeltaMapper.UnitTests/` — unit tests (xunit + FluentAssertions)
  - `DeltaMapper.IntegrationTests/` — integration tests
  - `DeltaMapper.Benchmarks/` — BenchmarkDotNet performance suite

### Samples
- Path: `samples/`
- Purpose: Example applications
  - `PatchEndpointSample/` — ASP.NET Core PATCH endpoint example
  - `AuditLogSample/` — audit log with MappingDiff

## Data Flow

```
User Code
    |
    v
MapperConfiguration.Create(cfg => { cfg.AddProfile<...>(); })
    |
    +--> Scans profiles for CreateMap<TSrc, TDst>() calls
    +--> For each type pair:
    |       1. Check GeneratedMapRegistry (source-gen compiled)
    |       2. Build Expression Tree (getter/setter per property)
    |       3. Compile to Func<object, object?, MapperContext, object>
    +--> Store in FrozenDictionary<(Type,Type), CompiledMap>
    |
    v
config.CreateMapper() --> Mapper instance
    |
    v
mapper.Map<TSrc, TDst>(source)
    |
    +--> Create MapperContext (circular ref tracking)
    +--> Run Middleware Pipeline (if any)
    +--> Lookup CompiledMap in FrozenDictionary
    +--> Execute compiled delegate
    +--> Return TDst

mapper.Patch<TSrc, TDst>(source, destination)
    |
    +--> Snapshot destination property values (before)
    +--> Execute mapping (apply source to destination)
    +--> Compare before/after for each property
    +--> Return MappingDiff<TDst> { Result, Changes }
```

## Shared Code
- `MapperContext`: shared across all mapping calls within a single `Map()`/`Patch()` invocation
- `IMappingMiddleware`: shared pipeline interface used by EFCore and OpenTelemetry packages
- `MappingProfile`: base class shared by all user-defined profiles and source-generated profiles

## External Integrations
- Microsoft.Extensions.DependencyInjection: DI registration (docs/DELTAMAP_PLAN.md:203-224)
- Microsoft.EntityFrameworkCore: proxy detection (docs/DELTAMAP_PLAN.md:430-453)
- System.Diagnostics.DiagnosticSource: OpenTelemetry ActivitySource (docs/DELTAMAP_PLAN.md:458-486)
- Microsoft.CodeAnalysis.CSharp: Roslyn source generator (docs/DELTAMAP_PLAN.md:350-362)
- NuGet.org: package publishing target (docs/DELTAMAP_PLAN.md:649-655)

## Configuration
- Fluent API via `MapperConfiguration.Create()` and `MappingProfile` subclasses
- DI via `services.AddDeltaMapper(cfg => ...)` extension method
- No config files, no environment variables — pure code configuration
- Source generator activated via `[GenerateMap]` attribute

## Dependency Graph
```
DeltaMapper.Core  <-- no external deps (zero runtime dependencies)
    ^       ^
    |       |
    |       +--- DeltaMapper.SourceGen (build-time only, netstandard2.0)
    |                depends on: Microsoft.CodeAnalysis.CSharp
    |
    +--- DeltaMapper.EFCore
    |        depends on: Microsoft.EntityFrameworkCore 8.*
    |
    +--- DeltaMapper.OpenTelemetry
             depends on: System.Diagnostics.DiagnosticSource 8.*

Tests:
    DeltaMapper.UnitTests --> DeltaMapper.Core, xunit, FluentAssertions
    DeltaMapper.IntegrationTests --> DeltaMapper.Core, DeltaMapper.EFCore, DeltaMapper.OpenTelemetry
    DeltaMapper.Benchmarks --> DeltaMapper.Core, BenchmarkDotNet
```
