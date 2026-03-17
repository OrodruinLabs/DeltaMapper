---
id: TASK-026
title: Generator support for nested types, collections, and Ignore
status: IMPLEMENTED
depends_on:
  - TASK-025
wave: 3
files_to_create: []
files_to_modify:
  - src/DeltaMapper.SourceGen/EmitHelper.cs
  - src/DeltaMapper.SourceGen/MapperGenerator.cs
acceptance_criteria:
  - Generator emits recursive Map call for nested complex type properties when a matching [GenerateMap] exists for the nested pair
  - Generator emits List/array mapping code using LINQ Select + ToList/ToArray for collection properties
  - Generator skips properties decorated with [DeltaMapper.Ignore] or properties named in ForMember(...Ignore()) — at minimum the [Ignore] attribute path
---

- **Status**: IMPLEMENTED

**Retry count**: 0/3

## Description

Extend the generator from TASK-025 to handle non-trivial property types:

### Nested Types

When a destination property is a complex type and a `[GenerateMap(typeof(NestedSrc), typeof(NestedDst))]` exists on the same or another profile:
```csharp
dst.Address = new AddressDto();
Map_Address_To_AddressDto(src.Address, dst.Address);
```

If no matching GenerateMap exists for the nested pair, skip the property (the runtime fallback will handle it).

### Collections

For `List<T>` destination properties:
```csharp
dst.Items = src.Items?.Select(x => {
    var mapped = new ItemDto();
    Map_Item_To_ItemDto(x, mapped);
    return mapped;
}).ToList();
```

For array destinations:
```csharp
dst.Tags = src.Tags?.Select(x => {
    var mapped = new TagDto();
    Map_Tag_To_TagDto(x, mapped);
    return mapped;
}).ToArray();
```

For primitive/string element types, direct assignment:
```csharp
dst.Names = src.Names?.ToList();  // or .ToArray()
```

### Ignore Support

Check if the destination property has an `[Ignore]` attribute (we need to define this attribute or check for one). For source-gen purposes, check for:
- A property-level attribute named `Ignore` or `DeltaMapperIgnore`
- OR: the property is not in the source type (already handled by convention matching)

## Pattern Reference

Extend `EmitHelper.cs` from TASK-025. Follow the same code emission patterns.

## Test Requirements

Write generator driver tests:
1. Nested type: source with Address property, verify recursive Map call emitted
2. Collection: source with `List<Item>` property, verify Select+ToList emitted
3. Array: source with `Tag[]` property, verify Select+ToArray emitted
4. Ignore: destination property with `[DeltaMapperIgnore]`, verify it is skipped
5. All generated code compiles without errors

## Traces To

docs/DELTAMAP_PLAN.md section 3.5 — "Generator handles nested types", "Generator handles List<T> and array", "Generator respects [Ignore] attribute"
