# TASK-005: MappingProfile and MappingExpression Fluent API

## Description
Create the `MappingProfile` abstract base class with `CreateMap<TSrc, TDst>()` returning `IMappingExpression<TSrc, TDst>`. Create `IMappingExpression<TSrc, TDst>` fluent interface with ForMember, BeforeMap, AfterMap, ReverseMap methods. Create `IMemberOptions<TSrc, TDst>` with MapFrom, Ignore, NullSubstitute. Create internal `MappingExpression<TSrc, TDst>` implementation and `MemberConfiguration` storage. Create internal `TypeMapConfiguration` to hold all configuration for a type pair.

## Status
PLANNED

## Metadata
- **Task ID**: TASK-005
- **Group**: 2
- **Wave**: 2
- **Depends on**: TASK-001
- **Retry count**: 0/3
- **Files modified**: src/DeltaMapper.Core/MappingProfile.cs, src/DeltaMapper.Core/MappingExpression.cs, tests/DeltaMapper.UnitTests/MappingProfileTests.cs
- **delegates_to**: implementer
- **traces_to**: TRD Section 3.3 (MappingProfile), TRD Section 3.4 (MappingExpression), TRD AC-6, TRD AC-7, TRD AC-8, TRD AC-9, TRD AC-10

## File Scope

### Creates
- `src/DeltaMapper.Core/MappingProfile.cs`
- `src/DeltaMapper.Core/MappingExpression.cs`
- `tests/DeltaMapper.UnitTests/MappingProfileTests.cs`

### Modifies
- (none)

## Acceptance Criteria
1. `MappingProfile` is abstract with protected `CreateMap<TSrc, TDst>()` that returns `IMappingExpression<TSrc, TDst>` and stores `TypeMapConfiguration` entries
2. `IMappingExpression<TSrc, TDst>` supports full fluent chain: `ForMember(dst => dst.X, opt => opt.MapFrom(src => ...))`, `.Ignore()`, `.NullSubstitute(val)`, `.BeforeMap(...)`, `.AfterMap(...)`, `.ReverseMap()`
3. A test profile can be instantiated and its internal type map configurations can be inspected (count of registered maps, member configurations stored correctly)

## Test Requirements
- `MappingProfileTests.cs`: Test that CreateMap registers a TypeMapConfiguration, test that ForMember stores MemberConfiguration with correct options, test that ReverseMap creates an inverse TypeMapConfiguration

## Pattern Reference
- TRD Section 3.3-3.4 for class design
- docs/DELTAMAP_PLAN.md:119-133 for usage pattern
