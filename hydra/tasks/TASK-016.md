---
id: TASK-016
title: Phase 2 test models for diff scenarios
status: READY
depends_on: []
wave: 1
delegates_to: implementer
traces_to: "Phase 2 spec section 2.5 (Tests)"
files_to_create:
  - tests/DeltaMapper.UnitTests/TestModels/DiffModels.cs
files_to_modify: []
acceptance_criteria:
  - DiffModels.cs contains source/destination pairs for flat, nested, and collection diff scenarios
  - Models include a type with a nullable property (for NullSubstitute tests)
  - All model classes follow existing TestModels conventions (public props, string.Empty defaults)
---

**Retry count**: 0/3

## Description
Create test model classes specifically designed for Phase 2 diff/patch testing. These need to cover: flat property changes, nested objects (for dot-notation), collections of complex objects, and nullable properties.

## Implementation Notes
- File: `tests/DeltaMapper.UnitTests/TestModels/DiffModels.cs`
- Namespace: `DeltaMapper.UnitTests.TestModels`
- Models needed:
  - `Product` / `ProductDto` — flat with string Name, decimal Price, int Stock (for single/multi change tests)
  - `Warehouse` / `WarehouseDto` — has nested `Address`/`AddressDto` property (reuse existing Address types for the nested object)
  - `Team` / `TeamDto` — has `List<Player>` / `List<PlayerDto>` (for collection diff)
  - `Player` / `PlayerDto` — name + score (collection element type)
  - `ProductWithNullable` / `ProductWithNullableDto` — has `string? Nickname` (for NullSubstitute test)
- Reuse existing `Address`/`AddressDto` from `NestedModels.cs` where possible — don't duplicate

### Pattern Reference
- `tests/DeltaMapper.UnitTests/TestModels/FlatModels.cs:1-41` — naming convention, property style
- `tests/DeltaMapper.UnitTests/TestModels/CollectionModels.cs:1-33` — collection model pattern
