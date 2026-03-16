# TASK-007: MapperConfiguration and MapperConfigurationBuilder

## Description
Create `MapperConfigurationBuilder` with `AddProfile<TProfile>()`, `AddProfile(MappingProfile)`, `Use<TMiddleware>()`, and `Build()` methods. Create `MapperConfiguration` with static `Create()` factory, expression tree compilation engine, FrozenDictionary registry, `Execute()` method, and `CreateMapper()`. The compilation engine must: match destination properties to source by convention (case-insensitive name matching, assignable types), apply ForMember overrides, build Expression<Func<object, object?, MapperContext, object>> per type pair, compile to delegates, and store in FrozenDictionary<(Type,Type), CompiledMap>.

## Status
PLANNED

## Metadata
- **Task ID**: TASK-007
- **Group**: 3
- **Wave**: 3
- **Depends on**: TASK-004, TASK-005, TASK-006
- **Retry count**: 0/3
- **Files modified**: src/DeltaMapper.Core/MapperConfiguration.cs, src/DeltaMapper.Core/MapperConfigurationBuilder.cs, tests/DeltaMapper.UnitTests/MapperConfigurationTests.cs
- **delegates_to**: implementer
- **traces_to**: TRD Section 3.5 (MapperConfiguration), TRD Section 3.6 (MapperConfigurationBuilder), TRD AC-18

## File Scope

### Creates
- `src/DeltaMapper.Core/MapperConfiguration.cs`
- `src/DeltaMapper.Core/MapperConfigurationBuilder.cs`
- `tests/DeltaMapper.UnitTests/MapperConfigurationTests.cs`

### Modifies
- (none)

## Acceptance Criteria
1. `MapperConfiguration.Create(cfg => cfg.AddProfile<P>())` compiles all type pairs from profiles into expression-compiled delegates stored in a `FrozenDictionary<(Type,Type), CompiledMap>`
2. Convention matching works for same-name (case-insensitive) + same/assignable type properties; ForMember overrides (MapFrom, Ignore, NullSubstitute) and BeforeMap/AfterMap hooks are applied during compilation; ReverseMap generates inverse type pair
3. `MapperConfigurationTests.cs` passes tests MC-01 through MC-05 from test plan (Create with profile, multiple profiles, duplicate last-wins, CreateMapper returns working mapper, FrozenDictionary used internally)

## Test Requirements
- `MapperConfigurationTests.cs`: Tests MC-01 (Create with profile compiles), MC-02 (multiple profiles all registered), MC-03 (duplicate mapping last wins), MC-04 (CreateMapper returns working IMapper), MC-05 (uses FrozenDictionary internally via reflection)

## Pattern Reference
- TRD Section 3.5 for compilation flow and convention matching
- docs/DELTAMAP_PLAN.md:586-604 for expression tree compilation pattern
