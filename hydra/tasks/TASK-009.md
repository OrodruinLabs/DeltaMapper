# TASK-009: ForMember Resolvers, Hooks, and ReverseMap Tests

## Description
Add comprehensive tests for ForMember with MapFrom (custom resolvers), ForMember with Ignore, ForMember with NullSubstitute, BeforeMap/AfterMap hooks, and ReverseMap. These test the fluent API end-to-end through the full mapping pipeline. Requires test profiles that exercise each feature.

## Status
PLANNED

## Metadata
- **Task ID**: TASK-009
- **Group**: 4
- **Wave**: 4
- **Depends on**: TASK-007
- **Retry count**: 0/3
- **Files modified**: tests/DeltaMapper.UnitTests/ForMemberTests.cs, tests/DeltaMapper.UnitTests/MappingHooksTests.cs, tests/DeltaMapper.UnitTests/ReverseMapTests.cs
- **delegates_to**: implementer
- **traces_to**: TRD AC-6 (ForMember MapFrom), TRD AC-7 (Ignore), TRD AC-8 (NullSubstitute), TRD AC-9 (BeforeMap/AfterMap), TRD AC-10 (ReverseMap)

## File Scope

### Creates
- `tests/DeltaMapper.UnitTests/ForMemberTests.cs`
- `tests/DeltaMapper.UnitTests/MappingHooksTests.cs`
- `tests/DeltaMapper.UnitTests/ReverseMapTests.cs`

### Modifies
- (none)

## Acceptance Criteria
1. ForMember tests FM-01 through FM-07 pass (MapFrom custom resolver, complex expression, Ignore skips, Ignore doesn't affect others, NullSubstitute when null, NullSubstitute when has value, multiple overrides)
2. Hook tests H-01 through H-04 pass (BeforeMap before assignment, AfterMap after assignment, both in order, receives src and dst)
3. ReverseMap tests R-01 through R-03 pass (registers inverse, both directions work, custom resolver not applied in reverse)

## Test Requirements
- `ForMemberTests.cs`: Tests FM-01 to FM-07 from test plan
- `MappingHooksTests.cs`: Tests H-01 to H-04 from test plan
- `ReverseMapTests.cs`: Tests R-01 to R-03 from test plan

## Pattern Reference
- Test Plan Sections 2.4, 2.5, 2.6 for test scenarios
- TRD Section 3.4 for MappingExpression fluent API
