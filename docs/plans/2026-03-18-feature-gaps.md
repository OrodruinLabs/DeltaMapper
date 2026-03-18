# Feature Gap Plan — DeltaMapper v0.2.0

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Close the most impactful feature gaps vs AutoMapper/Mapperly — one feature per branch, one PR per feature, ship as v0.2.0.

**Architecture:** Each feature is a standalone branch from `main`, with its own tests. No feature depends on another. All branches target `main`. Version bump happens once after all features merge.

**Tech Stack:** C# / .NET 10, xunit, FluentAssertions

---

## Branch Plan

Each feature = 1 branch = 1 PR. Run `/hydra:start "objective"` for each.

| # | Branch | Objective | Files | Tests | Effort |
|---|--------|-----------|-------|-------|--------|
| 1 | `feat/enum-mapping` | Enum-to-enum mapping by name and value | MapperConfigurationBuilder.cs | EnumMappingTests.cs | Small |
| 2 | `feat/dictionary-mapping` | Dictionary<K,V> ↔ Dictionary<K,V> mapping | MapperConfigurationBuilder.cs | DictionaryMappingTests.cs | Small |
| 3 | `feat/conditional-mapping` | `ForMember(d => d.X, o => o.Condition(src => ...))` | MemberConfiguration.cs, MemberOptions.cs, MapperConfigurationBuilder.cs | ConditionalMappingTests.cs | Small |
| 4 | `feat/flattening` | `Order.Customer.Name` → `CustomerName` convention | MapperConfigurationBuilder.cs | FlatteningTests.cs | Medium |
| 5 | `feat/unflattening` | `CustomerName` → `Order.Customer.Name` convention | MapperConfigurationBuilder.cs | UnflatteningTests.cs | Medium |
| 6 | `feat/assembly-scanning` | `cfg.AddProfilesFromAssembly(assembly)` | MapperConfigurationBuilder.cs | AssemblyScanningTests.cs | Small |
| 7 | `feat/type-converters` | Global `CreateTypeConverter<string, DateTime>(...)` | MapperConfigurationBuilder.cs, TypeConverter.cs | TypeConverterTests.cs | Medium |
| 8 | `feat/version-bump` | Bump all csproj to 0.2.0-alpha, update CHANGELOG | *.csproj, CHANGELOG.md | — | Tiny |

---

### Feature 1: Enum Mapping

**Branch:** `feat/enum-mapping`
**Objective:** Map between enum types by name (default) and by value (opt-in).

**What to implement:**

In `MapperConfigurationBuilder.CompileTypeMap()`, add enum detection before the existing convention matching:

```csharp
// After: if (IsDirectlyAssignable(...))
// Before: if (IsNumericWidening(...))
else if (srcPropCaptured.PropertyType.IsEnum && dstPropCaptured.PropertyType.IsEnum)
{
    // Map by name (default): Enum.Parse(dstType, src.ToString())
    var getter = CompileGetter(srcPropCaptured);
    var setter = CompileSetter(dstPropCaptured);
    var dstEnumType = dstPropCaptured.PropertyType;
    assignments.Add((src, dst, ctx) =>
    {
        var value = getter(src);
        if (value != null)
            setter(dst, Enum.Parse(dstEnumType, value.ToString()!));
    });
}
```

Also update the source generator `EmitHelper.cs` to handle enum properties.

**Tests (EnumMappingTests.cs):**
- Same enum type → direct assign (already works)
- Different enum types, matching names → maps by name
- Different enum types, mismatched name → throws or returns default
- Nullable enum → handles null

**Acceptance:** `dotnet test` passes, enum mapping works in both runtime and source-gen paths.

---

### Feature 2: Dictionary Mapping

**Branch:** `feat/dictionary-mapping`
**Objective:** Map `Dictionary<K,V>` properties between types.

**What to implement:**

In `MapperConfigurationBuilder`, add dictionary detection in `IsCollectionMapping()` or as a new branch:

```csharp
else if (IsDictionaryType(srcPropCaptured.PropertyType, dstPropCaptured.PropertyType,
             out var srcKeyType, out var srcValType, out var dstKeyType, out var dstValType))
{
    // Clone dictionary, mapping values if types differ
}
```

**Tests (DictionaryMappingTests.cs):**
- `Dictionary<string, int>` → same type (clone)
- `Dictionary<string, UserDto>` → map values
- Null dictionary → null
- Empty dictionary → empty

**Acceptance:** Dictionary properties map correctly.

---

### Feature 3: Conditional Mapping

**Branch:** `feat/conditional-mapping`
**Objective:** Add `ForMember(d => d.X, o => o.Condition(src => src.Age > 0))` — skip property if condition returns false.

**What to implement:**

Add to `IMemberOptions`:
```csharp
void Condition(Func<object, bool> predicate);
```

Add to `MemberConfiguration`:
```csharp
public Func<object, bool>? ConditionPredicate { get; set; }
```

In `MapperConfigurationBuilder.CompileTypeMap()`, wrap assignments:
```csharp
if (memberConfig?.ConditionPredicate != null)
{
    var predicate = memberConfig.ConditionPredicate;
    var innerAssign = assignment;
    assignment = (src, dst, ctx) =>
    {
        if (predicate(src))
            innerAssign(src, dst, ctx);
    };
}
```

**Tests (ConditionalMappingTests.cs):**
- Condition true → property mapped
- Condition false → property skipped (keeps default)
- Multiple conditions on different members
- Condition with null source property

**Acceptance:** Conditional mapping works with ForMember fluent API.

---

### Feature 4: Flattening

**Branch:** `feat/flattening`
**Objective:** Auto-flatten nested properties: `Order.Customer.Name` → `CustomerName`.

**What to implement:**

In `MapperConfigurationBuilder.CompileTypeMap()`, after the existing convention matching fails (no direct name match), try flattening:

```csharp
// If no direct match, try flattening: "CustomerName" → src.Customer.Name
if (matchingSrcProp == null)
{
    matchingSrcProp = TryFindFlattenedSource(srcProps, srcType, dstProp.Name);
}
```

`TryFindFlattenedSource()` splits the destination name by PascalCase segments and walks the source type's property chain:
- `CustomerName` → try `src.Customer` (is it a complex type?) → try `.Name` on it
- `AddressCity` → try `src.Address` → `.City`

Uses compiled expression chains for the getter.

**Tests (FlatteningTests.cs):**
- `Order.Customer.Name` → `CustomerName` ✓
- `Order.Address.City` → `AddressCity` ✓
- Multi-level: `Order.Customer.Address.Zip` → `CustomerAddressZip` ✓
- No match → property skipped (not an error)
- Null intermediate → null result

**Acceptance:** Flattening works automatically without configuration.

---

### Feature 5: Unflattening

**Branch:** `feat/unflattening`
**Objective:** Reverse of flattening: `CustomerName` → `Order.Customer.Name`.

**What to implement:**

In the reverse direction of CompileTypeMap, when destination has a complex property and source has flat properties matching the pattern:

- Source: `CustomerName`, `CustomerEmail`
- Destination: `Customer` (with `Name`, `Email` properties)

Detect the pattern: destination property is a complex type, and source has properties prefixed with the destination property name.

**Tests (UnflatteningTests.cs):**
- `CustomerName` → `dto.Customer.Name` ✓
- Multiple properties unflatten to same child ✓
- Mixed: some flat, some direct ✓

**Acceptance:** Unflattening works automatically for ReverseMap and explicit maps.

---

### Feature 6: Assembly Scanning

**Branch:** `feat/assembly-scanning`
**Objective:** `cfg.AddProfilesFromAssembly(typeof(UserProfile).Assembly)` — auto-discover all MappingProfile subclasses.

**What to implement:**

Add to `MapperConfigurationBuilder`:
```csharp
public MapperConfigurationBuilder AddProfilesFromAssembly(Assembly assembly)
{
    var profileTypes = assembly.GetTypes()
        .Where(t => t.IsSubclassOf(typeof(MappingProfile)) && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null);
    foreach (var type in profileTypes)
        _profiles.Add((MappingProfile)Activator.CreateInstance(type)!);
    return this;
}
```

**Tests (AssemblyScanningTests.cs):**
- Discovers profiles in test assembly
- Skips abstract profiles
- Skips profiles without parameterless constructor
- Multiple profiles discovered and all maps work

**Acceptance:** Assembly scanning works, backwards compatible.

---

### Feature 7: Type Converters

**Branch:** `feat/type-converters`
**Objective:** Global type converters: `cfg.CreateTypeConverter<string, DateTime>(s => DateTime.Parse(s))`.

**What to implement:**

Add to `MapperConfigurationBuilder`:
```csharp
private readonly Dictionary<(Type, Type), Func<object, object>> _typeConverters = new();

public MapperConfigurationBuilder CreateTypeConverter<TSource, TDest>(Func<TSource, TDest> converter)
{
    _typeConverters[(typeof(TSource), typeof(TDest))] = src => (object)converter((TSource)src)!;
    return this;
}
```

In `CompileTypeMap()`, check type converters before falling through to "type mismatch":
```csharp
// After all convention matching fails:
if (_typeConverters.TryGetValue((srcPropCaptured.PropertyType, dstPropCaptured.PropertyType), out var converter))
{
    var getter = CompileGetter(srcPropCaptured);
    var setter = CompileSetter(dstPropCaptured);
    assignments.Add((src, dst, ctx) => setter(dst, converter(getter(src)!)));
}
```

**Tests (TypeConverterTests.cs):**
- `string` → `DateTime` via converter
- `int` → `string` via converter
- Converter + convention matching (converter only for mismatched types)
- Multiple converters registered

**Acceptance:** Type converters work globally across all maps.

---

### Feature 8: Version Bump + Release

**Branch:** `feat/version-bump`
**Objective:** Bump to 0.2.0-alpha, update CHANGELOG, create release.

- Bump `<Version>` in all 4 .csproj files to `0.2.0-alpha`
- Update `<PackageReleaseNotes>` in Core csproj
- Add `[0.2.0-alpha]` section to CHANGELOG.md with all new features
- Merge to main
- `gh release create v0.2.0-alpha --title "v0.2.0-alpha" --prerelease`

---

## Execution Order

Features 1-3 are small and independent — can run in parallel.
Features 4-5 (flattening) are related — do 4 first, then 5.
Feature 6-7 are independent.
Feature 8 is last.

```
Wave 1 (parallel):  feat/enum-mapping, feat/dictionary-mapping, feat/conditional-mapping
Wave 2 (parallel):  feat/flattening, feat/assembly-scanning
Wave 3:             feat/unflattening (depends on flattening pattern)
Wave 4:             feat/type-converters
Wave 5:             feat/version-bump (after all merge)
```

## Estimated Scope

| Metric | Count |
|---|---|
| New features | 7 |
| New test files | 7 |
| Estimated new tests | ~35-45 |
| PRs | 8 |
| Target version | 0.2.0-alpha |
