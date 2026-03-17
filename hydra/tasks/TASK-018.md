---
id: TASK-018
title: Nested object diff with dot-notation paths
status: DONE
depends_on: [TASK-017]
wave: 3
delegates_to: implementer
traces_to: "Phase 2 spec section 2.3 (Deep diff rules - nested complex objects), 2.5 (Tests: nested object changes)"
files_to_create:
  - tests/DeltaMapper.UnitTests/PatchNestedTests.cs
files_to_modify:
  - src/DeltaMapper.Core/Diff/DiffEngine.cs
acceptance_criteria:
  - Nested property changes use dot-notation (e.g., "Address.City") in PropertyChange.PropertyName
  - Recursion handles at least 2 levels deep (e.g., "Customer.Address.City")
  - Test verifies dot-notation paths and correct From/To values for nested changes
---

**Retry count**: 0/3

## Implementation Log
- Added `IsSimpleType(Type)` helper to `DiffEngine.cs` — classifies primitives, string, decimal, DateTime, DateTimeOffset, Guid, enums, and nullable variants
- Extended `Compare` with optional `string prefix = ""` parameter — recurses into complex properties using dot-notation keys; emits single change for null-to-value transitions without recursing
- `Snapshot` remains flat as specified
- `Mapper.Patch` required no changes — backwards-compatible signature extension
- Created `tests/DeltaMapper.UnitTests/PatchNestedTests.cs` with 3 tests (PN-01, PN-02, PN-03)
- Build: 0 warnings, 0 errors; Tests: 85/85 passed

## Description
Extend `DiffEngine.Compare` to detect complex object properties and recurse into them, flattening change paths with dot notation. A property is "complex" if its type is not a primitive, string, decimal, DateTime, Guid, or enum.

## Implementation Notes
- In `DiffEngine`, when comparing a property value:
  - If the type is a known simple type (primitives, string, decimal, DateTime, Guid, enum, nullable of these) → direct `Equals` comparison (already implemented in TASK-017)
  - If the type is a complex object → recurse, prepending `parentPath + "."` to property names
  - Handle null cases: if before is null and after is not (or vice versa), emit a single change for the whole property (not recursed)
- Add a helper `IsSimpleType(Type)` to classify types
- Tests use `Warehouse`/`WarehouseDto` models (nested Address) and `Order`/`OrderDto` (2-level nesting: Order.Customer.Address)
- Reuse existing nested test models from `NestedModels.cs` where they fit

### Pattern Reference
- `src/DeltaMapper.Core/Diff/DiffEngine.cs` — extending the Compare method from TASK-017
- `tests/DeltaMapper.UnitTests/NestedMappingTests.cs` — nested mapping test patterns
