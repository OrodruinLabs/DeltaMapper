# Code Review: Transitive Assembly Scan + Smart Single-Generic Collection Mapping

## Feature 1: Transitive Assembly Scan

### Requirement: `AddProfilesFromAssembly(Assembly, bool includeReferencedAssemblies = false)`
- **Verdict**: PASS
- **File**: `src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:46`
- Signature matches spec exactly: `Assembly assembly, bool includeReferencedAssemblies = false`
- Default is `false`, opt-in `true`

### Requirement: `AddProfilesFromAssemblyContaining<T>(bool includeReferencedAssemblies = false)` delegates
- **Verdict**: PASS
- **File**: `src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:76-79`
- Delegates to `AddProfilesFromAssembly(typeof(T).Assembly, includeReferencedAssemblies)`

### Requirement: When true, iterate `assembly.GetReferencedAssemblies()`, load each, scan for Profile subclasses
- **Verdict**: PASS
- **File**: `src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:52-66`
- Uses `assembly.GetReferencedAssemblies()` in a foreach, loads with `Assembly.Load(referencedName)`, calls `ScanAssembly`

### Requirement: Deduplicate by type
- **Verdict**: PASS
- **File**: `src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:82-108`
- Two-level dedup: `HashSet<string>` prevents re-scanning same assembly (line 84), `_profiles.Any(p => p.GetType() == type)` prevents duplicate profile types (line 106)

### Requirement: Skip assemblies that fail to load
- **Verdict**: PASS
- **File**: `src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:62-64`
- Bare `catch` block skips failed loads

### Finding: Bare catch block swallows all exceptions
- **Severity**: LOW
- **Confidence**: 70
- **File**: `src/DeltaMapper.Core/Configuration/MapperConfigurationBuilder.cs:62-64`
- **Category**: Code Quality
- **Verdict**: CONCERN
- **Issue**: The bare `catch` at line 62 swallows all exceptions, not just load failures. Per the silent failure hunting checklist, this could hide unexpected issues.
- **Fix**: Narrow to `catch (Exception) when (ex is FileNotFoundException or FileLoadException or BadImageFormatException)` or at minimum log/trace the skipped assembly name.
- **Note**: The spec explicitly says "skip assemblies that fail to load," so the behavior is correct. This is a minor hardening concern only.

### Requirement: 4 tests
- **Verdict**: PASS
- Tests found (4 transitive-specific):
  1. `AddProfilesFromAssembly_WithIncludeReferenced_ScansReferencedAssemblies`
  2. `AddProfilesFromAssembly_DefaultFalse_DoesNotScanReferenced`
  3. `AddProfilesFromAssemblyContaining_WithIncludeReferenced_Works`
  4. `AddProfilesFromAssembly_WithIncludeReferenced_DeduplicatesProfiles`

---

## Feature 2: Smart Single-Generic Collection Mapping

### Requirement: `Map<TDest>(object)` detects collection intent
- **Verdict**: PASS
- **File**: `src/DeltaMapper.Core/Runtime/Mapper.cs:31-35`
- Calls `TryMapCollection<TDestination>(source)` before falling through to normal path

### Requirement: Collection types: IEnumerable\<T\>, List\<T\>, T[], IReadOnlyList\<T\>
- **Verdict**: PASS
- **File**: `src/DeltaMapper.Core/Runtime/Mapper.cs:169-180` (element type extraction) and `182-200` (collection building)
- `GetEnumerableElementType` handles arrays, direct `IEnumerable<>`, and interface search
- `BuildCollection` handles array construction and `IsAssignableFrom` for List/IReadOnlyList/IEnumerable

### Requirement: Preserve source ordering
- **Verdict**: PASS
- **File**: `src/DeltaMapper.Core/Runtime/Mapper.cs:159-164`
- Iterates source via `foreach` on `IEnumerable`, appends to `List<object>` in order

### Requirement: Skip string (IEnumerable\<char\>)
- **Verdict**: PASS
- **File**: `src/DeltaMapper.Core/Runtime/Mapper.cs:136-137`
- Explicit `if (dstType == typeof(string)) return default;` check

### Requirement: Same element type -> skip
- **Verdict**: PASS
- **File**: `src/DeltaMapper.Core/Runtime/Mapper.cs:148-149`
- `if (srcElementType == dstElementType) return default;`

### Requirement: No registered map -> fall through to normal error
- **Verdict**: PASS
- **File**: `src/DeltaMapper.Core/Runtime/Mapper.cs:152-154`
- Checks both `_config.HasMap` and `GeneratedMapRegistry.HasFactory`; returns `default` if neither exists, letting normal error path handle it

### Finding: Missing explicit test for string skip behavior
- **Severity**: MEDIUM
- **Confidence**: 85
- **File**: `tests/DeltaMapper.UnitTests/SingleGenericCollectionTests.cs`
- **Category**: Code Quality
- **Verdict**: CONCERN
- **Issue**: The spec requires "Skip string (IEnumerable\<char\>)" as a behavior. The implementation handles it correctly (Mapper.cs:136-137), but there is no dedicated test asserting that `mapper.Map<string>(someCharEnumerable)` does NOT trigger collection routing. This is a spec-required behavior without explicit test coverage.
- **Fix**: Add a test such as `Map_string_destination_does_not_trigger_collection_routing`.

### Finding: Missing explicit test for same-element-type skip
- **Severity**: MEDIUM
- **Confidence**: 85
- **File**: `tests/DeltaMapper.UnitTests/SingleGenericCollectionTests.cs`
- **Category**: Code Quality
- **Verdict**: CONCERN
- **Issue**: The spec requires "Same element type -> skip." The implementation handles it (Mapper.cs:148-149), but there is no dedicated test asserting that `mapper.Map<List<Student>>(listOfStudents)` skips collection auto-routing.
- **Fix**: Add a test such as `Map_same_element_type_skips_collection_detection`.

### Requirement: 8 tests
- **Verdict**: PASS
- Tests found (8):
  1. `Map_IEnumerable_Dest_from_list_source_maps_collection`
  2. `Map_List_Dest_from_list_source_maps_collection`
  3. `Map_Array_Dest_from_array_source_maps_collection`
  4. `Map_IReadOnlyList_Dest_from_list_source_maps_collection`
  5. `Map_collection_preserves_source_ordering`
  6. `Map_empty_collection_returns_empty`
  7. `Map_collection_null_source_throws`
  8. `Map_collection_no_registered_map_falls_through_to_error`

---

## Convention Compliance

- PASS: **Naming** -- `_camelCase` fields, PascalCase methods throughout
- PASS: **Null safety** -- `ArgumentNullException.ThrowIfNull()` on all public method parameters
- PASS: **XML doc comments** -- All new public methods have `/// <summary>` comments
- PASS: **File-scoped namespaces** -- All files use `namespace X.Y;`
- PASS: **Sealed classes** -- All implementation classes are sealed; test models appropriately sealed
- PASS: **Collection expressions** -- `_profiles = []` used on line 17 of builder
- PASS: **Error handling** -- `DeltaMapperException` used for mapping errors; `ReflectionTypeLoadException` handled gracefully
- PASS: **Test naming** -- `Method_Scenario_ExpectedBehavior` pattern followed
- PASS: **Build** -- `dotnet build -c Release` succeeds with 0 errors (4 pre-existing warnings from Benchmarks project only)
- PASS: **Tests** -- All 20 tests pass

---

## Summary

- PASS: Assembly scan signature and default parameter -- matches spec exactly
- PASS: `AddProfilesFromAssemblyContaining<T>` delegation -- delegates correctly
- PASS: Referenced assembly iteration -- uses `GetReferencedAssemblies()` + `Assembly.Load`
- PASS: Type deduplication -- two-level dedup by assembly name and profile type
- PASS: Skip failed assembly loads -- bare catch handles it
- PASS: 4 transitive assembly scan tests -- all present and passing
- PASS: Collection detection in `Map<TDest>(object)` -- correctly detects and routes
- PASS: All 4 collection types supported -- IEnumerable, List, array, IReadOnlyList
- PASS: Source ordering preserved -- foreach iteration order maintained
- PASS: String skip -- explicit check at Mapper.cs:136
- PASS: Same element type skip -- explicit check at Mapper.cs:148
- PASS: No registered map fallthrough -- returns default, normal error fires
- PASS: 8 collection tests -- all present and passing
- CONCERN: Bare catch in assembly scanning -- swallows all exceptions, not just load failures (confidence: 70/100, non-blocking)
- CONCERN: No explicit test for string skip behavior -- spec behavior implemented but untested (confidence: 85/100, non-blocking)
- CONCERN: No explicit test for same-element-type skip -- spec behavior implemented but untested (confidence: 85/100, non-blocking)

## Final Verdict

**APPROVED** -- All spec requirements are implemented correctly and all required tests are present and passing. The three concerns are non-blocking hardening suggestions: narrowing the catch clause and adding two targeted negative tests for explicitly specified skip behaviors.
