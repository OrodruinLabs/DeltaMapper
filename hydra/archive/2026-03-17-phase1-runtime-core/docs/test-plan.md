# Test Plan — DeltaMapper Phase 1: Runtime Core

**Version**: 1.0
**Date**: 2026-03-16
**Status**: Generated
**Framework**: xunit + FluentAssertions
**Project**: `tests/DeltaMapper.UnitTests/`

---

## 1. Test Strategy

### 1.1 Scope

All Phase 1 components: IMapper, Mapper, MappingProfile, MappingExpression (fluent API), MapperConfiguration (expression compilation + FrozenDictionary), MapperContext (circular reference detection), ServiceCollectionExtensions (DI), record/init-only support, and DeltaMapperException.

### 1.2 Approach

- **Unit tests only** for Phase 1 (integration tests deferred to Phase 4 with EF Core/OTel)
- Every public API method has at least one happy-path and one error-path test
- Tests use FluentAssertions for readable, expressive assertions
- Test models are defined as inner classes or in a shared `TestModels/` folder within the test project
- No mocking framework needed -- all components are concrete with no external I/O

### 1.3 Conventions

- Test class naming: `{ComponentUnderTest}Tests.cs`
- Test method naming: `{Method}_{Scenario}_{ExpectedResult}`
- One assertion concept per test (multiple FluentAssertions calls allowed if asserting the same concept)
- Arrange-Act-Assert pattern throughout

### 1.4 Test Project Configuration

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="xunit" Version="2.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
    <PackageReference Include="FluentAssertions" Version="7.*" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.*" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\DeltaMapper.Core\DeltaMapper.Core.csproj" />
  </ItemGroup>
</Project>
```

---

## 2. Test Scenarios

### 2.1 Convention Mapping — `ConventionMappingTests.cs`

| # | Test Name | Description | Expected Result |
|---|-----------|-------------|----------------|
| C-01 | `Map_SameNameSameType_MapsAllProperties` | Flat POCO with 5 string/int properties, identical names on src and dst | All destination properties match source values |
| C-02 | `Map_CaseInsensitiveNames_MapsCorrectly` | Source has `firstName`, destination has `FirstName` | Properties matched regardless of casing |
| C-03 | `Map_AssignableTypes_MapsWithImplicitConversion` | Source `int` property, destination `long` property with same name | Value assigned via implicit conversion |
| C-04 | `Map_UnmappedDestinationProperty_RemainsDefault` | Destination has a property with no source match and no ForMember config | Property retains default value (null/0/false) |
| C-05 | `Map_NullSourceProperty_MapsNull` | Source property is null, destination property is nullable reference type | Destination property is null |

### 2.2 Nested Object Mapping — `NestedMappingTests.cs`

| # | Test Name | Description | Expected Result |
|---|-----------|-------------|----------------|
| N-01 | `Map_NestedObject_MapsRecursively` | Source has `Address` property, both src/dst Address types have registered mappings | Nested object fully mapped |
| N-02 | `Map_NestedObjectIsNull_MapsToNull` | Source nested property is null | Destination nested property is null |
| N-03 | `Map_DeepNesting_ThreeLevels_MapsCorrectly` | A -> B -> C three-level nesting | All levels mapped correctly |
| N-04 | `Map_NestedWithNoRegisteredMapping_ThrowsDeltaMapperException` | Nested object type has no registered mapping | `DeltaMapperException` thrown with type names |

### 2.3 Collection Mapping — `CollectionMappingTests.cs`

| # | Test Name | Description | Expected Result |
|---|-----------|-------------|----------------|
| CL-01 | `Map_ListToList_MapsAllElements` | `List<UserSrc>` to `List<UserDst>` | All elements mapped, count matches |
| CL-02 | `Map_ListToArray_MapsAllElements` | Source `List<T>`, destination `T[]` | Array populated, length matches |
| CL-03 | `Map_IEnumerableToList_MapsAllElements` | Source `IEnumerable<T>`, destination `List<T>` | List populated from enumerable |
| CL-04 | `Map_EmptyCollection_MapsToEmptyCollection` | Source collection is empty | Destination collection is empty (not null) |
| CL-05 | `Map_NullCollection_MapsToNull` | Source collection property is null | Destination collection property is null |
| CL-06 | `MapList_EmptySource_ReturnsEmptyList` | `MapList` called with empty enumerable | Returns empty `IReadOnlyList<T>` |
| CL-07 | `MapList_SingleItem_ReturnsSingleElementList` | `MapList` with 1 item | Returns list with 1 mapped element |
| CL-08 | `MapList_MultipleItems_MapsAll` | `MapList` with N items | Returns list with N mapped elements |

### 2.4 ForMember — Custom Resolvers — `ForMemberTests.cs`

| # | Test Name | Description | Expected Result |
|---|-----------|-------------|----------------|
| FM-01 | `ForMember_MapFrom_AppliesCustomResolver` | `ForMember(d => d.FullName, o => o.MapFrom(s => $"{s.First} {s.Last}"))` | `FullName` equals concatenated first + last |
| FM-02 | `ForMember_MapFrom_WithComplexExpression` | Resolver computes a derived value (e.g., age from birthdate) | Computed value correct |
| FM-03 | `ForMember_Ignore_SkipsProperty` | `ForMember(d => d.Secret, o => o.Ignore())` | `Secret` property retains default value |
| FM-04 | `ForMember_Ignore_DoesNotAffectOtherProperties` | One property ignored, others mapped by convention | Non-ignored properties mapped normally |
| FM-05 | `ForMember_NullSubstitute_WhenSourceIsNull_UsesSubstitute` | Source property null, `NullSubstitute("N/A")` configured | Destination property equals "N/A" |
| FM-06 | `ForMember_NullSubstitute_WhenSourceHasValue_UsesSourceValue` | Source property has value, `NullSubstitute` configured | Destination property equals source value (substitute not used) |
| FM-07 | `ForMember_MultipleOverrides_AllApplied` | Multiple `ForMember` calls on same mapping | All overrides respected |

### 2.5 Hooks — `MappingHooksTests.cs`

| # | Test Name | Description | Expected Result |
|---|-----------|-------------|----------------|
| H-01 | `BeforeMap_ExecutesBeforePropertyAssignment` | `BeforeMap` sets a flag on source; mapping reads that flag | Destination reflects pre-map modification |
| H-02 | `AfterMap_ExecutesAfterPropertyAssignment` | `AfterMap` modifies destination after mapping completes | Destination reflects post-map modification |
| H-03 | `BeforeMap_AndAfterMap_BothExecute_InOrder` | Both hooks configured on same mapping | BeforeMap runs first, AfterMap runs second |
| H-04 | `BeforeMap_ReceivesBothSourceAndDestination` | Hook lambda receives `(src, dst)` | Both parameters are accessible and correct types |

### 2.6 ReverseMap — `ReverseMapTests.cs`

| # | Test Name | Description | Expected Result |
|---|-----------|-------------|----------------|
| R-01 | `ReverseMap_RegistersInverseMapping` | `CreateMap<A, B>().ReverseMap()` | `mapper.Map<B, A>(b)` works without separate registration |
| R-02 | `ReverseMap_ConventionPropertiesMapBothDirections` | Same-name properties on both types | Forward and reverse both map correctly |
| R-03 | `ReverseMap_CustomResolverNotAppliedInReverse` | `ForMember` with `MapFrom` on forward; reverse uses convention | Reverse mapping uses convention, not the forward custom resolver |

### 2.7 Record and Init-Only Properties — `RecordMappingTests.cs`

| # | Test Name | Description | Expected Result |
|---|-----------|-------------|----------------|
| REC-01 | `Map_RecordType_MapsViaConstructor` | C# `record` destination with positional parameters | All parameters populated from source properties |
| REC-02 | `Map_RecordWithAdditionalProperties_MapsAll` | Record with positional params + additional init properties | Both constructor params and init properties mapped |
| REC-03 | `Map_InitOnlyProperties_MapsCorrectly` | Class with `init` setters (not a record) | Init-only properties assigned |
| REC-04 | `Map_RecordToRecord_MapsCorrectly` | Both source and destination are records | Mapped via constructor on destination |
| REC-05 | `Map_RecordWithForMember_AppliesOverride` | Record destination with `ForMember` custom resolver | Constructor param overridden by custom resolver |

### 2.8 Circular Reference Detection — `CircularReferenceTests.cs`

| # | Test Name | Description | Expected Result |
|---|-----------|-------------|----------------|
| CR-01 | `Map_DirectCircularReference_DoesNotStackOverflow` | `A.B` references `B`, `B.A` references `A` | Mapping completes, no `StackOverflowException` |
| CR-02 | `Map_CircularReference_ReturnsPreviouslyMappedInstance` | Same circular setup | `mappedA.B.A` is the same reference as `mappedA` |
| CR-03 | `Map_SelfReferencing_DoesNotStackOverflow` | `Node.Parent` references another `Node` which references back | Mapping completes, graph preserved |
| CR-04 | `Map_DeepCircularChain_A_B_C_A_Resolves` | Three-node cycle: A -> B -> C -> A | Mapping completes, cycle detected at third hop |

### 2.9 Non-Generic Map Overload — `NonGenericMapTests.cs`

| # | Test Name | Description | Expected Result |
|---|-----------|-------------|----------------|
| NG-01 | `Map_ObjectOverload_MapsCorrectly` | `mapper.Map(source, typeof(User), typeof(UserDto))` | Returns correctly mapped `UserDto` boxed as `object` |
| NG-02 | `Map_ObjectOverload_ThrowsForUnregisteredTypes` | Unregistered type pair | `DeltaMapperException` with type names |
| NG-03 | `Map_InferredSource_MapsCorrectly` | `mapper.Map<UserDto>(userObject)` -- source type inferred from runtime type | Correct mapping |

### 2.10 Error Handling — `ErrorHandlingTests.cs`

| # | Test Name | Description | Expected Result |
|---|-----------|-------------|----------------|
| E-01 | `Map_NoMappingRegistered_ThrowsDeltaMapperException` | Map called for unregistered type pair | `DeltaMapperException` thrown |
| E-02 | `Map_NoMappingRegistered_ExceptionContainsTypeNames` | Same as E-01 | Exception message contains source and destination type names |
| E-03 | `Map_NoMappingRegistered_ExceptionContainsResolutionHint` | Same as E-01 | Exception message contains actionable hint (e.g., "Register a mapping...") |
| E-04 | `Map_NullSource_ThrowsArgumentNullException` | `mapper.Map<Src, Dst>(null)` | `ArgumentNullException` thrown |
| E-05 | `DeltaMapperException_IsSerializable` | Create and serialize a `DeltaMapperException` | Standard exception serialization works |

### 2.11 DI Integration — `DependencyInjectionTests.cs`

| # | Test Name | Description | Expected Result |
|---|-----------|-------------|----------------|
| DI-01 | `AddDeltaMapper_RegistersIMapper` | Call `AddDeltaMapper`, build provider, resolve `IMapper` | `IMapper` resolved successfully |
| DI-02 | `AddDeltaMapper_RegistersMapperConfiguration` | Same setup, resolve `MapperConfiguration` | `MapperConfiguration` resolved |
| DI-03 | `AddDeltaMapper_MapperIsSingleton` | Resolve `IMapper` twice | Same instance returned |
| DI-04 | `AddDeltaMapper_MappingWorks` | Register profile via DI, resolve `IMapper`, call `Map` | Mapping executes correctly |
| DI-05 | `AddDeltaMapper_MultipleProfiles` | Register two profiles via DI | Both mappings available |

### 2.12 MapperConfiguration — `MapperConfigurationTests.cs`

| # | Test Name | Description | Expected Result |
|---|-----------|-------------|----------------|
| MC-01 | `Create_WithProfile_CompilesMappings` | `MapperConfiguration.Create(cfg => cfg.AddProfile<...>())` | Configuration created, mapper works |
| MC-02 | `Create_WithMultipleProfiles_AllRegistered` | Two profiles with different type pairs | Both mappings available |
| MC-03 | `Create_DuplicateMapping_LastWins` | Same type pair registered twice | No exception; last registration takes precedence |
| MC-04 | `CreateMapper_ReturnsWorkingMapper` | `config.CreateMapper()` | Returns `IMapper` that can map |
| MC-05 | `Create_UsesFrozenDictionary_Internally` | Inspect configuration internals | Registry is `FrozenDictionary` type (via reflection or type check) |

### 2.13 Existing Destination Mapping — `ExistingDestinationTests.cs`

| # | Test Name | Description | Expected Result |
|---|-----------|-------------|----------------|
| ED-01 | `Map_WithExistingDestination_UpdatesProperties` | `mapper.Map(source, existingDest)` | Existing destination object's properties are updated |
| ED-02 | `Map_WithExistingDestination_PreservesUnmappedProperties` | Destination has properties not on source | Unmapped properties retain original values |
| ED-03 | `Map_WithExistingDestination_AppliesForMemberOverrides` | ForMember + existing destination | Overrides respected on existing object |

---

## 3. Test Models

Define the following reusable test models in `tests/DeltaMapper.UnitTests/TestModels/`:

### Flat POCOs
- `User` (Id, FirstName, LastName, Email, Age)
- `UserDto` (Id, FirstName, LastName, Email, Age)
- `UserSummaryDto` (Id, FullName, Email) -- for ForMember tests

### Nested Objects
- `Order` (Id, Customer, Items)
- `OrderDto` (Id, Customer, Items)
- `Customer` (Name, Address)
- `CustomerDto` (Name, Address)
- `Address` (Street, City, Zip)
- `AddressDto` (Street, City, Zip)

### Collections
- `Classroom` (Name, Students: `List<Student>`)
- `ClassroomDto` (Name, Students: `List<StudentDto>`)
- `Student` (Name, Grade)
- `StudentDto` (Name, Grade)

### Circular References
- `Parent` (Name, Child: `Child`)
- `Child` (Name, Parent: `Parent`)
- `ParentDto` (Name, Child: `ChildDto`)
- `ChildDto` (Name, Parent: `ParentDto`)
- `TreeNode` (Value, Left: `TreeNode`, Right: `TreeNode`)
- `TreeNodeDto` (Value, Left: `TreeNodeDto`, Right: `TreeNodeDto`)

### Records
- `record PersonRecord(string FirstName, string LastName, int Age)`
- `record PersonRecordDto(string FirstName, string LastName, int Age)`

### Init-Only
- `PersonInitOnly` with `string FirstName { get; init; }` etc.

---

## 4. Test Execution

### 4.1 Local Execution

```bash
dotnet test tests/DeltaMapper.UnitTests -c Release --logger trx
```

### 4.2 CI Execution

```bash
dotnet test --no-build -c Release --logger trx
```

### 4.3 Coverage Target

- **Line coverage**: 90%+ for `src/DeltaMapper.Core/`
- **Branch coverage**: 85%+ (covering null paths, error paths, collection edge cases)
- Phase 1 should achieve near-100% coverage since there are no external dependencies to mock

---

## 5. Test Priority

| Priority | Category | Rationale |
|----------|----------|-----------|
| P0 (must pass before merge) | Convention mapping (C-01 to C-05) | Core functionality |
| P0 | ForMember overrides (FM-01 to FM-07) | Core API contract |
| P0 | Error handling (E-01 to E-04) | User experience |
| P0 | DI integration (DI-01 to DI-04) | Primary consumption path |
| P1 (must pass before release) | Nested mapping (N-01 to N-04) | Common use case |
| P1 | Collection mapping (CL-01 to CL-08) | Common use case |
| P1 | Record/init-only (REC-01 to REC-05) | Modern .NET patterns |
| P1 | Circular references (CR-01 to CR-04) | Safety critical |
| P2 (nice to have for Phase 1) | ReverseMap (R-01 to R-03) | Convenience feature |
| P2 | Hooks (H-01 to H-04) | Advanced feature |
| P2 | Non-generic overload (NG-01 to NG-03) | Edge case API |
| P2 | Existing destination (ED-01 to ED-03) | Advanced use case |

---

## 6. Total Test Count

| Category | Count |
|----------|-------|
| Convention Mapping | 5 |
| Nested Mapping | 4 |
| Collection Mapping | 8 |
| ForMember | 7 |
| Hooks | 4 |
| ReverseMap | 3 |
| Record/Init-Only | 5 |
| Circular References | 4 |
| Non-Generic Map | 3 |
| Error Handling | 5 |
| DI Integration | 5 |
| MapperConfiguration | 5 |
| Existing Destination | 3 |
| **Total** | **61** |
