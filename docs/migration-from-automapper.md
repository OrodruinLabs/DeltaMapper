# Migrating from AutoMapper to DeltaMapper

This guide covers the mechanical steps for replacing AutoMapper with DeltaMapper in an existing project. DeltaMapper intentionally mirrors the most-used AutoMapper patterns so that the majority of migrations are search-and-replace operations.

---

## Package swap

```
dotnet remove package AutoMapper
dotnet remove package AutoMapper.Extensions.Microsoft.DependencyInjection
dotnet add package DeltaMapper
```

---

## Concept mapping

| AutoMapper | DeltaMapper | Notes |
|---|---|---|
| `Profile` | `MappingProfile` | Rename the base class; constructor and `CreateMap` calls are identical |
| `CreateMap<Src, Dst>()` | `CreateMap<Src, Dst>()` | Same signature |
| `ForMember(d => d.P, o => o.MapFrom(...))` | `ForMember(d => d.P, o => o.MapFrom(...))` | Same signature |
| `ForMember(d => d.P, o => o.Ignore())` | `ForMember(d => d.P, o => o.Ignore())` | Same signature |
| `ForMember(d => d.P, o => o.NullSubstitute(v))` | `ForMember(d => d.P, o => o.NullSubstitute(v))` | Same signature |
| `BeforeMap((src, dst) => ...)` | `BeforeMap((src, dst) => ...)` | Same signature |
| `AfterMap((src, dst) => ...)` | `AfterMap((src, dst) => ...)` | Same signature |
| `ReverseMap()` | `ReverseMap()` | Same signature |
| `MapperConfiguration` (ctor) | `MapperConfiguration.Create(cfg => ...)` | Static factory replaces `new MapperConfiguration(cfg => ...)` |
| `cfg.AddProfile<P>()` | `cfg.AddProfile<P>()` | Same signature |
| `cfg.AssertConfigurationIsValid()` | DM001/DM002 analyzer diagnostics (compile-time) | Remove the runtime call; the source generator emits DM001 (unmapped destination property) and DM002 (type not found) at compile time. Note: these cover common misconfiguration but are not a full equivalent of `AssertConfigurationIsValid()` |
| `mapper.Map<T>(src)` | `mapper.Map<T>(src)` | Same signature |
| `mapper.Map<Src, Dst>(src)` | `mapper.Map<Src, Dst>(src)` | Same signature |
| `mapper.Map(src, dst)` | `mapper.Map<Src, Dst>(src, dst)` | DeltaMapper requires explicit type arguments |
| `mapper.Map(src, srcType, dstType)` | `mapper.Map(src, srcType, dstType)` | Same signature |
| `IMapper.ProjectTo<T>(query)` | Not supported | Use Mapster for EF Core LINQ projections |

---

## DI registration

**AutoMapper:**

```csharp
// Program.cs
builder.Services.AddAutoMapper(typeof(UserProfile).Assembly);
```

**DeltaMapper:**

```csharp
// Program.cs
using DeltaMapper.Extensions;

builder.Services.AddDeltaMapper(cfg =>
{
    cfg.AddProfile<UserProfile>();
    cfg.AddProfile<OrderProfile>();
    // add all your profiles here
});
```

DeltaMapper requires profiles to be listed explicitly. Assembly scanning is not supported in v0.4; explicit registration is intentional — it makes the full mapping surface visible at a glance.

---

## Profile rename script

The `Profile` base class rename and `MapperConfiguration` construction pattern are the two mechanical changes that affect every file. The following shell commands handle both in a typical project.

> **Note:** The `sed -i` syntax differs between macOS (BSD) and Linux (GNU). The commands below show both variants. Use the one matching your platform, or use `dotnet format` / your IDE's find-and-replace for a portable alternative.

Rename the base class in all `.cs` files:

```bash
# macOS (BSD sed)
find . -name "*.cs" -not -path "*/obj/*" \
  -exec sed -i '' 's/: Profile$/ : MappingProfile/g; s/: Profile {/ : MappingProfile {/g' {} +

# Linux (GNU sed)
find . -name "*.cs" -not -path "*/obj/*" \
  -exec sed -i 's/: Profile$/ : MappingProfile/g; s/: Profile {/ : MappingProfile {/g' {} +
```

Update `using` directives (AutoMapper's namespace is `AutoMapper`; DeltaMapper types live in `DeltaMapper.*`):

```bash
# macOS (BSD sed)
find . -name "*.cs" -not -path "*/obj/*" \
  -exec sed -i '' 's/using AutoMapper;/using DeltaMapper.Abstractions;\nusing DeltaMapper.Configuration;/g' {} +

# Linux (GNU sed)
find . -name "*.cs" -not -path "*/obj/*" \
  -exec sed -i 's/using AutoMapper;/using DeltaMapper.Abstractions;\nusing DeltaMapper.Configuration;/g' {} +
```

Update the configuration construction pattern:

```bash
# macOS (BSD sed)
find . -name "*.cs" -not -path "*/obj/*" \
  -exec sed -i '' 's/new MapperConfiguration(\(cfg\)/MapperConfiguration.Create(\1/g' {} +

# Linux (GNU sed)
find . -name "*.cs" -not -path "*/obj/*" \
  -exec sed -i 's/new MapperConfiguration(\(cfg\)/MapperConfiguration.Create(\1/g' {} +
```

Review the diff after running these scripts — edge cases (inline `Profile` subclasses, `IMapper` injected as `AutoMapper.IMapper`, etc.) may need manual attention.

---

## Feature status as of v0.4

The following table summarises AutoMapper features relative to DeltaMapper v0.4. All five planned phases are now complete.

| AutoMapper feature | DeltaMapper v0.4 status |
|---|---|
| `AssertConfigurationIsValid()` | Implemented — DM001/DM002 compile-time analyzer diagnostics (Phase 3) |
| `MappingDiff<T>` / change tracking | Implemented — `MapWithDiff` and `Patch` methods (Phase 2) |
| Source generator / zero-overhead path | Implemented — `[GenerateMap]` attribute via `DeltaMapper.SourceGen` (Phase 3) |
| EF Core proxy awareness | Implemented — `AddEFCoreSupport()` via `DeltaMapper.EFCore` (Phase 4) |
| OpenTelemetry tracing | Implemented — `AddMapperTracing()` via `DeltaMapper.OpenTelemetry` (Phase 4) |
| `ProjectTo<T>()` for EF Core | Not planned — use Mapster |
| `TypeConverter<Src, Dst>` | Not implemented |
| `ValueConverter<Src, Dst>` | Not implemented — use `MapFrom` resolver |
| `IValueResolver<Src, Dst, TMember>` | Not implemented — use `MapFrom` resolver |
| `ConstructUsing(src => ...)` | Not needed — constructor injection is automatic for records and init-only types |
| `IMappingAction<Src, Dst>` | Use `BeforeMap` / `AfterMap` hooks |
| Global `ForAllMaps` / `ForAllPropertyMaps` | Not supported |
| Open generics mapping | Not supported |
